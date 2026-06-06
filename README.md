# GTI Rimworld Mods

A small collection of personal and published RimWorld mods by **WarezCrawler** (Steam: GTI), targeting RimWorld 1.6.

- **GTI Infinite Turrets** (on Steam) — turrets and mortars never need fuel or barrels
- **GTI Weapon Wear** (C#) — weapons wear out with use; repair benches + automatic upkeep
- **GTI Replicator** (personal) — workbenches that clone existing items
- **GTI Utilities** (personal) — grab-bag of small XML patches (currently all disabled)

# GTI Infinite Turrets (on Steam)

Refueling turrets and swapping out mortar barrels was added to RimWorld a while back, and I was never a fan of the change. GTI Infinite Turrets does away with that upkeep and brings back simple, no-maintenance defenses.

**Features**
- Turrets never need refueling
- Mortar barrels never need replacing
- Rocketswarm launchers recharge three times faster (1 hour instead of 3)

Why this mod? I wanted something that also works with modded turrets, not just the vanilla ones. It uses dynamic XML patches instead of hard-coded def lists, so any turret or mortar built on the standard vanilla turret setup is covered automatically.

No hard-coded defs. No dependencies. Safe to add to or remove from an existing save.

# GTI Weapon Wear

The repo's first C# mod. In vanilla RimWorld, weapons never wear out from use — this changes that, and adds easy ways to keep gear repaired so a worn weapon is never a dead end.

**Features**
- Weapons slowly lose condition as they are fired or swung. Most uses do nothing; it adds up gradually over a lot of fighting.
- Better quality weapons wear far slower.
- A weapon never destroys itself from use — once worn down to its floor the pawn simply stops using it, so you get a chance to fix it.
- Repair bills at the matching bench: weapons (machining table), armor (smithy), clothing (tailoring bench), utility gear (fabrication bench). Repairs are gradual and keep the item's exact quality, material, and name.
- Automatic upkeep: pawns repair their own equipped weapon when it drops below a condition you set — no per-pawn setup and no work type required.
- The inspect panel shows exactly which materials a repair needs and how many you have.
- Fully tunable: wear rate, quality influence, repair material cost, and the auto-repair threshold.

Requires Harmony. Safe to add to an existing save.

# GTI Replicator (Personal Mod)

A personal fork of the "Resource Replicator" mod. Adds workbenches that clone existing items — supply a sample plus resources and produce copies — so rare or hand-made items can be reproduced. Uses RimWorld's versioned `Defs/` folder layout, with recipe sets per game version.

# GTI Utilities (Personal Mod)

A personal grab-bag of small XML quality-of-life patches. All patches are currently parked/disabled (renamed to inert `.xm_` files), so the mod does nothing at runtime until one is re-enabled.
