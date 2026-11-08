# Redesign plan: extra capacity %, simple mode, animals (draft 2026-11-08, awaiting user approval)

## Core idea
Today's per-implant rows ("advanced mode") stay exactly as they are: XML adds flat kg statOffsets.
Everything new runs in C#, as StatParts on the carry stat (same pattern as the missing-parts
penalty: `CarryCapacityMissingParts.dll`, no Harmony, XE settings read once at startup, injected into
`VEF_MassCarryCapacity` and/or `CarryCapacityBonus`). Reason: animals and the extra % are a share of
each pawn's base capacity (body size × 35), which only exists at runtime. XE *does* have patch-time
math (`CreateVariable`, `PatchOperationMath`), but it can't see body size.

## Per-row data
`CCFB_Implant` / `CCFB_ImplantNativeVEF` gain 3 args: `part | efficiency | tier`
(part from a fixed list: Arm, Leg, Shoulder, Clavicle, Femur, Tibia, Foot, Hand, Humerus, Radius,
Finger, Toe, Spine, Pelvis, BothArms, BothLegs, Body, None; tier 2/3/-1/-2). The PatchDef always
writes them to the HediffDef as a DefModExtension (`CCFB.ImplantData`). Source = the Google Sheet:
add a Part column (scripted guess from weight + label, user checks), then script the arg edit
across all patch files. Native-VEF rows get data but simple mode leaves the author's value alone.

## 1. Extra capacity % (smallest; can ship first)
- New top section: one % field (`CCFB_ExtraPercent`, default 0), applies to everyone with capacity
  (humans and pack animals): + pct × base.
- Beside it, a custom XE `SettingContainer` (C#) drawing live examples from the field's current
  value and race body sizes read from DefDatabase: human, a dog, muffalo, thrumbo.

## 2. Simple mode
- `UseSimpleSettings` checkbox at the top. Ticked: per-mod sections hidden (CCFB_Section body wrapped
  in a ToggleableSettings on it), simple panel shown. Missing-parts section shows in both modes.
- Simple panel: part weights % (defaults = sheet weights), Bionic ×2, Archotech ×3, quadruped leg 20%,
  and "Include prosthetic penalties" (off; its rows hidden when off).
- Patch time: in simple mode `CCFB_Implant` skips the statOffset (still writes the data).
- Runtime StatPart: per installed hediff with data: tier > 0 → eff × weight × tier × base;
  tier < 0 (only if penalties on) → −(1 − eff) × weight × base. Rounded with the sheet ladder
  (ADDING_A_MOD.md) → default simple mode on an adult human reproduces the advanced defaults.
- Toggle needs a restart (patch-time switch), like every other setting.

## 3. Animals
- Pack animals only (vanilla or Caravan Packs; non-pack animals have capacity 0 — IL-verified).
- New `CCFB_AnimalImplant` PatchDef: row value is a % of the animal's base, applied by the StatPart
  in both modes. No missing-part penalty for animals.
- First mod: ADSAP2 (`SamBucher.ADogSaidAnimalProsthetics2`); Kenshi Fauna fork = `EL.KenshiFauna`.

## Build order
1. Extra % + examples. 2. Sheet Part column → per-row args in all files. 3. Simple mode StatPart + menu.
4. ADSAP2 animal rows. Each step deployed and tested by the user before the next.

## Decisions pending (recommendation first)
1. Simple mode scales with body size (children get less, as the missing-parts penalty does) — or flat kg.
2. Simple mode rounds each implant with the sheet ladder — or exact values.
3. Simple mode hides per-implant on/off checkboxes — or keeps them as overrides.
4. Extra % applies to pack animals too — or humans only.
