using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GTI_WeaponWear
{
    // Repair materials are computed per-item and never listed on the bill, so a player has no
    // way to know what a repair needs (or why nothing happens when the colony is out of it).
    // This appends a "Repair needs: N x Material (have M)" line to a damaged weapon/apparel's
    // inspect panel, making the requirement — and any shortfall — visible at a glance.
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetInspectString))]
    public static class Patch_Thing_RepairInfo
    {
        public static void Postfix(Thing __instance, ref string __result)
        {
            ThingDef def = __instance?.def;
            if (def == null || !def.useHitPoints || (!def.IsWeapon && !def.IsApparel))
            {
                return;
            }
            if (__instance.HitPoints >= __instance.MaxHitPoints)
            {
                return; // undamaged — nothing to repair
            }

            Dictionary<ThingDef, int> mats = WeaponRepairCost.Compute(__instance);
            if (mats.Count == 0)
            {
                return;
            }

            Map map = __instance.MapHeld;
            IEnumerable<string> parts = mats.Select(kv =>
            {
                string s = kv.Value + "x " + kv.Key.label;
                if (map != null)
                {
                    s += " (have " + map.resourceCounter.GetCount(kv.Key) + ")";
                }
                return s;
            });

            string line = "Repair needs: " + string.Join(", ", parts);
            __result = string.IsNullOrEmpty(__result) ? line : __result + "\n" + line;
        }
    }
}
