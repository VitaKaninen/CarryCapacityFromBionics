using System.Collections.Generic;
using System.Globalization;
using RimWorld;
using Verse;

namespace Vita.CarryCapacityFromBionics.MissingParts
{
    // Reads the XML Extensions settings once at startup (settings changes require a restart,
    // stated in the menu) and, only if the section's master checkbox is on, appends the penalty
    // StatPart to whichever mass-carry-capacity stat exists in this game:
    // VEF_MassCarryCapacity (VEF active) or CarryCapacityBonus (our Standalone folder, VEF absent).
    // The master is the "Missing body parts" section checkbox created by CCFB_Section
    // (key ToggleSectionMissingParts, default OFF) = the StatPart is never injected by default,
    // zero behavior change.
    [StaticConstructorOnStartup]
    public static class MissingPartsBootstrap
    {
        public const string ModId = "Vita.CarryCapacityFromBionics";

        public static float floorKg;

        // Weight in kg per body part def. ALL 14 weighted defs get an entry; a part that is
        // toggled off or set to 0 stays in the map with weight 0, because limb roots cap
        // their whole subtree: a Leg at 0 must zero out foot/bone/toe penalties too.
        public static readonly Dictionary<BodyPartDef, float> weights = new Dictionary<BodyPartDef, float>();

        // Defaults MUST stay in sync with 1.6/MissingParts/Patches/CCFB_MissingParts.xml
        // (XE only stores a value once the user saves the settings menu). The menu shows the
        // values as NEGATIVE kg (they remove capacity); the magnitude is what we work with.
        private static readonly (string defName, float kg)[] defaultTable =
        {
            ("Leg", -8.75f), ("Spine", -7f), ("Pelvis", -7f), ("Shoulder", -6.3f), ("Arm", -5.25f),
            ("Femur", -4.55f), ("Foot", -3.5f), ("Humerus", -2.1f), ("Tibia", -2.1f), ("Hand", -1.75f),
            ("Radius", -1.05f), ("Clavicle", -1.05f), ("Finger", -0.35f), ("Toe", -0.35f),
        };

        static MissingPartsBootstrap()
        {
            if (!GetBool("ToggleSectionMissingParts", false))
            {
                return;
            }
            floorKg = GetFloat("MissingPartFloor", 0f);

            bool anyPositive = false;
            foreach ((string defName, float kg) in defaultTable)
            {
                BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(defName);
                if (def == null)
                {
                    continue;
                }
                float value = 0f;
                if (GetBool("ToggleMissingPart" + defName, false))
                {
                    value = System.Math.Abs(GetFloat("MissingPart" + defName, kg));
                }
                weights[def] = value;
                anyPositive |= value > 0f;
            }
            if (!anyPositive)
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
            stat.parts.Add(new StatPart_MissingBodyParts { parentStat = stat });
        }

        private static bool GetBool(string key, bool fallback)
        {
            if (XmlExtensions.SettingsManager.TryGetSetting(ModId, key, out string value)
                && bool.TryParse(value, out bool parsed))
            {
                return parsed;
            }
            return fallback;
        }

        private static float GetFloat(string key, float fallback)
        {
            if (XmlExtensions.SettingsManager.TryGetSetting(ModId, key, out string value)
                && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }
            return fallback;
        }
    }
}
