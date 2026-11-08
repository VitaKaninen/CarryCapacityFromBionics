using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using XmlExtensions.Setting;

namespace Vita.CarryCapacityFromBionics.MissingParts
{
    [StaticConstructorOnStartup]
    public static class ExtraCapacityBootstrap
    {
        public const string HumanKey = "ExtraCapacityHuman";
        public const string AnimalKey = "ExtraCapacityAnimal";

        public static float humanFrac;
        public static float animalFrac;

        static ExtraCapacityBootstrap()
        {
            if (!MissingPartsBootstrap.GetBool("ToggleSectionExtraCapacity", true))
            {
                return;
            }
            humanFrac = MissingPartsBootstrap.GetFloat(HumanKey, 0f) / 100f;
            animalFrac = MissingPartsBootstrap.GetFloat(AnimalKey, 0f) / 100f;
            if (humanFrac == 0f && animalFrac == 0f)
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
            stat.parts.Add(new StatPart_ExtraCapacity { parentStat = stat });
        }
    }

    public class StatPart_ExtraCapacity : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            val += Extra(req, out _);
        }

        public override string ExplanationPart(StatRequest req)
        {
            float extra = Extra(req, out float frac);
            if (extra == 0f)
            {
                return null;
            }
            return "Extra carry capacity (" + frac.ToStringPercent() + "): " + extra.ToStringMassOffset();
        }

        private static float Extra(StatRequest req, out float frac)
        {
            frac = 0f;
            if (!(req.Thing is Pawn pawn) || pawn.RaceProps == null)
            {
                return 0f;
            }
            if (pawn.RaceProps.Humanlike)
            {
                if (pawn.DevelopmentalStage.Baby())
                {
                    return 0f;
                }
                frac = ExtraCapacityBootstrap.humanFrac;
            }
            else if (pawn.RaceProps.Animal && pawn.RaceProps.packAnimal)
            {
                frac = ExtraCapacityBootstrap.animalFrac;
            }
            return frac * pawn.BodySize * MassUtility.MassCapacityPerBodySize;
        }
    }

    public class Setting_CapacityExamples : SettingContainer
    {
        public string percentKey;
        public List<string> races = new List<string>();

        protected override float CalculateHeight(float width)
        {
            GameFont font = Text.Font;
            Text.Font = GameFont.Small;
            float height = Mathf.Ceil(Text.CalcHeight(BuildText(), width));
            Text.Font = font;
            return height;
        }

        protected override void DrawSettingContents(Rect inRect)
        {
            GameFont font = Text.Font;
            Text.Font = GameFont.Small;
            Widgets.Label(inRect, BuildText());
            Text.Font = font;
        }

        private string BuildText()
        {
            float pct = 0f;
            if (XmlExtensions.SettingsManager.TryGetSetting(MissingPartsBootstrap.ModId, percentKey, out string value))
            {
                float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out pct);
            }
            StringBuilder sb = new StringBuilder();
            foreach (string defName in races)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def?.race == null)
                {
                    continue;
                }
                float bodySize = def.race.baseBodySize;
                LifeStageAge adult = def.race.lifeStageAges?.LastOrDefault();
                if (adult?.def != null)
                {
                    bodySize *= adult.def.bodySizeFactor;
                }
                float baseKg = bodySize * MassUtility.MassCapacityPerBodySize;
                float totalKg = baseKg * (1f + pct / 100f);
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }
                sb.Append(def.LabelCap + ": " + baseKg.ToString("0.#") + " kg -> " + totalKg.ToString("0.#") + " kg");
                if (!def.race.Humanlike && !def.race.packAnimal)
                {
                    sb.Append(" (not a pack animal; needs Caravan Packs)");
                }
            }
            return sb.ToString();
        }
    }
}
