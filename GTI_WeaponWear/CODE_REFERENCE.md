# GTI Weapon Wear — Code Reference

Per-file summary of every C# type and method in `Source/`, with purpose and where it's called
from. Two subsystems: **wear** (HP loss on use) and **repair** (incremental in-place repair at a
bench). For gameplay/design see `ModFiles/DOCUMENTATION.md`.

---

## Bootstrap & settings

### `GTI_WeaponWearMod.cs`
- **`GTI_WeaponWearSettings`** (`ModSettings`) — persisted options: `repairFraction` (full-repair
  cost share), `tearMultiplier` (wear weight 0–2), `qualityInfluence` (quality effect 0–2),
  `equippedRepairThreshold` (auto-repair an equipped weapon below this HP fraction; 0 = off).
  - `ExposeData()` — save/load the four values. Called by RimWorld's Scribe.
- **`GTI_WeaponWearMod`** (`Mod`) — entry point.
  - `GTI_WeaponWearMod(ModContentPack)` — ctor; loads `Settings`, runs `Harmony.PatchAll()`.
    Called once by RimWorld at startup.
  - `SettingsCategory()` — returns the menu label "GTI Weapon Wear". Called by the options UI.
  - `WearChanceBlurb()` — formats the effective per-use % for a normal-quality weapon. Used only
    by `DoSettingsWindowContents`.
  - `DoSettingsWindowContents(Rect)` — draws the four sliders (repair cost, wear multiplier,
    quality influence, equipped-weapon auto-repair threshold). Called by the options UI.
- **`Settings`** (static) — global accessor read by `WeaponWear` and `WeaponRepairCost`.

### `GTI_JobDefOf.cs`
- **`GTI_JobDefOf`** (`[DefOf]`) — holds `JobDef GTI_RepairWeapon` (bench-bill repair) and
  `GTI_RepairEquippedWeapon` (auto-repair of an equipped weapon). Resolved by RimWorld;
  referenced by the respective WorkGivers.

---

## Wear subsystem

### `WeaponWear.cs` — `WeaponWear` (static)
Core wear rules. Consts: `BaseChancePerUse = 0.10`, `MinHitPointsFromWear = 1` (the floor).
- `IsWornOut(ThingWithComps)` — true when a hit-pointed weapon is at/below the 1-HP floor. Used by
  the `Verb.Available` safeguard patch to disable a worn weapon.
- `Notify_WeaponUsed(ThingWithComps)` — one wear roll per use: rolls `WearChance`, drops 1 HP
  (never below the floor). Called by the two `TryCastShot` patches.
- `WearChance(ThingWithComps)` — computes `base × tearMultiplier × qualityMult`. Used by
  `Notify_WeaponUsed` and `WearChanceBlurb`.
- `QualityWearMultiplier(Thing, influence)` — maps quality→wear factor, scaled by the influence
  slider. Used by `WearChance`.
- `QualityBaseFactor(QualityCategory)` — the per-quality table (Awful 1.4 … Legendary 0.2).
  Private; used by `QualityWearMultiplier`.

### `Patch_WeaponWear.cs` — Harmony patches (auto-discovered by `PatchAll`)
- **`Patch_VerbLaunchProjectile_TryCastShot.Postfix`** — after a ranged shot fires (`__result`
  true, pawn caster) → `Notify_WeaponUsed(EquipmentSource)`. Patches
  `Verb_LaunchProjectile.TryCastShot` (covers all guns).
- **`Patch_VerbMeleeAttack_TryCastShot.Postfix`** — same, per melee swing. Patches
  `Verb_MeleeAttack.TryCastShot`.
- **`Patch_Verb_Available.Postfix`** — forces `Available()` false for any verb whose
  `EquipmentSource` is a worn-out weapon (the "stop using at 1 HP" safeguard). Patches
  `Verb.Available`.

---

## Repair subsystem

### `WeaponRepairCost.cs` — `WeaponRepairCost` (static)
- `Compute(Thing)` — returns the materials to repair an item: `ceil(originalCount ×
  repairFraction × missingHP/maxHP)`, min 1, from `costStuffCount`+`costList`, components
  excluded. Called by `WorkGiver_RepairWeapon.JobOnThing`.
- `Add(dict, def, n)` — private accumulator helper.
- `IsComponent(ThingDef)` — private; excludes `ComponentIndustrial`/`ComponentSpacer`.

