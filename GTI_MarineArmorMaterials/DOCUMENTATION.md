# GTI Marine Armor Materials — Documentation

Personal GTI fork of **Marine Armor Materials** by *joe_the_tech*
(Steam Workshop ID `1638630888`, packageId originally `joethetech.marinearmormaterials`).
This fork is rebranded under the GTI namespace (`GTI.MarineArmorMaterials`), has its Steam
`PublishedFileId.txt` removed so it is a local-only mod, and has been updated to run on **RimWorld 1.6**.

---

## What the mod does (in one sentence)

It turns power-armor-class apparel — which vanilla forces you to build out of a **fixed amount of
plasteel** — into **stuff-buildable** items, so you can craft marine/recon/cataphract armor (and various
modded power armors) out of **any metal** (steel, plasteel, gold, uranium, silver, and metals added by
other mods), with the armor's protection scaling to the metal you chose.

This is a **pure-XML PatchOperation mod** — no C# assemblies, no new defs of its own. It only edits
existing apparel defs at load time.

---

## How it is implemented

Every affected armor piece is converted with the same recipe of `PatchOperation`s applied to its
`ThingDef` (or its shared `Name=` base def):

1. **Make it stuff-based** — add `<stuffCategories><li>Metallic</li></stuffCategories>` so the bench shows
   a material picker, and add a `<costStuffCount>` (how many metal bars the suit needs).
2. **Drop the hard-coded material** — remove `<costList><Plasteel>…</Plasteel></costList>` (a few modded
   pieces use `<Steel>` instead — those are removed too).
3. **Make protection scale with the metal** — replace the flat `<ArmorRating_Sharp>` value with a
   `<StuffEffectMultiplierArmor>` multiplier. RimWorld then derives the piece's Sharp/Blunt/Heat armor
   from the chosen stuff's own armor factors × this multiplier.
4. **Remove the flat overrides** — delete `<ArmorRating_Blunt>` and `<ArmorRating_Heat>` from `statBases`
   so those ratings also come from the stuff instead of being fixed.
5. **Reword the description** — most pieces get a description edit swapping "plasteel" wording for
   generic "metal plates".

Because step 3 picks a per-item multiplier, the author balanced each suit so that the **plasteel** build
lands a little above its vanilla strength, while cheaper metals make weaker (but cheaper) armor and exotic
metals make stronger (and far more expensive) armor.

### Conditional loading

- The **vanilla** patch always applies.
- Every **DLC / other-mod** patch is wrapped in `PatchOperationFindMod` and only fires if that mod is
  actually loaded, so the file is inert and harmless when the target mod is absent.

### Version layout (why the 1.6 update was more than a version bump)

The mod ships its patches inside **version-numbered folders** (`1.1/ … 1.6/`) with **no root `Patches/`
folder and no `LoadFolders.xml`**. RimWorld's default behavior then loads only the folder matching the
running game version. The original mod stopped at `1.4/`, so on a 1.6 game **nothing would have loaded**
even with the version bumped. This fork therefore adds `1.5/` and `1.6/` folders (copied from the canonical
`1.4/` set) **in addition** to listing `1.5`/`1.6` in `About.xml`. The vanilla patch targets
(`ApparelArmorPowerBase`, `ApparelArmorReconBase`, their helmets, and the `costList/Plasteel` +
`ArmorRating_*` fields) were verified to still exist unchanged in RimWorld 1.6 Core.

---

## Items affected

### Vanilla (always active) — `Vanilla_Patch.xml`

| Item | Def | Metal cost | Sharp multiplier |
|---|---|---|---|
| Marine (power) armor | `Apparel_PowerArmor` | 100 | 0.93 |
| Marine armor helmet | `Apparel_PowerArmorHelmet` | 40 | 0.93 |
| Recon armor | `Apparel_ArmorRecon` | 80 | 0.806 |
| Recon armor helmet | `Apparel_ArmorHelmetRecon` | 30 | 0.806 |

### Royalty DLC — `RoyaltyPatch.xml` (only if Royalty is loaded)

Full stuff-conversion (cost + scaling):

| Item | Def | Metal cost |
|---|---|---|
| Cataphract armor | `Apparel_ArmorCataphract` | 150 (Sharp ×1.05) |
| Cataphract helmet | `Apparel_ArmorHelmetCataphract` | 50 (Sharp ×1.0) |

