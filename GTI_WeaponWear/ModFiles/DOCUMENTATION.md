# GTI Weapon Wear

A C# + XML + Harmony RimWorld mod (RimWorld 1.6). Requires Harmony. **Work in progress.**

End goal: weapons lose hit points as they are used (firing / melee swings), i.e. weapon
degradation-on-use, which vanilla does not support. Repair is implemented first so wear is
never purely punishing.

## Step 2 — item repair (current work)

Repair covers **weapons AND apparel**, each at the bench that matches it:

| Item | Bill | Bench(es) |
|---|---|---|
| Weapons (any) | `repair weapon` | Machining table |
| Armor | `repair armor` | Electric/Fueled Smithy |
| Clothing | `repair clothing` | Electric/Hand Tailoring bench |
| Utility gear (belt slot) | `repair utility gear` | Fabrication bench |

Apparel is split into utility / armor / clothing by `ApparelClassifier`:
1. **Utility** first — anything in the `Belt` apparel layer (shield belt, smokepop belt,
   fire-foam popper, jump/mech packs, etc.) goes to the **fabrication bench** (late-game),
   regardless of where it is crafted (several are built at the machining table).
2. Otherwise by where it is *crafted* (`recipeMaker.recipeUsers`: tailoring bench → clothing,
   smithy/fabrication/machining → armor).
3. Falling back to material (fabric/leather → clothing, metal → armor).

Three `SpecialThingFilter`s (`GTI_ApparelNotArmor`, `GTI_ApparelNotCloth`,
`GTI_ApparelNotUtility`) route each apparel to the right recipe via the "disallow the unwanted
subset" pattern.

> Naming note: the repair engine is generic, but several classes keep `Weapon`/`weapon` in
> their names from when it was weapon-only — `WorkGiver_RepairWeapon`, `JobDriver_RepairWeapon`,
> `RecipeWorker_RepairWeapon`, `WeaponRepairCost`. They handle weapons and apparel alike. The
> four repair recipes all share `workerClass RecipeWorker_RepairWeapon` (the marker the
> WorkGiver/Harmony patch use). One generic WorkGiver class serves all benches via just two
> `WorkGiverDef`s — `GTI_RepairSmithing` (Smithing/120: machining + smithy + fabrication) and
> `GTI_RepairTailoring` (Tailoring/115: tailoring benches). They can't merge further because a
> `WorkGiverDef` has a single workType. Neither adds a row to the Work tab (both reuse vanilla
> work types).

The damaged item is listed as a `count:1` ingredient and is **not consumed** — it is repaired
in place, so the exact same Thing survives (quality, material, biocode, name, art, tainted
status preserved).

Repair is **incremental**: a custom WorkGiver + JobDriver raises the item's HitPoints one point
at a time and consumes materials with payment **leading** the repair (pay-before, rounded up),
so you can never gain HP you haven't paid for. Interrupting leaves the item partially repaired.

**Material cost = the item's own original resources.** The cost is computed per item from its
crafting cost:

    per material = ceil( originalCount × 0.25 × (missingHP / maxHP) ), min 1

- Sources: `costStuffCount` × the weapon's actual material (stuff weapons) plus the def's
  `costList`, with `ComponentIndustrial` / `ComponentSpacer` always excluded.
- So a steel gun costs steel, a plasteel sword costs plasteel, a wood bow costs wood; a fully
  broken weapon costs ~25% of its build cost, a lightly damaged one far less.
