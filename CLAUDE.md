# CLAUDE.md — GTI RimWorld Mods

Guidance for working in this repository. This folder is `RimWorld/Mods/` and contains a small
collection of personal/published RimWorld mods authored or maintained by **WarezCrawler** (Steam: GTI).

## Repository layout

```
Mods/
├── GTI_InfiniteTurrets/   Published Steam mod — turrets/mortars never need fuel or barrels
├── GTI_Replicator/        Personal fork of "Resource Replicator" — item-cloning workbenches
├── GTI_Utilities/         Personal grab-bag of XML patches (currently all disabled)
├── GTI_WeaponWear/        C# mod (repo's first) — weapons wear with use; repair benches + auto-repair
├── README.md              Short human-facing overview
└── CLAUDE.md              This file
```

Each mod also has its own `DOCUMENTATION.md` describing its functionality in detail — read those first
when changing a specific mod.

## Target game version

- **RimWorld 1.6** (game build at time of writing: `1.6.4633`). Version is in `RimWorld/Version.txt`.
- Installed DLCs on this machine: **Core, Biotech, Odyssey**. **Royalty, Ideology, and Anomaly are NOT installed.**
  - Vanilla + DLC defs live in `RimWorld/Data/<Core|Biotech|Odyssey|...>/Defs/`. Grep there to verify a
    def name still exists before patching or referencing it.
  - Anything that references a def from a non-installed DLC **must** be guarded with `MayRequire` (see below),
    or it produces red cross-reference errors on load.

## Mostly pure-XML mods — one C# exception

