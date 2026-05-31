# GTI Utilities — Documentation

- **Package ID:** `GTI.Utilities`
- **Author:** WarezCrawler
- **Supported game versions:** 1.6
- **Type:** Pure-XML patch mod (no assemblies)
- **DLC requirements:** None (one optional patch targets Odyssey gravship content)

## Purpose

A personal grab-bag of custom XML patches — tweaks the author applies to their own game that don't warrant
separate mods. It is **not** published to the Workshop.

## ⚠️ Current state: inert

**Every patch file in `Patches/` is currently disabled** (renamed to the `.xm_` extension, which RimWorld
does not load). As shipped, this mod loads but applies **nothing**. To activate a patch, rename it from
`.xm_` back to `.xml`. See the repo `CLAUDE.md` for the disabled-file convention.

## The patches (all currently disabled)

| File | What it would do | Conditional? |
|------|------------------|--------------|
| `PatchEngineRange.xm_` | Buffs **Odyssey gravship thruster range**. Replaces `GravshipRange` stat offsets on `CompProperties_GravshipThruster` (44→90, 32→64, 22→50, 16→40, 10→16), roughly doubling travel range per thruster. | No guard — requires Odyssey defs to be present, otherwise xpaths simply match nothing. |
| `PatchModMedicalCabinet.xm_` | Re-categorizes the **"Medical Cabinet"** mod's `Dead_MedicalCabinet` and `Dead_SimpleMedicalCabinet` into the `ASFstorage` category so they work with **Adaptive Storage Framework**. | Yes — `PatchOperationFindMod` checks for both "Medical Cabinet" and "Adaptive Storage Framework"; logs a skip message if absent. |
| `PatchModMedicalCabinet_simple.xm_` | An alternate/simpler version of the medical-cabinet re-categorization (this file actually contains two `<Patch>` blocks: a guarded `PatchOperationFindMod` copy and an unconditional copy). | Mixed — keep only one approach if re-enabling; avoid running this **and** `PatchModMedicalCabinet` together. |
| `PatchTurret_Infinite.xm_` | Duplicate of the **GTI Infinite Turrets** turret/mortar de-fueling patch. | Redundant if GTI Infinite Turrets is enabled — do not run both. |
| `PatchTurret_Infinite2.xm_` | Duplicate of the older `Turret_MiniTurret`-specific infinite-turret patch. | Redundant (see GTI Infinite Turrets docs). |

## Notes & cautions for re-enabling

- **Don't double-patch turrets.** The two `PatchTurret_*` files here overlap with the standalone
  *GTI Infinite Turrets* mod. Enable them here only if that mod is **not** subscribed.
- **Medical cabinet patches overlap.** `PatchModMedicalCabinet` (guarded) and the unconditional block inside
  `PatchModMedicalCabinet_simple` do the same thing. Re-enable one, not both, to avoid redundant operations.
- **Gravship patch is Odyssey-only.** `PatchEngineRange` has no `MayRequire`/`FindMod` guard. It is harmless
  without Odyssey (xpaths match nothing thanks to no `<success>` requirement failing only on
  `PatchOperationReplace`, which *does* error if the xpath is absent). If you re-enable it on a build without
  Odyssey, either install Odyssey or wrap the operations in a `PatchOperationFindMod` for the Odyssey DLC to
  avoid xpath-not-found errors.

## Maintenance

This mod's value is entirely in its (currently parked) patches. When updating for a new game version,
re-verify each patch's xpath against current vanilla/DLC/target-mod defs before re-enabling it.
