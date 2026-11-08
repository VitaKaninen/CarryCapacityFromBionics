using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using Verse;

namespace Vita.CarryCapacityFromBionics.MissingParts
{
    public class AnimalCapacityExtension : DefModExtension
    {
        public float percent;
    }

    [StaticConstructorOnStartup]
    public static class AnimalImplantsBootstrap
    {
        static AnimalImplantsBootstrap()
        {
            if (!DefDatabase<HediffDef>.AllDefsListForReading.Any(d => d.GetModExtension<AnimalCapacityExtension>() != null))
            {
                return;
            }
            InjectStatPart("VEF_MassCarryCapacity");
            InjectStatPart("CarryCapacityBonus");
        }

        private static void InjectStatPart(string statDefName)
        {
            StatDef stat = DefDatabase<StatDef>.GetNamedSilentFail(statDefName);
            if (stat == null)
            {
                return;
            }
            if (stat.parts == null)
            {
                stat.parts = new List<StatPart>();
            }
            stat.parts.Add(new StatPart_AnimalImplants { parentStat = stat });
        }
    }

    public class StatPart_AnimalImplants : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            val += Extra(req, out _, out _);
        }

        public override string ExplanationPart(StatRequest req)
        {
            float extra = Extra(req, out float pct, out bool found);
            if (!found)
            {
                return null;
            }
            return "Animal implants (" + pct.ToString("0.##", CultureInfo.InvariantCulture) + "% of base, replaces the kg values above): " + extra.ToStringMassOffset();
        }

        private float Extra(StatRequest req, out float pct, out bool found)
        {
            pct = 0f;
            found = false;
            if (!(req.Thing is Pawn pawn) || pawn.RaceProps == null || pawn.RaceProps.Humanlike || pawn.health?.hediffSet == null)
            {
                return 0f;
            }
            float flat = 0f;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                AnimalCapacityExtension ext = hediffs[i].def.GetModExtension<AnimalCapacityExtension>();
                if (ext == null)
                {
                    continue;
                }
                found = true;
                pct += ext.percent;
                List<StatModifier> offsets = hediffs[i].CurStage?.statOffsets;
                if (offsets != null)
                {
                    flat += offsets.GetStatOffsetFromList(parentStat);
                }
            }
            return pct / 100f * pawn.BodySize * MassUtility.MassCapacityPerBodySize - flat;
        }
    }
}
