using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Incremental weapon repair, adapted from RepairBench's JobDriver_RepairItem.
    //
    // Flow: reserve the bench + ingredient queue, haul every ingredient (the weapon
    // plus the wood/steel) to the bench, then run a work toil that raises the weapon's
    // HitPoints one point at a time and consumes the materials proportionally. If the
    // job is interrupted, the weapon keeps whatever HP it reached and only the matching
    // fraction of materials has been spent.
    public class JobDriver_RepairWeapon : JobDriver
    {
        private const TargetIndex BenchInd = TargetIndex.A;
        private const TargetIndex IngredientInd = TargetIndex.B;
        private const TargetIndex CellInd = TargetIndex.C;

        // Base game-ticks of work to restore one hit point (before work-speed factors).
        private const float TicksPerHitPoint = 25f;

        private Thing weapon;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.GetTarget(BenchInd), job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(IngredientInd), job);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Identify the weapon among the queued ingredients (the rest are materials).
            weapon = job.GetTargetQueue(IngredientInd)
                .Select(t => t.Thing)
                .FirstOrDefault(t => t != null && t.def.IsWeapon);

            this.FailOnDestroyedNullOrForbidden(BenchInd);
            this.FailOnBurningImmobile(BenchInd);
            AddEndCondition(() =>
                (job.GetTarget(BenchInd).Thing is Building b && b.Spawned)
                    ? JobCondition.Ongoing
                    : JobCondition.Incompletable);

            yield return Toils_Reserve.Reserve(BenchInd);
            yield return Toils_Reserve.ReserveQueue(IngredientInd);

            // ---- Collect ingredients (weapon + materials) and bring them to the bench ----
            Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(IngredientInd);
            yield return extract;

            Toil getToHaul = Toils_Goto.GotoThing(IngredientInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(IngredientInd);
            yield return getToHaul;

            yield return Toils_Haul.StartCarryThing(IngredientInd, false, false, false, true, false);
            yield return JumpToCollectNextIntoHandsForBill(getToHaul, IngredientInd);

            yield return Toils_Goto.GotoThing(BenchInd, PathEndMode.InteractionCell)
                .FailOnDestroyedOrNull(IngredientInd);

            Toil findPlace = Toils_JobTransforms.SetTargetToIngredientPlaceCell(BenchInd, IngredientInd, CellInd);
            yield return findPlace;
            yield return Toils_Haul.PlaceHauledThingInCell(CellInd, findPlace, false);
            yield return Toils_Jump.JumpIfHaveTargetInQueue(IngredientInd, extract);

            Toil gotoBench = Toils_Goto.GotoThing(BenchInd, PathEndMode.InteractionCell);
            yield return gotoBench;

            // ---- The incremental repair toil ----
            yield return MakeRepairToil();

            // ---- Finish the bill iteration (only reached on full repair) ----
            yield return new Toil
            {
                initAction = delegate
                {
                    if (weapon != null)
                    {
                        List<Thing> done = new List<Thing> { weapon };
                        job.bill?.Notify_IterationCompleted(pawn, done);
                        RecordsUtility.Notify_BillDone(pawn, done);
                    }
                }
            };
            yield return Toils_Reserve.Release(BenchInd);
        }

        private Toil MakeRepairToil()
        {
            RepairProgress progress = null;
            float ticksToNext = TicksPerHitPoint;

            Toil toil = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Never
            };
            Building_WorkTable table = job.GetTarget(BenchInd).Thing as Building_WorkTable;

            toil.initAction = delegate
            {
                if (weapon == null || table == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                job.bill?.Notify_DoBillStarted(pawn);
                progress = new RepairProgress(
                    pawn,
                    table.IngredientStackCells,
                    GatherStagedMaterials(table),
                    weapon.MaxHitPoints - weapon.HitPoints);
            };

            toil.tickAction = delegate
            {
                if (weapon == null || progress == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                job.bill?.Notify_PawnDidWork(pawn);
                job.SetTarget(IngredientInd, weapon);
                pawn.skills?.Learn(SkillDefOf.Crafting, 0.08f);

                float speed = pawn.GetStatValue(StatDefOf.WorkSpeedGlobal)
                              * table.GetStatValue(StatDefOf.WorkTableWorkSpeedFactor);
                ticksToNext -= speed;
                if (ticksToNext > 0f)
                {
                    return;
                }
                ticksToNext = TicksPerHitPoint;

                if (weapon.HitPoints < weapon.MaxHitPoints)
                {
                    weapon.HitPoints++;
                }
                if (!progress.AddRepairedAmount(1))
                {
                    // Ran out of staged materials.
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                if (weapon.HitPoints >= weapon.MaxHitPoints)
                {
                    ReadyForNextToil();
                }
            };

            toil.WithProgressBar(IngredientInd,
                () => weapon == null ? 1f : (float)weapon.HitPoints / weapon.MaxHitPoints);
            toil.FailOnDestroyedNullOrForbidden(BenchInd);
            toil.FailOnCannotTouch(BenchInd, PathEndMode.InteractionCell);
            return toil;
        }

        // Sum the non-weapon things currently staged in the bench's ingredient cells,
        // grouped by def — i.e. the materials available to consume.
        private List<ThingDefCountClass> GatherStagedMaterials(Building_WorkTable table)
        {
            Dictionary<ThingDef, int> counts = new Dictionary<ThingDef, int>();
            foreach (IntVec3 cell in table.IngredientStackCells)
            {
                foreach (Thing t in pawn.Map.thingGrid.ThingsListAt(cell))
                {
                    if (t == null || t.def.IsWeapon)
                    {
                        continue;
                    }
                    counts.TryGetValue(t.def, out int c);
                    counts[t.def] = c + t.stackCount;
                }
            }
            return counts.Select(kv => new ThingDefCountClass(kv.Key, kv.Value)).ToList();
        }

        // Port of JobDriver_DoBill's private helper: top up the carried stack with more
        // of the same ingredient from the queue before walking to the bench.
        private static Toil JumpToCollectNextIntoHandsForBill(Toil gotoGetTargetToil, TargetIndex ind)
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
