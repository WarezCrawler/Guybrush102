using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Reuses vanilla WorkGiver_DoBill to find the best doable bill and (for our repair
    // recipe) the damaged weapon. For repair bills it then computes the weapon's own
    // material cost (WeaponRepairCost), finds those materials on the map, appends them
    // to the ingredient queue, and issues the incremental GTI_RepairWeapon job. If the
    // materials can't be found, no job is issued. Non-repair bills pass through unchanged.
    //
    // Higher priorityInType than DoBillsMachiningTable so it is consulted first; a Harmony
    // safeguard (Patch_WorkGiverDoBill_SkipRepair) stops vanilla givers from doing repair
    // recipes at all.
    public class WorkGiver_RepairWeapon : WorkGiver_DoBill
    {
        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            Job job = base.JobOnThing(pawn, thing, forced);
            if (job == null || job.def != JobDefOf.DoBill)
            {
                return job;
            }

            RecipeDef recipe = job.bill?.recipe;
            if (recipe == null || recipe.workerClass != typeof(RecipeWorker_RepairWeapon))
            {
                return job; // normal crafting bill — leave untouched
            }

            Thing weapon = job.targetQueueB?
                .Select(t => t.Thing)
                .FirstOrDefault(t => t != null && t.def.IsWeapon);
            if (weapon == null)
            {
                return null;
            }

            Job repair = JobMaker.MakeJob(GTI_JobDefOf.GTI_RepairWeapon, job.targetA);
            repair.bill = job.bill;
            repair.haulMode = job.haulMode;
            repair.targetQueueB = new List<LocalTargetInfo>(job.targetQueueB);
            repair.countQueue = new List<int>(job.countQueue);

            Dictionary<ThingDef, int> mats = WeaponRepairCost.Compute(weapon);
            if (mats.Count > 0 && !TryFindMaterials(pawn, thing.Position, mats, repair.targetQueueB, repair.countQueue))
            {
                return null; // not enough materials reachable right now
            }
            return repair;
        }

        private static bool TryFindMaterials(Pawn pawn, IntVec3 near, Dictionary<ThingDef, int> needed,
            List<LocalTargetInfo> queue, List<int> counts)
        {
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
                    return false;
                }
            }
            return true;
        }
    }
}
