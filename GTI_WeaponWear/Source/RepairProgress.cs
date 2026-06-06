using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace GTI_WeaponWear
{
    // Consumes the staged material ingredients (sitting in the bench's ingredient
    // cells) proportionally to how much of the weapon has been repaired. Adapted
    // from RepairBench's ItemRepairProgress.
    //
    // Example: repairing 50 HP costing 5 wood -> 1 wood is removed roughly every
    // 10 HP restored. If the job is interrupted, only the wood for the HP actually
    // restored has been consumed.
    public class RepairProgress
    {
        private sealed class Consume
        {
            public ThingDef def;
            public int toConsume;
            public int consumed;
        }

        private readonly Pawn pawn;
        private readonly List<IntVec3> cells;
        private readonly int toRepair;
        private readonly List<Consume> table;
        private float repaired;

        public RepairProgress(Pawn pawn, IEnumerable<IntVec3> ingredientCells, List<ThingDefCountClass> toConsume, int repairAmount)
        {
            this.pawn = pawn;
            cells = ingredientCells.ToList();
            toRepair = Math.Max(1, repairAmount);
            table = toConsume.Select(c => new Consume { def = c.thingDef, toConsume = c.count, consumed = 0 }).ToList();
        }

        // Returns false when the required material could no longer be found (out of
        // staged materials) so the caller can abort the job.
        public bool AddRepairedAmount(int amount)
        {
            repaired += amount;
            float progress = repaired / toRepair;

            // Only ever look at loose resource items. The ingredient cells are the
            // bench's own cells, so the raw list also contains the bench building and
            // the weapon — neither must ever be consumed.
            List<Thing> staged = cells
                .SelectMany(c => pawn.Map.thingGrid.ThingsListAt(c))
                .Where(t => t != null && t.def.category == ThingCategory.Item && !t.def.IsWeapon)
                .ToList();

            bool ok = true;
            foreach (Consume c in table)
            {
                int want = (int)Math.Floor(c.toConsume * progress) - c.consumed;
                if (want > 0)
                {
                    c.consumed += want;
                    ok &= Remove(staged, c.def, want);
                }
            }
            return ok;
        }

        private static bool Remove(List<Thing> staged, ThingDef def, int amount)
        {
            foreach (Thing t in staged.Where(x => x.def == def).ToArray())
            {
                if (t.stackCount <= amount)
                {
                    amount -= t.stackCount;
                    t.Destroy();
                    staged.Remove(t);
                    if (amount == 0)
                    {
                        return true;
                    }
                }
                else
                {
                    t.SplitOff(amount).Destroy();
                    return true;
                }
            }
            return amount <= 0;
        }
    }
}
