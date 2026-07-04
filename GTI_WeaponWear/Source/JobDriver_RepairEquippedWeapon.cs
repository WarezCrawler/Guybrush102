using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Repairs the pawn's OWN equipped weapon at a bench (the GTI_RepairEquippedWeapon job).
    // The weapon stays equipped the whole time — only the materials are hauled — so there is
    // no drop / swap / re-equip. The toil skeleton and the pay-before repair loop live in
    // JobDriver_RepairBase; interrupting (e.g. a raid drafting the pawn) leaves the weapon
    // partially repaired and only the matching share of materials spent.
    public class JobDriver_RepairEquippedWeapon : JobDriver_RepairBase
    {
        // Re-derived from the pawn's equipment, so it needs no scribing and naturally
        // survives save/load; losing or swapping the weapon fails the job below (and the
        // repair toil itself aborts if the equipped item changes mid-repair).
        protected override Thing RepairedItem => pawn.equipment?.Primary;

        protected override void AddExtraFailConditions()
        {
            // Drafting (e.g. a raid) or losing/swapping the weapon cancels the job cleanly.
            this.FailOn(() => pawn.Drafted || RepairedItem == null);
        }
    }
}
