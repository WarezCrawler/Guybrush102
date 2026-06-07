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

        // Overall weight on weapon wear. 1 = default, 0 = no wear, 2 = double wear.
        public float tearMultiplier = 1f;

        // How strongly weapon quality affects wear. 0 = quality ignored (all weapons wear the
        // same), 1 = default spread, 2 = quality matters twice as much.
        public float qualityInfluence = 1f;

        // Whether items WITHOUT a RepairProperties tag fall back to the built-in C# routing
        // (weapons -> machining, apparel by craft bench / material). When false, only items that
        // carry an explicit GTI repair tag are repairable; everything else is non-repairable.
        public bool fallbackRouting = true;

        // Master on/off for the equipped-weapon auto-repair feature. When false, no pawn ever
        // auto-repairs its own weapon (the manual right-click "Repair ... now" still works).
        public bool autoRepairEquipped = true;

        // Undrafted pawns auto-repair their own equipped weapon when its HP falls below this
        // fraction. (Mirrors the apparel HP policy, which weapons lack.) Only consulted while
        // autoRepairEquipped is on.
        public float equippedRepairThreshold = 0.5f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref repairFraction, "repairFraction", 0.25f);
            Scribe_Values.Look(ref tearMultiplier, "tearMultiplier", 1f);
            Scribe_Values.Look(ref qualityInfluence, "qualityInfluence", 1f);
            Scribe_Values.Look(ref fallbackRouting, "fallbackRouting", true);
            Scribe_Values.Look(ref autoRepairEquipped, "autoRepairEquipped", true);
            Scribe_Values.Look(ref equippedRepairThreshold, "equippedRepairThreshold", 0.5f);
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

        // Effective per-use chance for a Normal-quality weapon, shown in the settings blurb.
        private static string WearChanceBlurb()
        {
            float pct = WeaponWear.BaseChancePerUse * Settings.tearMultiplier * 100f;
            return pct.ToString("0.#") + "% for a normal-quality weapon";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            // ===== Section 1: how fast weapons wear =====
            Text.Font = GameFont.Medium;
            list.Label("Weapon wear");
            Text.Font = GameFont.Small;
            list.Gap(4f);

            // Wear multiplier: 0 = no wear, 1 = default, 2 = double. Snap to 0.05 steps.
            list.Label("Wear rate: " + Settings.tearMultiplier.ToString("0.00") + "x");
            float tearRaw = list.Slider(Settings.tearMultiplier, 0f, 2f);
            Settings.tearMultiplier = Mathf.Round(tearRaw * 20f) / 20f;
            list.Label("How fast weapons take damage from being used.\n"
                + "    0 = off (weapons never wear)    1 = default    2 = wears twice as fast\n"
                + "At the current setting, each shot or melee swing has a " + WearChanceBlurb()
                + " to lose 1 hit point. A pawn stops using a weapon once it reaches 1 HP, so use "
                + "alone can never destroy it.");

            list.Gap();

            // Quality influence: 0 = ignored, 1 = default, 2 = double spread. Snap to 0.05 steps.
            list.Label("Quality influence: " + Settings.qualityInfluence.ToString("0.00") + "x");
            float qRaw = list.Slider(Settings.qualityInfluence, 0f, 2f);
            Settings.qualityInfluence = Mathf.Round(qRaw * 20f) / 20f;
            list.Label("How much a weapon's quality changes its wear rate.\n"
                + "    0 = quality ignored (all weapons wear the same)    1 = default    2 = quality "
                + "matters twice as much\n"
                + "Higher values make masterwork/legendary weapons last even longer, and awful/poor "
                + "ones wear out even faster.");

            list.GapLine();

            // ===== Section 2: repairing =====
            Text.Font = GameFont.Medium;
            list.Label("Repairs");
            Text.Font = GameFont.Small;
            list.Gap(4f);

            int pct = Mathf.RoundToInt(Settings.repairFraction * 100f);
            list.Label("Repair material cost: " + pct + "%");
            // Snap the slider to 5% steps.
            float raw = list.Slider(Settings.repairFraction, 0f, 1f);
            Settings.repairFraction = Mathf.Round(raw * 20f) / 20f;
            list.Label("What a FULL repair costs, as a share of the materials the item was originally "
                + "built from (steel for a steel gun, cloth for a shirt, and so on; components are "
                + "never required).\n"
                + "The real cost scales with damage, so a lightly worn item costs only a little.");

            list.Gap();

            // Fallback routing: whether un-tagged items are auto-assigned a repair bench.
            list.CheckboxLabeled("Auto-assign repair benches for un-tagged items",
                ref Settings.fallbackRouting,
                "When on, any weapon or apparel without an explicit GTI repair tag is routed by the "
                + "built-in rules (weapons at the machining table; armor at the smithy; clothing at the "
                + "tailoring bench; utility gear at the fabrication bench).\n"
                + "When off, only items a mod has explicitly tagged are repairable; everything else "
                + "cannot be repaired at all (though it still wears).");

            list.GapLine();

            // ===== Section 3: automatic repair of equipped weapons =====
            Text.Font = GameFont.Medium;
            list.Label("Automatic repair");
            Text.Font = GameFont.Small;
            list.Gap(4f);

            // Master on/off for equipped-weapon auto-repair.
            list.CheckboxLabeled("Auto-repair equipped weapons",
                ref Settings.autoRepairEquipped,
                "When on, undrafted pawns repair their own carried weapon in their spare time and keep "
                + "it equipped throughout. No per-pawn setup and no work type needs to be enabled; it "
                + "pauses while the pawn is drafted. You can always force a repair by selecting a pawn "
                + "and right-clicking a repair bench, even when this is off.");

            // The threshold only matters while the feature is on; hide the slider otherwise.
            int athr = Mathf.RoundToInt(Settings.equippedRepairThreshold * 100f);
            if (Settings.autoRepairEquipped)
            {
                list.Label("    Repair when below: " + athr + "% HP");
                // Snap the slider to 5% steps.
                float athrRaw = list.Slider(Settings.equippedRepairThreshold, 0f, 1f);
                Settings.equippedRepairThreshold = Mathf.Round(athrRaw * 20f) / 20f;
            }
            else
            {
                list.Label("    (off — would repair below " + athr + "% HP when enabled)");
            }

            list.End();
        }
    }
}
