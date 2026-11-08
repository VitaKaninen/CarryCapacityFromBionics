## Missing body part penalty (C# feature, June 2026)

Optional penalty (OFF by default, master toggle): pawns lose mass carry capacity for missing
body parts and, scaled by lost HP, for damaged ones. Design spec: `.claude/missing_parts_roadmap.md`.

- **`Common/Assemblies/CarryCapacityMissingParts.dll`** — always loads (Common folder). Source:
  `Standalone/Source/CarryCapacityMissingParts/` (SDK-style csproj, net472, build with
  `dotnet build -c Release`; references `Krafs.Rimworld.Ref` + XmlExtensions.dll by absolute Workshop path;
  output goes straight to Common/Assemblies). **No Harmony** — pure StatPart injection.
- `MissingPartsBootstrap` (`[StaticConstructorOnStartup]`): reads XE settings once via
  `XmlExtensions.SettingsManager.TryGetSetting("Vita.CarryCapacityFromBionics", key)`
  (string values; compiled defaults MUST mirror the XML row defaults — XE only stores a key
  after the user saves the menu). The feature master = the section's checkbox
  `ToggleSectionMissingParts` (created by CCFB_Section, default false). If it's on, appends
  `StatPart_MissingBodyParts` to `VEF_MassCarryCapacity` and/or `CarryCapacityBonus`
  (GetNamedSilentFail — whichever exists). Master off = StatPart never injected = provably
  zero behavior change.
- Why StatPart works for both games: VEF and our Standalone assembly both transpile
  `MassUtility.Capacity` to return `GetStatValue(<their stat>)`, and StatParts run in
  `StatWorker.FinalizeValue` — after base + all implant statOffsets, before the minValue clamp.
  (A Capacity postfix was rejected: it would run twice — inside the StatWorker base-value call
  AND the outer call — double-dipping the penalty.)
- Rules implemented in `StatPart_MissingBodyParts`: all values are **percentages of the
  pawn's base capacity** (`BodySize × 35` kg — penalties scale with body size, i.e. children
  lose less in kg; user decision June 2026). Topmost visible missing part only
  (`GetMissingPartsCommonAncestors`, verified: BFS from core, skips subtrees under added parts —
  bionics/prosthetics count as present); damaged attached part contributes
  `pct × base × (1 − HP/maxHP)`; contributions grouped by limb root (highest TABLE ancestor
  or self) and each group capped at the root's value (mangled leg never worse than amputation).
  **Spine/Pelvis are one pair**: kept OUT of the weights map, handled separately — the WORSE
  of the two counts (max, not sum), one explanation line (`Missing spine+pelvis` when both).
  **Total reduction is capped** by the `MissingPartMaxReduction` setting (default 100% of
  base); negative finals are impossible (both stats have `minValue 0` — ours got it for this).
  **Part defs stay in the weights map even at 0** (unticked or zeroed): a limb root at 0
  caps its whole subtree at 0, so "Leg = 0" really disables the entire leg; the StatPart is
  only injected if at least one value is positive.
  Explanation lines (itemized per part + cap lines + "Raised to minimum") appear in the
  stat-breakdown dialog via `ExplanationPart`; that dialog is the only display surface (decided).
- Settings: section "Missing body parts" (`1.6/MissingParts/Patches/CCFB_MissingParts.xml`,
  ungated LoadFolders entry placed BELOW `1.6/Ludeon/Core` so it is the **top menu section**,
  above Core Implants — deliberate exception to the alphabetical rule). First row = floor
  `MissingPartFloor` (default 0 kg, positive, no checkbox); then per-part
  `ToggleMissingPart<DefName>` (default **false** — everything in this section is off out of
  the box) + `MissingPart<DefName>` kg rows via the menu-row-only PatchDef **`CCFB_PartRow`**
  (no hediff patch — all missing parts share one MissingBodyPart hediff def, which is why this
  feature is C#-only; CCFB_PartRow takes a 6th arg = the row tooltip; numeric uses XE
  `percent=true` — the STORED value is the plain percent number, e.g. "25", the field shows
  "25%". Numeric-label gotcha: XE gives the label exactly half the (width − padLeft) box;
  dropping padLeft bought room for "Reduce carry capacity by"). Values are positive percents.
  Defaults (VitaKaninen's June 2026 table; children of each limb sum EXACTLY to their parent,
  and a fully stripped body = exactly 100%): Spine/Pelvis 20 (ONE row/key
  `MissingPartSpinePelvis`), Leg 25 = Femur 15 + Tibia 5 + Foot 5; Foot = 5 × Toe 1;
  Shoulder 15 = Clavicle 5 + Arm 10; Arm = Humerus 2.5 + Radius 2.5 + Hand 5;
  Hand = 5 × Finger 1. First row = `MissingPartMaxReduction` ("Cap reduction to this
  amount", default 100%). NOTE: keys `MissingPartFloor` and the old kg-value semantics are
  RETIRED — never reuse a key when its meaning changes (stale saved values would misparse).
  The section body opens with an explainer Text (inserted before the SplitColumn via
  `PatchOperationInsert`); every row has a tooltip (spine/pelvis explain why they matter
  for non-walking pawns: inventory + caravan weight pool).
