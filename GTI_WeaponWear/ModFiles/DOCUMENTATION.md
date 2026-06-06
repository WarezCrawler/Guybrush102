# GTI Weapon Wear

A C# + XML + Harmony RimWorld mod (RimWorld 1.6). Requires Harmony. **Work in progress.**

End goal: weapons lose hit points as they are used (firing / melee swings), i.e. weapon
degradation-on-use, which vanilla does not support.

## Current state — step 1 (scaffolding only)

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