### `RepairUtil.cs` — `RepairUtil` (static)
Helpers shared by both repair WorkGivers/JobDrivers.
- `TryFindMaterials(pawn, near, needed, queue, counts)` — nearest-first reachable/unforbidden
  material search, appending to the ingredient queue. Used by both WorkGivers.
- `TryFindMaterials(…, out missing)` — same, but reports the per-material shortfall when it
  fails. Used by both WorkGivers to build a `JobFailReason`.
- `DescribeMaterials(mats)` — "4x cloth, 2x steel" for player-facing messages. Used by both
  WorkGivers.
- `GatherStagedMaterials(Map, Building_WorkTable)` — sums loose resource items in the bench cells
  (excludes the bench and any weapon/apparel). Used by both JobDrivers to seed `RepairProgress`.
- `JumpToCollectNextIntoHandsForBill(Toil, TargetIndex)` — port of vanilla's hauling top-up
  helper. Used by both JobDrivers during the haul phase.

### `Patch_RepairInfo.cs`
- **`Patch_Thing_RepairInfo.Postfix`** — appends a `Repair needs: Nx Material (have M)` line to a
  damaged weapon/apparel's inspect panel (materials are otherwise never shown, since they're
  computed dynamically). Patches `Thing.GetInspectString`. Uses `WeaponRepairCost.Compute` and
  `map.resourceCounter`.

### `WorkGiver_RepairWeapon.cs` — `WorkGiver_RepairWeapon` (`WorkGiver_DoBill`)
Generic bench-bill repair giver (Machining/Smithy/Tailoring/Fabrication via the repair recipes).
- `JobOnThing(Pawn, Thing, forced)` — calls base to find a doable bill; for repair recipes,
  computes materials (`RepairUtil.TryFindMaterials`) and issues a `GTI_RepairWeapon` job (null +
  a `JobFailReason` naming the shortfall if materials unreachable); passes normal bills through.
  Called by the work scheduler.

### `JobDriver_RepairWeapon.cs` — `JobDriver_RepairWeapon` (`JobDriver`)
Runs the `GTI_RepairWeapon` (bench-bill) job. Const `TicksPerHitPoint = 25`.
- `TryMakePreToilReservations(bool)` — reserves bench + ingredient queue. Called by the job system.
- `MakeNewToils()` — toil sequence: reserve → haul weapon+materials to bench → repair toil →
  finish bill iteration. Called by the job system.
- `MakeRepairToil()` — incremental toil: each ~25 work-ticks, pay-before via
  `RepairProgress.TryPayForNextPoint()` then `HitPoints++` until full. Uses
  `RepairUtil.GatherStagedMaterials`. Used by `MakeNewToils`.

### `EquippedWeaponRepair.cs` — `EquippedWeaponRepair` (static)
Shared logic for repairing a pawn's own equipped weapon (used by the JobGiver and the float-menu
provider).
- `Recipe` (cached) — the `GTI_RepairWeapon` `RecipeDef` via DefDatabase.
- `RepairableWeapon(Pawn)` — the pawn's equipped primary weapon if it's a damaged, hit-pointed
  weapon; else null.
- `IsRepairBench(Thing)` — true if the thing is a `Building_WorkTable` hosting the repair recipe.
- `FindBench(Pawn)` — nearest reachable, usable repair bench.
- `MakeJobAt(pawn, bench, out missing)` — builds the `GTI_RepairEquippedWeapon` job at that bench;
  null (+ `missing`) if materials can't be found. Does NOT apply the HP threshold.

### `JobGiver_RepairEquippedWeapon.cs` — `JobGiver_RepairEquippedWeapon` (`ThinkNode_JobGiver`)
Passive auto-repair — run from the **think tree** (inserted after the apparel optimizer via
`Patches/EquippedWeaponRepair_ThinkTree.xml`), NOT a work giver, so it works regardless of
Work-tab settings. Sits after `JobGiver_Work` (spare time only).
- `TryGiveJob(Pawn)` — if threshold > 0, pawn is an undrafted player colonist with Manipulation
  and the equipped weapon is below the threshold, uses `EquippedWeaponRepair.FindBench/MakeJobAt`
  (throttled per pawn via `nextScanTick`) to build the job. Null otherwise. Called by the think
  tree each time the pawn seeks a job.
- `NotifyMissingMaterials(pawn, weapon, missing, now)` — private; fires a light transient
  `Messages.Message` (`NeutralEvent`, non-historical) when a personal repair is blocked on
  material, throttled per pawn (`nextMessageTick`, ~1 day). Used by `TryGiveJob`.

