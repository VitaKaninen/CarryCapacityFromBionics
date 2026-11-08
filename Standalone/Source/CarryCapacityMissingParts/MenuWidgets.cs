using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using Verse;
using XmlExtensions;
using XmlExtensions.Setting;

namespace Vita.CarryCapacityFromBionics.MissingParts
{
    public class ModeOption
    {
        public string value;
        public string label;
        public string description;
    }

    public class Setting_ModeChoice : KeyedSettingContainer
    {
        private const float RadioSize = 24f;
        private const float TextLeft = 34f;
        private const float OptionGap = 8f;

        public List<ModeOption> options = new List<ModeOption>();

        protected override float CalculateHeight(float width)
        {
            float height = 0f;
            foreach (ModeOption option in options)
            {
                height += OptionHeight(option, width) + OptionGap;
            }
            return height;
        }

        private static float OptionHeight(ModeOption option, float width)
        {
            GameFont font = Text.Font;
            Text.Font = GameFont.Medium;
            float h = Text.CalcHeight(option.label, width - TextLeft);
            Text.Font = GameFont.Small;
            h += Text.CalcHeight(option.description ?? "", width - TextLeft);
            Text.Font = font;
            return h;
        }

        protected override void DrawSettingContents(Rect inRect)
        {
            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;
            Color color = GUI.color;
            Text.Anchor = TextAnchor.UpperLeft;
            string current = SettingsManager.GetSetting(MissingPartsBootstrap.ModId, key) ?? defaultValue;
            float y = inRect.y;
            foreach (ModeOption option in options)
            {
                float h = OptionHeight(option, inRect.width);
                Rect row = new Rect(inRect.x, y, inRect.width, h);
                Widgets.DrawHighlightIfMouseover(row);
                bool chosen = string.Equals(current, option.value, System.StringComparison.OrdinalIgnoreCase);
                Text.Font = GameFont.Medium;
                float labelH = Text.CalcHeight(option.label, inRect.width - TextLeft);
                bool clicked = Widgets.RadioButton(new Vector2(inRect.x + 2f, y + (labelH - RadioSize) / 2f), chosen);
                Widgets.Label(new Rect(inRect.x + TextLeft, y, inRect.width - TextLeft, labelH), option.label);
                Text.Font = GameFont.Small;
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                Widgets.Label(new Rect(inRect.x + TextLeft, y + labelH, inRect.width - TextLeft, h - labelH), option.description ?? "");
                GUI.color = color;
                clicked |= Widgets.ButtonInvisible(row);
                if (clicked && !chosen)
                {
                    SettingsManager.SetSetting(MissingPartsBootstrap.ModId, key, option.value);
                }
                y += h + OptionGap;
            }
            Text.Font = font;
            Text.Anchor = anchor;
        }
    }

    public class Setting_SectionToggle : SettingContainer
    {
        private const float BoxSize = 24f;
        private const float TextLeft = 30f;

        public string memoryKey;
        public List<string> keys = new List<string>();

        private bool registered;

        protected override float CalculateHeight(float width)
        {
            return BoxSize;
        }

        protected override void DrawSettingContents(Rect inRect)
        {
            string modId = MissingPartsBootstrap.ModId;
            if (!registered)
            {
                registered = true;
                if (!SettingsManager.TryGetSetting(modId, memoryKey, out _))
                {
                    SettingsManager.SetSetting(modId, memoryKey, "");
                }
            }
            bool anyOn = keys.Any(k => IsOn(modId, k));
            bool value = anyOn;
            Widgets.Checkbox(new Vector2(inRect.x, inRect.y + (inRect.height - BoxSize) / 2f), ref value);
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new Rect(inRect.x + TextLeft, inRect.y, inRect.width - TextLeft, inRect.height);
            Widgets.Label(labelRect, label);
            Text.Anchor = anchor;
            if (Widgets.ButtonInvisible(labelRect))
            {
                value = !anyOn;
            }
            if (value == anyOn)
            {
                return;
            }
            if (!value)
            {
                SettingsManager.SetSetting(modId, memoryKey, string.Join(",", keys.Where(k => IsOn(modId, k))));
                foreach (string key in keys)
                {
                    SettingsManager.SetSetting(modId, key, "False");
                }
                return;
            }
            SettingsManager.TryGetSetting(modId, memoryKey, out string memory);
            HashSet<string> restore = new HashSet<string>((memory ?? "").Split(',').Where(k => keys.Contains(k)));
            foreach (string key in keys)
            {
                bool on = restore.Count > 0 ? restore.Contains(key) : DefaultOn(modId, key);
                SettingsManager.SetSetting(modId, key, on ? "True" : "False");
            }
        }

