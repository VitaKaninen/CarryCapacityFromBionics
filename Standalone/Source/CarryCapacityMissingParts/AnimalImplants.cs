using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using XmlExtensions;
using XmlExtensions.Setting;

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

    public class Setting_AnimalPicker : SettingContainer
    {
        public const string Key = "ADSAPExampleAnimal";
        public const string DefaultAnimal = "Muffalo";
        private const float RowHeight = 28f;
        private const float ButtonWidth = 240f;
        private const float LabelGap = 8f;

        public string label = "Example animal:";

        public static ThingDef Selected()
        {
            ThingDef def = null;
            if (SettingsManager.TryGetSetting(MissingPartsBootstrap.ModId, Key, out string stored))
            {
                def = DefDatabase<ThingDef>.GetNamedSilentFail(stored);
            }
            def = def ?? DefDatabase<ThingDef>.GetNamedSilentFail(DefaultAnimal);
            return def?.race != null && def.race.packAnimal ? def : Setting_CapacityExamples.PackAnimals().FirstOrDefault();
        }

        protected override float CalculateHeight(float width)
        {
            return RowHeight;
        }

        protected override void DrawSettingContents(Rect inRect)
        {
            ThingDef selected = Selected();
            string text = selected == null ? "-" : selected.LabelCap + " (" + Kg(Setting_CapacityExamples.BaseKg(selected)) + ")";
            Rect button = new Rect(inRect.xMax - ButtonWidth, inRect.y, ButtonWidth, inRect.height);
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(inRect.x, inRect.y, button.x - LabelGap - inRect.x, inRect.height), label);
            Text.Anchor = anchor;
            if (Widgets.ButtonText(button, text))
            {
                List<FloatMenuOption> options = Setting_CapacityExamples.PackAnimals()
                    .Select(d => new FloatMenuOption(d.LabelCap + " (" + Kg(Setting_CapacityExamples.BaseKg(d)) + ")",
                        () => SettingsManager.SetSetting(MissingPartsBootstrap.ModId, Key, d.defName)))
                    .ToList();
                if (options.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
        }

        internal static string Kg(float kg)
        {
            return kg.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
        }
    }

    public class Setting_AnimalTip : SettingContainer
    {
        private const float BoxHeight = 24f;

        public string valueKey;
        public float defaultValue;

        public Setting_AnimalTip()
        {
            addDefaultSpacing = false;
        }

        protected override float CalculateHeight(float width)
        {
            return 0f;
        }

        protected override void DrawSettingContents(Rect inRect)
        {
            Rect box = new Rect(inRect.x, inRect.y, inRect.width, BoxHeight);
            if (!Mouse.IsOver(box))
            {
                return;
            }
            ThingDef animal = Setting_AnimalPicker.Selected();
            if (animal == null)
            {
                return;
            }
            float pct = MissingPartsBootstrap.GetFloat(valueKey, defaultValue);
            float baseKg = Setting_CapacityExamples.BaseKg(animal);
            float kg = pct / 100f * baseKg;
            string sign = kg >= 0f ? "+" : "";
            TooltipHandler.TipRegion(box, animal.LabelCap + " (base " + Setting_AnimalPicker.Kg(baseKg) + "): " + sign + Setting_AnimalPicker.Kg(kg)
                + "\n\nChange the example animal at the top of this section.");
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
