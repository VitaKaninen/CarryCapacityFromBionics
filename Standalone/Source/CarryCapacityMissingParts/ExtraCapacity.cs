using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        public static bool AnimalProstheticsActive =>
            ModsConfig.IsActive("SamBucher.ADogSaidAnimalProsthetics2")
            || ModsConfig.IsActive("SamBucher.ADogSaidAnimalProsthetics2_steam");

        public static float humanFrac;
        public static float animalFrac;

        static ExtraCapacityBootstrap()
        {
            humanFrac = MissingPartsBootstrap.GetFloat(HumanKey, 0f) / 100f;
            if (AnimalProstheticsActive)
            {
                animalFrac = MissingPartsBootstrap.GetFloat(AnimalKey, 0f) / 100f;
            }
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
        private const float NumberWidth = 64f;
        private const float ArrowWidth = 24f;
        private const float RowGap = 2f;

        public string percentKey;
        public List<CapacityExample> examples = new List<CapacityExample>();

        private struct Row
        {
            public string label;
            public float before;
            public float after;
        }

        protected override float CalculateHeight(float width)
        {
            GameFont font = Text.Font;
            Text.Font = GameFont.Small;
            float labelWidth = LabelWidth(width);
            float height = 0f;
            foreach (Row row in BuildRows())
            {
                height += RowHeight(row, labelWidth) + RowGap;
            }
            Text.Font = font;
            return height;
        }

        protected override void DrawSettingContents(Rect inRect)
        {
            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;
            Color color = GUI.color;
            Text.Font = GameFont.Small;
            float labelWidth = LabelWidth(inRect.width);
            float y = inRect.y;
            foreach (Row row in BuildRows())
            {
                float h = RowHeight(row, labelWidth);
                float x = inRect.xMax;
                x -= NumberWidth;
                Rect after = new Rect(x, y, NumberWidth, h);
                x -= ArrowWidth;
                Rect arrow = new Rect(x, y, ArrowWidth, h);
                x -= NumberWidth;
                Rect before = new Rect(x, y, NumberWidth, h);
                Rect label = new Rect(inRect.x, y, labelWidth, h);

                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(label, row.label);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(arrow, "->");
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = new Color(1f, 0.4f, 0.4f);
                Widgets.Label(before, Kg(row.before));
                Widgets.Label(after, Kg(row.after));
                GUI.color = color;
                y += h + RowGap;
            }
            Text.Anchor = anchor;
            Text.Font = font;
        }

        private static float LabelWidth(float width)
        {
            return Mathf.Max(0f, width - NumberWidth * 2f - ArrowWidth - 6f);
        }

        private static float RowHeight(Row row, float labelWidth)
        {
            return Mathf.Max(22f, Mathf.Ceil(Text.CalcHeight(row.label, labelWidth)));
        }

        private static string Kg(float kg)
        {
            return kg.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
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

        private List<Row> BuildRows()
        {
            float pct = GetFloat(percentKey);
            List<Row> rows = new List<Row>();
            foreach (CapacityExample example in examples)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(example.race);
                if (def?.race == null || (!def.race.Humanlike && !def.race.packAnimal))
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
                rows.Add(new Row
                {
                    label = example.label ?? def.LabelCap.ToString(),
                    before = beforeKg,
                    after = Mathf.Max(0f, beforeKg + baseKg * pct / 100f),
                });
            }
            return rows;
        }
    }
}
