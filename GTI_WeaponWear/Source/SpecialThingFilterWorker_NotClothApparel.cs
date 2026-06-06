using Verse;

namespace GTI_WeaponWear
{
    // Matches apparel that is NOT clothing (i.e. armor). The tailoring "repair clothing"
    // recipe disallows this filter, leaving only clothing apparel selectable there.
    public class SpecialThingFilterWorker_NotClothApparel : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            return t.def.IsApparel && ApparelClassifier.Classify(t) != ApparelRepairClass.Clothing;
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def != null && def.IsApparel;
        }

        // Hide whole defs that are definitely NOT clothing (armor or utility) from the
        // clothing recipe's bill list.
        public override bool AlwaysMatches(ThingDef def)
        {
            if (def == null || !def.IsApparel)
            {
                return false;
            }
            ApparelRepairClass c = ApparelClassifier.ClassifyDef(def);
            return c == ApparelRepairClass.Armor || c == ApparelRepairClass.Utility;
        }
    }
}
