using System.Collections.Generic;
using Verse;

namespace GTI_WeaponWear
{
    // Data-driven replacement for the old per-apparel-class filter workers. One subclass per repair
    // recipe; each represents that recipe's bench set (its <recipeUsers>). The recipe DISALLOWS its
    // own filter, so an item is hidden from / rejected by the recipe exactly when it is NOT routed to
    // any of that recipe's benches (RepairRouting decides routing from the item's GTI node, falling
    // back to the built-in classification).
    //
    // A SpecialThingFilterWorker has no reference to the SpecialThingFilterDef that created it, so the
    // recipe identity can't be a field — hence one tiny subclass per recipe, each naming its recipe.
    public abstract class SpecialThingFilterWorker_NotRepairableAt : SpecialThingFilterWorker
    {
        // The repair recipe whose benches this filter guards.
        protected abstract string RecipeDefName { get; }

        private IReadOnlyList<ThingDef> MyBenches => RepairRouting.RecipeBenches(RecipeDefName);

        // Per-instance: reject (hide) the item when none of its repair benches is one of mine.
        public override bool Matches(Thing t)
        {
            return t?.def != null && Disjoint(RepairRouting.BenchesFor(t), MyBenches);
        }

        // The filter is relevant to hit-pointed weapons and apparel (the recipe's category narrows it
        // further to one or the other).
        public override bool CanEverMatch(ThingDef def)
        {
            return def != null && def.useHitPoints && (def.IsWeapon || def.IsApparel);
        }

        // Def-level: hide the whole def from the bill list only when we're CERTAIN every instance is
        // routed away from my benches (a node, a weapon, or apparel with a definite craft-bench class).
        public override bool AlwaysMatches(ThingDef def)
        {
            return def != null
                && RepairRouting.IsCertainAtDefLevel(def)
                && Disjoint(RepairRouting.BenchesFor(def), MyBenches);
        }

        // True when the two bench lists share no entry.
        private static bool Disjoint(IReadOnlyList<ThingDef> a, IReadOnlyList<ThingDef> b)
        {
            if (a.Count == 0 || b.Count == 0)
            {
                return true;
            }
            for (int i = 0; i < a.Count; i++)
            {
                for (int j = 0; j < b.Count; j++)
                {
                    if (a[i] == b[j])
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public class SpecialThingFilterWorker_NotRepairWeapon : SpecialThingFilterWorker_NotRepairableAt
    {
        protected override string RecipeDefName => "GTI_RepairWeapon";
    }

    public class SpecialThingFilterWorker_NotRepairArmor : SpecialThingFilterWorker_NotRepairableAt
    {
        protected override string RecipeDefName => "GTI_RepairArmor";
    }

    public class SpecialThingFilterWorker_NotRepairClothing : SpecialThingFilterWorker_NotRepairableAt
    {
        protected override string RecipeDefName => "GTI_RepairClothing";
    }

    public class SpecialThingFilterWorker_NotRepairUtility : SpecialThingFilterWorker_NotRepairableAt
    {
        protected override string RecipeDefName => "GTI_RepairUtility";
    }
}
