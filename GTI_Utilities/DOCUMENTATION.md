# GTI Utilities — Documentation

- **Package ID:** `GTI.Utilities`
- **Author:** WarezCrawler
- **Supported game versions:** 1.6
- **Type:** Pure-XML patch mod (no assemblies)
- **DLC requirements:** None (one optional patch targets Odyssey gravship content)

## Purpose

A personal grab-bag of custom XML patches — tweaks the author applies to their own game that don't warrant
separate mods. It is **not** published to the Workshop.

## Current state

**Two patches are active** (`PatchToxicEnvironmentResistance.xml` and `PatchAnimalProstheticsBench.xml`,
the latter paired with the `Defs/Buildings_AnimalBionics.xml` building def); **every other patch file in
`Patches/` is disabled** (renamed to the `.xm_` extension, which RimWorld does not load). To activate a
disabled patch, rename it from `.xm_` back to `.xml`. See the repo `CLAUDE.md` for the disabled-file
convention.

## The animal augmentation bench (active)

A new building, `GTI_AnimalBionicsTable` ("animal augmentation bench"), defined in
`Defs/Buildings_AnimalBionics.xml`. Its veterinary-themed artwork
(`Textures/Things/Building/Production/GTI_AnimalBionicsTable_{south,north,east}.png`; west is auto-mirrored)
is **derived from EPOE's `TableBionics`** — hue-shifted blue→green and stamped with a glowing paw decal on
the console, then baked into standalone PNGs, so the bench is **self-contained at runtime** (it keeps
RimWorld's native shading/perspective and needs no other mod's texture to draw). Because it is derived from
EPOE art it is **personal/unpublished use only**. Regenerate with `_art_gen.py` (Pillow) in the mod root;
RimWorld ignores the `.py`. Unlocked by A Dog Said's `SimpleAnimalProsthetics` ("Animal prosthetics")
research.

`Patches/PatchAnimalProstheticsBench.xml` then re-routes **A Dog Said... Animal Prosthetics 2** craft
recipes to this bench *exclusively*, by replacing `recipeMaker/recipeUsers` on its abstract bases:

| ADS base patched | Tier covered | Was crafted at |
|------------------|--------------|----------------|
| `BodyPartProstheticAnimalBase` | Simple prosthetics (+ surrogate organs by inheritance) | `TableMachining` |
| `BodyPartBionicAnimalBase` | Bionics | `FabricationBench` → EPOE `TableBionics` |
| `BodyPartSyntheticAnimalBase` *(EPOE only)* | Synthetic organs | EPOE `TableOrgans` |

The surrogate base (`BodyPartSurrogateAnimalBase`) has no `recipeUsers` of its own and inherits from the
simple base, so it needs no separate patch. The synthetic base explicitly overrides `recipeUsers`
(`Inherit="False"`), so it gets its own (EPOE-guarded) replace.

**Caveats:**
- The patch is guarded by `PatchOperationFindMod` (ADS, and EPOE for the synthetic-organ op), so it no-ops
  cleanly if those mods are absent.
- ADS's own EPOE-compat patch also sets the bionic base's `recipeUsers`. To win, **GTI Utilities must load
  after A Dog Said and EPOE-Forked** — enforced via `<loadAfter>` in `About.xml`.
- Because recipes are *exclusive* to this bench, the bench is gated on the earliest tier
  (`SimpleAnimalProsthetics`) so simple animal prosthetics are never locked behind bionics research.

## The patches

| File | What it does | Conditional? |
|------|------------------|--------------|
| `PatchToxicEnvironmentResistance.xml` **(ACTIVE)** | Grants **Toxic Environment Protection** (`ToxicEnvironmentResistance` `equippedStatOffsets`) to vanilla armor, mirroring the gas mask: recon armor +10%, recon helmet +20%, marine armor +20%, marine helmet +40%. | No guard — targets Core defs that are always present. |
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
