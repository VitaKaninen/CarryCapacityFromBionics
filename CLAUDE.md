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

## Value methodology (matches the Google Sheet)

`value ≈ partEfficiency × importanceWeight × techMultiplier × 35 kg`, then hand-rounded.

- **Weights**: arm 15%, leg 25%, spine/pelvis/exoskeleton/membrane 20%, foot 10%, hand 5%,
  femur ~13%, humerus/tibia 6%, radius/clavicle 3%, finger/toe 1%.
- **Tech multiplier**: industrial bionic 2, spacer/archotech 3, below-natural prosthetic −1,
  crude (peg leg/hook/wooden) −2, pure utility arms (drill/field/claw) → weight 0% → value 0.
- partEfficiency comes from the hediff's `addedPartProps/partEfficiency` (for implants without
  one, e.g. muscle stimulators, estimate from its capMods).
- **Defaults convention**: positive values default ON; zero-value utility parts and all
  negative (prosthetic-penalty) entries default OFF.
- **Implants that already grant `VEF_MassCarryCapacity` natively** (e.g. several Integrated
  Implants ones) must never go through `CCFB_Implant` (double-dip in VEF games) — they get
  `CCFB_ImplantNativeVEF` instead (override/add, see architecture), defaults = author's values.
- Implants whose native bonus is only vanilla `CarryingCapacity` (the **in-hands** stat) are
  patched normally — that stat never touches mass capacity, so there is no double-dip.

## Roadmap: adding support for a new mod

