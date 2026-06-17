using Verse;

namespace GTI_WeaponWear
{
    // Marker type only. The four repair recipes (weapon/armor/clothing/utility) all set
    // workerClass to this so the rest of the mod can recognise a repair bill by
    //   recipe.workerClass == typeof(RecipeWorker_RepairWeapon)
    // — see WorkGiver_RepairWeapon (which builds the incremental GTI_RepairWeapon job) and
    // Patch_WorkGiverDoBill_SkipRepair (which stops vanilla running these recipes).
    //
    // It deliberately has NO behaviour. The actual repair is done incrementally in
    // JobDriver_RepairWeapon, which restores the item's HitPoints in place and consumes the
    // dynamically-staged materials — the repaired item is protected by REFERENCE there (and in
    // RepairProgress / RepairUtil.GatherStagedMaterials), never by a def predicate. The vanilla
    // atomic bill flow (RecipeWorker.ConsumeIngredient) is never reached: our WorkGiver only ever
    // emits the custom job, and the skip-patch nulls any repair job a vanilla giver would make. An
    // override here would be dead code, so there isn't one.
    public class RecipeWorker_RepairWeapon : RecipeWorker
    {
    }
}
