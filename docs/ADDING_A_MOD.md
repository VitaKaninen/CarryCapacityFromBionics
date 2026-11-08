# Adding support for a new mod
## Value methodology (matches the Google Sheet)

`value ≈ partEfficiency × importanceWeight × techMultiplier × 35 kg`, then hand-rounded.

- **Weights** (the Google Sheet is the source; same table as the missing-parts penalty): arm 15%,
  leg 25%, spine/pelvis/exoskeleton/membrane 20%, femur 12.5%, foot 7.5%, tibia 5%, clavicle 5%,
  hand 5%, humerus/radius 2.5%, finger/toe 1%. Quadrupeds: 20% per leg (user 2026-11-08).
  Mod defaults were re-synced to the sheet 2026-11-08 (feet had used 10%, bones the old weights).
- **Tech multiplier**: industrial bionic 2, spacer/archotech 3, below-natural prosthetic −1,
  crude (peg leg/hook/wooden) −2, pure utility arms (drill/field/claw) → weight 0% → value 0.
- partEfficiency comes from the hediff's `addedPartProps/partEfficiency` (for implants without
  one, e.g. muscle stimulators, estimate from its capMods).
- **Defaults convention**: positive values default ON; zero-value utility parts and all
  negative (prosthetic-penalty) entries default OFF.
- **Implants that already grant `VEF_MassCarryCapacity` natively** (e.g. several Integrated
  Implants ones) must never go through `CCFB_Implant` (double-dip in VEF games) — they get
  `CCFB_ImplantNativeVEF` instead (override/add, see CLAUDE.md architecture), defaults = author's values.
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
   (filename MUST be unique across all folders — see CLAUDE.md gotchas). File = one
   `ApplyPatch → CCFB_Section` (label, tag) + one `ApplyPatch → CCFB_Implant` per hediff:
   args `defName | key | label | toggleDefault | kgDefault | sectionTag`.
6. **LoadFolders.xml**: add gated entries. The settings-menu sections are kept in
   ALPHABETICAL order by section label ("Missing body parts" first by design, then the
   Core/DLC sections); since the list applies
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
   no settings-key collisions; patch filenames unique. Then update `docs/SUPPORTED_MODS.md`.
9. **Commit + push** (folder rule), **deploy** with the parent folder's
   `python -I Tools/deploy/deploy.py . CarryCapacityFromBionics` (never `/MIR`: it deletes).
10. **In-game test** (user does this): settings rows appear only when the mod is active; a pawn
    with the implant shows base 35 + value in Gear tab / "Mass carry capacity" stat breakdown;
    re-test with VEF active if time permits. NOTE: thanks to the missing-def guard, absent
    hediffs are skipped *silently* — a clean log does NOT prove the patch landed; check the
    in-game number. Log location: see the parent `CLAUDE.md` (it moved in 1.7).
