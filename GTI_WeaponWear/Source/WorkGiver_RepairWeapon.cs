using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Reuses vanilla WorkGiver_DoBill to find the best doable bill and (for our repair
    // recipe) a damaged weapon/apparel. For repair bills it then computes the item's own
    // material cost (WeaponRepairCost), finds those materials on the map, appends them
    // to the ingredient queue, and issues the incremental GTI_RepairWeapon job.
    //
    // Materials are NOT part of the recipe, so vanilla considers the bill doable as long as
    // ANY damaged item exists and just hands us the single CLOSEST one. We try that closest
    // item first (the common, cheap path). Only if its material is unavailable do we enumerate
    // the other damaged items the same bill covers (closest-first) and issue a job for the first
    // one we can actually fund — so a shortfall on the nearest item never blocks the rest. Only
    // when none are fundable do we report the shortfall. Non-repair bills pass through unchanged.
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

            Bill bill = job.bill;

            // The item vanilla already chose (the closest match).
            Thing chosen = job.targetQueueB?
                .Select(t => t.Thing)
                .FirstOrDefault(t => t != null && (t.def.IsWeapon || t.def.IsApparel));
            if (chosen == null)
            {
                return null;
            }

            // Fast path: try the closest item first. When its materials are available (the common
            // case) we return immediately and skip the full map enumeration + sort + per-item
            // reachability scan below entirely.
            Job direct = TryFundRepair(pawn, thing, bill, job, chosen, out List<ThingDefCountClass> chosenMissing);
            if (direct != null)
            {
                // Work-scanner hot path — throttle per pawn+item so re-scans of the same repair
                // don't repeat; a different item logs immediately.
                GtiLog.MsgThrottled("issue:" + pawn.thingIDNumber + ":" + chosen.thingIDNumber,
                    "Issuing repair of " + chosen.LabelShortCap + " at " + thing.LabelShort
                    + " for " + pawn.LabelShort + " (closest damaged item).");
                return direct;
            }

            // Closest item can't be funded — only NOW enumerate the other damaged items this bill
            // covers (closest-first) and take the first one we can fully fund.
            GtiLog.MsgThrottled("fallbackscan:" + pawn.thingIDNumber + ":" + chosen.thingIDNumber,
                "Closest item (" + chosen.LabelShortCap + ") unfundable at " + thing.LabelShort
                + "; scanning other damaged items the bill covers.");
            foreach (Thing item in FindRepairCandidates(pawn, thing, bill, chosen))
            {
                Job repair = TryFundRepair(pawn, thing, bill, job, item, out _);
                if (repair != null)
                {
                    GtiLog.MsgThrottled("issue:" + pawn.thingIDNumber + ":" + item.thingIDNumber,
                        "Issuing repair of " + item.LabelShortCap + " at " + thing.LabelShort
                        + " for " + pawn.LabelShort + " (fallback after closest was unfundable).");
                    return repair; // found a damaged item we can fully fund — do this one
                }
            }

            // No damaged item the bill covers can be funded — report the nearest item's shortfall.
            if (chosenMissing != null && chosenMissing.Count > 0)
            {
                JobFailReason.Is("Not enough materials to repair (need "
                    + RepairUtil.DescribeMaterials(chosenMissing) + ")");
            }
            return null;
        }

        // Build a GTI repair job for 'item' if its materials can be found on the map; else return
        // null with the shortfall in 'missing'. (A zero-material repair is always fundable.)
        private static Job TryFundRepair(Pawn pawn, Thing bench, Bill bill, Job template, Thing item,
            out List<ThingDefCountClass> missing)
        {
            missing = null;
            Dictionary<ThingDef, int> mats = WeaponRepairCost.Compute(item);

            Job repair = JobMaker.MakeJob(GTI_JobDefOf.GTI_RepairWeapon, template.targetA);
            repair.bill = bill;
            repair.haulMode = template.haulMode;
            repair.targetQueueB = new List<LocalTargetInfo> { item };
            repair.countQueue = new List<int> { 1 };

            if (mats.Count == 0 || RepairUtil.TryFindMaterials(pawn, bench.Position, mats,
                    repair.targetQueueB, repair.countQueue, out missing))
            {
                return repair;
            }
            return null;
        }

        // All spawned, reachable, non-forbidden damaged items this bill allows EXCEPT the
        // vanilla-chosen item (already tried by the caller), nearest the bench first.
        private static List<Thing> FindRepairCandidates(Pawn pawn, Thing bench, Bill bill, Thing chosen)
        {
            List<Thing> result = new List<Thing>();
            if (bill.recipe?.ingredients == null)
            {
                return result; // chosen already tried by the caller; nothing else to enumerate
            }

            HashSet<ThingDef> defs = new HashSet<ThingDef>();
            foreach (IngredientCount ing in bill.recipe.ingredients)
            {
                if (ing?.filter == null)
                {
                    continue;
                }
                foreach (ThingDef d in ing.filter.AllowedThingDefs)
                {
                    if (d != null)
                    {
                        defs.Add(d);
                    }
                }
            }

            foreach (ThingDef d in defs)
            {
                List<Thing> things = pawn.Map.listerThings.ThingsOfDef(d);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t == chosen || t == null)
                    {
                        continue;
                    }
                    if (!t.def.IsWeapon && !t.def.IsApparel)
                    {
                        continue;
                    }
                    if (!t.def.useHitPoints || t.HitPoints >= t.MaxHitPoints)
                    {
                        continue;
                    }
                    if (!bill.IsFixedOrAllowedIngredient(t))
                    {
                        continue; // wrong class / outside HP filter for this bill
                    }
                    if (t.IsForbidden(pawn)
                        || !pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        continue;
                    }
                    result.Add(t);
                }
            }

            result.Sort((a, b) => (a.Position - bench.Position).LengthHorizontalSquared
                .CompareTo((b.Position - bench.Position).LengthHorizontalSquared));
            return result;
        }
    }
}
