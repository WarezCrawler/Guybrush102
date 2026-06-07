# GTI Weapon Wear

A C# + XML + Harmony RimWorld mod (RimWorld 1.6). Requires Harmony. **Work in progress.**

End goal: weapons lose hit points as they are used (firing / melee swings), i.e. weapon
degradation-on-use, which vanilla does not support. Repair was implemented first so wear is
never purely punishing; the wear mechanic itself is now in (Step 3 below).

## Step 2 — item repair

Repair covers **weapons AND apparel**, each at the bench that matches it:

| Item | Bill | Bench(es) |
|---|---|---|
| Weapons (any) | `repair weapon` | Machining table |
| Armor | `repair armor` | Electric/Fueled Smithy |
| Clothing | `repair clothing` | Electric/Hand Tailoring bench |
| Utility gear (belt slot) | `repair utility gear` | Fabrication bench |

### Where an item is repaired is data-driven (`RepairProperties` node)

Which bench repairs an item — **and whether it is repairable at all** — is decided by
`RepairRouting`, in this order:

1. **A `RepairProperties` modExtension on the item's def wins.** It lists the `<benches>` the item
   may be repaired at. An empty/omitted `<benches>` means **explicitly never repairable**. Add this
   node from any mod (via `PatchOperationAddModExtension`) to route or block your own items — no C#
   needed. See the disabled `Patches/Examples/RepairRouting_Example.xm_` for a copy-paste template.
2. **Otherwise the built-in fallback** classifies the (hit-pointed) item:
   - **Weapons** → the machining table.
   - **Apparel** via `ApparelClassifier`: **Utility** first (anything in the `Belt` apparel layer —
     shield belt, smokepop belt, packs — → **fabrication bench**, regardless of craft bench); else
     by where it is *crafted* (`recipeMaker.recipeUsers`: tailoring → clothing, smithy/fabrication/
     machining → armor); else by material (fabric/leather → clothing, metal → armor).

So an un-patched item (vanilla or third-party) keeps the sensible default; the node is a pure
override. Bench lists themselves live in the recipe `<recipeUsers>` (XML) — `RepairRouting` reads
them, never hardcodes them.

The fallback in step 2 can be **switched off** with the **Auto-assign repair benches for un-tagged
items** mod option (`fallbackRouting`, default on). With it off, *only* items carrying an explicit
`RepairProperties` tag are repairable; everything else is non-repairable (but still wears).

> For a full reference of the XML defs and patches (recipes, filters, work givers, the per-item tag,
> the compat patch) and how they tie together — with diagrams — see `XML_REFERENCE.md` in the mod's
> dev repo.

Four `SpecialThingFilter`s (`GTI_NotRepairWeapon`, `GTI_NotRepairArmor`, `GTI_NotRepairClothing`,
`GTI_NotRepairUtility`) — one per repair recipe — keep each bench's bill list to exactly the items
routed to it, via the "disallow the unwanted subset" pattern: a recipe rejects an item when the
item is not routed to any of that recipe's benches.

> Naming note: the repair engine is generic, but several classes keep `Weapon`/`weapon` in
> their names from when it was weapon-only — `WorkGiver_RepairWeapon`, `JobDriver_RepairWeapon`,
> `RecipeWorker_RepairWeapon`, `WeaponRepairCost`. They handle weapons and apparel alike. The
> four repair recipes all share `workerClass RecipeWorker_RepairWeapon` (the marker the
> WorkGiver/Harmony patch use). One generic WorkGiver class serves all benches via just two
> `WorkGiverDef`s — `GTI_RepairSmithing` (Smithing/120: machining + smithy + fabrication) and
> `GTI_RepairTailoring` (Tailoring/115: tailoring benches). They can't merge further because a
> `WorkGiverDef` has a single workType. Neither adds a row to the Work tab (both reuse vanilla
> work types).

The damaged item is listed as a `count:1` ingredient and is **not consumed** — it is repaired
in place, so the exact same Thing survives (quality, material, biocode, name, art, tainted
status preserved).

Repair is **incremental**: a custom WorkGiver + JobDriver raises the item's HitPoints one point
at a time and consumes materials with payment **leading** the repair (pay-before, rounded up),
so you can never gain HP you haven't paid for. Interrupting leaves the item partially repaired.

**Material cost = the item's own original resources.** The cost is computed per item from its
crafting cost:

    per material = ceil( originalCount × 0.25 × (missingHP / maxHP) ), min 1

- Sources: `costStuffCount` × the weapon's actual material (stuff weapons) plus the def's
  `costList`, with `ComponentIndustrial` / `ComponentSpacer` always excluded.
