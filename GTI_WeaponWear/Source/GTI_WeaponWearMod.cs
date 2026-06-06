using HarmonyLib;
using UnityEngine;
using Verse;

namespace GTI_WeaponWear
{
    public class GTI_WeaponWearSettings : ModSettings
    {
        // Fraction of a weapon's original build resources a FULL repair costs (before the
        // damage-based scaling). 0.25 = 25%.
        public float repairFraction = 0.25f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref repairFraction, "repairFraction", 0.25f);
        }
    }

    // Mod entry point: holds settings and applies Harmony patches at load.
    public class GTI_WeaponWearMod : Mod
    {
        public static GTI_WeaponWearSettings Settings;

        public GTI_WeaponWearMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<GTI_WeaponWearSettings>();
            new Harmony("gti.weaponwear").PatchAll();
            Log.Message("[GTI Weapon Wear] Harmony patches applied.");
        }

        public override string SettingsCategory()
        {
            return "GTI Weapon Wear";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            int pct = Mathf.RoundToInt(Settings.repairFraction * 100f);
            list.Label("Repair material cost: " + pct + "% of the weapon's original resources");
            // Snap the slider to 5% steps.
            float raw = list.Slider(Settings.repairFraction, 0f, 1f);
            Settings.repairFraction = Mathf.Round(raw * 20f) / 20f;

            list.Gap();
            list.Label("Cost of a FULL repair, as a share of the weapon's original build materials "
                + "(components always excluded). The actual amount scales down with how damaged the "
                + "weapon is, so a lightly damaged weapon costs much less.");

            list.End();
        }
    }
}
