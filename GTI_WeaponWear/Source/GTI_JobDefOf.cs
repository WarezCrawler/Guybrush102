using RimWorld;
using Verse;

namespace GTI_WeaponWear
{
    [DefOf]
    public static class GTI_JobDefOf
    {
        public static JobDef GTI_RepairWeapon;

        // Undrafted pawn repairs its own equipped weapon at a bench (auto-repair).
        public static JobDef GTI_RepairEquippedWeapon;

        static GTI_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(GTI_JobDefOf));
        }
    }
}
