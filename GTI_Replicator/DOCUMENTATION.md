# GTI Resource Replicator — Documentation

- **Package ID:** `A3I.RimMods.ResourceReplicator`
- **Display name:** GTI Resource Replicator [1.0->1.6]
- **Authors:** Comrade_Alexey, LeonusDH, ✬Rustic Fox✬, Chicken Plucker (GTI maintenance fork by WarezCrawler)
- **Steam Workshop ID:** `3187349754`
- **Supported game versions:** 1.0 – 1.6
- **Type:** Pure-XML content mod (no assemblies)
- **DLC requirements:** None for core function. Neurotrainer recipes require **Royalty** (auto-skipped if absent).
- **Internal version:** 1.8.6.9

## What it does

Adds three power-hungry workbenches that **duplicate items** — you place a stack of something as the
ingredient and get back more of the same. A late-game, deliberately overpowered convenience mod.

## The three workbenches

All three are `Building_WorkTable`s (3×1, 6,000W, require research, link to a Tool Cabinet for work-speed
bonus). Defined in [`Defs/ThingDefs/Base_Buildings_ResourceReplication.xml`](Defs/ThingDefs/Base_Buildings_ResourceReplication.xml).

| Building | defName | Replicates |
|----------|---------|-----------|
| Resource Replicator | `ResourceReplicator` | Raw resources: steel, wood, components, plasteel, uranium, gold, silver, jade, chemfuel, cloth, hyperweave, synthread, stone blocks, etc. |
| Organic Replicator | `OrganicReplicator` | Food & organics: meals, raw foods, meat, hay, kibble, pemmican, insect jelly, chocolate, beer, ambrosia, etc. |
| Medicinal Replicator | `MedicinalReplicator` | Medicine & drugs: herbal/industrial/ultratech medicine, neutroamine, penoxycyline, the hard drugs (flake/yayo/go-juice/wake-up/luciferium), mech serums, and (with Royalty) skill neurotrainers. |

Each recipe consumes 1 of an item and produces 2 of the same item (`workAmount` 300, Crafting skill).

## Research

Defined in [`Defs/ResearchProjectDefs/`](Defs/ResearchProjectDefs/). All Spacer tech-level, require a
Hi-Tech Research Bench + Multi-Analyzer, and appear under a custom research tab **"Rustic's Mods"**
(`ResearchTabDef` defName `RMRT`).

```
ItemReplication (concept, 5000)
  ├── ResourceReplicator  (1500)  → unlocks Resource Replicator bench
  ├── OrganicReplicator   (1500)  → unlocks Organic Replicator bench
  └── MedicinalReplicator (1500)  → unlocks Medicinal Replicator bench
```

## Work giver

[`Defs/WorkGiverDefs/Base_WorkGiverDefs.xml`](Defs/WorkGiverDefs/Base_WorkGiverDefs.xml) defines
`DoBillsReplicate` (`WorkGiver_DoBill`, Crafting work type, requires Manipulation) tied to all three benches.

## Folder structure & versioning

Uses RimWorld's multi-version layout. With no `LoadFolders.xml`, on a 1.6 game RimWorld loads the **root
`Defs/` plus the `1.6/` folder**; the `1.0`–`1.5` folders are ignored.

| Location | Contents |
|----------|----------|
| `Defs/` (root) | Shared buildings, research, work giver, and the **base resource/organic/medicinal recipes** |
| `1.0/` … `1.5/` | Recipe sets for those older game versions (not loaded on 1.6) |
| `1.6/Defs/RecipeDefs/v1.6_Recipes_ResourceReplication.xml` | The 12 **neurotrainer** replication recipes (Royalty) |
| `1.0/Languages/Russian/` | Russian translations (DefInjected) for the base content |

> The root recipe file also defines an unused abstract `TechReplicatorBase` (and a `TechReplicator`
> recipeUser that has no matching building). It is an inert template — no concrete recipe inherits from it,
> so it produces no errors.

## DLC guarding (important)

The neurotrainer recipes in `1.6/.../v1.6_Recipes_ResourceReplication.xml` reference Royalty-only items
(`Neurotrainer_Shooting`, `Neurotrainer_Melee`, … 12 total). Each `<RecipeDef>` there carries
`MayRequire="Ludeon.RimWorld.Royalty"`, so the recipes **only load when Royalty is installed** and produce
no cross-reference errors on installs without it (this machine does not have Royalty).

If you add recipes for any other DLC-specific items, guard them the same way with the appropriate
`MayRequire` packageId.

## Maintenance

To support a future game version:
1. Create a new numbered folder (e.g. `1.7/Defs/RecipeDefs/`) and add a `v1.7_Recipes_*.xml` recipe file.
2. Verify all referenced item def names still exist in `RimWorld/Data/*/Defs/` (guard DLC-only items with `MayRequire`).
3. Add the version to `<supportedVersions>` in `About/About.xml` and bump the `Version:` line in the description.
