using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace GTI_WeaponWear
{
    // Right-click a repair bench with a pawn selected -> "Repair <weapon> now", forcing an
    // immediate repair of that pawn's equipped weapon to full, REGARDLESS of the auto-repair HP
    // threshold. RimWorld 1.6 auto-discovers FloatMenuOptionProvider subclasses (no Def/Harmony).
    //
    // Gated by the base class to undrafted, single-select, Manipulation-capable pawns. Shown only
    // when the clicked thing is a weapon-repair bench and the pawn's equipped weapon is damaged.
    public class FloatMenuOptionProvider_RepairEquippedWeapon : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool RequiresManipulation => true;

        public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
        {
            Pawn pawn = context.FirstSelectedPawn;
            ThingWithComps weapon = EquippedWeaponRepair.RepairableWeapon(pawn);
            if (weapon == null)
            {
                yield break; // no equipped weapon, or it's already at full HP
            }
            if (!EquippedWeaponRepair.IsRepairBenchFor(clickedThing, weapon))
            {
                yield break; // this bench doesn't repair this weapon (per its routing)
            }
            Building_WorkTable bench = (Building_WorkTable)clickedThing;

            string label = "Repair " + weapon.LabelShortCap + " now";

            if (!bench.CurrentlyUsableForBills()
                || !pawn.CanReserveAndReach(bench, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption(label + " (cannot use bench)", null);
                yield break;
            }

            Job job = EquippedWeaponRepair.MakeJobAt(pawn, bench, out List<ThingDefCountClass> missing);
            if (job == null)
            {
                string need = missing.NullOrEmpty() ? "materials" : RepairUtil.DescribeMaterials(missing);
                yield return new FloatMenuOption(label + " (needs " + need + ")", null);
                yield break;
            }

            Job captured = job;
            yield return new FloatMenuOption(label, delegate
            {
                captured.playerForced = true;
                pawn.jobs.TryTakeOrderedJob(captured, JobTag.Misc);
            });
        }
    }
}
