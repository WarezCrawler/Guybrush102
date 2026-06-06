using RimWorld;
using Verse;

namespace GTI_WeaponWear
{
    [DefOf]
    public static class GTI_JobDefOf
    {
        public static JobDef GTI_RepairWeapon;

        static GTI_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(GTI_JobDefOf));
        }
    }
}
