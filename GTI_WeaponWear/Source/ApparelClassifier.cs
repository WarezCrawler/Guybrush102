using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GTI_WeaponWear
{
    public enum ApparelRepairClass
    {
        NotApparel,
        Clothing,
        Armor,
        Utility
    }

    // Routes apparel to the right repair bench: clothing -> tailoring bench, armor -> smithy.
    // Primary signal is where the apparel is actually crafted (its recipeMaker benches), which
    // matches the player's mental model and auto-covers modded apparel. Falls back to material
    // type for apparel that has no crafting recipe.
    public static class ApparelClassifier
    {
        // Def-level classification — CERTAIN only. Uses the crafting bench, which is the same
        // for every instance of the def. Returns NotApparel when it can't be sure at the def
        // level (no recipe). Used by the SpecialThingFilters' AlwaysMatches to hide whole defs
        // from the wrong bench's bill list.
        public static ApparelRepairClass ClassifyDef(ThingDef def)
        {
            if (def == null || !def.IsApparel)
            {
                return ApparelRepairClass.NotApparel;
            }
            // Utility (belt slot) items — shields, packs, control packs, smokepop, etc. —
            // route to the fabrication bench regardless of where they are crafted (many are
            // built at the machining table). Detected by the "Belt" apparel layer.
            if (IsUtilityApparel(def))
            {
                return ApparelRepairClass.Utility;
            }
            List<ThingDef> benches = def.recipeMaker?.recipeUsers;
            if (benches != null)
            {
                if (benches.Any(IsTailoringBench))
                {
                    return ApparelRepairClass.Clothing;
                }
                if (benches.Any(IsArmorBench))
                {
                    return ApparelRepairClass.Armor;
                }
            }
            return ApparelRepairClass.NotApparel; // uncertain at the def level
        }

        // Instance-level classification (always resolves to Clothing or Armor for apparel):
        // def-level first, then the actual material as a fallback.
        public static ApparelRepairClass Classify(Thing t)
        {
            if (t?.def == null || !t.def.IsApparel)
            {
                return ApparelRepairClass.NotApparel;
            }

            ApparelRepairClass byDef = ClassifyDef(t.def);
            if (byDef != ApparelRepairClass.NotApparel)
            {
                return byDef;
            }

            // (Utility is always resolved at the def level above via the Belt layer.)
            List<StuffCategoryDef> cats = t.Stuff?.stuffProps?.categories;
            if (cats != null)
            {
                if (cats.Any(c => c.defName == "Fabric" || c.defName == "Leathery"))
                {
                    return ApparelRepairClass.Clothing;
                }
                if (cats.Any(c => c.defName == "Metallic"))
                {
                    return ApparelRepairClass.Armor;
                }
            }
            return ApparelRepairClass.Clothing; // default soft items to tailoring
        }

        // Utility apparel occupies the "Belt" apparel layer (shield belt, smokepop belt,
        // fire-foam popper, jump/mech packs, etc.).
        private static bool IsUtilityApparel(ThingDef def)
        {
            List<ApparelLayerDef> layers = def.apparel?.layers;
            return layers != null && layers.Any(l => l != null && l.defName == "Belt");
        }

        private static bool IsTailoringBench(ThingDef b)
        {
            return b != null && (b.defName == "ElectricTailoringBench" || b.defName == "HandTailoringBench");
        }

        private static bool IsArmorBench(ThingDef b)
        {
            return b != null && (b.defName == "ElectricSmithy" || b.defName == "FueledSmithy"
                || b.defName == "FabricationBench" || b.defName == "TableMachining");
        }
    }
}