Stuff-conversion only (made buildable from any metal; armor scaling left at the prestige base):

- Prestige recon armor `Apparel_ArmorReconPrestige` (100), prestige recon helmet `Apparel_ArmorHelmetReconPrestige` (40)
- Prestige marine armor `Apparel_ArmorMarinePrestige` (120), prestige marine helmet `Apparel_ArmorMarineHelmetPrestige` (50)
- Prestige cataphract armor `Apparel_ArmorCataphractPrestige` (190), prestige cataphract helmet `Apparel_ArmorHelmetCataphractPrestige` (75)

Balance-only (these are already stuff-based in Royalty/Biotech — the patch just re-tunes the Sharp
multiplier and drops the flat Blunt/Heat):

- Locust armor `Apparel_ArmorLocust` (×0.764)
- Grenadier armor `Apparel_ArmorMarineGrenadier` (×0.886)
- Phoenix armor `Apparel_ArmorCataphractPhoenix` (×1.01)

### Vanilla Armour Expanded — `VAEPatch.xml` (only if VAE is loaded)

| Item | Def | Metal cost | Sharp multiplier |
|---|---|---|---|
| Marine boots | `VAE_Footwear_MarineBoots` | 20 | 0.6 |
| Marine gloves | `VAE_Handwear_MarineGloves` | 20 | 0.6 |
| Heavy marine helmet | `VAE_Headgear_HeavyMarineHelmet` | 50 | 0.95 |
| Heavy marine armor | `VAE_Apparel_HeavyMarineArmor` | 160 | 1.06 |

### Rimworld: Altered Carbon — `ACPatch.xml` (only if loaded)

- Protectorate armor `AC_Apparel_ProtectorateArmor` (85, ×0.842)
- Protectorate helmet `AC_Apparel_ProtectorateArmorHelmet` (60, ×0.842)

### Jin-Roh Kerberos Panzer Cop Armor (Continued) — `JKPCPatch.xml` (only if loaded)

- Kerberos armor `JRA_Police_Armor` (120, ×1.01) — removes a **Steel** cost, not plasteel
- Kerberos helmet `JRA_Police_Helmet` (40, ×0.8) — removes a **Steel** cost

### [LB] SectorVII — `LBSC7Patch.xml` (only if loaded)

- Power armor `LBSC7_PowerArmor` (100, ×0.93), power helmet `LBSC7_PowerHelmet` (40, ×0.93)
- Recon armor `LBSC7_ReconArmor` (80, ×0.807), recon helmet `LBSC7_ReconHelmet` (30, ×0.807)

### Rimworld: Spartan Foundry — `RSFPatch.xml` (only if loaded)

The largest patch — 12 armor suits + 6 helmets, all converted to Metallic stuff (most armors cost 100,
helmets 40–75) and additionally stripped of their hard-coded `MarketValue` so value follows the metal:

- Armors: Powered Assault (×1.0), Engineer (×0.9), Explorer (×0.74), Grenadier (×1.3), Commando (×0.9),
  Hazard Operations (×0.95), Diplomat (×0.95), Defender (×0.9), Scout (×0.87), Samurai (×0.7),
  Shock Trooper (×0.85), Recon (×0.9)
- Helmets: Powered Assault (×0.9), Engineer (×0.85), Explorer (×0.73), Grenadier, Shock Trooper (×0.77),
  Recon (×0.65)

---

## Compatibility notes

- **Not save-compatible** with games where you have already built the affected armor — the cost model
  changes from a fixed `costList` to stuff, so start a new colony (or only adopt it before crafting any of
  these pieces).
- **Conflicts** with any other mod that also edits vanilla power/recon armor's cost or armor stats. The
  VAE conflict is resolved by the bundled VAE patch.
- All other (non-overlapping) mods are fine; the `PatchOperationFindMod` guards mean absent target mods
  cost nothing.

## Fork changes vs. the Steam original

- Added `1.5/` and `1.6/` patch folders (cloned from `1.4/`) and listed `1.5`/`1.6` in `About.xml` →
  **functional on RimWorld 1.6**.
- Rebranded name/author/packageId under GTI; removed `PublishedFileId.txt` (local-only mod).
- No gameplay/balance values were changed from the original.
