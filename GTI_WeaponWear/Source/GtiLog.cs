using System.Collections.Generic;
using Verse;

namespace GTI_WeaponWear
{
    // Central debug-logging gate. Every diagnostic [GTI Weapon Wear] line routes through here so
    // the whole set can be toggled on/off from the mod options (off by default). The one-time
    // startup "Harmony patches applied." line is intentionally NOT gated — it always prints, as a
    // load marker.
    //
    // Several diagnostics sit in hot paths (the work scanner and the auto-repair think-tree node
    // fire many times per second per pawn). Those must use MsgThrottled, NOT Msg, or they flood
    // Player.log. Msg is only for genuine one-off events (a job actually issued, a repair finished,
    // a manual order).
    public static class GtiLog
    {
        // A given (pawn+outcome) key re-logs at most this often. State CHANGES still log
        // immediately because a changed outcome is a different key; only an unchanged, repeating
        // outcome is suppressed. ~2500 ticks ≈ 40 s at normal speed, ~7 s at 6x — long enough that
        // a pawn stuck on the same situation doesn't spam, short enough to confirm it's ongoing.
        public const int ThrottleTicks = 2500;

        private static readonly Dictionary<string, int> lastTickByKey = new Dictionary<string, int>();

        public static bool Enabled =>
            GTI_WeaponWearMod.Settings != null && GTI_WeaponWearMod.Settings.debugLogging;

        // Unconditional (gated) log — use only for low-frequency, genuine events.
        public static void Msg(string message)
        {
            if (Enabled)
            {
                Log.Message("[GTI Weapon Wear] " + message);
            }
        }

        // Rate-limited log for hot paths. 'key' identifies the situation (typically pawn id +
        // outcome); the same key logs at most once per ThrottleTicks, but a new/changed key logs
        // right away. Falls back to logging unthrottled if no tick manager exists (e.g. main menu).
        public static void MsgThrottled(string key, string message)
        {
            if (!Enabled)
            {
                return;
            }
            if (Find.TickManager != null)
            {
                int now = Find.TickManager.TicksGame;
                if (lastTickByKey.TryGetValue(key, out int prev) && now - prev < ThrottleTicks)
                {
                    return;
                }
                lastTickByKey[key] = now;
            }
            Log.Message("[GTI Weapon Wear] " + message);
        }
    }
}
