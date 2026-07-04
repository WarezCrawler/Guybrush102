using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Shared helpers for the repair jobs (bench-bill repair and equipped-weapon auto-repair).
    public static class RepairUtil
    {
        // Find the materials in 'needed' on the map, nearest-first, and append them to the
        // job's ingredient queue. Returns false if any required material can't be fully
        // reserved/reached. Used by WorkGiver_RepairWeapon and WorkGiver_RepairEquippedWeapon.
        public static bool TryFindMaterials(Pawn pawn, IntVec3 near, Dictionary<ThingDef, int> needed,
            List<LocalTargetInfo> queue, List<int> counts)
        {
            return TryFindMaterials(pawn, near, needed, queue, counts, out _);
        }

        // As above, but also reports (via 'missing') the per-material shortfall when it returns
        // false, so callers can show the player exactly what is lacking.
        public static bool TryFindMaterials(Pawn pawn, IntVec3 near, Dictionary<ThingDef, int> needed,
            List<LocalTargetInfo> queue, List<int> counts, out List<ThingDefCountClass> missing)
        {
            missing = new List<ThingDefCountClass>();
            if (GtiLog.Enabled && needed.Count > 0)
            {
                // One line as the search begins — NOT one per material/stack. This runs in the
                // work-scanner hot path, so it's throttled per pawn+material-set: the same search
                // repeating each scan is suppressed, but a different material need logs at once.
                string desc = DescribeMaterials(needed.Select(kv => new ThingDefCountClass(kv.Key, kv.Value)));
                GtiLog.MsgThrottled("matsearch:" + pawn.thingIDNumber + ":" + desc,
                    pawn.LabelShort + " searching map for repair materials: " + desc + ".");
            }
            foreach (KeyValuePair<ThingDef, int> kv in needed)
            {
                int remaining = kv.Value;

                // Fast-fail via the map's resource counter: if the TOTAL spawned amount (an O(1)
                // lookup that even counts forbidden/unreachable stacks) is below the need, no
                // stack search can succeed — skip the sort + reachability checks entirely. Only
                // valid for counted resources (GetCount reports 0 for everything else, which
                // must NOT read as "none on map"); uncounted defs take the full search below.
                if (kv.Key.CountAsResource)
                {
                    int onMap = pawn.Map.resourceCounter.GetCount(kv.Key);
                    if (onMap < remaining)
                    {
                        missing.Add(new ThingDefCountClass(kv.Key, remaining - onMap));
                        continue;
                    }
                }

                List<Thing> stacks = pawn.Map.listerThings.ThingsOfDef(kv.Key)
                    .OrderBy(t => (t.Position - near).LengthHorizontalSquared)
                    .ToList();

                foreach (Thing t in stacks)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }
                    if (t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        continue;
                    }
                    int take = Mathf.Min(remaining, t.stackCount);
                    queue.Add(t);
                    counts.Add(take);
                    remaining -= take;
                }
                if (remaining > 0)
                {
                    missing.Add(new ThingDefCountClass(kv.Key, remaining));
                }
            }
            return missing.Count == 0;
        }

        // "4x cloth, 2x steel" — for player-facing job-failure messages.
        public static string DescribeMaterials(IEnumerable<ThingDefCountClass> mats)
        {
            return string.Join(", ", mats.Select(m => m.count + "x " + m.thingDef.label));
        }

        // One-line debug summary emitted when a repair job ends (success OR interruption), shared
        // by both repair JobDrivers. Gated by the caller via GtiLog.Enabled.
        public static void LogRepairSummary(Pawn pawn, string itemLabel, int startHp, int maxHp,
            RepairProgress progress)
        {
            int done = progress.PointsDone;
            int endHp = startHp + done;
            int startPct = maxHp > 0 ? Mathf.RoundToInt(100f * startHp / maxHp) : 0;
            int endPct = maxHp > 0 ? Mathf.RoundToInt(100f * endHp / maxHp) : 0;
            List<ThingDefCountClass> consumed = progress.ConsumedMaterials();
            string mats = consumed.Count > 0 ? DescribeMaterials(consumed) : "no materials";
            string verb = endHp >= maxHp
                ? "fully repaired"
                : (done > 0 ? "partially repaired" : "made no progress repairing");
            GtiLog.Msg(pawn.LabelShort + " " + verb + " " + itemLabel + ": +" + done + " HP ("
                + startPct + "% -> " + endPct + "%), consumed " + mats + ".");
        }

        // Port of JobDriver_DoBill's private helper: top up the carried stack with more of the
        // same ingredient from the queue before walking to the bench. Used by both repair
        // JobDrivers during the haul phase.
        public static Toil JumpToCollectNextIntoHandsForBill(Toil gotoGetTargetToil, TargetIndex ind)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                List<LocalTargetInfo> queue = curJob.GetTargetQueue(ind);
                if (queue.NullOrEmpty() || actor.carryTracker.CarriedThing == null || actor.carryTracker.Full)
                {
                    return;
                }
                for (int i = 0; i < queue.Count; i++)
                {
                    Thing queued = queue[i].Thing;
                    if (queued == null || !GenAI.CanUseItemForWork(actor, queued))
                    {
                        continue;
                    }
                    if (!queued.CanStackWith(actor.carryTracker.CarriedThing))
                    {
                        continue;
                    }
                    if ((actor.Position - queued.Position).LengthHorizontalSquared > 64f)
                    {
                        continue;
                    }
                    int carried = actor.carryTracker.CarriedThing?.stackCount ?? 0;
                    int want = Mathf.Min(curJob.countQueue[i], queued.def.stackLimit - carried);
                    want = Mathf.Min(want, actor.carryTracker.AvailableStackSpace(queued.def));
                    if (want <= 0)
                    {
                        continue;
                    }
                    curJob.count = want;
                    curJob.SetTarget(ind, queued);
                    curJob.countQueue[i] -= want;
                    if (curJob.countQueue[i] == 0)
                    {
                        curJob.countQueue.RemoveAt(i);
                        queue.RemoveAt(i);
                    }
                    actor.jobs.curDriver.JumpToToil(gotoGetTargetToil);
                    return;
                }
            };
            return toil;
        }
    }
}
