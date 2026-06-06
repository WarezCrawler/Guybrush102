using Verse;

namespace GTI_WeaponWear
{
    // Matches weapons that are NOT metal (i.e. wooden or stone). The steel-repair
    // recipe disallows this filter, so only metal weapons remain selectable there.
    // Stone weapons match both NotWood and NotMetal, so they are excluded from both
    // recipes -> stone weapons are intentionally not repairable.
    public class SpecialThingFilterWorker_WeaponNotMetal : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            WeaponMat mat = WeaponMaterial.Classify(t);
            return mat != WeaponMat.NotWeapon && mat != WeaponMat.Metal;
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def != null && def.IsWeapon;
        }
    }
}
