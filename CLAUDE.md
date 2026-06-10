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

## Architecture (PatchDef-based, single tree)

> History: the mod used to ship two mirrored patch trees (`Standalone/` and `VEF/`, 252 files,
> one per implant) plus two identical 3001-line settings menus. That was replaced by this
> PatchDef architecture (one declaration per implant). If something looks missing, check git history.

**One PatchDef is the whole mod.** `Common/Defs/PatchDefs.xml` defines:

- **`CCFB_Implant`** — the template applied per implant. Args (in order):
  `defName | settings key | menu label | toggle default | kg default | menu section tag`.
  It does two things:
  1. Injects the implant's **settings-menu row** (checkbox + numeric field) into its section.
  2. If the toggle setting is on (`XmlExtensions.OptionalPatch` on `Toggle<key>`), reads the kg
     value (`UseSetting` on `<key>`) and adds it to the hediff's `stages/li/statOffsets`
     (creating `stages`/`li`/`statOffsets` via `XmlExtensions.Conditional` if absent).
     **The VEF-vs-standalone choice happens here at patch time**: `XmlExtensions.FindMod`
     (packageId) on `OskarPotocki.VanillaFactionsExpanded.Core` —
     active → `<VEF_MassCarryCapacity>`, not active → `<CarryCapacityBonus>` (our own stat).
- **`CCFB_Section`** — creates a labeled menu section (header + `SplitColumn` tagged
  `CCFB_<section>`). **Idempotent** (guarded by an existence check), so every patch file calls it
  for the section it needs and file/folder ordering doesn't matter.

`Common/Defs/SettingsMenu.xml` is just a skeleton (title + restart warning). All sections and rows
are injected at patch time by the PatchDefs, **only for folders that actually loaded** — so the
menu needs no `MayRequire` gating at all.

**Per-source patch files** live at `1.6/<Source>/Patches/CarryCapacity.xml`
(`1.6/Ludeon/{Core,Royalty,Anomaly}` and `1.6/Mods/<ModShortName>`). Each file:
1. `CreateDocument` ×2 — copies our SettingsMenuDef (`CCFB_Menu`) and just the HediffDefs it
   touches (`CCFB_Hediffs`, or-predicate of defNames) into side documents, so the many xpath
   lookups don't scan the whole merged Def database (the XML Extensions speed pattern).
2. One `ApplyPatch → CCFB_Section` call, then one `ApplyPatch → CCFB_Implant` call per implant.
3. `MergeDocument` ×2 — writes changes back.

**Adding an implant** = one `ApplyPatch` block + its defName in that file's `CreateDocument`
xpath. **Adding a mod** = new folder + `LoadFolders.xml` entry (+ section label).

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

## Conventions / gotchas
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
