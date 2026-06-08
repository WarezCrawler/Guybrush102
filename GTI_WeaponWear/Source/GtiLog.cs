using Verse;

namespace GTI_WeaponWear
{
    // Central debug-logging gate. Every diagnostic [GTI Weapon Wear] line routes through here so
    // the whole set can be toggled on/off from the mod options (off by default). The one-time
    // startup "Harmony patches applied." line is intentionally NOT gated — it always prints, as a
    // load marker.
    //
    // Keep messages low-frequency: anything called per-tick, per-use, or per-think-tick must not
    // log here unless explicitly throttled, or it will flood Player.log.
    public static class GtiLog
    {
        public static bool Enabled =>
            GTI_WeaponWearMod.Settings != null && GTI_WeaponWearMod.Settings.debugLogging;

        public static void Msg(string message)
        {
            if (Enabled)
            {
                Log.Message("[GTI Weapon Wear] " + message);
            }
        }
    }
}
