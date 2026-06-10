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

## The single most important fact: two parallel branches

The mod ships **two complete copies** of all patches, in top-level `Standalone/` and `VEF/`.
Exactly one branch loads at runtime, decided by `LoadFolders.xml` on whether
**Vanilla Expanded Framework** (`OskarPotocki.VanillaFactionsExpanded.Core`, "VEF") is active:

| | When loaded | Stat the bonus is written to | Needs C# assembly? |
|---|---|---|---|
| `Standalone/` | VEF **not** active (`IfModNotActive`) | `CarryCapacityBonus` (our own StatDef) | **Yes** |
| `VEF/` | VEF **active** (`IfModActive`) | `VEF_MassCarryCapacity` (provided by VEF) | No |

**A given patch file is byte-identical between the two branches except for one line** — the stat tag
injected into `statOffsets`:
- Standalone: `<CarryCapacityBonus>{Key}</CarryCapacityBonus>`
- VEF: `<VEF_MassCarryCapacity>{Key}</VEF_MassCarryCapacity>`

So any edit to a patch's *logic* must be mirrored in both branches. The `Standalone/Defs/SettingsMenuDef.xml`
and `VEF/Defs/SettingsMenuDef.xml` are currently **identical** (verified via diff) — they live in
different load folders only so the right one loads per branch.

## How the bonus is actually applied to the pawn (the Standalone C# half)

`VEF/` relies on VEF's own `VEF_MassCarryCapacity` stat machinery, so it has **no assembly**.
`Standalone/` ships a small assembly (`Standalone/Assemblies/CarryCapacityFromBionics.dll`,
source in `Standalone/Source/CarryCapacityStandalone/`) that makes our custom stat actually affect
carrying capacity:

- `Standalone/Defs/Stats/CarryCapacityBonus.xml` — defines the `CarryCapacityBonus` StatDef
  (label "Mass carry capacity", `hideAtValue` 35, custom `workerClass`).
