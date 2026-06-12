using System;
using System.Globalization;
using UnityEngine;
using Verse;
using XmlExtensions;
using XmlExtensions.Setting;

namespace Vita.CarryCapacityFromBionics.UI
{
    // Settings-menu row with TWO linked numeric fields - percent of an adult's base capacity
    // (35 kg) and the kg equivalent - editing either writes the other. Only the canonical
    // unit (storedUnit) is saved; the other box is a live view, so existing settings keys
    // keep their stored meaning. Used by CCFB_PartRow (storedUnit Percent) and
    // CCFB_Implant/CCFB_ImplantNativeVEF (storedUnit Kg) in Common/Defs/PatchDefs.xml.
    // Lives in this assembly because it is the always-loaded one (Common/Assemblies) that
    // already references XmlExtensions.dll - the menu must render in every game.
    public class DualUnitNumeric : KeyedSettingContainer
    {
        // XML parameters (label, key, defaultValue, tooltip inherited)
        public string storedUnit = "Percent";   // "Percent" or "Kg": which unit {key} stores
        public int decimals = 1;                // rounding of the stored value
        public float min = -1000000000f;
        public float max = 1000000000f;
        public float boxWidth = 80f;

        private const float KgPerPercent = 0.35f;   // adult base capacity 35 kg
        private const string DerivedTip = "Percent of base capacity and the kg equivalent for an adult human (base 35 kg). Edit either field. Actual kg scale with body size - children lose/gain less.";

        private string pctBuf;
        private string kgBuf;
        private float lastStored = float.NaN;

        private bool StoredIsPercent => storedUnit != "Kg";

        protected override bool PreOpen()
        {
            lastStored = float.NaN;     // resync both buffers on first draw
            return true;
        }

        protected override float CalculateHeight(float width)
        {
            return 22f;
        }

        protected override void DrawSettingContents(Rect inRect)
        {
            float stored = GetStored();
            if (float.IsNaN(lastStored) || stored != lastStored)
            {
                // First draw, reset button, or another widget changed the key.
                pctBuf = Fmt(PctOf(stored));
                kgBuf = Fmt(KgOf(stored));
                lastStored = stored;
            }

            // Right-aligned controls: [pct box] % = [kg box] kg  (canonical box first)
            float unitW = 24f;
            float eqW = 16f;
            float gap = 4f;
            float controlsW = 2f * (boxWidth + gap + unitW) + eqW + gap;
            float x = inRect.xMax - controlsW;

            Rect labelRect = new Rect(inRect.x, inRect.y, x - inRect.x - gap, inRect.height);
            if (label != null)
            {
                TextAnchor prev = Verse.Text.Anchor;
                Verse.Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, label);
                Verse.Text.Anchor = prev;
                if (tooltip != null)
                {
                    TooltipHandler.TipRegion(labelRect, tooltip);
                }
            }

            Rect firstBox, secondBox;
            string firstUnit, secondUnit;
            if (StoredIsPercent)
            {
                firstUnit = "%";
                secondUnit = "kg";
            }
            else
            {
                firstUnit = "kg";
                secondUnit = "%";
            }
            firstBox = new Rect(x, inRect.y, boxWidth, inRect.height);
            x = firstBox.xMax + gap;
            Rect firstUnitRect = new Rect(x, inRect.y, unitW, inRect.height);
            x = firstUnitRect.xMax;
            Rect eqRect = new Rect(x, inRect.y, eqW, inRect.height);
            x = eqRect.xMax;
            secondBox = new Rect(x, inRect.y, boxWidth, inRect.height);
            x = secondBox.xMax + gap;
            Rect secondUnitRect = new Rect(x, inRect.y, unitW, inRect.height);

            TextAnchor prevAnchor = Verse.Text.Anchor;
            Verse.Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(firstUnitRect, firstUnit);
            Widgets.Label(secondUnitRect, secondUnit);
            Verse.Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(eqRect, "=");
            Verse.Text.Anchor = prevAnchor;

            Rect pctRect = StoredIsPercent ? firstBox : secondBox;
            Rect kgRect = StoredIsPercent ? secondBox : firstBox;
            Rect controlsRect = new Rect(firstBox.x, inRect.y, secondUnitRect.xMax - firstBox.x, inRect.height);
            TooltipHandler.TipRegion(controlsRect, DerivedTip);

            // Percent box. Compare against the value we displayed: a difference means the user
            // typed here, so write the store and resync only the OTHER buffer (leaving the
            // buffer being typed in untouched).
            float pctShown = Round(PctOf(stored));
            float pf = pctShown;
            Widgets.TextFieldNumeric(pctRect, ref pf, ref pctBuf, PctOf(min), PctOf(max));
            if (!Mathf.Approximately(pf, pctShown))
            {
                stored = Round(StoredIsPercent ? pf : pf * KgPerPercent);
                SetStored(stored);
                kgBuf = Fmt(KgOf(stored));
                lastStored = stored;
            }

            // Kg box, same pattern.
            float kgShown = Round(KgOf(stored));
            float kf = kgShown;
            Widgets.TextFieldNumeric(kgRect, ref kf, ref kgBuf, KgOf(min), KgOf(max));
            if (!Mathf.Approximately(kf, kgShown))
            {
                stored = Round(StoredIsPercent ? kf / KgPerPercent : kf);
                SetStored(stored);
                pctBuf = Fmt(PctOf(stored));
                lastStored = stored;
            }
        }

        private float PctOf(float stored) => StoredIsPercent ? stored : stored / KgPerPercent;

        private float KgOf(float stored) => StoredIsPercent ? stored * KgPerPercent : stored;

        private float Round(float v) => (float)Math.Round(v, decimals + 1);

        private float GetStored()
        {
            if (SettingsManager.TryGetSetting(modId, key, out string value)
                && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }
            if (defaultValue != null
                && float.TryParse(defaultValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float def))
            {
                return def;
            }
            return 0f;
        }

        private void SetStored(float value)
        {
            SettingsManager.SetSetting(modId, key, value.ToString(CultureInfo.InvariantCulture));
        }

        private string Fmt(float v)
        {
            return ((float)Math.Round(v, decimals + 1)).ToString("0." + new string('#', decimals + 1), CultureInfo.InvariantCulture);
        }
    }
}