- (Modded components aren't auto-excluded yet — only the two vanilla component defs.)

Code:
- `WeaponRepairCost.Compute` — the cost formula above.
- `WorkGiver_RepairWeapon : WorkGiver_DoBill` (`GTI_RepairWeaponMachining`, workType Smithing,
  priority 80). Reuses vanilla to find the weapon, then computes + finds the materials on the
  map and appends them to the job's ingredient queue. Returns no job if materials aren't
  reachable. Passes normal crafting bills through.
- `Patch_WorkGiverDoBill_SkipRepair` — Harmony postfix so **vanilla** machining givers never
  run the (material-less) repair recipe for free; only our giver does.
- `JobDriver_RepairWeapon` — hauls weapon + computed materials to the bench, `HitPoints++`
  every ~25 work-ticks (scaled by work speed) until full; `RepairProgress` consumes the hauled
  materials proportionally. The recipe `workerClass` `RecipeWorker_RepairWeapon` is the marker
  the WorkGiver/patch use and an atomic in-place fallback.

Default ingredient HP filter is `0~99%`, so only damaged weapons are picked up. All weapons
(including stone) are now repairable with their own material.

### How to test repair
1. Have a Machining table; damage a weapon (combat, or dev-mode "lower HP").
2. Add the **repair weapon** bill.
3. A crafter hauls the weapon + the right materials, repairs it gradually to full HP with the
   same quality/material, consuming roughly 25%×damage of its original resources.
4. Note: the bill UI does not list a material requirement (materials are computed
   dynamically); a repair won't start unless the needed material is available on the map.

## Step 1 — scaffolding (kept as a load indicator)

This build does **no** gameplay change. It exists to prove the whole pipeline (XML patch +
C# assembly + Harmony) builds, loads, and shows a visible effect. For weapons only it shows
`GTI ranged weapon` / `GTI melee weapon` in two places:

1. **XML-patch path** — `Patches/WeaponWear_HitPointsLabel.xml` adds a `StatPart`
   (`GTI_WeaponWear.StatPart_WeaponWearLabel`) to the vanilla `MaxHitPoints` stat, which
   appends the line to the **Max Hit Points** stat breakdown. The stat *value* is never
   changed (`TransformValue` is a no-op).
2. **Harmony path** — `GTI_WeaponWear.Patch_Thing_GetInspectString` postfixes
   `Thing.GetInspectString()` to append the line to a weapon's **inspect panel** (bottom-left
   when the weapon is selected). `GTI_WeaponWearMod` (`[StaticConstructorOnStartup]`) runs
   `Harmony.PatchAll()` at startup and logs `[GTI Weapon Wear] Harmony patches applied.`

Ranged is checked before melee (guns also carry melee tools).

### How to see it in-game
- **Inspect panel (most visible):** click a weapon lying on the ground or in a stockpile —
  the GTI line appears in the bottom-left selection info.
- **Stat breakdown:** open the weapon's Info Card (the **ⓘ**) → click the **Max Hit Points**
  row → the GTI line is at the bottom of the explanation.
- Neither appears on non-weapons (apparel, walls, etc.).
- Confirm load in `Player.log`: search for `[GTI Weapon Wear] Harmony patches applied.`

## Source / build / deploy

Everything originates from the dev repo and is deployed by the C# build:
`O:\Mod Development\Rimworld\Guybrush102\GTI_WeaponWear\`

```
GTI_WeaponWear/                    (dev repo — source of truth)
├── GTI_WeaponWear.csproj
├── Properties/AssemblyInfo.cs
├── Source/*.cs                    (compiled into the DLL)
└── ModFiles/                      (mod payload — NOT compiled)
    ├── About/About.xml
    ├── Patches/WeaponWear_HitPointsLabel.xml
    └── DOCUMENTATION.md           (this file)
```

- Build with `dotnet build GTI_WeaponWear.csproj -c Release` (net472), or the VS Code
  "Build & Deploy (Release)" task.
- `ModFiles\**` is excluded from compilation (`<Compile Remove="ModFiles\**" />`).
- The `Deploy` target wipes the live mod folder, copies `ModFiles\**` verbatim, then drops
  the freshly built DLL into `Assemblies\`. The deployed folder is fully generated:

```
T:\...\RimWorld\Mods\GTI_WeaponWear\   (deployed — do NOT edit by hand)
├── About/About.xml
├── Patches/WeaponWear_HitPointsLabel.xml
├── Assemblies/GTI_WeaponWear.dll
└── DOCUMENTATION.md
```

- No Harmony dependency yet (a `StatPart` needs none). The real wear mechanic will add Harmony.
