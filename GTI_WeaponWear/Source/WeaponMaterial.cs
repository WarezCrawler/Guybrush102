using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GTI_WeaponWear
{
    public enum WeaponMat
    {
        NotWeapon,
        Wood,
        Stone,
        Metal
    }

    // Classifies a weapon Thing into a repair-material bucket. Drives both the
    // SpecialThingFilters (which recipe a weapon is eligible for) and is the single
    // source of truth for the wood / stone / metal split.
    public static class WeaponMaterial
    {
        public static WeaponMat Classify(Thing t)
        {
            if (t?.def == null || !t.def.IsWeapon)
            {
                return WeaponMat.NotWeapon;
            }

            // Stuff-based weapons (most melee): classify by the stuff's category.
            ThingDef stuff = t.Stuff;
            if (stuff?.stuffProps?.categories != null)
            {
                List<StuffCategoryDef> cats = stuff.stuffProps.categories;
                if (cats.Contains(StuffCategoryDefOf.Stony))
                {
                    return WeaponMat.Stone;
                }
                if (cats.Contains(StuffCategoryDefOf.Woody))
                {
                    return WeaponMat.Wood;
                }
                return WeaponMat.Metal;
            }

            // No stuff (guns, bows): infer from the crafting cost list.
            return IsWoodBased(t.def) ? WeaponMat.Wood : WeaponMat.Metal;
        }

        private static bool IsWoodBased(ThingDef def)
        {
            List<ThingDefCountClass> cost = def.costList;
            if (cost == null)
            {
                return false;
            }
            foreach (ThingDefCountClass c in cost)
            {
                if (c.thingDef == ThingDefOf.WoodLog)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
