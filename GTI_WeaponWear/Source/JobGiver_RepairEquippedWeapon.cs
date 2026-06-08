using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Auto-repair of a pawn's OWN equipped weapon, run from the THINK TREE (right after vanilla's
    // apparel optimization) rather than as a work giver. This is deliberate: like the vanilla
    // "drop worn-out clothes / equip better" behaviour, it must work regardless of the pawn's
    // Work-tab settings. Gating it behind the Smithing work type meant it silently never fired
    // for combat pawns — exactly the pawns that carry weapons usually have crafting disabled.
    //
    // It sits AFTER JobGiver_Work in the tree, so it only uses spare time and never interrupts
    // real work. The forced manual version (right-click bench -> "Repair ... now") lives in
    // FloatMenuOptionProvider_RepairEquippedWeapon and ignores the threshold.
    public class JobGiver_RepairEquippedWeapon : ThinkNode_JobGiver
    {
        // The bench + material scan is comparatively expensive and this node is re-evaluated
        // often while a pawn is idle, so throttle it per pawn.
        private const int CheckIntervalTicks = 600;

        // How long to wait before re-notifying the same pawn about a material shortfall (~1 day),
        // so the light message can't spam while the situation persists.
        private const int MessageIntervalTicks = 60000;

        private static readonly Dictionary<int, int> nextScanTick = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> nextMessageTick = new Dictionary<int, int>();

        protected override Job TryGiveJob(Pawn pawn)
        {
            GTI_WeaponWearSettings settings = GTI_WeaponWearMod.Settings;
            if (settings == null || !settings.autoRepairEquipped)
            {
                return null; // feature switched off in mod options
            }
            float threshold = settings.equippedRepairThreshold;
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

            int now = Find.TickManager.TicksGame;
            if (nextScanTick.TryGetValue(pawn.thingIDNumber, out int next) && now < next)
            {
                return null;
            }
            nextScanTick[pawn.thingIDNumber] = now + CheckIntervalTicks;

            // Past the cheap per-tick gates and the scan throttle: from here on we only run once
            // per CheckIntervalTicks per pawn, so logging here can't flood the log.
            int hpPct = Mathf.RoundToInt(100f * weapon.HitPoints / weapon.MaxHitPoints);
            GtiLog.Msg(pawn.LabelShort + " checking auto-repair of " + weapon.LabelShortCap
                + " (" + hpPct + "% HP, below " + Mathf.RoundToInt(threshold * 100f) + "% threshold).");

            Building_WorkTable bench = EquippedWeaponRepair.FindBench(pawn);
            if (bench == null)
            {
                GtiLog.Msg(pawn.LabelShort + " cannot auto-repair " + weapon.LabelShortCap
                    + ": no reachable, usable repair bench for this weapon.");
                return null;
            }

            Job job = EquippedWeaponRepair.MakeJobAt(pawn, bench, out List<ThingDefCountClass> missing);
            if (job == null)
            {
                GtiLog.Msg(pawn.LabelShort + " cannot auto-repair " + weapon.LabelShortCap + " at "
                    + bench.LabelShort + ": missing materials ("
                    + (missing.NullOrEmpty() ? "unreachable" : RepairUtil.DescribeMaterials(missing)) + ").");
                NotifyMissingMaterials(pawn, weapon, missing, now);
                return null; // materials not reachable right now — re-checked after the throttle
            }
            GtiLog.Msg(pawn.LabelShort + " starting auto-repair of " + weapon.LabelShortCap
                + " at " + bench.LabelShort + ".");
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
            if (nextMessageTick.TryGetValue(pawn.thingIDNumber, out int next) && now < next)
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
