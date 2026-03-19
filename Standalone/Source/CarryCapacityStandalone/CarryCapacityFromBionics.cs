using Verse;
using RimWorld;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;

namespace Vita.CarryCapacityFromBionics
{
    [HarmonyPatch(typeof(MassUtility), nameof(MassUtility.Capacity))]
    public static class CarryCapacityBonus_MassUtility_Capacity_Patch
    {
        public static bool includeStatWorkerResult = true;
        public static MethodInfo SetCarryCapacityInfo = AccessTools.Method(typeof(CarryCapacityBonus_MassUtility_Capacity_Patch), "SetCarryCapacity");
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            foreach (var code in codeInstructions)
            {
                yield return code;
                if (code.opcode == OpCodes.Stloc_0)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0);
                    yield return new CodeInstruction(OpCodes.Call, SetCarryCapacityInfo);
                }
            }
        }

        public static void SetCarryCapacity(Pawn p, ref float __result)
        {
            if (includeStatWorkerResult)
            {
                __result = p.GetStatValue(CarryCapacityDefOf.CarryCapacityBonus);
            }
        }
    }
}
