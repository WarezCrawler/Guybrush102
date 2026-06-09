using System.Collections.Generic;
using Verse;

namespace GTI_WeaponWear
{
    // The "generic GTI node": a per-item DefModExtension that declares WHERE (and IF) the item is
    // repaired, replacing the C# routing. Attach it to a weapon/apparel ThingDef in XML:
    //
    //   <modExtensions>
    //     <li Class="GTI_WeaponWear.RepairProperties">
    //       <benches><li>ElectricTailoringBench</li><li>HandTailoringBench</li></benches>
    //     </li>
    //   </modExtensions>
    //
    // Semantics (see RepairRouting):
    //   - node present with one or more benches => repairable at exactly those benches (overrides
    //     the built-in classification entirely);
    //   - node present with an empty/absent <benches> => EXPLICITLY never repairable;
    //   - no node at all => fall back to the built-in routing (ApparelClassifier / weapon->machining).
    //
    // RimWorld resolves each <li> defName string to a ThingDef during cross-reference, so a bad
    // bench name surfaces as a normal load error rather than a silent miss.
    public class RepairProperties : DefModExtension
    {
        public List<ThingDef> benches;
    }
}
