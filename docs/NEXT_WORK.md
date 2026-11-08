# Next work — handoff (session 2026-11-08)

## User's asks
1. Add support for **A Dog Said... Animal Prosthetics 2** (ADSAP2, `SamBucher.ADogSaidAnimalProsthetics2`,
   ws 3238353862) incl. its EPOE-Forked/MSE2 subparts and Kenshi Fauna parts; skip organs and wings.
   Facts + verified mechanics: `docs/SUPPORTED_MODS.md` (planned section).
2. **Settings redesign** (user's idea):
   - "Use simple settings" checkbox at the top. Simple = editable body-part weights (%) + tech
     multipliers (bionic 2, archotech 3, prosthetic −1, crude −2); penalties below it, off by default
     and hidden when unticked. Unticked = today's per-implant rows.
   - Extra capacity on top: one percentage for everyone, with live kg examples beside it (normal
     human, Dog, Muffalo, Thrumbo — read their body sizes from game data).

## Decisions made (user)
- Animal bonuses = % of the animal's base capacity (body size × 35); support goes through the pack
  flag (vanilla or Caravan Packs), no capacity-granting C#. Quadrupeds: 20% per leg.
- No missing-part penalty for animals.
- The Google Sheet is the source of truth for values. Value rules + rounding: `docs/ADDING_A_MOD.md`
  (penalties = `−(1 − eff) × weight × 35`, user 2026-11-08; smaller penalties wanted).
- Sheet's missing-part kg column need not sum to parents (user): the mod uses the Weight % only.
- **No auto-scan of mod files** (user 2026-11-08): support stays a hand-curated list of mods. Simple
  mode computes from per-row data we write ourselves (body part / efficiency / tech tier, as in the
  sheet), not from the hediff defs at runtime. The scan's "blockers" (no efficiency, torso, II modular
  parts, multi-part, no tech level) are just values we choose once per row; efficiency fallback 1.
- Standing read permission for the RimWorld install and workshop 294100 (in folder `CLAUDE.md`).

## Done this session
- ADSAP2 + Caravan Packs research; game IL verified (`MassUtility.Capacity` returns 0 for non-pack
  animals; `Pawn.BodySize` = life-stage factor × race base size, no cap).
- Sheet audited against every source def's `partEfficiency` (user fixed it), then 143 defaults in
  25 patch files synced to it; Sewn-In Bag → `NeoP_NativeVEF` (author's 15). Deployed.
  **Not yet tested in game.** Shared keys now have per-file defaults (EPOE vs EPOE-Forked, FSF ABE 40
  vs VBE 25 Archotech Spine); the pairs can't run together, so no conflict.
- CLAUDE.md split into `docs/`.
- Scan of all 320 patched hediffs vs source defs (simple-mode blockers): 64 non-replacement implants
  (no efficiency, torso), 6 replacement parts with default efficiency 1.0, 6 II modular parts with no
  fixed body part, torso/waist parts, 5 multi-part parts, 3 with no tech level; VOID + SoS2 not
  installed, so unchecked. Rerun: scratch scripts are gone; re-derive from `docs/ADDING_A_MOD.md` step 8.

## Answered
- Kenshi Fauna 1.6 fork (ws 3535797656) keeps packageId `EL.KenshiFauna`, so ADSAP2's Kenshi parts load.
- `loadAfter` must list every packageId LoadFolders gates on (except `MSE2.Core`, used only to disable):
  NeoP defines parts in its Patches/, which run in load order.

## Next steps
1. User tests the synced defaults in game.
2. Design simple mode + extra-capacity % + animal support (C# StatPart, settings layout, new keys)
   as one plan for user approval before code. Rows gain part/efficiency/tier args; open: whether
   the kg math runs in XML Extensions (no arithmetic op found in `XmlExtensionsReference`) or C#.
3. Then ADSAP2 rows (draft % table was shown to the user; recompute with 20%-per-leg weights).
