using RimWorld;
using Verse;

namespace Vita.CarryCapacityFromBionics
{
    public class StatWorker_CarryCapacityBonus : StatWorker
    {
        // The vanilla capacity (BodySize x 35) is injected in GetValueUnfinalized, NOT in
        // GetBaseValueFor: StatWorker.GetBaseValueFor is only virtual since RimWorld 1.6, so a
        // GetBaseValueFor override silently never runs on 1.5 (base value stays 0, every pawn
        // shows 0/0 kg). GetValueUnfinalized is virtual in both 1.5 and 1.6.
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            float result = base.GetValueUnfinalized(req, applyPostProcess);
            if (req.Thing is Pawn pawn)
            {
                result += VanillaCapacity(pawn);
            }
            return result;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            string text = base.GetExplanationUnfinalized(req, numberSense);
            if (req.Thing is Pawn pawn)
            {
                string valueStr = VanillaCapacity(pawn).ToStringByStyle(stat.toStringStyle, numberSense);
                if (!stat.formatString.NullOrEmpty())
                {
                    valueStr = string.Format(stat.formatString, valueStr);
                }
                string baseLine = "StatsReport_BaseValue".Translate() + ": " + valueStr;
                text = text.NullOrEmpty() ? baseLine : baseLine + "\n" + text;
            }
            return text;
        }

        // MassUtility.Capacity with the transpiler's injection suppressed, to avoid recursion.
        private static float VanillaCapacity(Pawn pawn)
        {
            CarryCapacityBonus_MassUtility_Capacity_Patch.includeStatWorkerResult = false;
            float capacity = MassUtility.Capacity(pawn);
            CarryCapacityBonus_MassUtility_Capacity_Patch.includeStatWorkerResult = true;
            return capacity;
        }
    }
}
