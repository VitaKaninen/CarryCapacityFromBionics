# Next work — handoff (session 2026-11-08)

## User's asks
1. Add support for **A Dog Said... Animal Prosthetics 2** (ADSAP2, `SamBucher.ADogSaidAnimalProsthetics2`,
   ws 3238353862) incl. its EPOE-Forked/MSE2 subparts and Kenshi Fauna parts; skip organs and wings.
   Facts + verified mechanics: `docs/SUPPORTED_MODS.md` (planned section).
2. **Settings redesign** — design as built is in `docs/REDESIGN_PLAN.md` (main box, Easy/Advanced,
   penalty blocks, examples). Superseded early idea: per-part weights + tech multipliers (dropped:
   Easy = one scale % over the defaults).

## Decisions made (user)
- Animal bonuses = % of the animal's base capacity (body size × 35); support goes through the pack
  flag (vanilla or Caravan Packs), no capacity-granting C#. Quadrupeds: 20% per leg.
- No missing-part penalty for animals.
- The Google Sheet is the source of truth for values. Value rules + rounding: `docs/ADDING_A_MOD.md`
  (penalties = `−(1 − eff) × weight × 35`, user 2026-11-08; smaller penalties wanted).
- Sheet's missing-part kg column need not sum to parents (user): the mod uses the Weight % only.
- **No auto-scan of mod files** (user 2026-11-08): support stays a hand-curated list of mods.
  Part efficiency / tech tier only informed the chosen values; they are never stored or patched.
- Standing read permission for the RimWorld install and workshop 294100 (in folder `CLAUDE.md`).

## Done this session
- ADSAP2 + Caravan Packs research; game IL verified (`MassUtility.Capacity` returns 0 for non-pack
  animals; `Pawn.BodySize` = life-stage factor × race base size, no cap).
- Sheet audited against every source def's `partEfficiency` (user fixed it), then 143 defaults in
  25 patch files synced to it; Sewn-In Bag → `NeoP_NativeVEF` (author's 15). Deployed.
  Tested in game by user (peg leg, prosthetic, menu values). Shared keys now have per-file defaults (EPOE vs EPOE-Forked, FSF ABE 40
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

## Session 2 (2026-11-08, handoff at 343k tokens)
Built over 3 rounds, each deployed + pushed (last commit 897d7f5): Easy/Advanced mode (scale %,
patch time), one top box (custom radio, options left / data right, extra % moved in, ExtraCapacity
folder removed), prosthetic-penalty + missing-part global checkboxes with blocks inside sections
(missing parts inside Core Implants), penalty/missing rows default on, MedP penalty-only section,
starting mode from saved values, table tooltips, all pack animals on hover.
**Not yet seen in game:** round 3 (radio widget, option layout, TableTip ImmediateWindow, DecideMode).
**Asked last, unanswered:**
- Add two one-line C# comments? DecideMode: "Picks Easy or Advanced on first run: saved values that
  differ from the defaults mean Advanced." TableTip: "Hover tooltip as a table: name left, numbers
  right-aligned, extra columns when it is too tall."
- May I recycle the stale `1.6/ExtraCapacity` folder in `<RimWorld>/Mods/CarryCapacityFromBionics`
  (not loaded any more, but would be uploaded)? Needs the user's yes (game-folder write).
- About.xml description doesn't mention Easy/Advanced yet (user edits descriptions).

## Next steps
1. User tests round 3 in game; fix what they report. Then the open questions above.
2. Then ADSAP2 rows (draft % table was shown to the user; recompute with 20%-per-leg weights).
