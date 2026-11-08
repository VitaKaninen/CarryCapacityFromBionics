# Redesign plan: extra capacity %, simple mode, animals (2026-11-08)

## Core idea (revised by user 2026-11-08)
Advanced mode = today's per-implant rows (flat kg statOffsets, XML). Simple mode = the same
defaults scaled by ONE % — no per-row part/efficiency/tier data: those game values only informed
the chosen numbers and are never patched or stored. Extra % and animals are % of base
(body size × 35), so they stay C# StatParts (`CarryCapacityMissingParts.dll`, no Harmony).

## 1+2. Main box: mode, options, extra % — built 2026-11-08 (layout v2, awaiting in-game test)
`1.6/SimpleMode/Patches/CCFB_SimpleMode.xml` (LoadFolders lowest = applies first = top of menu)
adds one Section box (tag `CCFB_MainBox`): title, `Setting_ModeChoice` on `ToggleSectionSimple`
(our C# radio: button left, Medium label, gray description below — user; XE RadioButtons puts
the button on the right; values "true" = Easy / "false" = Advanced), then a SplitColumn — left =
options, right = data (user):
- Left: `SimpleScale` % (Easy only), `SimplePenalties`, `ToggleSectionMissingParts`,
  `ExtraCapacityHuman` % (+ `ExtraCapacityAnimal`). Each option = Group: SplitColumn 0.79 (label
  left | control right; checkboxes `padRight 25` to line up with % boxes) + a Tiny gray one-line
  explanation (inline rich-text color, 1.5-safe). One "Reset these settings" button resets the
  box incl. the mode → Easy (user). The footer "Reset all settings" shows only in Advanced (user).
- Right: `Setting_SimpleExamples` (Easy only, header "(hover for details)"), then
  `Setting_CapacityExamples` (fields `header`, `percentKey`, `hoverAllPackAnimals`; label left,
  numbers right) — Colonist; with ADSAP2 Husky/Muffalo/Thrumbo + hover table of every pack
  animal in DefDatabase (modded included, user).
- Hover tooltips are `TableTip` (MenuWidgets.cs): an ImmediateWindow table — name left, numbers
  right-aligned after a gap, wrapping into side-by-side column groups when taller than 85% of
  the screen (user: multiple columns).
- **Starting mode** (user): `PatchOperation_DecideMode`, first op of the SimpleMode file (runs at
  patch time before any row; XE loads saved settings in its Mod constructor, earlier). Saved mode
  "True"/"False" is normalised to lowercase (radio compares strings). No saved mode → reads our
  `1.6/**/Patches/*.xml` ApplyPatch args: any saved non-negative row toggle/value or section
  master differing from its default → Advanced, else Easy; exception → Advanced. Penalty rows
  and missing-part rows are ignored (their defaults changed 2026-11-08).
- Extra %: no on/off (user): 0 = off; old key `ToggleSectionExtraCapacity` retired (C# ignores it).
  `StatPart_ExtraCapacity` adds pct × body size × 35 (humanlike non-babies; animals only if
  `packAnimal`), injected only if a value ≠ 0. `Setting_CapacityExamples` field is `percentKey`
  (NOT `key`, which SettingContainer already has); non-pack races are omitted (user).
- **ADSAP2 only** (user): `1.6/ExtraCapacity_Animals` (LoadFolders `IfModActive` ADSAP2, above
  SimpleMode so it applies after) inserts the pack-animals option after `CCFB_ExtraHumanRow`, the
  animal examples after `CCFB_ExtraHumanExamples`, and swaps the humans explanation (which
  otherwise mentions ADSAP2). C# applies `ExtraCapacityAnimal` only if
  ADSAP2 is active (packageId or `_steam` form).

### Easy vs advanced at patch time (`CCFB_ImplantCore` → `CCFB_ModeOffset`)
- Negative rows (prosthetic penalties) apply only with `SimplePenalties`, in both modes; they
  default ON (gated by that box instead, user). Their menu rows go into a "Prosthetic penalties"
  block (ToggleableSettings on SimplePenalties, inserted after the section's `CCFB_{section}`
  SplitColumn on the first negative row, rows tagged `CCFB_{section}_Penalties`). A mod whose rows
  are all penalties (MedP) uses `CCFB_PenaltyOnlySection`: whole section hidden without penalties.
- Easy: default ≠ 0 → default × SimpleScale / 100 (`CCFB_ScaledOffset`); section/row checkboxes
  ignored. Advanced: section gate → row toggle → UseSetting. Both end in `CCFB_AddOffset`
  (vefOperation: Add, or AddOrReplace for native-VEF rows → 100% = author's value).
- Mod sections sit in a ToggleableSettings on ToggleSectionSimple (tag `CCFB_AdvancedSections`,
  `CCFB_AdvancedContainer`) → hidden in Easy mode.

### Missing body parts (user 2026-11-08)
- Switched by the main box checkbox. Easy: every part at its compiled default %, cap 100%.
  Advanced: rows live in a "Missing body part penalties" block inside Core Implants (inserted
  after `CCFB_Core` by the MissingParts file, which creates the Core section itself), per-part
  rows default ON; Core section master off = no missing-part penalty either.

### Examples (`Setting_SimpleExamples`, `SimpleExamples.cs`)
`CCFB_RowMenu` registers key/label/section/default into the widget's `rows` (inside the dedup
guard, once per key). Groups in the SimpleMode file: per section a `featured` priority list (first
3 found), optional `penalties` (first found, shown with SimplePenalties), `missingLeg` (Vanilla
only; shown with ToggleSectionMissingParts, −default leg % × 35, not scaled). Group hidden if
nothing of its section registered. Hover = whole section's scaled list (zeros dropped, penalties
only when on, identical lines deduped).

## 3. Animals
- Pack animals only (vanilla or Caravan Packs; non-pack animals have capacity 0 — IL-verified).
- New `CCFB_AnimalImplant` PatchDef: row value is a % of the animal's base, applied by a StatPart;
  simple mode scales it too. No missing-part penalty for animals.
- First mod: ADSAP2 (`SamBucher.ADogSaidAnimalProsthetics2`); Kenshi Fauna fork = `EL.KenshiFauna`.

## Build order
1. Extra % + examples (done). 2. Main box, easy/advanced, penalty blocks (built, untested). 3. ADSAP2 animal rows. Each step deployed and tested by the user before the next.

## Decisions (user 2026-11-08)
1. Simple mode = one scale % over the defaults; default ON.
2. Easy mode hides per-implant and section checkboxes; options left, data right; one top box.
3. Extra %: TWO fields (built as `ExtraCapacityHuman` / `ExtraCapacityAnimal`, see section 1).
