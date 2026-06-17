# GTI Runtime GC — Fork & Maintenance Notes

Personal fork of **RuntimeGC** (the in-game cleaner). Upstream credit: user19990313
(original), louize & RunningBugs (1.5/1.6 fork). This document records what was
verified, what was broken, what was fixed, and the decisions still open.

- **Target game:** RimWorld 1.6 (verified against installed build 1.6.4633).
- **Source origin:** The upstream Steam mod ships its own C# source (an ILSpy
  decompile). We copied that source rather than re-decompiling — it is the same
  code the published DLL is built from.
- **How verification was done:** every feature was traced to its code, and every
  reflection target / API it touches was checked against the *installed* vanilla
  `Assembly-CSharp.dll`. Reflection is the main risk here because the compiler
  cannot catch a renamed field — it only fails at runtime.

---

## Feature status at a glance

| Feature | Status |
|---|---|
| Remove world pawns (incl. 1.6 quest/ceremony-pawn protection) | ✅ Works |
| Clear avoid grids | ✅ Works |
| Remove animal-family members | ✅ Works |
| Remove filth / snow (home area) | ✅ Works |
| Remove corpses | ✅ Works |
| Remove battle-log entries | ✅ Works *(after fix #1)* |
| Fix faction relationships | ✅ Works |
| Re-generate faction leaders | ✅ Works |
| Reclaim memory (system GC) | ✅ Works |
| Memory monitor (bar + tooltip, customizable) | ✅ Works |
| Show debug log without dev mode | ✅ Works |
| Hacks: close letters / unlock speed / open settings | ✅ Works |
| Mod settings page | ✅ Works |
| Manual cleanup menu: ModMetaData / Language / DefPackage | ✅ Works *(after fixes #2, #3)* |
| **Auto-cleanup on startup** | ⛔ Not wired up (see Open Decisions) |
| **MuteGC / MuteBL integration** | ⛔ Not wired up (see Open Decisions) |

---

## Bugs found & fixes applied

All three bugs existed in the upstream fork too — they are **not** regressions we
introduced. Two are caused by RimWorld renaming internal fields since the version
the fork was built against; one is a leftover from an incomplete decompile.

### Fix #1 — "Remove battle-log entries" did nothing useful
- **Symptom:** clicking it logged an error block and reported "0 removed".
- **Cause:** the code looked up an internal field `activeEntries` that 1.6 renamed
  to `cachedActiveEntries`. The lookup quietly returned nothing, then blew up when
  used — and the failure was hidden by a catch-all.
- **Fix:** point at the current field name and reset it cleanly; also count the
  total entries removed instead of just the last batch.
- **File:** `Source/RuntimeGC/CleanserUtil.cs`

### Fix #2 — "Clean DefPackage" crashed
- **Symptom:** the menu option threw an error and did nothing.
- **Cause:** same kind of rename — the internal field `defPackages` is now `defs`.
- **Fix:** use the current field name.
- **File:** `Source/Toolbox/DefPackageCleaner.cs`

### Fix #3 — "Clean ModMetaData" crashed
- **Symptom:** the menu option threw "not implemented".
- **Cause:** it was wired to an empty placeholder class (`Toolbox.Toolbox`) left
  behind by the decompile. The real, working cleaner (`ModMetaDataCleaner`) was
  sitting right next to it, unused.
- **Fix:** point the menu at the real cleaner, matching the other two options.
- **Files:** `Source/RuntimeGC/FloatMenuUtil.cs` (the placeholder class is now
  marked unused/safe-to-delete in `Source/Toolbox/Toolbox.cs`).

### Also cleaned up
- Removed a dead reflection field (`selMod`) that pointed at a non-existent member.

---

## Open decisions (to make later)

Two advertised features are **present in code but never actually switched on** —
the fork dropped the startup calls that would activate them. Today their settings
toggles exist and save, but nothing reads them at launch. Leaving them off is
**safe and inert**: vanilla behaviour is untouched. Each needs a deliberate choice
before re-enabling. The exact wiring (with code snippets) is documented inline in
`Source/RuntimeGC/StaticConstructor.cs`.

### Decision 1 — Auto-cleanup on startup
- **What it would do:** automatically run the ModMetaData / Language / DefPackage
  cleaners when the game loads, based on the settings checkboxes.
- **Status:** the underlying cleaners now work (fixes #2, #3), so this is just a
  matter of adding the missing startup call.
- **To consider:** these are aggressive memory hacks meant for the main menu.
  Worth confirming the behaviour is desirable on every launch before enabling, and
  whether the "restart to apply" UI flow still makes sense.
- **Risk if enabled:** low–medium (cleaners are verified, but they mutate game
  databases).

### Decision 2 — MuteGC / MuteBL integration
- **What it would do:** suppress RimWorld's own world-pawn GC and/or battle-log
  recording.
- **Status:** dead code, never called.
- **To consider:** ⚠️ the implementation patches function pointers in memory by
  hand (raw "detour"). That technique is fragile on the current runtime and is the
  one piece most likely to crash if switched on as-is.
- **Recommendation:** do **not** re-enable the hand-written detour. If this feature
  is wanted, reimplement it with Harmony (which the rest of the GTI mods already
  depend on).
- **Risk if enabled as-is:** high.

### Minor / cosmetic
- `Faction.GenerateNewLeader()` is marked obsolete by RimWorld ("will be removed in
  the future"). Still works on 1.6; watch for removal in a future game version.
- Leftover decompiler scaffolding remains (`DirectXmlToObject`, an unused
  copyright string, the `Toolbox.Toolbox` placeholder). Harmless; can be deleted
  during a cleanup pass.

---

## Change log

### 2026-06-17 — Initial GTI fork
- Created `GTI_runtimeGC` from upstream RuntimeGC source; restructured to match the
  GTI build/deploy layout (ModFiles payload + Source, builds & deploys like
  GTI Weapon Wear). Re-branded About; removed the upstream Workshop file id.
- **Fixed:** "Remove battle-log entries" (field rename `activeEntries` →
  `cachedActiveEntries`); now also reports an accurate count.
- **Fixed:** "Clean DefPackage" crash (field rename `defPackages` → `defs`).
- **Fixed:** "Clean ModMetaData" crash (re-pointed from a broken placeholder to the
  real cleaner).
- **Cleaned up:** removed a dead reflection field; documented two unwired features
  (auto-cleanup, mute integration) inline and in this document.
- Verified all features against installed RimWorld 1.6.4633; builds with 0 errors.
