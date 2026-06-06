using HarmonyLib;
using Verse;

namespace GTI_WeaponWear
{
    // Harmony bootstrap. Runs once at game startup (after defs are loaded) and
    // applies every [HarmonyPatch] in this assembly. Harmony itself is provided
    // by the brrainz.harmony mod, which About.xml declares as a dependency that
    // loads first.
    [StaticConstructorOnStartup]
    public static class GTI_WeaponWearMod
    {
        static GTI_WeaponWearMod()
        {
            Harmony harmony = new Harmony("gti.weaponwear");
            harmony.PatchAll();
            Log.Message("[GTI Weapon Wear] Harmony patches applied.");
        }
    }
}