- So a steel gun costs steel, a plasteel sword costs plasteel, a wood bow costs wood; a fully
  broken weapon costs ~25% of its build cost, a lightly damaged one far less.
- (Modded components aren't auto-excluded yet — only the two vanilla component defs.)

Code:
- `WeaponRepairCost.Compute` — the cost formula above.
- `WorkGiver_RepairWeapon : WorkGiver_DoBill` (`GTI_RepairWeaponMachining`, workType Smithing,
  priority 80). Reuses vanilla to find the weapon, then computes + finds the materials on the
  map and appends them to the job's ingredient queue. Returns no job if materials aren't
  reachable. Passes normal crafting bills through.
- `Patch_WorkGiverDoBill_SkipRepair` — Harmony postfix so **vanilla** machining givers never
  run the (material-less) repair recipe for free; only our giver does.
- `JobDriver_RepairWeapon` — hauls weapon + computed materials to the bench, `HitPoints++`
  every ~25 work-ticks (scaled by work speed) until full; `RepairProgress` consumes the hauled
  materials proportionally. The recipe `workerClass` `RecipeWorker_RepairWeapon` is the marker
  the WorkGiver/patch use and an atomic in-place fallback.

Default ingredient HP filter is `0~99%`, so only damaged weapons are picked up. All weapons
(including stone) are now repairable with their own material.

**Material feedback.** Because the cost is computed per item and never listed on the bill, a
damaged weapon/apparel's **inspect panel** shows a `Repair needs: Nx Material (have M)` line
(`Patch_Thing_RepairInfo`), so you can see exactly what a repair will consume and whether you
have it. If a pawn can't start a repair because the colony is out of that material, ordering it
manually (right-click) shows a "Not enough materials to repair …" reason (`JobFailReason`). This
is the common reason a valid-looking repair target is silently skipped.

### How to test repair
1. Have a Machining table; damage a weapon (combat, or dev-mode "lower HP").
2. Add the **repair weapon** bill.
3. A crafter hauls the weapon + the right materials, repairs it gradually to full HP with the
   same quality/material, consuming roughly 25%×damage of its original resources.
4. Note: the bill UI does not list a material requirement (materials are computed
   dynamically); a repair won't start unless the needed material is available on the map.

## Step 3 — weapon wear on use (current work)

Each time a weapon is **used** — a ranged shot actually fired, or a melee swing — there is a
chance it loses **1 hit point**. The roll is random, so most uses do nothing.

    chance per use = 0.10 × tearMultiplier × qualityMultiplier(quality, qualityInfluence)

- **Base** = 10% per use (a normal-quality weapon at default settings).
- **`tearMultiplier`** (mod settings, 0–2, default 1): overall weight. 0 = no wear, 2 = double.
- **`qualityInfluence`** (mod settings, 0–2, default 1): how strongly quality matters. At 0 all
  weapons wear equally; at 1 the per-quality factors below apply; at 2 the spread doubles.
- Per-quality factor (multiplier at influence 1): Awful 1.4, Poor 1.2, Normal 1.0, Good 0.8,
  Excellent 0.6, Masterwork 0.4, Legendary 0.2 — so better weapons wear more slowly.

**Safeguard / floor:** wear never takes a weapon below **1 HP**. At 1 HP the weapon is
considered worn out and its combat verbs report unavailable (`Verb.Available()` patch), so the
pawn **stops using it** — meaning ordinary use can never destroy a weapon. A worn weapon is
still repairable (the repair filter is 0–99%). If a weapon reaches **0 HP** by other means it
is destroyed by the vanilla rules and is therefore gone / unrepairable.

Code (`Source/`):
- `WeaponWear.Notify_WeaponUsed(weapon)` — the wear roll + 1-HP floor. `WearChance` /
  `QualityWearMultiplier` implement the formula above. `IsWornOut` is the floor test.
- `Patch_VerbLaunchProjectile_TryCastShot` — postfix on `Verb_LaunchProjectile.TryCastShot`
  (covers `Verb_Shoot`/`Verb_ShootOneUse`); fires when a pawn actually shoots.
- `Patch_VerbMeleeAttack_TryCastShot` — postfix on `Verb_MeleeAttack.TryCastShot`; fires once
  per pawn melee swing. Body-part attacks have no `EquipmentSource` and are ignored.
- `Patch_Verb_Available` — postfix on `Verb.Available()`; forces `false` for any verb whose
  `EquipmentSource` is a worn-out weapon. Only real weapons are gated (apparel/body verbs are
  untouched). Turrets are skipped (`CasterIsPawn` guard in the use patches).

### How to test wear
1. Equip a pawn with a weapon; note its HP. Set `tearMultiplier` high (e.g. 2) in mod settings
   to see it quickly.
2. Have the pawn fire / melee repeatedly (hunt, or draft + attack). HP ticks down ~1 at a time.
3. Compare a Legendary vs Awful weapon (with `qualityInfluence` at 1) — the Awful one wears
   noticeably faster.
4. Let one drop to 1 HP: the pawn should refuse to keep using it (switches to fists / can't
   fire). Repair it at the matching bench to restore it.

Scaffolding from Steps 1–2 (the `GTI ranged/melee weapon` inspect-string and `MaxHitPoints`
`StatPart`) has been **removed** — it was only ever a load indicator.

### Auto-repair of equipped weapons

Vanilla lets you set an apparel HP policy (drop/replace clothes below X% HP) but has **no
equivalent for the equipped weapon**, so a degrading weapon needs constant micromanagement.
This adds it: while the **Auto-repair equipped weapons** option is on (mod options, default on) and
an **undrafted** pawn's primary weapon falls below the **`equippedRepairThreshold`** setting
(default 50%), the pawn carries the needed materials to the bench that repairs that weapon (per its
routing — `RepairRouting.BenchesFor`, so rerouting a weapon's node also moves its auto-repair) and
repairs the weapon **back to full**.

- The weapon **stays equipped** the whole time — only the materials are hauled — so there is no
  drop, swap, spare-weapon requirement, or re-equip. The same weapon (quality/material) is kept.
- Cost and pay-before consumption are identical to bench repair (`WeaponRepairCost` +
  `RepairProgress`). Interrupting (e.g. a raid drafts the pawn) leaves it partially repaired.
- **Not a work type.** It runs from the **think tree**, inserted right after vanilla's apparel
  optimizer (`Patches/EquippedWeaponRepair_ThinkTree.xml`), exactly like the "drop worn clothes /
  equip better" behaviour. This is deliberate: it must work **regardless of the Work tab**, since
  the combat pawns that carry weapons usually have crafting disabled. The node sits *after*
  `JobGiver_Work`, so it only uses **spare time** and never interrupts real work, and never runs
  while drafted. The pawn still needs **Manipulation** and a reachable, usable repair bench
  with the right material; the bench/material scan is throttled per pawn (~10 s).
- Master control is the **Auto-repair equipped weapons** on/off checkbox; the threshold slider
  (greyed out while off) sets the trigger level. There is no per-pawn Work-tab toggle.
- **Manual override:** select a pawn and **right-click a repair bench** → **"Repair `<weapon>` now"**
  forces an immediate repair to full, **ignoring the threshold** (works for any damaged weapon, even
  lightly damaged). If the colony lacks the material the option is shown greyed with the shortfall
  (e.g. *"… (needs 4x steel)"*). Implemented as a 1.6 `FloatMenuOptionProvider` (auto-discovered;
  no patch). The job is `playerForced`, so it runs even if the pawn is busy.
- **Out-of-material heads-up:** if a pawn wants to repair its own weapon but the colony lacks the
  material, a light top-left message fires (e.g. *"Brick can't repair pistol: needs 4x steel"*) —
  a transient message, not a letter. Throttled to at most once per pawn per in-game day so it
  can't spam, and only for these **personal** repairs (bench bills show the shortfall via the
  item's inspect line / right-click reason instead).
- Code: `JobGiver_RepairEquippedWeapon.TryGiveJob` (finds bench + materials, builds the job) and
  `JobDriver_RepairEquippedWeapon` (hauls materials, ticks the equipped weapon's HP up).

Synergy with the 1-HP safeguard: a weapon worn down to 1 HP is below the threshold, so an
undrafted pawn will fix it automatically; the safeguard only blocks *using* a 1-HP weapon, not
repairing it.

## Mod settings — sliders and the formulas they feed

The settings (`GTI_WeaponWearSettings`, shown in *Options → Mod Settings → GTI Weapon Wear*) are
grouped into three sections, top to bottom: **Weapon wear** (the mechanic) → **Repairs** (cost +
what's repairable where) → **Automatic repair** (the convenience layer). Each plugs into one place:

| Section | Setting (field) | Range / default | Used in |
|---|---|---|---|
| Weapon wear | **Wear rate** (`tearMultiplier`) | 0–2, default 1 | wear chance (below) |
| Weapon wear | **Quality influence** (`qualityInfluence`) | 0–2, default 1 | quality multiplier (below) |
| Repairs | **Repair material cost** (`repairFraction`) | 0–1, default 0.25 | repair cost (below) |
| Repairs | **Auto-assign repair benches for un-tagged items** (`fallbackRouting`) | on/off, default on | `RepairRouting` fallback |
| Automatic repair | **Auto-repair equipped weapons** (`autoRepairEquipped`) | on/off, default on | master switch for auto-repair (below) |
| Automatic repair | **Repair when below** (`equippedRepairThreshold`) | 0–1, default 0.5 | auto-repair trigger (below) |

**Wear chance** — rolled once per weapon use (a fired shot or a melee swing); on success the weapon
loses 1 HP (floored at 1, so use alone never destroys it). `WeaponWear.WearChance`:

    wearChance = 0.10 × tearMultiplier × qualityMultiplier

- `0.10` is `WeaponWear.BaseChancePerUse` (10% for a normal weapon at defaults).
- If `tearMultiplier ≤ 0` the chance is 0 (wear off). Result is clamped to ≥ 0.

**Quality multiplier** — how quality bends the wear chance. `WeaponWear.QualityWearMultiplier`:

    qualityMultiplier = max( 0, 1 + (qualityFactor − 1) × qualityInfluence )

- `qualityFactor` is a **fixed constant looked up from the weapon's quality category**
  (`WeaponWear.QualityBaseFactor`) — it is *not* a setting. It is the raw wear multiplier a quality
  would impose on its own (i.e. the value of `qualityMultiplier` when `qualityInfluence = 1`).
  Values below 1 slow wear, above 1 speed it up:

  | Quality | Awful | Poor | Normal | Good | Excellent | Masterwork | Legendary |
  |---|---|---|---|---|---|---|---|
  | `qualityFactor` | 1.4 | 1.2 | 1.0 | 0.8 | 0.6 | 0.4 | 0.2 |

- `qualityInfluence` (the setting) then scales how far `qualityFactor` is allowed to pull the
  multiplier away from 1.0: at `0` the multiplier is 1 for every quality (quality ignored), at `1`
  it equals `qualityFactor`, at `2` the deviation from 1.0 doubles. A weapon with no quality
  category uses `qualityFactor = 1` (no effect).

**Repair material cost** — per material, computed from the item's own original resources.
`WeaponRepairCost.Compute`:

    perMaterial = max( 1, ceil( originalCount × repairFraction × (missingHP / maxHP) ) )

- `originalCount` = `costStuffCount` × the item's stuff (stuff items) plus the def's `costList`,
  with `ComponentIndustrial` / `ComponentSpacer` excluded.
- `missingHP / maxHP` is the current damage fraction, so cost scales from ~0 (pristine) up to
  `repairFraction` of the full build cost (fully broken).

**Auto-repair trigger** — when an undrafted pawn's equipped weapon is repaired automatically.
`JobGiver_RepairEquippedWeapon`:

    auto-repair fires when   autoRepairEquipped  AND  (HitPoints / MaxHitPoints) < equippedRepairThreshold

- `autoRepairEquipped = false` (the on/off checkbox) disables the feature entirely (no scan); the
  manual right-click repair still works. `equippedRepairThreshold = 0` also yields no scan.

## Source / build / deploy

Everything originates from the dev repo and is deployed by the C# build:
`O:\Mod Development\Rimworld\Guybrush102\GTI_WeaponWear\`

```
GTI_WeaponWear/                    (dev repo — source of truth)
├── GTI_WeaponWear.csproj
├── Properties/AssemblyInfo.cs
├── CODE_REFERENCE.md              (per-file C# method reference — dev only, not deployed)
├── Source/*.cs                    (compiled into the DLL)
└── ModFiles/                      (mod payload — NOT compiled)
    ├── About/About.xml
    ├── Defs/                      (recipes, work givers, jobs, filters)
    ├── Patches/                   (think-tree insert, Repair Workbench compat, routing examples)
    ├── CHANGELOG.txt              (dated changelog — Steam Workshop copy source)
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
├── Defs/
├── Patches/
├── CHANGELOG.txt
├── Assemblies/GTI_WeaponWear.dll
└── DOCUMENTATION.md
```

- Requires Harmony (brrainz.harmony) — declared in `About.xml` (`modDependencies` + `loadAfter`).

## Changelog

The changelog has moved to a dated plain-text file, **`CHANGELOG.txt`** (in the dev repo at
`ModFiles/CHANGELOG.txt`; deployed to the mod root), for easy copy-paste into the Steam Workshop
description. Every entry there is dated.