1. **Read the source mod**: find it under
   `C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\<workshopId>`. Read its
   `About/About.xml` (packageId!) and its `loadFolders.xml` — conditional subfolders there
   (DLC- or mod-gated) must be mirrored by our gating, or we'll patch defs that don't exist.
   Also sanity-check its XML actually parses (one stray bad file under `Defs/` can silently
   kill the whole mod's def loading — happened with Integrated Implants, Dec 2025).
2. **Extract candidate hediffs** (script it): every `HediffDef` touching arms / legs / spine /
   pelvis / hands / feet / tails / extra limbs, with `partEfficiency`, `capMods`
   (Manipulation/Moving), and any **native** `CarryingCapacity`/`VEF_MassCarryCapacity`
   statOffsets. Recipes' `appliedOnFixedBodyParts` tell you the body part. Implants with a
   native `VEF_MassCarryCapacity` must NOT go through `CCFB_Implant` (double-dip in VEF
   games); use `CCFB_ImplantNativeVEF` in a `<ModShortName>_NativeVEF` folder (no VEF gating
   needed), with the author's own values as defaults.
3. **Compute values** with the formula above; present the table (math vs proposed) for approval;
   update the Google Sheet.
4. **Keys**: `<ModShortName><defName>` (strip `Left`/`Right` — L/R pairs share one key, the
   menu dedup-guard shows a single row controlling both hediffs).
5. **Create folder(s)** `1.6/Mods/<ModShortName>[_<Condition>]/Patches/CCFB_<Folder>.xml`
   (filename MUST be unique across all folders — see gotchas). File = one
   `ApplyPatch → CCFB_Section` (label, tag) + one `ApplyPatch → CCFB_Implant` per hediff:
   args `defName | key | label | toggleDefault | kgDefault | sectionTag`.
6. **LoadFolders.xml**: add gated entries. The settings-menu sections are kept in
   ALPHABETICAL order by section label (Core/DLC section first); since the list applies
   bottom-up, the 3rd-party block is sorted reverse-alphabetically — insert the new mod at
   its alphabetical slot, base folder LAST within its folder group (so its rows show first
   in the section). Combine `IfModActiveAll="modA,modB"` and `IfModNotActive="modC"` freely.
   **Row order inside a section** (June 2026 convention, mirrors the Google Sheet):
   negative-value prosthetics first (−1 tech tier before the −2 crude tier), then zero-value
   utility parts, then positives ascending by tech tier; within a tier small parts → large
   (finger, toe, hand, foot, arm, leg, spine, pelvis/torso, whole-body/exoskeleton last);
   variant families ascending by value (e.g. IP's Utility → base → Noble → Unique).
7. **About.xml**: add the mod to the description's supported list and to `loadAfter`.
   If any new **root-level** file/folder is added to the repo, check `.rimignore` (dev files
   must not be uploaded to Steam — folder-wide rule, see parent CLAUDE.md).
8. **Validate by script before testing**: all XML parses; every patched defName exists in the
   source mod's def XML (catches typos AND naming-order surprises like `RBSE_LeftExtra...`);
   no settings-key collisions; patch filenames unique. Then update this file's supported list.
9. **Commit + push** (folder rule), **deploy** with:
   `robocopy <repo> "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\CarryCapacityFromBionics" /MIR /XD .git .claude`
10. **In-game test** (user does this): settings rows appear only when the mod is active; a pawn
    with the implant shows base 35 + value in Gear tab / "Mass carry capacity" stat breakdown;
    re-test with VEF active if time permits. NOTE: thanks to the missing-def guard, absent
    hediffs are skipped *silently* — a clean log does NOT prove the patch landed; check the
    in-game number. The log can be read directly at
    `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`.

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
  Tail 25, Hulkification 50) and `NeoP_NativeVEF` (see below). Replaced the older
  `CCFB_ImplantStandaloneOnly` design (VEF-absent-only catch-up, no override) in June 2026.
- **`CCFB_Section`** — creates a labeled menu section (header + `SplitColumn` tagged
  `CCFB_<section>`). **Idempotent** (guarded by an existence check), so every patch file calls it
  for the section it needs and file/folder ordering doesn't matter.

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

## The Standalone C# half (unchanged by the refactor)

When VEF is active, VEF's own `VEF_MassCarryCapacity` stat machinery applies the bonus — no
assembly needed. Otherwise the `Standalone` folder loads (`IfModNotActive` in LoadFolders):

- `Standalone/Defs/Stats/CarryCapacityBonus.xml` — our `CarryCapacityBonus` StatDef
  (label "Mass carry capacity", `hideAtValue` 35, custom `workerClass`).
- `StatWorker_CarryCapacityBonus.cs` — base value = vanilla `MassUtility.Capacity(pawn)`
  (computed with the transpiler's injection suppressed to avoid recursion).
- `CarryCapacityFromBionics.cs` — Harmony **transpiler** on `MassUtility.Capacity`; overwrites the
  result with `pawn.GetStatValue(CarryCapacityBonus)`; `includeStatWorkerResult` guards re-entry.
- `CarryCapacityDefOf.cs` / `HarmonyPatches.cs` — DefOf handle, `PatchAll()` on startup.

C# targets .NET Framework 4.7.2; solution at `Standalone/Source/CarryCapacityStandalone/`.
Built DLL is committed under `Standalone/Assemblies/`.

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

### Supported third-party mods (folder ↔ packageId)
- FSFABE — `FrozenSnowFox.AdvancedBionicsExpansion`
- FSFVBE — `FrozenSnowFox.VanillaBionicsExpansion`
- RBSE — `rah.rbse` **and** `rah.rbsehc`
- EPOE — `ykara.epoe` **and** `ykara.elstrages.epoe`
- EPOEForked — `vat.epoeforked`
- EPOEForkedRoyalty — `vat.epoeforkedroyalty` (+ Royalty)
- ArchotechExpanded — `teok25.archotechexpanded` **and** `teok25.archotechexpanded.prosthetics`
- AdvancedArchotechArm — `NightKosh.AdvancedArchotechArm`
- VOID — `RH2.Faction.VOID`
- SoS2 — `kentington.saveourship2`
- II (Integrated Implants) — `lts.I`, plus suffixed folders mirroring that mod's own conditional
  loading: `II_NoAEP` (needs Archotech Expanded Prosthetics ABSENT), `II_NoMSE2` (needs Medical
  System Expansion 2 ABSENT), and donor-mod extra-arm folders `II_EPOEForked`,
  `II_EPOEForkedRoyalty`, `II_FSFABE`, `II_FSFABE_Royalty`, `II_RBSE` (each needs lts.I + the
  donor mod (+DLC) + MSE2 absent). All inject into the single "Integrated Implants" section.
  Left/right extra-arm hediffs are separate defs sharing one settings key (menu shows one row).
  Implants the mod buffs natively via VEF_MassCarryCapacity (StrengthEnhancer, SkeletalBracing,
  LTS_ManipulationTail, HulkificationSurgery) are NOT patched normally (double-dip), but go
  through the **native-VEF override** `CCFB_ImplantNativeVEF` in `II_NativeVEF` /
  `II_NativeVEF_Anomaly` (gated lts.I (+Anomaly), no VEF gate) at the author's values 25/25/25/50.
  Fully skipped: ghoul claws (ghouls don't haul/caravan), venom tail (no carrying-relevant caps).
- YAPE (Yet Another Prosthetic Expansion - Core) — `MrKociak.YetAnotherProstheticExpansionModCore`.
  YAPE ships its own copies of SoS2's archotech parts (same defNames `SoSArchotechSpine`/`Skin`);
  `YAPE_NoSoS2` (gated YAPE + SoS2 ABSENT) patches those with the keys **shared** with the SoS2
  section, so exactly one folder ever patches them and one menu row renders.
- MedP (Medieval Prosthetics) — `NunahurAlShamikh.MedP`. Ambroisen tier is eff 1.0
  (natural-equivalent) → 0-value rows, default off.
- Astral (Astraltech Bionic Implants) — `AnthonyTherrien.AstraltechBionics`. Replacement parts
  (arm/leg/spine/pelvis/torso, eff 2.0–2.5) plus eff-1.0 booster implants valued from their
  Moving/Manipulation capMods (muscle-stimulator precedent). Organs/senses skipped.
- NeoP (Neolithic Prosthetics) — `Uga.Booga`, plus suffixed folders mirroring its conditional
  loading: `NeoP_Odyssey` (Odyssey DLC), `NeoP_VAE` (Vanilla Animals Expanded),
  `NeoP_VAE_NoOdyssey` (VAE + Odyssey ABSENT), `NeoP_AlphaAnimals`. Variant defs share keys
  (BeaverTail/O_BeaverTail/AlphaBeaverTail; GorillaArm/OD_GorillaArm/OD_ArmGorilla;
  CharmOfPanda/OD_CharmOfPanda; KangarooLeg/O_KangarooLeg). MuffaloSpine (50) and
  SyntheticMuffaloSpine (75) are native-VEF → `CCFB_ImplantNativeVEF` override in `NeoP_NativeVEF`.
  SewnInBag's native bonus is vanilla CarryingCapacity (in-hands stat, not mass) → patched
  normally; same reasoning applies to any future implant with an in-hands-only native bonus.
- IP (Industrial Prosthetics) — `Mey.Prosthetics`. 46 hediffs → 16 keys: the
  Combat/Labor/Construct/Social/Military variants of a tier share one key (L/R convention).

## Planned future support (per VitaKaninen, 2026-06)
- **Missing body part penalty** (next up): reduce mass carry capacity for missing/damaged
  body parts, HP-scaled, off by default, in a separate always-loaded DLL. Design fully
  settled with VitaKaninen — read `.claude/missing_parts_roadmap.md` (local, gitignored)
  before starting any work on it.
- YAPE - Animals (ws 2808876573) and A Dog Said... Animal Prosthetics 2 (ws 3238353862) —
  deferred animal wave (animal mass capacity = caravan capacity, worth doing; needs a per-leg
  weight convention for quadrupeds first).
- EXCLUDED by decision (2026-06-10, not updated past RimWorld 1.5): Vanilla Archotech
  Implants (ws 2715093425, 1.3), [SMP] Simple Archotech Implants (ws 2462646185, 1.5),
  Cybernetic Organism and Neural Network (ws 2045064990, 1.4). If they ever update to 1.6,
  candidate hediffs + proposed values are in `.claude/new_mods_proposal.md`. Note for VAI:
  it defines `ArchotechSpine` — the same defName Archotech Expanded already patches — so its
  folder would need `IfModNotActive` on both teok25 packageIds.

## Conventions / gotchas
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
  LoadFolders.xml is deliberately listed in reverse (Core last). Correctness never depends on
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
- The mod only ever adds `statOffsets` to the implant's `HediffDef` stage — never touches body
  parts or recipes.
- Historic quirk kept for behavior parity: if a hediff has multiple `stages/li` with
  `statOffsets`, the offset is added to every such stage (only one stage is active at a time).
