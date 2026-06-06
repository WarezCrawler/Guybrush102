using HarmonyLib;
using RimWorld;
using Verse;

namespace GTI_WeaponWear
{
    // Step-1 scaffolding (visible variant): appends a "GTI ranged/melee weapon"
    // line to a weapon's inspect string. This is the text shown in the bottom-left
    // selection info panel when you click a weapon (on the ground or in storage).
    //
    // Purely cosmetic; proves the Harmony pipeline works before the real wear
    // mechanic is added.
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetInspectString))]
    public static class Patch_Thing_GetInspectString
    {
        public static void Postfix(Thing __instance, ref string __result)
        {
            ThingDef def = __instance?.def;
            if (def == null)
            {
                return;
            }

            // Ranged first: guns also carry melee tools, so IsMeleeWeapon can be true for them.
            string tag;
            if (def.IsRangedWeapon)
            {
                tag = "GTI ranged weapon";
            }
            else if (def.IsMeleeWeapon)
            {
                tag = "GTI melee weapon";
            }
            else
            {
                return;
            }

            __result = string.IsNullOrEmpty(__result) ? tag : __result + "\n" + tag;
        }
    }
}
