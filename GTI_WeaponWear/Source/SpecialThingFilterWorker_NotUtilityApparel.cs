using Verse;

namespace GTI_WeaponWear
{
    // Matches apparel that is NOT utility (i.e. clothing or armor). The fabrication-bench
    // "repair utility gear" recipe disallows this filter, leaving only utility (belt-slot)
    // apparel selectable there.
    public class SpecialThingFilterWorker_NotUtilityApparel : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            return t.def.IsApparel && ApparelClassifier.Classify(t) != ApparelRepairClass.Utility;
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def != null && def.IsApparel;
        }

        // Hide whole defs that are definitely NOT utility (clothing or armor) from the
        // utility recipe's bill list.
        public override bool AlwaysMatches(ThingDef def)
        {
            if (def == null || !def.IsApparel)
            {
                return false;
            }
            ApparelRepairClass c = ApparelClassifier.ClassifyDef(def);
            return c == ApparelRepairClass.Clothing || c == ApparelRepairClass.Armor;
        }
    }
}
