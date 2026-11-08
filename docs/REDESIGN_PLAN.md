# Redesign plan: extra capacity %, simple mode, animals (2026-11-08)

## Core idea (revised by user 2026-11-08)
Advanced mode = today's per-implant rows (flat kg statOffsets, XML). Simple mode = the same
defaults scaled by ONE % — no per-row part/efficiency/tier data: those game values only informed
the chosen numbers and are never patched or stored. Extra % and animals are % of base
(body size × 35), so they stay C# StatParts (`CarryCapacityMissingParts.dll`, no Harmony).

## 1. Extra capacity % — BUILT; % math passed in game 2026-11-08 (+3.5 kg at 10%)
- `1.6/ExtraCapacity` (LoadFolders entry right after MissingParts = top menu section).
  Keys `ExtraCapacityHuman`, `ExtraCapacityAnimal` (percent numbers, default 0), master
  `ToggleSectionExtraCapacity` (default **false**, user; C# fallback must match).
- Layout (user): description first, then per group a row (label | narrow % box) with its examples
  right under the box, right-aligned, numbers red (= they update live). Built by replacing the
  section's tagged SplitColumn (tag kept on the Humans row for CCFB_Section's idempotency guard).
- C# in `CarryCapacityMissingParts.dll` (`ExtraCapacity.cs`): `StatPart_ExtraCapacity` adds
  pct × body size × 35 (humanlike non-babies; animals only if `packAnimal`), injected only if a
  value ≠ 0. `Setting_CapacityExamples` (field `percentKey` — NOT `key`, which SettingContainer
  already has; list of `CapacityExample`: label, race, section, implant keys) shows
  adult base + enabled implant kg (read live from that section's settings) → + pct × base.
  Human example: plain adult human only (user dropped the archotech example: the % is of base,
  not a multiplier on implants, so it added nothing; `section`/`implants` support kept). Animals: Husky, Muffalo, Thrumbo — non-pack races are omitted (user), so
  Husky/Thrumbo only show with Caravan Packs. Drawn as aligned rows: label | red before -> red after.
- **Animal part exists only with ADSAP2** (user): `1.6/ExtraCapacity_Animals` (LoadFolders
  `IfModActive` ADSAP2, listed above `1.6/ExtraCapacity` so it applies after it) appends the
  Pack animals row and swaps the description (Text tagged `CCFB_ExtraCapacityText`; the base text
  tells players to install ADSAP2). C# applies `ExtraCapacityAnimal` only if ADSAP2 is active
  (checks the packageId and its `_steam` form; `ModsConfig.IsActive` ignores the suffix).

## 2. Simple mode (default ON, user)
- `UseSimpleSettings` checkbox at the top + `SimpleScale` % (default 100). Ticked: per-mod
  sections hidden (CCFB_Section body wrapped in a ToggleableSettings on it), simple panel shown.
  Missing-parts and Extra sections show in both modes.
- Patch time, `CCFB_Implant`: simple → value = kg default × SimpleScale / 100 (XE `CreateVariable`
  `*`), toggle = the row's default toggle (per-row and section checkboxes ignored); advanced →
  today's `UseSetting` path. Restart needed, like every setting.
- `CCFB_ImplantNativeVEF`: simple → author's value × scale (100% = author's value), replacing the
  native offset with VEF / added as `CarryCapacityBonus` without; advanced → unchanged (box
  replaces the author's value, toggle off leaves it).
- Examples (live, red, like Extra): one row per loaded mod section — a human with that mod's best
  build. Defaults read via `XmlExtensions.SettingsManager.GetDefaultValue(modId, key)` (no
  duplicated numbers); simple shows default × scale, advanced shows the current row values.
  Vanilla: archotech man = 2 Archotech Arm + 2 Archotech Leg + Bionic Spine (35 + 145 = 180 kg).

## 3. Animals
- Pack animals only (vanilla or Caravan Packs; non-pack animals have capacity 0 — IL-verified).
- New `CCFB_AnimalImplant` PatchDef: row value is a % of the animal's base, applied by a StatPart;
  simple mode scales it too. No missing-part penalty for animals.
- First mod: ADSAP2 (`SamBucher.ADogSaidAnimalProsthetics2`); Kenshi Fauna fork = `EL.KenshiFauna`.

## Build order
1. Extra % + examples (done). 2. Simple mode toggle + scale + PatchDef branch. 3. Per-mod examples.
4. ADSAP2 animal rows. Each step deployed and tested by the user before the next.

## Decisions (user 2026-11-08)
1. Simple mode = one scale % over the defaults; default ON.
2. Simple mode hides per-implant and section checkboxes.
3. Extra %: TWO fields (built as `ExtraCapacityHuman` / `ExtraCapacityAnimal`, see section 1).
