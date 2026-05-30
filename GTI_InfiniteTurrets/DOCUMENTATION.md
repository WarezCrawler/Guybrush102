# GTI Infinite Turrets — Documentation

- **Package ID:** `GTI.InfiniteTurrets`
- **Author:** WarezCrawler
- **Steam Workshop ID:** `3548232334`
- **Supported game versions:** 1.6
- **Type:** Pure-XML patch mod (no assemblies)
- **DLC requirements:** None

## What it does

Removes all fuel/ammo upkeep from turrets and mortars so they behave the way they did in RimWorld 1.0 —
no refueling, no replacing mortar barrels. It does this dynamically via xpath PatchOperations rather than
hard-coding individual turret defs, so it also covers **modded** turrets that follow the vanilla turret
structure.

## How it works

All logic is in [`Patches/PatchTurret_Infinite.xml`](Patches/PatchTurret_Infinite.xml). It currently uses
the **consumption-only** approach — a single `PatchOperationReplace` that sets `consumeFuelPerShot` to `0`
on every weapon def that has it:

```xml
<xpath>Defs/ThingDef/verbs/li/consumeFuelPerShot</xpath>
```

Every vanilla turret spawns full (`initialFuelPercent=1`) and only depletes its barrel/rockets via
`consumeFuelPerShot`. Zeroing it means the fuel/barrel/rocket count never drains → effectively infinite
ammo, with **no exclusions** and **no removed components**. In vanilla 1.6, `consumeFuelPerShot` appears
*only* in `Buildings_Security_Turrets.xml` (turrets, mortar, rocketswarm), so the broad xpath is safe and
also covers modded turrets such as **Bean's Turret Pack**.

### Why this approach (and not removing the refuelable comp)

The previous version *removed* `CompProperties_Refuelable` from turret buildings. That worked for standard
turrets (whose `Building_TurretGun` treats the comp as optional "barrel durability" upkeep) but made the
**rocketswarm launcher invisible**. The rocketswarm uses `thingClass="Building_TurretRocket"` and its
top sprite is fuel-state-driven: `Gun_RocketswarmLauncher` defines an empty top (`TurretRocketEmpty_Top`)
and a `turretTopLoadedGraphic` (`TurretRocketFull_Top`), and `CompInteractableRocketswarmLauncher` reads
the comp for its activation/reload logic. Removing the comp null-derefs during draw → the turret renders
as nothing. Keeping the comp and just zeroing consumption sidesteps this entirely — the rocketswarm stays
visible *and* gets infinite rockets.

> **Status: this is currently a test build.** The open question is whether keeping the refuelable comp is
> purely cosmetic (turrets just show a permanently-full bar) or whether it changes behavior — e.g. a turret
> demanding a fresh barrel/refuel after being uninstalled and re-installed. See the in-file comment block
> for the test checklist. If the comp turns out to be "more than cosmetic," the fallback is to go back to
> removing it, but keyed structurally on `thingClass!="Building_TurretRocket"` (robust against modded
> rocket turrets) rather than a hardcoded `defName`.

## Compatibility notes (verified against vanilla 1.6)

`consumeFuelPerShot` still exists on the turret/mortar/rocketswarm weapon defs in
`Data/Core/Defs/ThingDefs_Buildings/Buildings_Security_Turrets.xml`, and is the only place it appears in
vanilla. The operation uses `<success>Always</success>`, so the patch never errors even if the xpath
matches nothing — safe alongside any other turret mods.

## Files

| File | Status | Purpose |
|------|--------|---------|
| `Patches/PatchTurret_Infinite.xml` | **Active** | The infinite-turret patch (consumption-only approach, see above) |
| `Patches/PatchTurret_Infinite2.xm_` | Disabled | Older `Turret_MiniTurret`-specific patch; redundant now. Kept for reference. |

## Maintenance

To support a future game version: confirm the turret structure above is unchanged in the new
`Buildings_Security_Turrets.xml`, then add the version to `<supportedVersions>` in `About/About.xml`.
No other changes are normally needed.
