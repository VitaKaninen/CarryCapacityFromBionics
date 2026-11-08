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

## 2. Simple mode — BUILT 2026-11-08, awaiting in-game test
- Section "Simple settings" (`1.6/SimpleMode`, LoadFolders below ExtraCapacity = top of menu); its
  master checkbox `ToggleSectionSimple` (default true) IS the simple/advanced switch. Body: text,
  `SimplePenalties` checkbox (default false), `SimpleScale` % (default 100), examples widget.
- Mod sections live inside a ToggleableSettings container (tag `CCFB_AdvancedSections`, key
  `ToggleSectionSimple`, created by the first `CCFB_Section` call) → hidden in simple mode.
  Simple/Extra/MissingParts call `CCFB_SectionAt` with the menu root so they show in both modes.
- Patch time (both implant PatchDefs): simple → `CCFB_SimpleOffset`: default > 0 always applies,
  < 0 only with SimplePenalties, 0 skipped; value = default × SimpleScale / 100
  (`CCFB_ScaledOffset`, two CreateVariables). Section/row checkboxes ignored. Advanced → the old
  section gate → toggle → UseSetting path. Both end in `CCFB_AddOffset` (vefOperation arg:
  Add, or AddOrReplace for native-VEF rows → 100% = author's value).
- Examples (`Setting_SimpleExamples`, `SimpleExamples.cs`): every row PatchDef registers
  key/label/section/default into the widget's `rows` (inside the dedup guard, so once per key).
  Groups in the SimpleMode file: per section a `featured` priority list (first 3 found shown),
  optional `penalties` (first found, shown when SimplePenalties on), `missingLeg` (Vanilla only;
  shown when the Missing body parts master + Leg row are on, value −pct × 35, not scaled).
  Group hidden if no row of its section registered. Hover = whole section's scaled list
  (zeros dropped, penalties only when on, identical lines deduped).

## 3. Animals
- Pack animals only (vanilla or Caravan Packs; non-pack animals have capacity 0 — IL-verified).
- New `CCFB_AnimalImplant` PatchDef: row value is a % of the animal's base, applied by a StatPart;
  simple mode scales it too. No missing-part penalty for animals.
- First mod: ADSAP2 (`SamBucher.ADogSaidAnimalProsthetics2`); Kenshi Fauna fork = `EL.KenshiFauna`.

## Build order
1. Extra % + examples (done). 2. Simple mode + examples (built, untested). 3. ADSAP2 animal rows. Each step deployed and tested by the user before the next.

## Decisions (user 2026-11-08)
1. Simple mode = one scale % over the defaults; default ON.
2. Simple mode hides per-implant and section checkboxes.
3. Extra %: TWO fields (built as `ExtraCapacityHuman` / `ExtraCapacityAnimal`, see section 1).