- `StatWorker_CarryCapacityBonus.cs` — the stat's worker. Its base value = vanilla `MassUtility.Capacity(pawn)`
  (computed with the transpiler's injection suppressed to avoid recursion).
- `CarryCapacityFromBionics.cs` — Harmony **transpiler** on `MassUtility.Capacity`. After the method
  computes its local, it calls `SetCarryCapacity` which (when not re-entrant) overwrites the result with
  `pawn.GetStatValue(CarryCapacityBonus)`. The `includeStatWorkerResult` bool guards the recursion between
  the two. Net effect: the implants' `statOffsets` flow into the `CarryCapacityBonus` stat, which becomes
  the pawn's effective `MassUtility.Capacity`.
- `CarryCapacityDefOf.cs` — `[DefOf]` handle for the StatDef. `HarmonyPatches.cs` — `PatchAll()` on startup.

When editing C#: it targets .NET Framework 4.7.2; solution at
`Standalone/Source/CarryCapacityStandalone/CarryCapacityFromBionics.sln`. The built DLL is committed
under `Standalone/Assemblies/`.

## Patch file structure & the buckets

Every patch lives at `<Branch>/1.6/<Source>/Patches/<DefName>.xml` where `<Source>` is one of:
`Ludeon/Core`, `Ludeon/Royalty`, `Ludeon/Anomaly`, or `Mods/<ModShortName>`.

**Two axes determine a patch's exact shape:**

### Axis 1 — branch (Standalone vs VEF)
Only the injected stat tag differs (see table above).

### Axis 2 — what guards it (this is the "buckets")
There are **two internal patch shapes**. Which one is used depends on whether the implant's def is
guaranteed to exist once its containing folder loads:

**Bucket A — plain `OptionalPatch`** (the common case). Used for Core implants, vanilla **DLC** implants
(Royalty/Anomaly), and most third‑party mod implants. The folder is only loaded when the relevant
DLC/mod is present (handled entirely in `LoadFolders.xml`), so the patch body needs no further guard:

```
Operation XmlExtensions.OptionalPatch  (key=Toggle<Key>, defaultValue bool)
  caseTrue:
    Operation XmlExtensions.UseSetting (key=<Key>, defaultValue number)
      apply:
        1. PatchOperationConditional — ensure HediffDef/stages exists (nomatch → add <stages><li/></stages>)
        2. PatchOperationConditional — ensure stages/li/statOffsets exists (nomatch → add <statOffsets/>)
        3. PatchOperationAdd        — add <StatTag>{<Key>}</StatTag> into statOffsets
```

**Bucket B — wrapped in `PatchOperationFindMod`** (DLC‑guarded). Used when a *third‑party mod's* hediff
only exists if a **DLC** is also active, and the folder can't be DLC‑gated cleanly via `LoadFolders`
(e.g. one mod folder contains both DLC‑dependent and DLC‑independent hediffs). The same A body is nested
inside a `FindMod` check for the DLC:

```
Operation PatchOperationFindMod  (mods: [Royalty] or [Anomaly])
  match: XmlExtensions.OptionalPatch  ... (identical inner body to Bucket A)
```

Known Bucket B cases: `Mods/EPOEForkedRoyalty/*` (needs **Royalty**),
`Mods/FSFABE/FSFAdvBionicRevenantSpine.xml` (needs **Anomaly**).

> Note: vanilla DLC implants in `Ludeon/Royalty` & `Ludeon/Anomaly` are **Bucket A**, not B — their
> DLC is gated by `LoadFolders` (`IfModActive="Ludeon.RimWorld.Royalty"` etc.), so no inner `FindMod`.

### The three "factors" the user mentioned, mapped to mechanics
1. **VEF or not** → which top-level branch folder (Axis 1).
2. **Requires a DLC** → for vanilla DLC content, gated by `LoadFolders IfModActive(DLC)` (Bucket A).
3. **Requires another mod** → gated by `LoadFolders IfModActive(mod)` / `IfModActiveAll(VEF,mod)` (Bucket A).
4. **Requires both a mod AND a DLC** → mod gated by `LoadFolders`, DLC gated by inner `PatchOperationFindMod` (**Bucket B**).

## Settings keys — naming convention (matters for collisions)

- Each implant has two setting keys: `Toggle<Key>` (checkbox bool) and `<Key>` (numeric kg value),
  referenced identically in the patch (`OptionalPatch`/`UseSetting`) and in the settings menu
  (`ToggleableSettings` → `Numeric`).
- The **xpath always targets the real `defName`** (e.g. `AdvancedBionicArm`), but the **setting `<Key>`
  is namespaced with the mod's short name** to avoid clashes when several mods define the same `defName`.
  - RBSE's `AdvancedBionicArm` → key `RBSEAdvancedBionicArm` / `ToggleRBSEAdvancedBionicArm`.
  - EPOE **and** EPOE‑Forked both use `EPOEAdvancedBionicArm` — deliberately **shared** (they're
    alternative versions of the same mod; only one runs at a time, so settings carry over).
  - EPOE‑Forked Royalty drill arm: key `EPOEEPOE_AdvancedDrillArm` (prefix `EPOE` + defName `EPOE_AdvancedDrillArm`).
- In the settings menu, per‑mod sections are shown/hidden with `MayRequire` / `MayRequireAnyOf` on the
  mod packageId; individual DLC items use `MayRequire="Ludeon.RimWorld.Anomaly"` etc.

## LoadFolders.xml — the dispatcher

`LoadFolders.xml` is the source of truth for which folder loads under which mod/DLC combination.
Standalone entries carry `IfModNotActive="OskarPotocki.VanillaFactionsExpanded.Core"`; VEF entries carry
`IfModActive=` (and `IfModActiveAll="VEF,<mod>"` for third‑party). When **adding support for a new mod**:
add a folder under both `Standalone/1.6/Mods/<X>` and `VEF/1.6/Mods/<X>`, add both `LoadFolders` entries,
and add a settings section (gated by `MayRequire`) to **both** `SettingsMenuDef.xml` files.

### Supported third-party mods (folder ↔ packageId, from LoadFolders)
- FSFABE — `FrozenSnowFox.AdvancedBionicsExpansion`
- FSFVBE — `FrozenSnowFox.VanillaBionicsExpansion`
- RBSE — `rah.rbse` **and** `rah.rbsehc` (hardcore edition; both point at the same `RBSE` folder)
- EPOE — `ykara.elstrages.epoe`
- EPOEForked — `vat.epoeforked`
- EPOEForkedRoyalty — `vat.epoeforkedroyalty` (Bucket B, needs Royalty)
- ArchotechExpanded — `teok25.archotechexpanded` **and** `teok25.archotechexpanded.prosthetics`
- AdvancedArchotechArm — `NightKosh.AdvancedArchotechArm`
- VOID — `RH2.Faction.VOID`
- SoS2 — `kentington.saveourship2`

## Conventions / gotchas for editing
- **Mirror every logic change across `Standalone/` and `VEF/`** — only the injected stat tag should differ.
- **Keep both `SettingsMenuDef.xml` files in sync** (currently identical).
- Patch XML is the same boilerplate ~3-operation body everywhere; when scripting bulk edits, the only
  per-file variables are: the `defName` (in xpaths), the setting `<Key>`, the toggle `defaultValue`,
  the numeric `defaultValue`, and (Bucket B only) the `FindMod` DLC.
- Settings changes require a **game restart** to take effect (stated in the menu UI).
- All implant bonuses are added as `statOffsets` on the implant's `HediffDef` stage — the mod never
  touches the body part or recipe defs.
