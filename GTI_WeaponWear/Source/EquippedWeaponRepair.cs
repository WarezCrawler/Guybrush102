using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Shared logic for repairing a pawn's OWN equipped weapon, used by both the passive
    // think-tree JobGiver (threshold-gated) and the manual right-click float-menu option
    // (forced, threshold-ignored).
    public static class EquippedWeaponRepair
    {
        // The pawn's equipped primary weapon if it is a damaged, hit-pointed weapon; else null.
        public static ThingWithComps RepairableWeapon(Pawn pawn)
        {
            ThingWithComps w = pawn?.equipment?.Primary;
            if (w == null || !w.def.IsWeapon || !w.def.useHitPoints
                || w.MaxHitPoints <= 0 || w.HitPoints >= w.MaxHitPoints)
            {
                return null;
            }
            return w;
        }

        // True if 'thing' is a bench that can repair this specific weapon (per its routing).
        public static bool IsRepairBenchFor(Thing thing, ThingWithComps weapon)
        {
            return thing is Building_WorkTable && weapon != null
                && RepairRouting.BenchesFor(weapon.def).Contains(thing.def);
        }

        // Nearest reachable, usable bench that repairs the pawn's equipped weapon, or null. The bench
        // set comes from RepairRouting (the weapon's GTI node, else the built-in fallback), so
        // rerouting a weapon in XML also moves where it is auto-repaired.
        public static Building_WorkTable FindBench(Pawn pawn)
        {
            ThingWithComps weapon = pawn?.equipment?.Primary;
            if (weapon == null)
            {
                return null;
            }
            Building_WorkTable best = null;
            float bestDistSq = float.MaxValue;
            foreach (ThingDef benchDef in RepairRouting.BenchesFor(weapon.def))
            {
                foreach (Thing t in pawn.Map.listerThings.ThingsOfDef(benchDef))
                {
                    if (!(t is Building_WorkTable table) || !table.Spawned)
                    {
                        continue;
                    }
                    if (t.IsForbidden(pawn) || !table.CurrentlyUsableForBills())
                    {
                        continue;
                    }
                    if (!pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Some))
                    {
                        continue;
                    }
                    float distSq = (t.Position - pawn.Position).LengthHorizontalSquared;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        best = table;
                    }
                }
            }
            return best;
        }

        // Build a repair job for the pawn's equipped weapon at 'bench'. Returns null (with the
        // shortfall in 'missing') if the required materials can't be found on the map. Does NOT
        // apply the HP threshold — the caller decides whether to repair.
        public static Job MakeJobAt(Pawn pawn, Building_WorkTable bench, out List<ThingDefCountClass> missing)
        {
            missing = null;
            ThingWithComps weapon = pawn.equipment?.Primary;
            if (weapon == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(GTI_JobDefOf.GTI_RepairEquippedWeapon, bench);
            job.targetQueueB = new List<LocalTargetInfo>();
            job.countQueue = new List<int>();
            job.haulMode = HaulMode.ToCellNonStorage;

            Dictionary<ThingDef, int> mats = WeaponRepairCost.Compute(weapon);
            if (mats.Count > 0 && !RepairUtil.TryFindMaterials(pawn, bench.Position, mats,
                    job.targetQueueB, job.countQueue, out missing))
            {
                return null;
            }
            return job;
        }
    }
}
