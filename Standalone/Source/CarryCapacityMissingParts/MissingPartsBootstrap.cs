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

        // Weight in kg per body part def. ALL weighted defs get an entry; a part that is
        // toggled off or set to 0 stays in the map with weight 0, because limb roots cap
        // their whole subtree: a Leg at 0 must zero out foot/bone/toe penalties too.
        public static readonly Dictionary<BodyPartDef, float> weights = new Dictionary<BodyPartDef, float>();

        // Spine and Pelvis share one setting and one penalty group: the penalty applies if
        // either is missing/damaged and is NOT doubled when both are gone.
        public static readonly HashSet<BodyPartDef> sharedGroupDefs = new HashSet<BodyPartDef>();
        public static float sharedGroupKg;

        // Defaults MUST stay in sync with 1.6/MissingParts/Patches/CCFB_MissingParts.xml
        // (XE only stores a value once the user saves the settings menu). The menu shows the
        // values as NEGATIVE kg (they remove capacity); the magnitude is what we work with.
        // Children of each limb sum exactly to their parent (femur 5.25 + tibia 1.75 +
        // foot 1.75 = leg 8.75; clavicle 1.75 + arm 3.5 = shoulder 5.25; humerus 0.875 +
        // radius 0.875 + hand 1.75 = arm 3.5; 5 fingers/toes = hand/foot 1.75).
        private static readonly (string key, string[] defNames, float kg)[] defaultTable =
        {
            ("SpinePelvis", new[] { "Spine", "Pelvis" }, -7f),
            ("Leg", new[] { "Leg" }, -8.75f),
            ("Femur", new[] { "Femur" }, -5.25f),
            ("Tibia", new[] { "Tibia" }, -1.75f),
            ("Foot", new[] { "Foot" }, -1.75f),
            ("Toe", new[] { "Toe" }, -0.35f),
            ("Shoulder", new[] { "Shoulder" }, -5.25f),
            ("Clavicle", new[] { "Clavicle" }, -1.75f),
            ("Arm", new[] { "Arm" }, -3.5f),
            ("Humerus", new[] { "Humerus" }, -0.875f),
            ("Radius", new[] { "Radius" }, -0.875f),
            ("Hand", new[] { "Hand" }, -1.75f),
            ("Finger", new[] { "Finger" }, -0.35f),
        };

        static MissingPartsBootstrap()
        {
            if (!GetBool("ToggleSectionMissingParts", false))
            {
                return;
            }
            floorKg = GetFloat("MissingPartFloor", 0f);

            bool anyPositive = false;
            foreach ((string key, string[] defNames, float kg) in defaultTable)
            {
                float value = 0f;
                if (GetBool("ToggleMissingPart" + key, false))
                {
                    value = System.Math.Abs(GetFloat("MissingPart" + key, kg));
                }
                foreach (string defName in defNames)
                {
                    BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(defName);
                    if (def == null)
                    {
                        continue;
                    }
                    weights[def] = value;
                    if (defNames.Length > 1)
                    {
                        sharedGroupDefs.Add(def);
                    }
                }
                if (defNames.Length > 1)
                {
                    sharedGroupKg = value;
                }
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
