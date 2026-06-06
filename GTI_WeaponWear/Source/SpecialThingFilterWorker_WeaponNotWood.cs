using Verse;

namespace GTI_WeaponWear
{
    // Matches weapons that are NOT wooden (i.e. metal or stone). The wood-repair
    // recipe disallows this filter, so only wooden weapons remain selectable there.
    public class SpecialThingFilterWorker_WeaponNotWood : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            WeaponMat mat = WeaponMaterial.Classify(t);
            return mat != WeaponMat.NotWeapon && mat != WeaponMat.Wood;
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def != null && def.IsWeapon;
        }
    }
}
