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

All logic is in [`Patches/PatchTurret_Infinite.xml`](Patches/PatchTurret_Infinite.xml), a single
`PatchOperationSequence` with five operations:

1. **Remove `CompProperties_Refuelable` from turret buildings** — targets any `ThingDef` that has a
   `statBases/ShootingAccuracyTurret` stat (the reliable signature of a turret building), excluding
   `Turret_RocketswarmLauncher`.
2. **Remove `CompProperties_Refuelable` from `Building_TurretGun` defs** — a second pass keyed on
   `thingClass="Building_TurretGun"`, added specifically to also catch **Bean's Turret Pack** turrets.
   Again excludes `Turret_RocketswarmLauncher`.
3. **Zero `consumeFuelPerShot`** on every gun def parented to `BaseWeaponTurret` (the weapon half of a
   turret, e.g. `Gun_MiniTurret`), excluding the rocketswarm launcher.
4. **Remove the mortar-barrel `CompProperties_Refuelable`** — the refuelable comp flagged with
   `fuelIsMortarBarrel="true"`.
5. **Zero `consumeFuelPerShot`** on every weapon parented to `BaseArtilleryWeapon` (mortars).

### Why `Turret_RocketswarmLauncher` is excluded
The rocketswarm launcher is a special one-shot/charge-based turret; leaving its refuelable comp intact keeps
its intended limited-ammo behavior instead of making it fire infinitely.

## Compatibility notes (verified against vanilla 1.6)

All patch targets still exist in `Data/Core/Defs/ThingDefs_Buildings/Buildings_Security_Turrets.xml`:
`ShootingAccuracyTurret`, `CompProperties_Refuelable`, `consumeFuelPerShot`, `fuelIsMortarBarrel`, and the
abstract parents `BaseWeaponTurret` / `BaseArtilleryWeapon`. The mod is fully 1.6-compatible.

Because the operations use `<success>Always</success>`, the patch never errors even if a given xpath matches
nothing — safe to run regardless of which other turret mods are active.

## Files

| File | Status | Purpose |
|------|--------|---------|
| `Patches/PatchTurret_Infinite.xml` | **Active** | The main infinite-turret patch (see above) |
| `Patches/PatchTurret_Infinite2.xm_` | Disabled | Older `Turret_MiniTurret`-specific patch; now redundant because operation #1 covers MiniTurret generically. Kept for reference. |

## Maintenance

To support a future game version: confirm the turret structure above is unchanged in the new
`Buildings_Security_Turrets.xml`, then add the version to `<supportedVersions>` in `About/About.xml`.
No other changes are normally needed.
