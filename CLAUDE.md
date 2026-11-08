# Carry Capacity from Bionics — Project Overview

RimWorld 1.6 mod. Adds extra pawn **inventory + caravan carrying capacity** when a colonist
has certain implants (bionic arm/leg, spines, prosthetics, etc.). Every implant is individually
**toggleable** and its bonus amount is **adjustable** via an in‑game settings menu, all driven by
**XML Extensions** (`imranfish.xmlextensions`, a hard mod dependency — see `About/About.xml`).

Intended to pair with hauling mods like *Pick Up and Haul* / *While You're Up* that use pawn inventory.

- `packageId`: `Vita.CarryCapacityFromBionics`
- Author: VitaKaninen
- Steam: https://steamcommunity.com/sharedfiles/filedetails/?id=3564369362
- Values spreadsheet: https://docs.google.com/spreadsheets/d/1k_BQcZxK3mnv6ZPR2nTX4FIXxrspoflj-XeAvqf1gKI
- XML Extensions wiki mirror (operation reference): `..\XmlExtensionsReference\pages\`

## What the mod actually changes (game mechanics)

RimWorld has **two separate carrying systems** — keep them straight:

1. **In-hands carrying capacity** (vanilla `CarryingCapacity` stat, default 75): how many items
   of one stack a pawn can carry in hand. Scales natively with **manipulation**, so any
   manipulation-boosting implant raises it in pure vanilla (bionic arm = +12.5% manipulation
   → 75×1.125 ≈ 84). **This mod never touches it.**
2. **Inventory / caravan mass capacity** (`MassUtility.Capacity` = body size × 35 kg; shown in
   the Gear tab; hard-coded in vanilla, not even a stat): the limit for pawn inventory and
   caravan loading. **This is the only thing this mod changes**, via per-hediff `statOffsets`
   (`VEF_MassCarryCapacity` when VEF is active, our own `CarryCapacityBonus` stat otherwise).
   It matters mostly with inventory-hauling mods (Pick Up and Haul / While You're Up).

## Reference docs — load on demand
| Load it when | Doc |
|---|---|
| Adding a mod or implant, or choosing kg values | [`docs/ADDING_A_MOD.md`](docs/ADDING_A_MOD.md) — value formula, weights, step-by-step roadmap |
| Checking which mods/folders are supported, or planned/excluded ones | [`docs/SUPPORTED_MODS.md`](docs/SUPPORTED_MODS.md) |
| Touching the missing-body-part penalty (C#, `CCFB_PartRow`, MissingParts section) | [`docs/MISSING_PARTS.md`](docs/MISSING_PARTS.md) |
| Touching the Standalone C# (stat worker, `MassUtility.Capacity` transpiler) | [`docs/STANDALONE_CSHARP.md`](docs/STANDALONE_CSHARP.md) |
| Starting a session, or resuming animal support / settings redesign | [`docs/NEXT_WORK.md`](docs/NEXT_WORK.md) — handoff, open questions |
| Building simple mode, extra capacity %, or animal support | [`docs/REDESIGN_PLAN.md`](docs/REDESIGN_PLAN.md) |

## Architecture (PatchDef-based, single tree)

> History: the mod used to ship two mirrored patch trees (`Standalone/` and `VEF/`, 252 files,
> one per implant) plus two identical 3001-line settings menus. That was replaced by this
> PatchDef architecture (one declaration per implant). If something looks missing, check git history.

**One PatchDef is the whole mod.** `Common/Defs/PatchDefs.xml` defines:

- **`CCFB_Implant`** — the template applied per implant. Args (in order):
  `defName | settings key | menu label | toggle default | kg default | menu section tag`.
  The whole body is wrapped in an existence check on the HediffDef: if the def is missing
  (source mod renamed it, or its defs failed to load — e.g. Integrated Implants' broken
  stray XML file, Dec 2025), the implant is skipped silently: no menu row, no patch, no
  errors. Once the def exists again it patches normally.
  It does two things:
  1. Injects the implant's **settings-menu row** (checkbox + numeric field) into its section.
  2. If the toggle setting is on (`XmlExtensions.OptionalPatch` on `Toggle<key>`), reads the kg
     value (`UseSetting` on `<key>`) and adds it to the hediff's `stages/li/statOffsets`
     (creating `stages`/`li`/`statOffsets` via `XmlExtensions.Conditional` if absent).
     **The VEF-vs-standalone choice happens here at patch time**: `XmlExtensions.FindMod`
     (packageId) on `OskarPotocki.VanillaFactionsExpanded.Core` —
     active → `<VEF_MassCarryCapacity>`, not active → `<CarryCapacityBonus>` (our own stat).
- **`CCFB_ImplantNativeVEF`** — for implants whose source mod already grants
  `VEF_MassCarryCapacity` natively (a dead stat in non-VEF games). Same menu row as
  `CCFB_Implant`, but the hediff patch differs: with VEF the setting **replaces** the author's
  native offset (`PatchOperationAddOrReplace`) so the player can override it — toggle OFF
  leaves the author's value untouched; without VEF it adds our `CarryCapacityBonus` like
  `CCFB_Implant` would. Defaults are the author's own values, so out of the box everyone gets
  the author's bonus; the folders need no VEF gating and the rows show in every game. Used by
  `II_NativeVEF` / `II_NativeVEF_Anomaly` (Strength Enhancer 25, Skeletal Bracing 25, Claw
  Tail 25, Hulkification 50) and `NeoP_NativeVEF` (see `docs/SUPPORTED_MODS.md`). Replaced the older
  `CCFB_ImplantStandaloneOnly` design (VEF-absent-only catch-up, no override) in June 2026.
- **`CCFB_Section`** — creates a labeled menu section: header row = the section's **master
  checkbox** (key `ToggleSection<tag>`, third arg = its default; true everywhere except
  MissingParts) + title, then a collapsible body (`ToggleableSettings`) holding the
  `SplitColumn` tagged `CCFB_<section>`. Master unticked → section collapses to just the
  title (live, no restart) AND `CCFB_Implant`/`CCFB_ImplantNativeVEF` skip every hediff patch
  in that section (an `OptionalPatch` on the master wraps the per-implant one; restart applies).
  Args: `label | tag | master default`. **Idempotent** (guarded by an existence check), so every
  patch file calls it for the section it needs and file/folder ordering doesn't matter.
  Known edge case (YAPE/SoS2 only; EPOE/EPOE-Forked and FSF ABE/VBE are mutually exclusive
  in game, so their shared keys never meet): deliberately-shared keys render
  their one row in whichever section's file patches first, but each mod's hediff patches are
  gated by its OWN file's section master — unticking the section that shows the row does not
  disable the other mod's bonuses.
  Each section body also ends with a **"Reset this section" button**: CCFB_Section creates it
  with an `XmlExtensions.Action.ResetSettings` whose `keys` list starts with the master key,
  and every row PatchDef appends its `Toggle<key>`/`<key>` pair (inside the dedup guard, so
  shared keys register with the section that shows the row). A **"Reset all settings"** button
  (ResetSettings with no keys = whole mod) lives in `1.6/MenuFooter/` — its LoadFolders entry
  must stay FIRST in the list so it applies last and lands at the bottom of the menu.

`Common/Defs/SettingsMenu.xml` is just a skeleton (title + restart warning). All sections and rows
are injected at patch time by the PatchDefs, **only for folders that actually loaded** — so the
menu needs no `MayRequire` gating at all.

**Per-source patch files** live at `1.6/<Source>/Patches/CCFB_<Folder>.xml`
(`1.6/Ludeon/{Core,Royalty,Anomaly}` and `1.6/Mods/<ModShortName>`; filenames MUST stay unique
across folders — see gotchas). Each file is just one `ApplyPatch → CCFB_Section` call, then one
`ApplyPatch → CCFB_Implant` call per implant.

**Adding an implant** = one `ApplyPatch` block. **Adding a mod** = new folder +
`LoadFolders.xml` entry (+ section label).

### DLC-dependent implants of third-party mods
No more `PatchOperationFindMod` wrappers — a mod's DLC-dependent implants live in a suffixed
folder gated with `IfModActiveAll="<mod>,<DLC>"` in `LoadFolders.xml`, and inject their rows into
the parent mod's menu section:
- `FSFABE_Royalty` (4 implants), `FSFABE_Anomaly` (1), `FSFVBE_Royalty` (2)
- `EPOEForkedRoyalty` (whole mod needs Royalty; has its own section)

## RimWorld 1.5 support (July 2026)

`About.xml` lists 1.5–1.7; the `<v1.5>` LoadFolders block reuses the same `1.6/...` folders
(the folder name is just a path). It is NOT a mirror of `<v1.6>` — 1.5 lacks the
`IfModActiveAll` attribute (see gotchas), so multi-mod folders are gated on the single
section-owning mod and the PatchDefs' HediffDef existence guard enforces the rest;
`NeoP_Odyssey` is omitted (Odyssey can't exist on 1.5). Any `<v1.6>` edit must be translated
into `<v1.5>` accordingly (comment above the block explains the translation).

## Settings keys — naming convention (matters for collisions)

- Each implant: `Toggle<Key>` (checkbox bool) + `<Key>` (numeric kg). Defaults now live in
  exactly one place — the `ApplyPatch` arguments (PatchDef threads them into both the patch
  and the menu row, which previously had to be kept in sync by hand).
- The xpath targets the real `defName`; the **key is prefixed with the mod's short name**
  (e.g. RBSE's `AdvancedBionicArm` → `RBSEAdvancedBionicArm`).
- **Deliberately shared keys** (alternative editions / overlapping mods): EPOE & EPOE‑Forked share
  `EPOE*` keys; FSF ABE & VBE share `FSF*` keys for their common implants. `CCFB_Implant`'s menu
  guard skips a row if any section already shows that key, so nothing renders twice when both
  are active.
- EPOE‑Forked Royalty drill arm: key `EPOEEPOE_AdvancedDrillArm` (prefix `EPOE` + defName).

## LoadFolders.xml — the dispatcher

Source of truth for what loads when. `Common` always loads; `Standalone` loads only without VEF;
each source folder is gated on its mod (`IfModActive`) or mod+DLC (`IfModActiveAll`). Folders
reachable via two packageIds get two entries (RBSE/RBSE‑HC, EPOE old/new, ArchotechExpanded both).

## Conventions / gotchas
- **RimWorld 1.5 vs 1.6 differences that bit us (July 2026):**
  - `IfModActiveAll`/unknown loadFolders attributes are silently IGNORED by 1.5 → the folder
    loads unconditionally (symptom: empty settings sections for inactive mods). 1.5 only knows
    `IfModActive` / `IfModNotActive` (comma list = ANY-of; both attrs on one li = AND).
  - `StatWorker.GetBaseValueFor` is non-virtual in 1.5 (virtual since 1.6) — overrides compile
    fine and are silently dead. Override `GetValueUnfinalized` instead (virtual in both).
  - XML Extensions' 1.5 assembly lacks the `Setting.Text` `<color>` field → red XML error +
    Def Error on load. Use inline Unity rich text (`&lt;color=red&gt;...`) in `<text>` instead.
  - Verify a 1.5 game-API assumption by IL dump: Mono.Cecil script pattern in scratchpad
    (`ildump`) — `strings` on Assembly-CSharp works for attribute names.
- **THE BIG ONE — patch files in different load folders MUST have unique filenames.**
  RimWorld treats `loadFolders` entries as override layers: files at the same mod-relative path
  (e.g. two folders each containing `Patches/CarryCapacity.xml`) shadow each other and only one
  loads — **silently, no error anywhere**. The first refactor build named every per-source file
  `CarryCapacity.xml`; only one of six files ran (menu showed only the EPOE section, other
  implants got no bonus). Hence the `CCFB_<Folder>.xml` naming. The OLD 252-file layout dodged
  this by accident with its unique-per-implant names — though not entirely: RBSE, EPOE and
  EPOE‑Forked shared names like `AdvancedBionicArm.xml`, a latent version of the same bug when
  two of those mods were active together.
- **RimWorld processes `loadFolders` bottom-up** (last entry applies first), so patch-file
  application order — and therefore settings-menu section order — follows the reversed list.
  LoadFolders.xml is deliberately listed in reverse (MissingParts below Core, Common last).
  Correctness never depends on
  this (CCFB_Section is idempotent); only the menu's section order does.
- The `CreateDocument`/`MergeDocument` side-document speed pattern was first blamed for the
  shadowing symptom and removed (see git history). It was exonerated — the patch files never
  ran at all — but it has not been re-verified in-game since, so re-adding it is optional
  future work, to be tested with the diagnostic logging pattern from the debugging session.
  XE source mirror for reference: `..\XmlExtensionsSource` (note: XE rewrote `MergeDocument`
  to in-place `ReplaceWith` in v1.9.2).
- Patch-time substitution nesting: in `CCFB_Implant`, `{{key}}` becomes `{<actual key>}` after
  PatchDef substitution, which `UseSetting` then replaces with the setting's value (same pattern
  as the wiki's Mood Matters tutorial).
- `XmlExtensions.PatchDef` must be `Abstract="True"` with a `Name` attribute; called via
  `ApplyPatch`'s `<patchName>`.
- Only XmlExtensions operations accept `<xmlDoc>`; that's why the PatchDef uses
  `XmlExtensions.Conditional` / `XmlExtensions.PatchOperationAdd` instead of the vanilla ops.
- Settings changes require a **game restart** (stated in the menu UI).
- XE `Setting.Numeric` **saves** its value rounded (half-to-even) to `<decimals>` every time the
  menu draws it (`Numeric.cs:92-95`), so `<decimals>` must cover the finest default (kg rows: 2,
  for −2.75 etc.; at 1 they silently became −2.8). A `<percent>true</percent>` field saves the
  typed percent number ("10" = 10%; vanilla's TextFieldPercent fraction is overwritten from the
  text buffer), default min/max ±1e9; RimWorld forces en-US culture, so parse with Invariant.
  (Decompiled 2026-11-08, XE 1.5 + 1.6 DLLs.)
- The mod only ever adds `statOffsets` to the implant's `HediffDef` stage — never touches body
  parts or recipes.
- Historic quirk kept for behavior parity: if a hediff has multiple `stages/li` with
  `statOffsets`, the offset is added to every such stage (only one stage is active at a time).
