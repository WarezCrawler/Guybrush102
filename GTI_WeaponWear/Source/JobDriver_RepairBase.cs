using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Shared toil skeleton for the two incremental repair jobs (bench-bill repair and
    // equipped-weapon auto-repair): reserve the bench + ingredient queue, haul the queued
    // materials to the bench, then run a work toil that raises the repaired item's HitPoints
    // one point at a time with payment LEADING the repair (RepairProgress). Subclasses supply
    // the item being repaired and any job-specific fail conditions / bill bookkeeping.
    //
    // Save/load safety: toil-local state (the consumption plan, tick countdown) does not
    // survive a load, so the repair toil rebuilds it lazily on the first tick after a resume.
    // The plan is recomputed from the item's CURRENT damage (WeaponRepairCost), so a resumed
    // repair never re-charges the hit points already paid for.
    public abstract class JobDriver_RepairBase : JobDriver
    {
        protected const TargetIndex BenchInd = TargetIndex.A;
        protected const TargetIndex IngredientInd = TargetIndex.B;
        protected const TargetIndex CellInd = TargetIndex.C;

        // Base game-ticks of work to restore one hit point (before work-speed factors).
        protected const float TicksPerHitPoint = 25f;

        // The item being repaired. Must stay valid across a save/load mid-job (scribed
        // reference or re-derived from stable state such as the pawn's equipment).
        protected abstract Thing RepairedItem { get; }

        // Job-specific fail conditions added on top of the shared bench checks.
        protected virtual void AddExtraFailConditions() { }

        // Called once when the repair toil starts (bench: Notify_DoBillStarted).
        protected virtual void OnRepairStarted() { }

        // Called every repair tick (bench: bill bookkeeping + report-string target).
        protected virtual void OnRepairTick() { }

        // Toils appended after a completed repair (bench: bill-iteration completion).
        protected virtual IEnumerable<Toil> FinishToils() { yield break; }

        // The target the repair progress bar hangs on.
        protected virtual TargetIndex ProgressBarInd => BenchInd;

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
            this.FailOnDestroyedNullOrForbidden(BenchInd);
            this.FailOnBurningImmobile(BenchInd);
            AddExtraFailConditions();
            AddEndCondition(() =>
                (job.GetTarget(BenchInd).Thing is Building b && b.Spawned)
                    ? JobCondition.Ongoing
                    : JobCondition.Incompletable);

            yield return Toils_Reserve.Reserve(BenchInd);
            yield return Toils_Reserve.ReserveQueue(IngredientInd);

            // Created up front so the empty-queue jump below can target it.
            Toil gotoBench = Toils_Goto.GotoThing(BenchInd, PathEndMode.InteractionCell);

            // A zero-cost repair (an item with no costList that isn't made from stuff, e.g. a
            // thrumbo horn) has nothing to haul. The haul toils cannot run with an empty queue
            // (ExtractNextTargetFromQueue would leave the goto target unset), so skip straight
            // to the bench.
            yield return Toils_Jump.JumpIf(gotoBench,
                () => job.GetTargetQueue(IngredientInd).NullOrEmpty());

            // ---- Collect the queued ingredients and bring them to the bench ----
            Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(IngredientInd);
            yield return extract;

            Toil getToHaul = Toils_Goto.GotoThing(IngredientInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(IngredientInd);
            yield return getToHaul;

            yield return Toils_Haul.StartCarryThing(IngredientInd, false, false, false, true, false);
            yield return RepairUtil.JumpToCollectNextIntoHandsForBill(getToHaul, IngredientInd);

            yield return Toils_Goto.GotoThing(BenchInd, PathEndMode.InteractionCell)
                .FailOnDestroyedOrNull(IngredientInd);

            Toil findPlace = Toils_JobTransforms.SetTargetToIngredientPlaceCell(BenchInd, IngredientInd, CellInd);
            yield return findPlace;
            yield return Toils_Haul.PlaceHauledThingInCell(CellInd, findPlace, false);
            yield return Toils_Jump.JumpIfHaveTargetInQueue(IngredientInd, extract);

            yield return gotoBench;

            // ---- The incremental repair toil ----
            yield return MakeRepairToil();

            foreach (Toil t in FinishToils())
            {
                yield return t;
            }
            yield return Toils_Reserve.Release(BenchInd);
        }

        private Toil MakeRepairToil()
        {
            RepairProgress progress = null;
            float ticksToNext = TicksPerHitPoint;

            // Captured for the debug repair summary emitted when the toil ends (any reason).
            int startHp = 0;
            int maxHp = 0;
            string itemLabel = null;

            Toil toil = new Toil { defaultCompleteMode = ToilCompleteMode.Never };

            // (Re)build the consumption plan. Runs when the toil starts AND lazily on the first
            // tick after a save/load mid-repair (the closure state above does not survive a
            // load). The plan is the item's computed repair cost at its CURRENT damage — NOT
            // whatever happens to be staged in the bench cells — so unrelated items on the
            // bench are never consumed and a resumed repair only charges what is still owed.
            bool EnsureStarted()
            {
                Thing item = RepairedItem;
                if (item == null || item.Destroyed
                    || !(job.GetTarget(BenchInd).Thing is Building_WorkTable table))
                {
                    return false;
                }
                if (progress != null)
                {
                    // The item changed under us (e.g. the pawn swapped weapons) — abort.
                    return progress.IsFor(item);
                }
                startHp = item.HitPoints;
                maxHp = item.MaxHitPoints;
                itemLabel = item.LabelShortCap;
                progress = new RepairProgress(
                    pawn,
                    table.IngredientStackCells,
                    WeaponRepairCost.Compute(item),
                    item.MaxHitPoints - item.HitPoints,
                    item);
                return true;
            }

            toil.initAction = delegate
            {
                if (!EnsureStarted())
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                OnRepairStarted();
            };

            toil.tickAction = delegate
            {
                if (!EnsureStarted())
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                Thing item = RepairedItem;

                OnRepairTick();
                pawn.skills?.Learn(SkillDefOf.Crafting, 0.08f);

                Building_WorkTable table = (Building_WorkTable)job.GetTarget(BenchInd).Thing;
                float speed = pawn.GetStatValue(StatDefOf.WorkSpeedGlobal)
                              * table.GetStatValue(StatDefOf.WorkTableWorkSpeedFactor);
                ticksToNext -= speed;
                if (ticksToNext > 0f)
                {
                    return;
                }
                ticksToNext = TicksPerHitPoint;

                if (item.HitPoints >= item.MaxHitPoints)
                {
                    ReadyForNextToil();
                    return;
                }
                // Pay for the point BEFORE granting it. If the material isn't available,
                // stop without restoring this hit point.
                if (!progress.TryPayForNextPoint())
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                item.HitPoints++;
                if (item.HitPoints >= item.MaxHitPoints)
                {
                    ReadyForNextToil();
                }
            };

            toil.AddFinishAction(delegate
            {
                if (GtiLog.Enabled && progress != null)
                {
                    RepairUtil.LogRepairSummary(pawn, itemLabel, startHp, maxHp, progress);
                }
            });

            toil.WithProgressBar(ProgressBarInd,
                () => RepairedItem == null ? 1f : (float)RepairedItem.HitPoints / RepairedItem.MaxHitPoints);
            toil.FailOnDestroyedNullOrForbidden(BenchInd);
            toil.FailOnCannotTouch(BenchInd, PathEndMode.InteractionCell);
            return toil;
        }
    }
}
