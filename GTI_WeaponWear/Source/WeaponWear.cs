using RimWorld;
using UnityEngine;
using Verse;

namespace GTI_WeaponWear
{
    // Core wear mechanic. Called once per weapon "use" (a fired ranged shot or a melee swing).
    // Each use has a chance to knock 1 hit point off the weapon. Wear never reduces a weapon
    // below 1 HP — at 1 HP the safeguard (Patch_VerbAvailable) makes the pawn stop using it,
    // so a weapon is never destroyed purely by being used. Destruction (0 HP) only happens via
    // other damage, and a destroyed weapon is gone (and thus unrepairable).
    public static class WeaponWear
    {
        // Base probability of losing 1 HP per use, before the tear/quality multipliers.
        public const float BaseChancePerUse = 0.10f;

        // The HP floor wear will not cross. At this value the weapon is still repairable.
        public const int MinHitPointsFromWear = 1;

        // True if this weapon is at (or below) the wear floor and should no longer be used.
        public static bool IsWornOut(ThingWithComps weapon)
        {
            return weapon != null && weapon.def != null && weapon.def.IsWeapon
                && weapon.def.useHitPoints && weapon.HitPoints <= MinHitPointsFromWear;
        }

        // Apply one wear roll for a single use of the given weapon.
        public static void Notify_WeaponUsed(ThingWithComps weapon)
        {
            if (weapon == null || weapon.Destroyed || weapon.def == null)
            {
                return;
            }
            // Only real, hit-pointed weapons wear (skips body parts, apparel verbs, etc.).
            if (!weapon.def.IsWeapon || !weapon.def.useHitPoints)
            {
                return;
            }
            // Already at the floor — nothing to do (the safeguard should have stopped use anyway).
            if (weapon.HitPoints <= MinHitPointsFromWear)
            {
                return;
            }

            float chance = WearChance(weapon);
            if (chance <= 0f)
            {
                return;
            }
            if (!Rand.Chance(chance))
            {
                return;
            }

            weapon.HitPoints = Mathf.Max(MinHitPointsFromWear, weapon.HitPoints - 1);
        }

        // Effective per-use wear chance = base x player tear multiplier x quality multiplier.
        public static float WearChance(ThingWithComps weapon)
        {
            GTI_WeaponWearSettings s = GTI_WeaponWearMod.Settings;
            float tearMult = s != null ? s.tearMultiplier : 1f;
            if (tearMult <= 0f)
            {
                return 0f;
            }
            float qualityMult = QualityWearMultiplier(weapon, s != null ? s.qualityInfluence : 1f);
            return Mathf.Max(0f, BaseChancePerUse * tearMult * qualityMult);
        }

        // Higher quality -> less wear. The per-quality factor below is the multiplier at
        // influence = 1; the influence slider scales the deviation from 1.0 (0 = quality
        // ignored, 2 = quality matters twice as much).
        public static float QualityWearMultiplier(Thing weapon, float influence)
        {
            if (weapon == null || !weapon.TryGetQuality(out QualityCategory q))
            {
                return 1f;
            }
            float factorAtFull = QualityBaseFactor(q);
            return Mathf.Max(0f, 1f + (factorAtFull - 1f) * influence);
        }

        private static float QualityBaseFactor(QualityCategory q)
        {
            switch (q)
            {
                case QualityCategory.Awful: return 1.4f;
                case QualityCategory.Poor: return 1.2f;
                case QualityCategory.Normal: return 1.0f;
                case QualityCategory.Good: return 0.8f;
                case QualityCategory.Excellent: return 0.6f;
                case QualityCategory.Masterwork: return 0.4f;
                case QualityCategory.Legendary: return 0.2f;
                default: return 1.0f;
            }
        }
    }
}