**`GTI_WeaponWear` is a C# mod** (the repo's first). Its source (`Source/*.cs` + a net472 `.csproj`) lives in
the dev tree at `O:\Mod Development\Rimworld\Guybrush102\GTI_WeaponWear\`; `dotnet build -c Release` compiles
it and an MSBuild **Deploy** target wipes the live mod folder and copies `ModFiles\**` plus the freshly built
DLL into `Assemblies\`. Verification still = launch RimWorld + read `Player.log` (no unit tests). Architecture,
formulas, and code gotchas live in that mod's own `CODE_REFERENCE.md` / `DOCUMENTATION.md`; build/deploy
specifics are in the `rimworld-csharp-mod-build` memory. **Everything below applies to the other (XML-only) mods.**

The XML mods have **no C# assemblies** — no `Assemblies/` folders, no `.dll`, no source to compile.
Everything is Defs and PatchOperations. This means:
- "Updating for a new game version" = checking that def names / fields / xpaths still match vanilla, and
  bumping `<supportedVersions>` in `About/About.xml`. There is nothing to recompile.
- Compatibility breaks show up as RimWorld dev-console errors (cross-reference failures, xpath-not-found),
  not crashes.

## Conventions used in this repo

### Disabled files: `.xm_` and `.pn_`
Files renamed from `.xml` → `.xm_` (and `.png` → `.pn_`) are **intentionally disabled**: RimWorld only
loads `.xml` / `.png`, so the trailing-underscore variants are inert. This is the project's way of parking
a patch/asset without deleting it. To re-enable, rename back to the real extension. To disable, rename to
`.xm_` / `.pn_`. (Note: `GTI_Utilities` currently has *all* patches disabled, so that mod does nothing at runtime.)

### Versioned Def folders + LoadFolders
`GTI_Replicator` uses RimWorld's multi-version folder layout: a root `Defs/` plus numbered folders
(`1.0/`, `1.1/`, … `1.6/`). With **no `LoadFolders.xml` present**, RimWorld's default behavior loads the
**root folder AND the folder matching the current game version** (e.g. on 1.6: root `Defs/` + `1.6/Defs/`).
Older numbered folders (`1.0`–`1.5`) are ignored on a 1.6 game. Keep version-specific recipe files inside the
matching numbered folder; keep shared buildings/research/workgivers in root `Defs/`.

When you add content for a new game version, create a new numbered folder and name recipe files
`v<version>_Recipes_*.xml` to match the convention.

### DLC-conditional content: `MayRequire`
To make a Def (or a list `<li>`) load only when a DLC/mod is present, add the attribute:
```xml
<RecipeDef ParentName="MedicinalReplicatorBase" MayRequire="Ludeon.RimWorld.Royalty">
```
DLC packageIds: `Ludeon.RimWorld.Royalty`, `Ludeon.RimWorld.Ideology`, `Ludeon.RimWorld.Biotech`,
`Ludeon.RimWorld.Anomaly`, `Ludeon.RimWorld.Odyssey`. For cross-mod patches use `PatchOperationFindMod`
(by mod name) instead, as the Utilities patches do.

## Pitfalls / gotchas (learned the hard way)

### ⚠️ Never put `--` (two ASCII hyphens) inside an XML comment
The XML spec forbids `--` between `<!--` and `-->`, and RimWorld's loader is strict: one stray `--` makes
the **entire file fail to parse**. On a heavy modlist this cascades — the null patch asset NREs in
`ModContentPack.LoadPatches()`, RimWorld resets the mods config and aborts the load, then the menu spams
per-frame NREs from *unrelated* mods. It looks like someone else's mod broke, but it didn't. (Happened once.)

- Offender is **two ASCII hyphens only**. Safe in comments: a single `-`, the arrow `->`, em-dashes (`—`).
  Unsafe: `--`, `----` separator rules. Use `====` for separators instead.
- Validate after editing any `.xml`:
  ```powershell
  try { [xml](Get-Content path\file.xml -Raw); 'VALID' } catch { "INVALID: $($_.Exception.Message)" }
  ```

### ⚠️ (C#) Wood (`WoodLog`) is itself a weapon def
`WoodLog` derives from `ResourceVerbBase`, so it carries a melee verb and `def.IsWeapon == true` — a colonist
can wield a log as a club. It is the **only** stuff-resource that does (steel/plasteel/uranium/jade are plain
`ResourceBase`). Consequence for any code that separates "the item being worked on" from "the materials":
**never** filter by `def.IsWeapon` / `def.IsApparel` to exclude the worked item — wood *materials* get silently
skipped too. Identify the specific item by **reference** (`thing == theItem`). This caused a real GTI_WeaponWear
bug where wooden items (war mask, bows) repaired for free because the staged wood was never counted or consumed.

### (C#) Repair model: materials are dynamic, not in the recipe
GTI_WeaponWear's repair recipe lists **no material ingredient** — the "ingredient" is the damaged item itself
(category + `allowedHitPointsPercents 0~0.99`), and the real material cost is computed per-item at runtime
(`WeaponRepairCost`). So vanilla `WorkGiver_DoBill` always thinks the bill is doable and hands back only the
single *closest* damaged item. `WorkGiver_RepairWeapon` therefore tries the closest item first (fast path) and,
only if its materials are unavailable, enumerates the other damaged items the bill covers. A Harmony postfix
(`Patch_WorkGiverDoBill_SkipRepair`) stops vanilla givers from running the material-less repair recipe for free.

### After a failed load, check the log
RimWorld log: `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`.
The **first** error usually identifies the trigger; later errors are often downstream of a mod-config reset.
A trailing `[ALLOC_*]` memory dump means the process terminated. There is no build/test step for these mods —
verification = launch RimWorld and read `Player.log`.

## Git

- Active development happens on dated branches (e.g. `Dev_20260530`); PRs merge into `main`.
- Git author: `WarezCrawler`. The repo root is `Mods/`.

## Quick checklist when "updating a mod to the current game version"

1. Read the mod's `DOCUMENTATION.md`.
2. Confirm `About/About.xml` `<supportedVersions>` includes the target major version (e.g. `1.6`).
3. For each PatchOperation xpath, verify the target still exists in `RimWorld/Data/*/Defs/`.
4. For each referenced def name (ingredients, products, parents, comps, ITabs), verify it exists in an
   **installed** DLC — guard DLC-only refs with `MayRequire`.
5. Bump the `Version:` line in the About description if the mod tracks one (Replicator does).
6. **Validate every `.xml` file you edited** (see the `--`-in-comments pitfall above) before declaring done.
