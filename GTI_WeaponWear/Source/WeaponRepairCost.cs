using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace GTI_WeaponWear
{
    // Computes the materials needed to repair a weapon, relative to its original
    // crafting cost: a fraction of the weapon's own resources, scaled by how damaged
    // it is, with components always excluded ("advanced stuff out").
    //
    //   per material = ceil( originalCount * fraction * (missingHP / maxHP) ), min 1
    //
    // Sources of "original cost":
    //   - stuff weapons (most melee): costStuffCount of the weapon's actual material
    //   - everything else (guns, bows): the def's costList, minus components
    public static class WeaponRepairCost
    {
        public static Dictionary<ThingDef, int> Compute(Thing weapon, float fraction)
        {
            Dictionary<ThingDef, int> result = new Dictionary<ThingDef, int>();
            if (weapon?.def == null)
            {
                return result;
            }
            ThingDef def = weapon.def;

            if (def.MadeFromStuff && weapon.Stuff != null && def.costStuffCount > 0)
            {
                Add(result, weapon.Stuff, def.costStuffCount);
            }
            if (def.costList != null)
            {
                foreach (ThingDefCountClass c in def.costList)
                {
                    if (c?.thingDef != null && !IsComponent(c.thingDef))
                    {
                        Add(result, c.thingDef, c.count);
                    }
                }
            }

            int missing = weapon.MaxHitPoints - weapon.HitPoints;
            float damageFraction = weapon.MaxHitPoints > 0
                ? (float)missing / weapon.MaxHitPoints
                : 1f;

            foreach (ThingDef key in result.Keys.ToList())
            {
                result[key] = Mathf.Max(1, Mathf.CeilToInt(result[key] * fraction * damageFraction));
            }
            return result;
        }

        private static void Add(Dictionary<ThingDef, int> dict, ThingDef def, int n)
        {
            dict.TryGetValue(def, out int c);
            dict[def] = c + n;
        }

        private static bool IsComponent(ThingDef def)
        {
            return def.defName == "ComponentIndustrial" || def.defName == "ComponentSpacer";
        }
    }
}
