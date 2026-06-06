# GTI Weapon Wear

A C# + XML + Harmony RimWorld mod (RimWorld 1.6). Requires Harmony. **Work in progress.**

End goal: weapons lose hit points as they are used (firing / melee swings), i.e. weapon
degradation-on-use, which vanilla does not support. Repair is implemented first so wear is
never purely punishing.

## Step 2 — weapon repair (current work)

All repair bills live at the **Machining table** (`TableMachining`). The damaged weapon is
listed as a `count:1` ingredient and is **not consumed** — it is repaired in place, so the
exact same Thing survives (quality, material, biocode, name, art preserved). Recipes declare
empty `<products/>`.

Repair is **incremental**: a custom WorkGiver + JobDriver raises the weapon's HitPoints one
point at a time and consumes the wood/steel **proportionally** as it goes. Interrupting the
job leaves the weapon partially repaired, having spent only the materials for the HP actually
restored. (The recipe `workerClass` `RecipeWorker_RepairWeapon` remains as an atomic fallback
and as the marker the WorkGiver uses to recognise repair bills.)

- `WorkGiver_RepairWeapon : WorkGiver_DoBill` — `GTI_RepairWeaponMachining` (workType
  Smithing, priority 80 > vanilla `DoBillsMachiningTable` 75). Reuses vanilla ingredient
  finding; swaps repair bills to the `GTI_RepairWeapon` job, passes normal bills through.
- `JobDriver_RepairWeapon` — hauls weapon + materials to the bench, then a work toil does
  `HitPoints++` every ~25 work-ticks (scaled by work speed) until full; `RepairProgress`
  consumes materials proportionally from the bench's ingredient cells.

Material split (starter balance, rebalance later):

| Weapon material | Recipe | Cost |
|---|---|---|
| Wooden (stuff = Woody, or wood-cost guns/bows) | `repair wooden weapon` | 5 wood |
| Metal (metal stuff, all guns) | `repair weapon` | 5 steel |
| Stone (stuff = Stony) | — | not repairable |

How the split works: two custom `SpecialThingFilter`s (`GTI_WeaponNotWood`,
`GTI_WeaponNotMetal`) each match the weapons a recipe should *reject*; the recipe disallows
its filter (the vanilla "disallow the unwanted subset" pattern). `WeaponMaterial.Classify`
is the single source of truth. The default ingredient HP filter is `0~99%`, so only damaged
weapons are picked up.

### How to test repair
1. Have a Machining table; damage a weapon (combat, or dev-mode "lower HP").
2. Add a bill: **repair wooden weapon** (wood) or **repair weapon** (steel).
3. A crafter hauls the weapon + materials, works, and the weapon returns to full HP with the
   same quality/material. Stone weapons should NOT appear as repairable.

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