        private static bool IsOn(string modId, string key)
        {
            return SettingsManager.TryGetSetting(modId, key, out string stored) && bool.TryParse(stored, out bool on)
                ? on
                : DefaultOn(modId, key);
        }

        private static bool DefaultOn(string modId, string key)
        {
            try
            {
                return !bool.TryParse(SettingsManager.GetDefaultValue(modId, key), out bool on) || on;
            }
            catch (KeyNotFoundException)
            {
                return true;
            }
        }
    }

    public class Setting_UnfocusOnClick : SettingContainer
    {
        public Setting_UnfocusOnClick()
        {
            addDefaultSpacing = false;
        }

        protected override float CalculateHeight(float width)
        {
            return 0f;
        }

        protected override void DrawSettingContents(Rect inRect)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                UI.UnfocusCurrentControl();
            }
        }
    }

    // Picks Easy or Advanced on first run: saved values that differ from the defaults mean Advanced.
    public class PatchOperation_DecideMode : PatchOperation
    {
        private const string ModeKey = "ToggleSectionSimple";

        protected override bool ApplyWorker(XmlDocument xml)
        {
            string modId = MissingPartsBootstrap.ModId;
            typeof(SettingsManager)
                .GetMethod("AddMod", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string) }, null)
                .Invoke(null, new object[] { modId });
            if (SettingsManager.TryGetSetting(modId, ModeKey, out string stored) && bool.TryParse(stored, out bool easy))
            {
                SettingsManager.SetSetting(modId, ModeKey, easy ? "true" : "false");
                return true;
            }
            bool custom;
            try
            {
                custom = HasCustomValues(modId);
            }
            catch (System.Exception e)
            {
                Log.Warning("[Carry Capacity from Bionics] Could not compare saved settings, using Advanced settings: " + e.Message);
                custom = true;
            }
            SettingsManager.SetSetting(modId, ModeKey, custom ? "false" : "true");
            return true;
        }

        private static bool HasCustomValues(string modId)
        {
            ModContentPack mod = LoadedModManager.RunningModsListForReading
                .FirstOrDefault(m => m.PackageId.StartsWith(modId.ToLowerInvariant()));
            if (mod == null)
            {
                return true;
            }
            string root = Path.Combine(mod.RootDir, "1.6");
            foreach (string file in Directory.GetFiles(root, "*.xml", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(Path.GetDirectoryName(file)) != "Patches")
                {
                    continue;
                }
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                foreach (XmlNode op in doc.SelectNodes("//Operation[@Class='XmlExtensions.ApplyPatch']"))
                {
                    string patch = op["patchName"]?.InnerText;
                    List<string> args = new List<string>();
                    XmlNode argNode = op["arguments"];
                    if (argNode == null)
                    {
                        continue;
                    }
                    foreach (XmlNode li in argNode.ChildNodes)
                    {
                        if (li.NodeType == XmlNodeType.Element)
                        {
                            args.Add(li.InnerText);
                        }
                    }
                    if ((patch == "CCFB_Implant" || patch == "CCFB_ImplantNativeVEF") && args.Count >= 5)
                    {
                        if (!float.TryParse(args[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || value < 0f)
                        {
                            continue;
                        }
                        if (DiffersBool(modId, "Toggle" + args[1], args[3]) || DiffersFloat(modId, args[1], value))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool DiffersBool(string modId, string key, string def)
        {
            return SettingsManager.TryGetSetting(modId, key, out string stored)
                && bool.TryParse(stored, out bool s) && bool.TryParse(def, out bool d) && s != d;
        }

        private static bool DiffersFloat(string modId, string key, float def)
        {
            return SettingsManager.TryGetSetting(modId, key, out string stored)
                && float.TryParse(stored, NumberStyles.Float, CultureInfo.InvariantCulture, out float s)
                && Mathf.Abs(s - def) > 0.001f;
        }
    }

    // Hover tooltip as a table: name left, numbers right-aligned, extra columns when it is too tall.
    public static class TableTip
    {
        private const float RowHeight = 20f;
        private const float NameGap = 24f;
        private const float CellGap = 6f;
        private const float GroupGap = 32f;
        private const float Pad = 8f;

        public static void DoTip(Rect hoverRect, int id, string header, List<string[]> rows)
        {
            if (!Mouse.IsOver(hoverRect) || rows.Count == 0)
            {
                return;
            }
            GameFont font = Text.Font;
            Text.Font = GameFont.Small;
            int cells = rows.Max(r => r.Length);
            float[] widths = new float[cells];
            foreach (string[] row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    widths[i] = Mathf.Max(widths[i], Text.CalcSize(row[i]).x);
                }
            }
            float tableWidth = widths.Sum() + (cells > 1 ? NameGap + CellGap * (cells - 2) : 0f);
            float maxHeight = UI.screenHeight * 0.85f;
            float headerWidth = Mathf.Max(260f, tableWidth);
            float headerHeight = string.IsNullOrEmpty(header) ? 0f : Text.CalcHeight(header, headerWidth) + 6f;
            int perGroup = Mathf.Max(1, Mathf.FloorToInt((maxHeight - headerHeight - Pad * 2f) / RowHeight));
            int groups = Mathf.CeilToInt(rows.Count / (float)perGroup);
            perGroup = Mathf.CeilToInt(rows.Count / (float)groups);
            float width = Mathf.Max(headerWidth, groups * tableWidth + (groups - 1) * GroupGap) + Pad * 2f;
            if (groups > 1)
            {
                headerWidth = width - Pad * 2f;
                headerHeight = string.IsNullOrEmpty(header) ? 0f : Text.CalcHeight(header, headerWidth) + 6f;
            }
            float height = headerHeight + perGroup * RowHeight + Pad * 2f;
            Vector2 mouse = UI.MousePositionOnUIInverted;
            float x = mouse.x + 18f;
            if (x + width > UI.screenWidth)
            {
                x = Mathf.Max(0f, mouse.x - width - 18f);
            }
            float y = Mathf.Clamp(mouse.y, 0f, Mathf.Max(0f, UI.screenHeight - height));
            Rect window = new Rect(x, y, width, height);
            Text.Font = font;

            Find.WindowStack.ImmediateWindow(id, window, WindowLayer.Super, delegate
            {
                GameFont f = Text.Font;
                TextAnchor a = Text.Anchor;
                Text.Font = GameFont.Small;
                Rect inner = new Rect(Pad, Pad, width - Pad * 2f, height - Pad * 2f);
                if (headerHeight > 0f)
                {
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(new Rect(inner.x, inner.y, inner.width, headerHeight), header);
                }
                for (int g = 0; g < groups; g++)
                {
                    float gx = inner.x + g * (tableWidth + GroupGap);
                    for (int r = 0; r < perGroup; r++)
                    {
                        int index = g * perGroup + r;
                        if (index >= rows.Count)
                        {
                            break;
                        }
                        string[] row = rows[index];
                        float ry = inner.y + headerHeight + r * RowHeight;
                        float cx = gx;
                        for (int c = 0; c < cells; c++)
                        {
                            string cell = c < row.Length ? row[c] : "";
                            Text.Anchor = c == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                            Widgets.Label(new Rect(cx, ry, widths[c], RowHeight), cell);
                            cx += widths[c] + (c == 0 ? NameGap : CellGap);
                        }
                    }
                }
                Text.Anchor = a;
                Text.Font = f;
            }, true, false, 1f);
        }
    }
}
