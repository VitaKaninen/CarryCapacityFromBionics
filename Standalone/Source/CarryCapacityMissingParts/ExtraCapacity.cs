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
            || ModsConfig.IsActive("SamBucher.ADogSaidAnimalProsthetics2_steam")
            || ModsConfig.IsActive("MrKociak.YetAnotherProstheticExpansionModAnimals")
            || ModsConfig.IsActive("MrKociak.YetAnotherProstheticExpansionModAnimals_steam");

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
    }

    public class Setting_CapacityExamples : SettingContainer
    {
        private const float NumberWidth = 64f;
        private const float ArrowWidth = 24f;
        private const float Indent = 16f;
        private const float LineHeight = 22f;
        private const float LabelGap = 12f;

        public string percentKey;
        public string header;
        public string advancedHint;
        public bool hoverAllPackAnimals;
        public List<CapacityExample> examples = new List<CapacityExample>();

        private static List<ThingDef> packAnimals;

        private struct Row
        {
            public string label;
            public float before;
            public float after;
        }

        protected override float CalculateHeight(float width)
        {
            return ((string.IsNullOrEmpty(header) ? 0 : 1) + BuildRows().Count) * LineHeight;
        }

        protected override void DrawSettingContents(Rect inRect)
        {
            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;
            Color color = GUI.color;
            Text.Font = GameFont.Small;
            float pct = MissingPartsBootstrap.GetFloat(percentKey, 0f);
            List<Row> rows = BuildRows(pct);
            float tableWidth = Indent + ArrowWidth + NumberWidth * 2f + LabelGap;
            float labelWidth = rows.Count == 0 ? 0f : rows.Max(r => Text.CalcSize(r.label).x);
            string hint = MissingPartsBootstrap.GetBool("ToggleSectionSimple", true) ? null : advancedHint;
            float headerWidth = string.IsNullOrEmpty(header) ? 0f : Text.CalcSize(header).x;
            if (!string.IsNullOrEmpty(hint))
            {
                headerWidth += LabelGap + Text.CalcSize(hint).x;
            }
            float width = Mathf.Min(inRect.width, DataColumn.Width(Mathf.Max(tableWidth + labelWidth, headerWidth)));
            Rect block = new Rect(inRect.xMax - width, inRect.y, width, inRect.height);
            float y = block.y;
            if (!string.IsNullOrEmpty(header))
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(block.x, y, block.width, LineHeight), header);
                if (!string.IsNullOrEmpty(hint))
                {
                    Text.Anchor = TextAnchor.MiddleRight;
                    GUI.color = new Color(0.6f, 0.6f, 0.6f);
                    Widgets.Label(new Rect(block.x, y, block.width, LineHeight), hint);
                    GUI.color = color;
                }
                y += LineHeight;
            }
            foreach (Row row in rows)
            {
                DrawRow(block, y, row.label, row.before, row.after);
                y += LineHeight;
            }
            GUI.color = color;
            Text.Anchor = anchor;
            Text.Font = font;

            if (hoverAllPackAnimals)
            {
                inRect = block;
                Widgets.DrawHighlightIfMouseover(inRect);
                List<string[]> tip = new List<string[]>();
                foreach (ThingDef def in PackAnimals())
                {
                    float baseKg = BaseKg(def);
                    tip.Add(new[] { def.LabelCap.ToString(), Kg(baseKg), "->", Kg(baseKg + baseKg * pct / 100f) });
                }
                TableTip.DoTip(inRect, 0x3CCFB80, "Every pack animal in this game (adult):", tip);
            }
        }

        private static void DrawRow(Rect inRect, float y, string label, float before, float after)
        {
            float x = inRect.xMax - NumberWidth;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(inRect.x + Indent, y, x - ArrowWidth - NumberWidth - inRect.x - Indent, LineHeight), label);
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = new Color(1f, 0.4f, 0.4f);
            Widgets.Label(new Rect(x, y, NumberWidth, LineHeight), Kg(after));
            Widgets.Label(new Rect(x - ArrowWidth - NumberWidth, y, NumberWidth, LineHeight), Kg(before));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(x - ArrowWidth, y, ArrowWidth, LineHeight), "->");
        }

        private static string Kg(float kg)
        {
            return kg.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
        }

        internal static float BaseKg(ThingDef def)
        {
            float bodySize = def.race.baseBodySize;
            LifeStageAge adult = def.race.lifeStageAges?.LastOrDefault();
            if (adult?.def != null)
            {
                bodySize *= adult.def.bodySizeFactor;
            }
            return bodySize * MassUtility.MassCapacityPerBodySize;
        }

        internal static List<ThingDef> PackAnimals()
        {
            if (packAnimals == null)
            {
                packAnimals = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(d => d.category == ThingCategory.Pawn && d.race != null && d.race.Animal && d.race.packAnimal)
                    .OrderBy(d => d.label)
                    .ToList();
            }
            return packAnimals;
        }

        private List<Row> BuildRows(float pct = 0f)
        {
            List<Row> rows = new List<Row>();
            foreach (CapacityExample example in examples)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(example.race);
                if (def?.race == null || (!def.race.Humanlike && !def.race.packAnimal))
                {
                    continue;
                }
                float baseKg = BaseKg(def);
                rows.Add(new Row
                {
                    label = example.label ?? def.LabelCap.ToString(),
                    before = baseKg,
                    after = Mathf.Max(0f, baseKg + baseKg * pct / 100f),
                });
            }
            return rows;
        }
    }
}
