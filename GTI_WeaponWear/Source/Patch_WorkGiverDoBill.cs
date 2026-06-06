using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Safeguard: only our WorkGiver_RepairWeapon may perform repair recipes.
    //
    // Our repair recipe lists no material ingredient (materials are computed and added
    // dynamically by WorkGiver_RepairWeapon). If a vanilla machining bill giver
    // (DoBillsMachiningTable, etc.) were allowed to run it, it would repair the weapon
    // atomically and for free. This postfix nulls any repair-recipe job produced by a
    // DoBill giver that is NOT ours. (When our giver calls base.JobOnThing, __instance is
    // WorkGiver_RepairWeapon, so its job is left intact.)
    [HarmonyPatch(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.JobOnThing))]
    public static class Patch_WorkGiverDoBill_SkipRepair
    {
        public static void Postfix(WorkGiver_DoBill __instance, ref Job __result)
        {
            if (__result == null || __instance is WorkGiver_RepairWeapon)
            {
                return;
            }
            if (__result.bill?.recipe?.workerClass == typeof(RecipeWorker_RepairWeapon))
            {
                __result = null;
            }
        }
    }
}
