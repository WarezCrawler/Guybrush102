using RimWorld;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Reuses vanilla WorkGiver_DoBill entirely to find the best doable bill and its
    // ingredients on the Machining table. If that bill is one of our repair recipes,
    // we swap the standard atomic DoBill job for our incremental GTI_RepairWeapon job
    // (same bench + ingredient targets). Any other (normal crafting) bill is returned
    // untouched, so this giver fully stands in for the vanilla machining giver.
    //
    // Its WorkGiverDef has a higher priorityInType than DoBillsMachiningTable so it is
    // consulted first for TableMachining.
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
                return job; // normal crafting bill — leave it as a standard DoBill job
            }

            // Convert to our incremental repair job, carrying over the bill, the bench
            // (targetA) and the chosen ingredient queue (weapon + materials).
            Job repair = JobMaker.MakeJob(GTI_JobDefOf.GTI_RepairWeapon, job.targetA);
            repair.targetQueueB = job.targetQueueB;
            repair.countQueue = job.countQueue;
            repair.haulMode = job.haulMode;
            repair.bill = job.bill;
            return repair;
        }
    }
}
