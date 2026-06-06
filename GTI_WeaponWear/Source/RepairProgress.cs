using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace GTI_WeaponWear
{
    // Charges the staged material ingredients for an incremental repair. Payment LEADS the
    // repair: before each hit point is granted, the materials owed up to that point (rounded
    // up) must be available and are consumed first. This guarantees the player can never gain
    // a hit point they haven't paid for, even if the job is interrupted.
    //
    // Example: 4 steel over 53 HP -> a steel is consumed as each ~13-HP slice begins, with the
    // first taken before the first point is restored.
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
        private int pointsDone;

        public RepairProgress(Pawn pawn, IEnumerable<IntVec3> ingredientCells, List<ThingDefCountClass> toConsume, int repairAmount)
        {
            this.pawn = pawn;
            cells = ingredientCells.ToList();
            toRepair = Math.Max(1, repairAmount);
            table = toConsume.Select(c => new Consume { def = c.thingDef, toConsume = c.count, consumed = 0 }).ToList();
        }

        // Call BEFORE granting one hit point. Consumes (rounded up) the materials owed up to
        // and including that point. Returns false and consumes nothing if any required
        // material is unavailable — the caller must then NOT grant the point.
        public bool TryPayForNextPoint()
        {
            int next = pointsDone + 1;
            float progress = (float)next / toRepair;

            List<Thing> staged = StagedItems();

            List<KeyValuePair<Consume, int>> due = new List<KeyValuePair<Consume, int>>();
            foreach (Consume c in table)
            {
                int need = Mathf.CeilToInt(c.toConsume * progress) - c.consumed;
                if (need > 0)
                {
                    due.Add(new KeyValuePair<Consume, int>(c, need));
                }
            }

            // Affordability check first, so we never partially consume and then abort.
            foreach (KeyValuePair<Consume, int> d in due)
            {
                if (Available(staged, d.Key.def) < d.Value)
                {
                    return false;
                }
            }

            foreach (KeyValuePair<Consume, int> d in due)
            {
                Remove(staged, d.Key.def, d.Value);
                d.Key.consumed += d.Value;
            }
            pointsDone = next;
            return true;
        }

        private List<Thing> StagedItems()
        {
            // Only loose resource items — never the bench or the weapon/apparel being repaired.
            return cells
                .SelectMany(c => pawn.Map.thingGrid.ThingsListAt(c))
                .Where(t => t != null && t.def.category == ThingCategory.Item && !t.def.IsWeapon && !t.def.IsApparel)
                .ToList();
        }

        private static int Available(List<Thing> staged, ThingDef def)
        {
            return staged.Where(t => t.def == def).Sum(t => t.stackCount);
        }

        private static void Remove(List<Thing> staged, ThingDef def, int amount)
        {
            foreach (Thing t in staged.Where(x => x.def == def).ToArray())
            {
                if (amount <= 0)
                {
                    break;
                }
                if (t.stackCount <= amount)
                {
                    amount -= t.stackCount;
                    t.Destroy();
                    staged.Remove(t);
                }
                else
                {
                    t.SplitOff(amount).Destroy();
                    amount = 0;
                }
            }
        }
    }
}
