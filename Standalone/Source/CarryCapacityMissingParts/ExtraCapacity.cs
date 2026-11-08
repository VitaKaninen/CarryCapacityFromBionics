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
            if (!MissingPartsBootstrap.GetBool("ToggleSectionExtraCapacity", false))
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

    public class CapacityExample
    {
        public string label;
        public string race;
        public string section;
        public List<string> implants = new List<string>();
    }

    public class Setting_CapacityExamples : SettingContainer
    {
        public string percentKey;
        public List<CapacityExample> examples = new List<CapacityExample>();

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
            TextAnchor anchor = Text.Anchor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(inRect, BuildText());
            Text.Anchor = anchor;
            Text.Font = font;
        }

        private static float GetFloat(string key)
        {
            if (XmlExtensions.SettingsManager.TryGetSetting(MissingPartsBootstrap.ModId, key, out string value)
                && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }
            return 0f;
        }

        private static bool GetBool(string key)
        {
            return XmlExtensions.SettingsManager.TryGetSetting(MissingPartsBootstrap.ModId, key, out string value)
                && bool.TryParse(value, out bool parsed) && parsed;
        }

        private static string Red(float kg)
        {
            return "<color=#ff5050>" + kg.ToString("0.##", CultureInfo.InvariantCulture) + " kg</color>";
        }

        private string BuildText()
        {
            float pct = GetFloat(percentKey);
            StringBuilder sb = new StringBuilder();
            foreach (CapacityExample example in examples)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(example.race);
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
                float implantKg = 0f;
                if (example.section == null || GetBool("ToggleSection" + example.section))
                {
                    foreach (string key in example.implants)
                    {
                        if (GetBool("Toggle" + key))
                        {
                            implantKg += GetFloat(key);
                        }
                    }
                }
                float beforeKg = Mathf.Max(0f, baseKg + implantKg);
                float afterKg = Mathf.Max(0f, beforeKg + baseKg * pct / 100f);
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }
                sb.Append((example.label ?? def.LabelCap.ToString()) + ": " + Red(beforeKg) + " -> " + Red(afterKg));
                if (!def.race.Humanlike && !def.race.packAnimal)
                {
                    sb.Append(" (needs Caravan Packs)");
                }
            }
            return sb.ToString();
        }
    }
}
