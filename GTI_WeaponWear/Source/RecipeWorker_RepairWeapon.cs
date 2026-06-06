using RimWorld;
using Verse;

namespace GTI_WeaponWear
{
    // Repairs the weapon ingredient IN PLACE rather than destroying it.
    //
    // The vanilla bill flow calls RecipeWorker.ConsumeIngredient once per ingredient
    // after the work is done. For the material ingredient (wood/steel) we let the base
    // class consume it normally; for the weapon we instead restore its HitPoints to max
    // and return without destroying it — so the exact same Thing survives, preserving
    // quality, material, biocode, custom name and art. The recipe declares empty
    // <products/>, so nothing new is created; the repaired weapon is left at the bench
    // and hauled to storage as usual.
    public class RecipeWorker_RepairWeapon : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (ingredient?.def != null && ingredient.def.IsWeapon)
            {
                if (ingredient.HitPoints < ingredient.MaxHitPoints)
                {
                    ingredient.HitPoints = ingredient.MaxHitPoints;
                }
                // Deliberately do NOT call base: the weapon must survive.
                return;
            }

            base.ConsumeIngredient(ingredient, recipe, map);
        }
    }
}
