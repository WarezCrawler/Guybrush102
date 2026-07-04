using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Auto-repair of a pawn's OWN equipped weapon, run from the THINK TREE rather than as a
    // work giver. This is deliberate: like the vanilla "drop worn-out clothes / equip better"
    // behaviour, it must work regardless of the pawn's Work-tab settings. Gating it behind the
    // Smithing work type meant it silently never fired for combat pawns — exactly the pawns
    // that carry weapons usually have crafting disabled.
    //
    // Injected TWICE via RimWorld's official insertion hooks (Defs/ThinkTreeDefs/
    // GTI_EquippedWeaponRepair.xml), distinguished by the XML-set 'urgent' field:
    //   - spare-time node (urgent=false) at Humanlike_PostMain — evaluated AFTER the main
    //     colonist behavior core (needs, joy, JobGiver_Work), so it never preempts real work;
    //   - urgent node (urgent=true) at Humanlike_PreMain — evaluated BEFORE the main core but
    //     after the emergency block (firefighting, emergency work, starving-food). It only
    //     fires when the weapon has fallen below UrgentFraction of the configured threshold,
    //     so a pawn who is busy around the clock still gets a badly worn weapon fixed.
    // The forced manual version (right-click bench -> "Repair ... now") lives in
    // FloatMenuOptionProvider_RepairEquippedWeapon and ignores the threshold.
    public class JobGiver_RepairEquippedWeapon : ThinkNode_JobGiver
    {
        // Set from XML on the think-tree node (same pattern as vanilla JobGiver_Work's
        // <emergency> field). See the class comment.
        public bool urgent;

        // The urgent node kicks in below this fraction OF the configured threshold (e.g.
        // threshold 50% -> urgent below 12.5% HP).
        public const float UrgentFraction = 0.25f;
        // The bench + material scan is comparatively expensive and this node is re-evaluated
        // often while a pawn is idle, so throttle it per pawn. A worn weapon is a maintenance
        // concern, not an emergency, so a few in-game hours of latency is fine; this also keeps
        // the (gated) diagnostic logging quiet. GenDate.TicksPerHour == 2500.
        private const int CheckIntervalTicks = 3 * GenDate.TicksPerHour; // 7500 = 3 in-game hours

        // How long to wait before re-notifying the same pawn about a material shortfall (~1 day),
        // so the light message can't spam while the situation persists.
        private const int MessageIntervalTicks = 60000;

        private static readonly Dictionary<int, int> nextScanTick = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> nextMessageTick = new Dictionary<int, int>();

        // Forget all per-pawn throttle timestamps. Called by GTI_GameComponent when a game is
        // created or loaded: the keys are thingIDNumbers (which restart per game, so collisions
        // across games are guaranteed) and the values are absolute TicksGame of the PREVIOUS
        // game — which can be far in the new game's future and would silently block auto-repair
        // for a very long time.
        public static void ResetState()
        {
            nextScanTick.Clear();
            nextMessageTick.Clear();
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            GTI_WeaponWearSettings settings = GTI_WeaponWearMod.Settings;
            if (settings == null || !settings.autoRepairEquipped)
            {
                return null; // feature switched off in mod options
            }
            float threshold = settings.equippedRepairThreshold;
            if (urgent)
            {
                // The urgent (pre-work) node only reacts to a much more badly worn weapon;
                // anything above that is left to the spare-time node.
                threshold *= UrgentFraction;
            }
            if (threshold <= 0f)
            {
                return null;
            }
            if (pawn.Map == null || pawn.Drafted || pawn.Faction == null
                || !pawn.Faction.IsPlayer || pawn.IsPrisoner)
            {
                return null;
            }

            ThingWithComps weapon = EquippedWeaponRepair.RepairableWeapon(pawn);
            if (weapon == null)
            {
                return null;
            }
            if ((float)weapon.HitPoints / weapon.MaxHitPoints >= threshold)
            {
                return null; // above the auto-repair threshold (manual right-click ignores this)
            }
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }

            // The two nodes (spare-time and urgent) throttle independently — a spare-time scan
            // that just failed must not block the urgent node once the weapon crosses the
            // urgent line. Urgent entries are keyed on the negated pawn id (-id - 1, so id 0
            // can't collide). The 'next - now' sanity bound makes a stale far-future entry
            // (belt-and-suspenders on top of the GTI_GameComponent reset) expire instead of
            // blocking forever.
            int now = Find.TickManager.TicksGame;
            int key = urgent ? -pawn.thingIDNumber - 1 : pawn.thingIDNumber;
            if (nextScanTick.TryGetValue(key, out int next) && now < next && next - now <= CheckIntervalTicks)
            {
                return null;
            }
            nextScanTick[key] = now + CheckIntervalTicks;

            Building_WorkTable bench = EquippedWeaponRepair.FindBench(pawn);
            if (bench == null)
            {
                // Hot path (re-checked every scan while the weapon stays worn) — throttle so a
                // pawn with no reachable bench doesn't repeat the same line forever.
                GtiLog.MsgThrottled("autorepair-nobench:" + pawn.thingIDNumber,
                    pawn.LabelShort + " cannot auto-repair " + weapon.LabelShortCap
                    + ": no reachable, usable repair bench for this weapon.");
                return null;
            }

            Job job = EquippedWeaponRepair.MakeJobAt(pawn, bench, out List<ThingDefCountClass> missing);
            if (job == null)
            {
                string need = missing.NullOrEmpty() ? "unreachable" : RepairUtil.DescribeMaterials(missing);
                // Key on the shortfall so a different missing-material set logs immediately, but the
                // same shortfall repeating each scan is suppressed.
                GtiLog.MsgThrottled("autorepair-missing:" + pawn.thingIDNumber + ":" + need,
                    pawn.LabelShort + " cannot auto-repair " + weapon.LabelShortCap + " at "
                    + bench.LabelShort + ": missing materials (" + need + ").");
                NotifyMissingMaterials(pawn, weapon, missing, now);
                return null; // materials not reachable right now — re-checked after the throttle
            }
            // A job is actually being issued — a genuine one-off event, so log it unthrottled.
            GtiLog.Msg(pawn.LabelShort + " starting auto-repair of " + weapon.LabelShortCap
                + " at " + bench.LabelShort + (urgent ? " (urgent, ahead of work)." : "."));
            return job;
        }

        // A light, top-left transient message (not a letter) when a pawn wants to repair its own
        // weapon but the colony lacks the material. Throttled separately (and much longer than the
        // scan) so it can't spam. Personal repairs only — bench bills surface this via the item's
        // inspect line / right-click reason instead.
        private static void NotifyMissingMaterials(Pawn pawn, Thing weapon, List<ThingDefCountClass> missing, int now)
        {
            if (missing.NullOrEmpty())
            {
                return;
            }
            // Shared by both nodes on purpose (one heads-up per pawn, not one per node), with
            // the same stale-entry sanity bound as the scan throttle.
            if (nextMessageTick.TryGetValue(pawn.thingIDNumber, out int next) && now < next
                && next - now <= MessageIntervalTicks)
            {
                return;
            }
            nextMessageTick[pawn.thingIDNumber] = now + MessageIntervalTicks;

            string text = pawn.LabelShortCap + " can't repair " + weapon.LabelShortCap
                + ": needs " + RepairUtil.DescribeMaterials(missing);
            Messages.Message(text, pawn, MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }
}
