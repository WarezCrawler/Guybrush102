using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Bench-bill repair (the GTI_RepairWeapon job): hauls the damaged item plus its computed
    // materials to the bench, then repairs it incrementally. The toil skeleton and the
    // pay-before repair loop live in JobDriver_RepairBase; this subclass contributes the
    // repaired item (scribed so it survives save/load) and the bill bookkeeping.
    public class JobDriver_RepairWeapon : JobDriver_RepairBase
    {
        private Thing weapon;

        protected override Thing RepairedItem => weapon;

        // The job's reportString is "repairing TargetB." and OnRepairTick keeps TargetB on the
        // item, so the progress bar hangs on it too.
        protected override TargetIndex ProgressBarInd => IngredientInd;

        public override void ExposeData()
        {
            base.ExposeData();
            // Scribed by reference so a save/load mid-job keeps pointing at the right item.
            // Re-deriving it from the ingredient queue after a load is NOT safe: the queue is
            // consumed while hauling, and the first leftover entry can be a staged WoodLog
            // stack — wood is itself a weapon def (see CLAUDE.md), so it would be mistaken
            // for the item being repaired.
            Scribe_References.Look(ref weapon, "weapon");
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fresh job: the item is the first weapon/apparel in the just-built queue (the
            // WorkGiver puts it first, ahead of the materials). After a load the scribed
            // reference is already set and the (partially consumed) queue is not consulted.
            if (weapon == null)
            {
                weapon = job.GetTargetQueue(IngredientInd)
                    .Select(t => t.Thing)
                    .FirstOrDefault(t => t != null && (t.def.IsWeapon || t.def.IsApparel));
            }
            return base.MakeNewToils();
        }

        protected override void OnRepairStarted()
        {
            job.bill?.Notify_DoBillStarted(pawn);
        }

        protected override void OnRepairTick()
        {
            job.bill?.Notify_PawnDidWork(pawn);
            job.SetTarget(IngredientInd, weapon);
        }

        // Finish the bill iteration (only reached on full repair).
        protected override IEnumerable<Toil> FinishToils()
        {
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
        }
    }
}
