using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GTI_WeaponWear
{
    // Repair materials are computed per-item and never listed on the bill, so a player has no
    // way to know what a repair needs (or why nothing happens when the colony is out of it).
    // This appends a "Repair needs: N x Material (have M)" line to a damaged weapon/apparel's
    // inspect panel, making the requirement — and any shortfall — visible at a glance.
    //
    // GetInspectString is called every frame while a thing is selected, and building the line
    // allocates (cost dictionary + strings). A one-entry cache reuses the built line while the
    // same item at the same damage stays selected, refreshed every ~30 frames so the "(have M)"
    // counts and any settings change stay current. Keyed on real frames, not game ticks, so it
    // also refreshes while the game is paused.
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetInspectString))]
    public static class Patch_Thing_RepairInfo
    {
        private const int CacheRefreshFrames = 30;

        private static Thing cachedThing;
        private static int cachedHitPoints;
        private static int cacheExpireFrame;
        private static string cachedLine; // null = "no repair line for this item"

        // Drop the cached Thing reference — a static Thing pins its map (and thus a whole
        // previous game) from garbage collection. Called by GTI_GameComponent on every game
        // start/load, like the other per-game static state.
        public static void ResetState()
        {
            cachedThing = null;
            cachedLine = null;
        }

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

            if (__instance != cachedThing || __instance.HitPoints != cachedHitPoints
                || Time.frameCount >= cacheExpireFrame)
            {
                cachedThing = __instance;
                cachedHitPoints = __instance.HitPoints;
                cacheExpireFrame = Time.frameCount + CacheRefreshFrames;
                cachedLine = BuildLine(__instance);
            }
            if (cachedLine == null)
            {
                return;
            }
            __result = string.IsNullOrEmpty(__result) ? cachedLine : __result + "\n" + cachedLine;
        }

        private static string BuildLine(Thing thing)
        {
            Dictionary<ThingDef, int> mats = WeaponRepairCost.Compute(thing);
            if (mats.Count == 0)
            {
                return null;
            }

            Map map = thing.MapHeld;
            IEnumerable<string> parts = mats.Select(kv =>
            {
                string s = kv.Value + "x " + kv.Key.label;
                if (map != null)
                {
                    s += " (have " + map.resourceCounter.GetCount(kv.Key) + ")";
                }
                return s;
            });

            return "Repair needs: " + string.Join(", ", parts);
        }
    }
}
