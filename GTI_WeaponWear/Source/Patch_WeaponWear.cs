using HarmonyLib;
using RimWorld;
using Verse;

namespace GTI_WeaponWear
{
    // Ranged use: Verb_LaunchProjectile.TryCastShot returns true when a shot is actually fired.
    // Verb_Shoot / Verb_ShootOneUse inherit this method, so one patch covers all guns.
    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class Patch_VerbLaunchProjectile_TryCastShot
    {
        public static void Postfix(Verb_LaunchProjectile __instance, bool __result)
        {
            if (__result && __instance.CasterIsPawn)
            {
                WeaponWear.Notify_WeaponUsed(__instance.EquipmentSource);
            }
        }
    }

    // Melee use: Verb_MeleeAttack.TryCastShot fires once per swing. Verb_MeleeAttackDamage
    // inherits it. Body-part attacks have no EquipmentSource, so they are ignored downstream.
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Patch_VerbMeleeAttack_TryCastShot
    {
        public static void Postfix(Verb_MeleeAttack __instance, bool __result)
        {
            if (__result && __instance.CasterIsPawn)
            {
                WeaponWear.Notify_WeaponUsed(__instance.EquipmentSource);
            }
        }
    }

    // Safeguard: once a weapon is worn down to its HP floor, its combat verbs report as
    // unavailable so the pawn stops using it (and therefore can't wear it to destruction).
    // Verb.Available() is the gate every verb-selection path checks. Only verbs whose source
    // is an actual weapon are affected; body-part and apparel verbs are untouched.
    [HarmonyPatch(typeof(Verb), "Available")]
    public static class Patch_Verb_Available
    {
        public static void Postfix(Verb __instance, ref bool __result)
        {
            if (__result && WeaponWear.IsWornOut(__instance.EquipmentSource))
            {
                __result = false;
            }
        }
    }
}
