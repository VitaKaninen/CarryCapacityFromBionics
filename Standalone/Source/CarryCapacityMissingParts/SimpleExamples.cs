using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;
using XmlExtensions.Setting;

namespace Vita.CarryCapacityFromBionics.MissingParts
{
    public class SimpleRow
    {
        public string key;
        public string label;
        public string section;
        public string value;
    }

    public class SimpleGroup
    {
        public string label;
        public string section;
        public List<string> featured = new List<string>();
        public List<string> penalties = new List<string>();
        public bool missingLeg;
    }

    public class Setting_SimpleExamples : SettingContainer
    {
        private const int FeaturedCount = 3;
        private const float NumberWidth = 72f;
        private const float Indent = 16f;
        private const float LineHeight = 22f;
        private const float GroupGap = 8f;
        private const float LabelGap = 12f;
        private const float HumanBaseKg = 35f;

        public string header;
        public List<SimpleRow> rows = new List<SimpleRow>();
        public List<SimpleGroup> groups = new List<SimpleGroup>();

        private struct Line
        {
            public string label;
            public float kg;
        }

        private class Block
        {
            public string title;
            public List<Line> lines = new List<Line>();
            public string tipHeader;
            public List<string[]> tipRows = new List<string[]>();
        }

        protected override float CalculateHeight(float width)
        {
            float height = string.IsNullOrEmpty(header) ? 0f : LineHeight;
            foreach (Block block in BuildBlocks())
            {
                height += BlockHeight(block) + GroupGap;
            }
            return height;
        }

        protected override void DrawSettingContents(Rect inRect)
        {
            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;
            Color color = GUI.color;
            Text.Font = GameFont.Small;
            List<Block> blocks = BuildBlocks();
            float width = string.IsNullOrEmpty(header) ? 0f : Text.CalcSize(header).x;
            foreach (Block block in blocks)
            {
                width = Mathf.Max(width, Text.CalcSize(block.title + ":").x);
                foreach (Line line in block.lines)
                {
                    width = Mathf.Max(width, Indent + Text.CalcSize(line.label).x + LabelGap + NumberWidth);
                }
            }
            inRect.xMin = inRect.xMax - Mathf.Min(inRect.width, width);
            float y = inRect.y;
            if (!string.IsNullOrEmpty(header))
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(inRect.x, y, inRect.width, LineHeight), header);
                y += LineHeight;
            }
            int index = 0;
            foreach (Block block in blocks)
            {
                Rect blockRect = new Rect(inRect.x, y, inRect.width, BlockHeight(block));
                Widgets.DrawHighlightIfMouseover(blockRect);
                TableTip.DoTip(blockRect, 0x3CCFB00 + index++, block.tipHeader, block.tipRows);

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(inRect.x, y, inRect.width, LineHeight), block.title + ":");
                y += LineHeight;
                foreach (Line line in block.lines)
                {
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(new Rect(inRect.x + Indent, y, inRect.width - Indent - NumberWidth, LineHeight), line.label);
                    Text.Anchor = TextAnchor.MiddleRight;
                    GUI.color = new Color(1f, 0.4f, 0.4f);
                    Widgets.Label(new Rect(inRect.xMax - NumberWidth, y, NumberWidth, LineHeight), Kg(line.kg));
                    GUI.color = color;
                    y += LineHeight;
                }
                y += GroupGap;
            }
            Text.Anchor = anchor;
            Text.Font = font;
        }

        private static float BlockHeight(Block block)
        {
            return LineHeight * (1 + block.lines.Count);
        }

        private static string Kg(float kg)
        {
            return kg.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
        }

        private static float Parse(string value)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : 0f;
        }

        private SimpleRow FindRow(string key)
        {
            foreach (SimpleRow row in rows)
            {
                if (row.key == key)
                {
                    return row;
                }
            }
            return null;
        }

        private List<Block> BuildBlocks()
        {
            float scale = MissingPartsBootstrap.GetFloat("SimpleScale", 100f) / 100f;
            bool penalties = MissingPartsBootstrap.GetBool("SimplePenalties", false);
            bool missingLeg = MissingPartsBootstrap.GetBool("ToggleSectionMissingParts", false);
            float missingLegKg = -MissingPartsBootstrap.DefaultPct("Leg") / 100f * HumanBaseKg;

            List<Block> blocks = new List<Block>();
            foreach (SimpleGroup group in groups)
            {
                if (!rows.Any(r => r.section == group.section))
                {
                    continue;
                }
                Block block = new Block { title = group.label };
                foreach (string key in group.featured)
                {
                    if (block.lines.Count >= FeaturedCount)
                    {
                        break;
                    }
                    SimpleRow row = FindRow(key);
                    if (row != null)
                    {
                        block.lines.Add(new Line { label = row.label, kg = Parse(row.value) * scale });
                    }
                }
                if (penalties)
                {
                    foreach (string key in group.penalties)
                    {
                        SimpleRow row = FindRow(key);
                        if (row != null)
                        {
                            block.lines.Add(new Line { label = row.label, kg = Parse(row.value) * scale });
                            break;
                        }
                    }
                }
                Line? missing = null;
                if (group.missingLeg && missingLeg)
                {
                    missing = new Line { label = "Missing leg", kg = missingLegKg };
                    block.lines.Add(missing.Value);
                }
                if (block.lines.Count == 0)
                {
                    continue;
                }

                block.tipHeader = "Choose Advanced settings to change any of these.\n\n" + group.label + ":";
                HashSet<string> seen = new HashSet<string>();
                foreach (SimpleRow row in rows)
                {
                    if (row.section != group.section)
                    {
                        continue;
                    }
                    float kg = Parse(row.value);
                    if (kg == 0f || (kg < 0f && !penalties))
                    {
                        continue;
                    }
                    string text = Kg(kg * scale);
                    if (seen.Add(row.label + "|" + text))
                    {
                        block.tipRows.Add(new[] { row.label, text });
                    }
                }
                if (missing.HasValue)
                {
                    block.tipRows.Add(new[] { missing.Value.label, Kg(missing.Value.kg) });
                }
                blocks.Add(block);
            }
            return blocks;
        }
    }
}