### `FloatMenuOptionProvider_RepairEquippedWeapon.cs` — (`FloatMenuOptionProvider`)
Right-click a repair bench → "Repair `<weapon>` now", forcing a repair regardless of threshold.
Auto-discovered by 1.6's float-menu system (no Def/Harmony). Gated to undrafted, single-select,
Manipulation-capable pawns.
- `Drafted`/`Undrafted`/`Multiselect`/`RequiresManipulation` — base gates (false/true/false/true).
- `GetOptionsFor(Thing clickedThing, FloatMenuContext)` — if the thing is a repair bench and the
  pawn's weapon is damaged, yields an enabled "Repair … now" option (`playerForced` job via
  `TryTakeOrderedJob`), or a greyed option naming the missing material / unreachable bench.

### `JobDriver_RepairEquippedWeapon.cs` — `JobDriver_RepairEquippedWeapon` (`JobDriver`)
Runs the `GTI_RepairEquippedWeapon` job. The weapon stays equipped — only materials are hauled.
Const `TicksPerHitPoint = 25`; `Weapon` => `pawn.equipment.Primary`.
- `TryMakePreToilReservations(bool)` — reserves bench + material queue. Called by the job system.
- `MakeNewToils()` — reserve → haul materials to bench → repair toil. Fails on draft / weapon
  lost. Called by the job system.
- `MakeRepairToil()` — incremental toil ticking up the equipped weapon's HP, pay-before via
  `RepairProgress` over `RepairUtil.GatherStagedMaterials`. Used by `MakeNewToils`.

### `RepairProgress.cs` — `RepairProgress`
Pay-before material consumption so HP is never granted unpaid.
- `RepairProgress(pawn, cells, toConsume, repairAmount)` — ctor; captures the consumption plan.
  Created in `MakeRepairToil`.
- `TryPayForNextPoint()` — consumes (rounded up) the materials owed up to the next HP; affordability
  checked first, returns false consuming nothing if short. Called each granted point by the toil.
- `StagedItems()` — private; loose resources currently in the bench cells.
- `Available(staged, def)` / `Remove(staged, def, amount)` — private stack tally / destroy helpers.

### `RecipeWorker_RepairWeapon.cs` — `RecipeWorker_RepairWeapon` (`RecipeWorker`)
- `ConsumeIngredient(Thing, RecipeDef, Map)` — for a weapon, restores HP in place and does NOT
  destroy it (preserves quality/material/etc.); other ingredients consumed normally. Also the
  marker type that the WorkGiver / skip-patch recognise. Called by the vanilla bill flow (atomic
  fallback path).

### `Patch_WorkGiverDoBill.cs`
- **`Patch_WorkGiverDoBill_SkipRepair.Postfix`** — nulls any repair-recipe job produced by a
  non-GTI `WorkGiver_DoBill`, so vanilla can't run the material-less recipe for free. Patches
  `WorkGiver_DoBill.JobOnThing`.

---

## Apparel routing (which bench repairs which apparel)

### `ApparelClassifier.cs`
- **`ApparelRepairClass`** (enum) — `NotApparel | Clothing | Armor | Utility`.
- **`ApparelClassifier`** (static):
  - `ClassifyDef(ThingDef)` — def-level *certain* class: Belt layer→Utility, else by crafting
    bench; `NotApparel` if unsure. Used by all three filters' `AlwaysMatches` (def-level list
    hiding) and by `Classify`.
  - `Classify(Thing)` — instance-level: `ClassifyDef` first, then material fallback (always
    resolves apparel to a class). Used by all three filters' `Matches`.
  - `IsUtilityApparel(ThingDef)` — private; true if the def uses the `Belt` apparel layer.
  - `IsTailoringBench(ThingDef)` / `IsArmorBench(ThingDef)` — private bench-name tests used by
    `ClassifyDef`.

### `SpecialThingFilterWorker_NotArmorApparel.cs` / `_NotClothApparel.cs` / `_NotUtilityApparel.cs`
Three filters, one per apparel class. Each is *disallowed* by its recipe so only the target class
remains selectable. All share the same three methods:
- `Matches(Thing)` — true for apparel that is NOT the target class (per-instance selection block).
- `CanEverMatch(ThingDef)` — true for any apparel (lets the filter apply to apparel defs).
- `AlwaysMatches(ThingDef)` — true when the def is *certainly* a different class, so it's hidden
  from the recipe's bill list entirely.

All three are referenced from `ModFiles/Defs/SpecialThingFilterDefs/GTI_ApparelFilters.xml` and
invoked by RimWorld's `ThingFilter`.
