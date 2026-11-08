# XML Extensions — gotchas for this mod

XE source mirror: `..\XmlExtensionsSource` (decompile the shipped DLL with ilspycmd when it matters;
workshop 2574315206, folders `1.5/`–`1.7/`).

- `XmlExtensions.PatchDef` must be `Abstract="True"` with a `Name` attribute; called via
  `ApplyPatch`'s `<patchName>`.
- Patch-time substitution nesting: in `CCFB_Implant`, `{{key}}` becomes `{<actual key>}` after
  PatchDef substitution, which `UseSetting` then replaces with the setting's value (same pattern
  as the wiki's Mood Matters tutorial).
- Only XmlExtensions operations accept `<xmlDoc>`; that's why the PatchDef uses
  `XmlExtensions.Conditional` / `XmlExtensions.PatchOperationAdd` instead of the vanilla ops.
- C# calling XE `SettingsManager.SetSetting` at **patch time** throws KeyNotFound unless an XE op
  for our modId already ran (`settingsPerMod[modId]` is filled by internal `AddMod`); XE logs it
  only as "Patch operation … failed" plus an empty-looking "[mod - Start of stack trace]" entry
  (the message is inside that entry). `PatchOperation_DecideMode` runs first, so it calls
  `AddMod` via reflection (present in XE 1.5–1.7 DLLs).
- XE `Setting.Numeric` **saves** its value rounded (half-to-even) to `<decimals>` every time the
  menu draws it (`Numeric.cs:92-95`), so `<decimals>` must cover the finest default (kg rows: 2,
  for −2.75 etc.; at 1 they silently became −2.8). A `<percent>true</percent>` field saves the
  typed percent number ("10" = 10%; vanilla's TextFieldPercent fraction is overwritten from the
  text buffer), default min/max ±1e9; RimWorld forces en-US culture, so parse with Invariant.
- **Every number box is the same narrow width** (user): a label-less `Numeric` in the right
  column of a `SplitColumn` with `<split>0.79</split>` (label = right-anchored Text on the left),
  inside a row's 0.5 right column; kg fields add `<padRight>25</padRight>` to line up with
  percent fields (TextFieldPercent reserves 25 px for "%"). XE's `Numeric` is `internal`, so it
  can't be subclassed.
- Vanilla `Widgets.RadioButton` is itself a `ButtonInvisible` that consumes the click; use its
  return value, or a row-wide `ButtonInvisible` drawn after it never sees clicks on the button.
- A focused text field stays focused when the user clicks empty space; `Setting_UnfocusOnClick`
  (0 px, first setting in `SettingsMenu.xml`) calls `UI.UnfocusCurrentControl()` on every
  MouseDown, before any field draws, so a clicked field still takes focus. Must stay first.
- **A changed default reaches only players who never saved.** Every keyed setting's
  `Initialize` → `SetDefaultValue` → `RegisterKey` writes the default into
  `allSettings.dataDict`, which is saved whole; `OptionalPatch`/`UseSetting` do the same at
  patch time. Afterwards the stored value wins. To force a new default on everyone, use a new key.
- XE draws every setting each frame (no culling), so a per-frame hook in any setting runs.
- XE `PatchOperationInsert` (Prepend) reverses multi-node values; `PatchOperationReplace` keeps
  their order.
- `CreateDocument`/`MergeDocument` side-document speed pattern: removed after being wrongly
  blamed for the loadFolders shadowing bug; never re-verified in game, so re-adding it is optional
  and needs an in-game test (XE rewrote `MergeDocument` to in-place `ReplaceWith` in v1.9.2).
