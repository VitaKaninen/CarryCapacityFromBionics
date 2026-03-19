using RimWorld;
using Verse;

namespace Vita.CarryCapacityFromBionics
{
    public class StatWorker_CarryCapacityBonus : StatWorker
    {
        public override float GetBaseValueFor(StatRequest request)
        {
            float __result = base.GetBaseValueFor(request);
            if (request.Thing is Pawn pawn)
            {
                CarryCapacityBonus_MassUtility_Capacity_Patch.includeStatWorkerResult = false;
                __result += MassUtility.Capacity(pawn);
                CarryCapacityBonus_MassUtility_Capacity_Patch.includeStatWorkerResult = true;
            }
            return __result;
        }
    }
}