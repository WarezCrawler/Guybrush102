# GTI Weapon Wear — Code Reference

Per-file summary of every C# type and method in `Source/`, with purpose and where it's called
from. Two subsystems: **wear** (HP loss on use) and **repair** (incremental in-place repair at a
bench). For gameplay/design see `ModFiles/DOCUMENTATION.md`; for the XML defs/patches and how they
tie together (with diagrams) see [`XML_REFERENCE.md`](XML_REFERENCE.md).

---

## Bootstrap & settings

### `GTI_WeaponWearMod.cs`
- **`GTI_WeaponWearSettings`** (`ModSettings`) — persisted options: `repairFraction` (full-repair
  cost share), `tearMultiplier` (wear weight 0–2), `qualityInfluence` (quality effect 0–2),
  `fallbackRouting` (whether un-tagged items use the built-in C# routing; default true — read by
  `RepairRouting`), `autoRepairEquipped` (master on/off for equipped-weapon auto-repair; default
  true), `equippedRepairThreshold` (auto-repair an equipped weapon below this HP fraction; gated by
  the on/off switch).
  - `ExposeData()` — save/load the four values. Called by RimWorld's Scribe.
- **`GTI_WeaponWearMod`** (`Mod`) — entry point.
  - `GTI_WeaponWearMod(ModContentPack)` — ctor; loads `Settings`, runs `Harmony.PatchAll()`.
    Called once by RimWorld at startup.
  - `SettingsCategory()` — returns the menu label "GTI Weapon Wear". Called by the options UI.
  - `WearChanceBlurb()` — formats the effective per-use % for a normal-quality weapon. Used only
    by `DoSettingsWindowContents`.
  - `DoSettingsWindowContents(Rect)` — draws three sections top-to-bottom: **Weapon wear** (wear
    rate, quality influence), **Repairs** (repair material cost, the "Auto-assign repair benches for
    un-tagged items" fallback checkbox), and **Automatic repair** (the "Auto-repair equipped weapons"
    checkbox + HP-threshold slider, hidden while off). Called by the options UI.
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
  delegates to `TryRepairBill`. **Because the repair recipe has no material ingredient, vanilla
  always treats a repair bill as doable and commits to the first one in the stack.** If that bill
  is unfundable, returning null would abort the scan and the bench would never reach the bills
  below it (the "repair-on-top stalls the workbench" bug). So an unfundable repair bill is
  temporarily `suspended` and `base.JobOnThing` is re-asked for the next doable bill, in a loop;
  every bill touched is un-suspended in a `finally`. Passes normal bills through. Called by the
  work scheduler.
- `TryRepairBill(pawn, thing, job, bill)` — private; the per-repair-bill logic: tries the
  vanilla-chosen closest item (fast path), then enumerates the bill's other damaged items
  (closest-first) via `FindRepairCandidates`, issuing a `GTI_RepairWeapon` job for the first one it
  can fully fund. Returns null (and records a `JobFailReason` naming the nearest item's shortfall)
  when none can be funded.

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
- `RepairableWeapon(Pawn)` — the pawn's equipped primary weapon if it's a damaged, hit-pointed
  weapon; else null.
- `IsRepairBenchFor(Thing bench, ThingWithComps weapon)` — true if `bench` is a `Building_WorkTable`
  in `RepairRouting.BenchesFor(weapon.def)` (i.e. it repairs that specific weapon).
- `FindBench(Pawn)` — nearest reachable, usable bench from `RepairRouting.BenchesFor(weapon.def)`,
  so rerouting the weapon's node moves where it auto-repairs.
- `MakeJobAt(pawn, bench, out missing)` — builds the `GTI_RepairEquippedWeapon` job at that bench;
  null (+ `missing`) if materials can't be found. Does NOT apply the HP threshold.

### `JobGiver_RepairEquippedWeapon.cs` — `JobGiver_RepairEquippedWeapon` (`ThinkNode_JobGiver`)
Passive auto-repair — run from the **think tree** (inserted after the apparel optimizer via
`Patches/EquippedWeaponRepair_ThinkTree.xml`), NOT a work giver, so it works regardless of
Work-tab settings. Sits after `JobGiver_Work` (spare time only).
- `TryGiveJob(Pawn)` — if `autoRepairEquipped` is on and threshold > 0, pawn is an undrafted player
  colonist with Manipulation and the equipped weapon is below the threshold, uses
  `EquippedWeaponRepair.FindBench/MakeJobAt`
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

## Repair routing (which bench repairs which item — data-driven)

### `RepairProperties.cs` — `RepairProperties` (`DefModExtension`)
The "generic GTI node" attached to a weapon/apparel def in XML: `List<ThingDef> benches`. Presence
with ≥1 bench = repairable at exactly those benches; present but empty = explicitly never repairable;
absent = use the built-in fallback. Read by `RepairRouting`.

### `RepairRouting.cs` — `RepairRouting` (static)
Single source of truth for "which bench(es) repair this item, if any". Used by the filters,
`WorkGiver_RepairWeapon` (via the bill filter), and `EquippedWeaponRepair`.
- `BenchesFor(ThingDef)` / `BenchesFor(Thing)` — the node's `<benches>` if present; else, **only when
  the `fallbackRouting` option is on**, the fallback: hit-pointed weapons → `GTI_RepairWeapon`'s
  `recipeUsers`; apparel → `ApparelClassifier` class → that class's recipe's `recipeUsers`; else
  empty. The `useHitPoints` guard keeps `WoodLog` (an `IsWeapon` resource) from being treated as a
  repairable weapon. With the fallback off, un-tagged items return empty (non-repairable).
- `RepairableAt(Thing, ThingDef bench)` / `IsRepairable(Thing)` — convenience predicates.
- `RecipeBenches(string recipeDefName)` — a recipe's `recipeUsers` (the XML bench list), cached.
- `IsCertainAtDefLevel(ThingDef)` — whether def-level routing is authoritative for every instance
  (false only for un-noded apparel with no crafting recipe). Used by the filters' `AlwaysMatches`.
- The only routing constant in C# is the 4-entry apparel-class → recipe-defName map; benches live in
  the recipe `recipeUsers`.

### `ApparelClassifier.cs` — built-in fallback only (used when an item has no node)
- **`ApparelRepairClass`** (enum) — `NotApparel | Clothing | Armor | Utility`.
- **`ApparelClassifier`** (static): `ClassifyDef(ThingDef)` (def-level certain class: Belt→Utility,
  else craft bench, else `NotApparel`), `Classify(Thing)` (instance-level, material fallback),
  plus private `IsUtilityApparel` / `IsTailoringBench` / `IsArmorBench`. Now consulted only through
  `RepairRouting` for un-noded items.

### `SpecialThingFilterWorker_NotRepairableAt.cs` — one base + four recipe subclasses
Replaces the old three apparel-class workers. Abstract base with `RecipeDefName`; the recipe
disallows its own filter, so an item is rejected when it is NOT routed to any of that recipe's
benches (`RepairRouting.RecipeBenches`). Methods:
- `Matches(Thing)` — reject when the item's `BenchesFor` shares no bench with this recipe's benches.
- `CanEverMatch(ThingDef)` — true for hit-pointed weapons/apparel.
- `AlwaysMatches(ThingDef)` — def-level hide, only when `IsCertainAtDefLevel` and disjoint.

Subclasses `…_NotRepairWeapon / _NotRepairArmor / _NotRepairClothing / _NotRepairUtility` each name
their recipe. Referenced from `ModFiles/Defs/SpecialThingFilterDefs/GTI_RepairFilters.xml` and
invoked by RimWorld's `ThingFilter`.
