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

## Session 3 (2026-11-08, handoff at ~330k tokens) — settings menu polish, rounds 4–7
User asked (from in-game screenshots), all built, deployed and pushed (last 580f6af), **none of
rounds 4–7 seen in game yet**:
- DecideMode patch failure fixed (XE `AddMod` via reflection); two approved C# comments added.
- Radio button clickable; click-away unfocuses text boxes (`Setting_UnfocusOnClick`).
- Section hide checkboxes removed; each section starts with `Setting_SectionToggle`
  ("Disable all" ticked / "Enable all" unticked, sets every row toggle, no memory — user
  rejected the remembering version). Penalty and missing-part blocks: always visible, line above
  the title, own toggle; their rows default OFF in Advanced; Easy keeps the main-box checkboxes.
- Main box = aligned rows (option left, data right); data blocks share one width
  (`DataColumn.Width`), right-aligned; "(hover for details)" = gray line above the data in Easy,
  right end of "Pack animals:" in Advanced; hover list = pack animals only, no corpses.
- One prosthesis example per mod in Easy when penalties are on; "Disabled" lined up with boxes.
- Round 8: section toggle drawn like the row checkboxes (CheckboxLabeled, 22 px); missing-parts
  toggle moved below the cap row (cap row now its own SplitColumn `CCFB_MissingPartsCap`).
- Stale `1.6/ExtraCapacity` in the game copy recycled (user allowed deletions).
**Asked last, unanswered:** rename the penalty/missing-part toggle keys so every player gets the
new OFF defaults? (XE freezes saved defaults; renaming drops saved values for those rows.)
Buttons offered: "Keep keys as is" / "Rename the keys". The user's own saved file still has those
rows ON.
**Still open:** About.xml description doesn't mention Easy/Advanced (user edits descriptions).

- Round 9: section toggle deliberately offset (+12 x, −4 y; user: lean into it); About.xml got a
  short Settings block. User asked to re-explain the key-rename question (re-explained, unanswered).

## ADSAP2 draft values (shown to user round 9, not approved)
% of the animal's base. Weights: leg/tremor leg 20, spine 20, shoulder (arm) 15, MSE2 forearm
(`Arm`) 10, paw/hoof/foot/RG foot claw 6 (= human foot share of a leg), hand/pincer 5,
toe/finger/claw 1, internal support 2.5 (its parts span 2.5–12.5). Bionic ×2, ladder-rounded:
leg 50, spine 40, arm 35, forearm 25, paw/hoof/foot 15, hand/pincer 12.5, support 6,
toe/finger/claw 2.5. Penalties −(1−eff)×w: leg −4, replacement spine −4, arm −8 (0.5; −3 with
EPOE-Forked's 0.8), forearm −2, wooden paw −2.5, wooden hoof −1.5 (−2.5 EPOE), paw/hoof/foot
−1.25, hand −1, toe/finger/claw −0.2. Vanilla PegLeg/WoodenFoot/WoodenHand/PowerClaw/HookHand
also install on animals and already get the human flat-kg rows (open question to user).

## Next steps
1. User tests rounds 4–9 in game; fix what they report. Then the key-rename question.
2. Then ADSAP2 rows once the table above is approved.
