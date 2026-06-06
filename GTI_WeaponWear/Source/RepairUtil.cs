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
            foreach (KeyValuePair<ThingDef, int> kv in needed)
            {
                int remaining = kv.Value;
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

        // Sum the loose resource items currently staged in the bench's ingredient cells,
        // grouped by def. Excludes the bench itself and any weapon/apparel being repaired.
        // Used by both repair JobDrivers to seed RepairProgress.
        public static List<ThingDefCountClass> GatherStagedMaterials(Map map, Building_WorkTable table)
        {
            Dictionary<ThingDef, int> counts = new Dictionary<ThingDef, int>();
            foreach (IntVec3 cell in table.IngredientStackCells)
            {
                foreach (Thing t in map.thingGrid.ThingsListAt(cell))
                {
                    if (t == null || t.def.category != ThingCategory.Item || t.def.IsWeapon || t.def.IsApparel)
                    {
                        continue;
                    }
                    counts.TryGetValue(t.def, out int c);
                    counts[t.def] = c + t.stackCount;
                }
            }
            return counts.Select(kv => new ThingDefCountClass(kv.Key, kv.Value)).ToList();
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
