using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Repairs the pawn's OWN equipped weapon at a bench. The weapon stays equipped the whole
    // time — only the materials are hauled to the bench — so there is no drop / swap / re-equip.
    // Materials are consumed pay-before (RepairProgress) exactly like the bench-bill repair, so
    // interrupting (e.g. a raid drafting the pawn) leaves the weapon partially repaired and only
    // the matching share of materials spent.
    public class JobDriver_RepairEquippedWeapon : JobDriver
    {
        private const TargetIndex BenchInd = TargetIndex.A;
        private const TargetIndex IngredientInd = TargetIndex.B;
        private const TargetIndex CellInd = TargetIndex.C;

        private const float TicksPerHitPoint = 25f;

        private ThingWithComps Weapon => pawn.equipment?.Primary;

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
            // Drafting (e.g. a raid) or losing/swapping the weapon cancels the job cleanly.
            this.FailOn(() => pawn.Drafted || Weapon == null);
            AddEndCondition(() =>
                (job.GetTarget(BenchInd).Thing is Building b && b.Spawned)
                    ? JobCondition.Ongoing
                    : JobCondition.Incompletable);

            yield return Toils_Reserve.Reserve(BenchInd);
            yield return Toils_Reserve.ReserveQueue(IngredientInd);

            // ---- Haul the materials to the bench (the weapon is NOT hauled — it stays equipped) ----
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

            yield return Toils_Goto.GotoThing(BenchInd, PathEndMode.InteractionCell);

            // ---- Incremental repair of the equipped weapon ----
            yield return MakeRepairToil();
            yield return Toils_Reserve.Release(BenchInd);
        }

        private Toil MakeRepairToil()
        {
            RepairProgress progress = null;
            float ticksToNext = TicksPerHitPoint;
            Building_WorkTable table = job.GetTarget(BenchInd).Thing as Building_WorkTable;

            // Captured for the debug repair summary emitted when the toil ends (any reason).
            int startHp = 0;
            int maxHp = 0;
            string itemLabel = null;

            Toil toil = new Toil { defaultCompleteMode = ToilCompleteMode.Never };

            toil.initAction = delegate
            {
                ThingWithComps weapon = Weapon;
                if (weapon == null || table == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                startHp = weapon.HitPoints;
                maxHp = weapon.MaxHitPoints;
                itemLabel = weapon.LabelShortCap;
                progress = new RepairProgress(
                    pawn,
                    table.IngredientStackCells,
                    RepairUtil.GatherStagedMaterials(pawn.Map, table, weapon),
                    weapon.MaxHitPoints - weapon.HitPoints,
                    weapon);
            };

            toil.AddFinishAction(delegate
            {
                if (GtiLog.Enabled && progress != null)
                {
                    RepairUtil.LogRepairSummary(pawn, itemLabel, startHp, maxHp, progress);
                }
            });

            toil.tickAction = delegate
            {
                ThingWithComps weapon = Weapon;
                if (weapon == null || progress == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                pawn.skills?.Learn(SkillDefOf.Crafting, 0.08f);

                float speed = pawn.GetStatValue(StatDefOf.WorkSpeedGlobal)
                              * table.GetStatValue(StatDefOf.WorkTableWorkSpeedFactor);
                ticksToNext -= speed;
                if (ticksToNext > 0f)
                {
                    return;
                }
                ticksToNext = TicksPerHitPoint;

                if (weapon.HitPoints >= weapon.MaxHitPoints)
                {
                    ReadyForNextToil();
                    return;
                }
                if (!progress.TryPayForNextPoint())
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                weapon.HitPoints++;
                if (weapon.HitPoints >= weapon.MaxHitPoints)
                {
                    ReadyForNextToil();
                }
            };

            toil.WithProgressBar(BenchInd,
                () => Weapon == null ? 1f : (float)Weapon.HitPoints / Weapon.MaxHitPoints);
            toil.FailOnDestroyedNullOrForbidden(BenchInd);
            toil.FailOnCannotTouch(BenchInd, PathEndMode.InteractionCell);
            return toil;
        }
    }
}
