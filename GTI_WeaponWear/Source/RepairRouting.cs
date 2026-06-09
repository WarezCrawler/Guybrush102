using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GTI_WeaponWear
{
    // Single source of truth for "which bench(es) repair this item, if any". Every routing consumer
    // (the bill-list filters, WorkGiver_RepairWeapon, and the equipped-weapon repair) goes through
    // here so routing is decided in exactly one place.
    //
    // Resolution order for an item:
    //   1. A RepairProperties modExtension on its def, if present, is authoritative — its <benches>
    //      list IS the answer (an empty list means "explicitly never repairable").
    //   2. Otherwise the built-in fallback classifies the item and maps it to a repair recipe, whose
    //      <recipeUsers> ARE the benches. Bench lists therefore live in XML (the recipes), never
    //      hardcoded here: the only routing constant in C# is the apparel-class -> recipe-defName map.
    //
    // The fallback only ever routes hit-pointed items, so WoodLog (which is IsWeapon but a plain
    // resource) is never treated as a repairable weapon — see CLAUDE.md's wood gotcha.
    public static class RepairRouting
    {
        // Apparel class -> the repair recipe that covers it. Weapons use WeaponRecipeName.
        private const string WeaponRecipeName = "GTI_RepairWeapon";
        private static readonly Dictionary<ApparelRepairClass, string> ApparelRecipeName =
            new Dictionary<ApparelRepairClass, string>
            {
                { ApparelRepairClass.Clothing, "GTI_RepairClothing" },
                { ApparelRepairClass.Armor, "GTI_RepairArmor" },
                { ApparelRepairClass.Utility, "GTI_RepairUtility" },
            };

        private static readonly List<ThingDef> Empty = new List<ThingDef>();
        private static readonly Dictionary<string, RecipeDef> recipeCache = new Dictionary<string, RecipeDef>();

        // Whether un-tagged items fall back to the built-in classification (mod option, default on).
        private static bool FallbackEnabled => GTI_WeaponWearMod.Settings?.fallbackRouting ?? true;

        // Def-level routing (no instance material info). Used by the filters' AlwaysMatches and by
        // the equipped-weapon bench search (a weapon's routing never depends on its stuff).
        public static IReadOnlyList<ThingDef> BenchesFor(ThingDef def)
        {
            if (def == null)
            {
                return Empty;
            }
            RepairProperties props = def.GetModExtension<RepairProperties>();
            if (props != null)
            {
                return props.benches ?? (IReadOnlyList<ThingDef>)Empty;
            }
            if (!FallbackEnabled || !def.useHitPoints)
            {
                return Empty;
            }
            if (def.IsWeapon)
            {
                return RecipeBenches(WeaponRecipeName);
            }
            if (def.IsApparel)
            {
                return BenchesForClass(ApparelClassifier.ClassifyDef(def));
            }
            return Empty;
        }

        // Instance-level routing. Same as the def-level path but, for un-noded apparel whose def-level
        // class is uncertain, falls back to the actual material (ApparelClassifier.Classify).
        public static IReadOnlyList<ThingDef> BenchesFor(Thing t)
        {
            if (t?.def == null)
            {
                return Empty;
            }
            RepairProperties props = t.def.GetModExtension<RepairProperties>();
            if (props != null)
            {
                return props.benches ?? (IReadOnlyList<ThingDef>)Empty;
            }
            if (!FallbackEnabled || !t.def.useHitPoints)
            {
                return Empty;
            }
            if (t.def.IsWeapon)
            {
                return RecipeBenches(WeaponRecipeName);
            }
            if (t.def.IsApparel)
            {
                return BenchesForClass(ApparelClassifier.Classify(t));
            }
            return Empty;
        }

        // True if 'item' can be repaired at 'bench'.
        public static bool RepairableAt(Thing item, ThingDef bench)
        {
            return bench != null && BenchesFor(item).Contains(bench);
        }

        // True if 'item' is repairable at any bench at all.
        public static bool IsRepairable(Thing item)
        {
            return BenchesFor(item).Count > 0;
        }

        // The benches a given repair recipe is hosted at — i.e. its <recipeUsers> (the XML data).
        public static IReadOnlyList<ThingDef> RecipeBenches(string recipeDefName)
        {
            RecipeDef recipe = Recipe(recipeDefName);
            return recipe?.recipeUsers ?? (IReadOnlyList<ThingDef>)Empty;
        }

        // Whether the def-level BenchesFor(def) is authoritative for EVERY instance of the def (so a
        // filter may hide the whole def from a bill list). Only un-noded apparel whose def-level class
        // is uncertain (no crafting recipe, decided per-instance by material) is NOT certain.
        public static bool IsCertainAtDefLevel(ThingDef def)
        {
            if (def == null || def.HasModExtension<RepairProperties>() || !FallbackEnabled
                || !def.useHitPoints || def.IsWeapon)
            {
                return true;
            }
            if (def.IsApparel)
            {
                return ApparelClassifier.ClassifyDef(def) != ApparelRepairClass.NotApparel;
            }
            return true;
        }

        private static IReadOnlyList<ThingDef> BenchesForClass(ApparelRepairClass cls)
        {
            return ApparelRecipeName.TryGetValue(cls, out string recipeName)
                ? RecipeBenches(recipeName)
                : Empty;
        }

        private static RecipeDef Recipe(string defName)
        {
            if (!recipeCache.TryGetValue(defName, out RecipeDef recipe) || recipe == null)
            {
                recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(defName);
                recipeCache[defName] = recipe;
            }
            return recipe;
        }
    }
}
