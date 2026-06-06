using RimWorld;
using Verse;

namespace GTI_WeaponWear
{
    // Step-1 scaffolding proof-of-concept.
    //
    // This StatPart is attached to the vanilla MaxHitPoints StatDef by
    // Patches/WeaponWear_HitPointsLabel.xml. It does NOT change any value
    // (TransformValue is a no-op); it only appends a line to the Max Hit Points
    // stat explanation, and only for weapons:
    //   - ranged weapons get "GTI ranged weapon"
    //   - melee  weapons get "GTI melee weapon"
    // Anything that is not a weapon contributes nothing.
    //
    // The goal here is purely to prove that the XML patch + C# assembly wiring
    // builds, loads, and shows a visible change in-game before any real wear
    // mechanic is added.
    public class StatPart_WeaponWearLabel : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            // No-op: scaffolding only touches the explanation text, not the value.
        }

        public override string ExplanationPart(StatRequest req)
        {
            ThingDef def = req.Def as ThingDef;
            if (def == null)
            {
                return null;
            }

            // Check ranged first: ranged weapons usually also carry melee tools
            // (for bashing), so IsMeleeWeapon can be true for a gun too.
            if (def.IsRangedWeapon)
            {
                return "GTI ranged weapon";
            }
            if (def.IsMeleeWeapon)
            {
                return "GTI melee weapon";
            }
            return null;
        }
    }
}
