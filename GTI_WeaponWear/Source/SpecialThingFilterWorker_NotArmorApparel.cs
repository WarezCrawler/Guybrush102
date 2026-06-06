using Verse;

namespace GTI_WeaponWear
{
    // Matches apparel that is NOT armor (i.e. clothing). The smithy "repair armor" recipe
    // disallows this filter, leaving only armor apparel selectable there.
    public class SpecialThingFilterWorker_NotArmorApparel : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            return t.def.IsApparel && ApparelClassifier.Classify(t) != ApparelRepairClass.Armor;
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def != null && def.IsApparel;
        }

        // Hide whole defs that are definitely NOT armor (clothing or utility) from the
        // armor recipe's bill list.
        public override bool AlwaysMatches(ThingDef def)
        {
            if (def == null || !def.IsApparel)
            {
                return false;
            }
            ApparelRepairClass c = ApparelClassifier.ClassifyDef(def);
            return c == ApparelRepairClass.Clothing || c == ApparelRepairClass.Utility;
        }
    }
}
