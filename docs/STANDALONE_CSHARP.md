## The Standalone C# half (unchanged by the refactor)

When VEF is active, VEF's own `VEF_MassCarryCapacity` stat machinery applies the bonus — no
assembly needed. Otherwise the `Standalone` folder loads (`IfModNotActive` in LoadFolders):

- `Standalone/Defs/Stats/CarryCapacityBonus.xml` — our `CarryCapacityBonus` StatDef
  (label "Mass carry capacity", `hideAtValue` 35, custom `workerClass`).
- `StatWorker_CarryCapacityBonus.cs` — adds vanilla `MassUtility.Capacity(pawn)` (computed
  with the transpiler's injection suppressed to avoid recursion) in **`GetValueUnfinalized`**,
  NOT `GetBaseValueFor`: that method is only virtual since RimWorld 1.6, so a GetBaseValueFor
  override silently never runs on 1.5 — base stays `defaultBaseValue` 0 and every pawn shows
  0/0 kg with an empty breakdown (July 2026 1.5-support bug). `GetExplanationUnfinalized`
  override prepends the "Base value" line the vanilla explanation no longer produces.
- `CarryCapacityFromBionics.cs` — Harmony **transpiler** on `MassUtility.Capacity`; overwrites the
  result with `pawn.GetStatValue(CarryCapacityBonus)`; `includeStatWorkerResult` guards re-entry.
- `CarryCapacityDefOf.cs` / `HarmonyPatches.cs` — DefOf handle, `PatchAll(typeof(HarmonyPatches).Assembly)` on startup.

C# targets .NET Framework 4.7.2; solution at `Standalone/Source/CarryCapacityStandalone/`
(SDK-style csproj since July 2026, `dotnet build -c Release` outputs straight to
`Standalone/Assemblies/`; references `Krafs.Rimworld.Ref` 1.6.4871 + `Lib.Harmony` 2.4.1 from NuGet,
not the install — one net472 DLL serves 1.5, 1.6 and 1.7; the code only uses members that exist
in all three). Built DLL is committed under `Standalone/Assemblies/`.
