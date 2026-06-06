# GTI Weapon Wear — what it does

Weapons in vanilla RimWorld never wear out from use — a pistol you've fired for ten years is as
good as new. This mod changes that: **weapons slowly take damage as you use them**, and adds easy
ways to **keep them repaired** so a worn weapon is never a dead end.

## Weapons wear out as you use them

- Every shot fired and every melee swing has a **small chance** to knock a point of condition off
  the weapon. Most uses do nothing — it adds up gradually over a lot of fighting, not all at once.
- **Better quality weapons last longer.** A legendary rifle wears far slower than an awful one.
- A weapon **won't destroy itself** from normal use. Once it's worn down to almost nothing, your
  pawn stops using it (so you get a chance to fix it). A weapon is only truly lost if it's
  destroyed some other way (like an explosion).

You can tune all of this in the mod's options:

- **Weapon wear multiplier** — how fast weapons wear. `1` is normal, `2` is double, `0` turns wear
  off completely. Anything in between works too.
- **Quality influence** — how much weapon quality matters. `0` means quality is ignored (everything
  wears the same), higher values make good weapons last even longer and bad ones wear even faster.

## Repairing your gear

You can repair worn equipment at the workbench that fits it:

| Item | Where to repair it |
|---|---|
| Weapons | Machining table |
| Armor | Smithy |
| Clothing | Tailoring bench |
| Utility gear (shield belts, packs, etc.) | Fabrication bench |

Add the matching "repair" bill, and your crafters will fix damaged items there.

- Repairs happen **gradually** and the item keeps its **exact quality, material, and name** — you're
  fixing the real item, not replacing it. Stop a repair partway and it keeps the progress made.
- A repair costs a **fraction of what the item was originally made from** (steel for a steel gun,
  plasteel for a plasteel sword, cloth for a shirt, and so on). The more damaged it is, the more it
  costs; a lightly worn item is cheap to top up. You set the fraction in the options
  (**repair material cost**, default 25%). Components are never needed.

## Automatic weapon upkeep (no micromanagement)

Just like colonists automatically replace worn-out clothes, your pawns will **automatically repair
the weapon they're carrying** when it drops below a condition you choose:

- Set the threshold in the options (**auto-repair equipped weapons below**, default 50%, set to 0 to
  turn it off). There's nothing to enable per-pawn — it just works in their spare time, and never
  pulls a pawn out of a fight (it pauses while drafted).
- A pawn fixes their **own** carried weapon and keeps it equipped the whole time — no juggling spare
  weapons or unequipping anything.
- **Need it right now?** Select a pawn, right-click a machining table, and choose **"Repair `<weapon>`
  now"** to force an immediate full repair, no matter how worn (or not) the weapon is.

## Always know what a repair needs

Because repair costs depend on the item, the mod tells you what's required:

- Select a damaged weapon or piece of gear and its info shows **`Repair needs: 4x steel (have 0)`** —
  the exact materials and how many you currently have.
- If a pawn wants to auto-repair their weapon but you're out of the material, you'll get a quick
  **heads-up message** in the corner naming the pawn and what's missing.

## Mod options

Everything is tunable in **Options → Mod Settings → GTI Weapon Wear**. There are four sliders, split
into two groups:

**Weapon wear**

- **Wear rate** (default `1x`) — how fast weapons take damage from being used.
  `0` = off (weapons never wear), `1` = default, `2` = wears twice as fast. The menu also shows the
  live per-use chance at your current setting.
- **Quality influence** (default `1x`) — how much a weapon's quality changes its wear rate.
  `0` = quality ignored (everything wears the same), `1` = default, `2` = quality matters twice as
  much. Higher values make great weapons last even longer and bad ones wear out even faster.

**Repairs**

- **Repair material cost** (default `25%`) — what a *full* repair costs, as a share of the materials
  the item was originally built from (components never needed). The real cost scales with damage, so
  a lightly worn item is cheap to top up.
- **Auto-repair equipped weapons below** (default `50%`) — the condition at which pawns start fixing
  their own carried weapon in their spare time. Set to `0` to turn auto-repair off. No per-pawn
  setup, no work type to enable, and it pauses while a pawn is drafted.

## In short

Weapons degrade with use (tunable, quality-aware, never self-destructing), everything is repairable
at the right bench using its own materials, and your pawns keep their personal weapons in shape on
their own — with a manual "repair now" button and clear material readouts whenever you want them.
