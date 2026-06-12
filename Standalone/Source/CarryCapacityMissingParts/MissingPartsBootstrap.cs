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

        // All values are PERCENTAGES of the pawn's base capacity (body size x 35 kg), stored
        // here as fractions - so penalties scale with body size (children lose less in kg).

        // Maximum total reduction as a fraction of base capacity (setting default 100%).
        public static float maxReductionFrac = 1f;

        // Reduction fraction per body part def. ALL weighted defs get an entry; a part that is
        // toggled off or set to 0 stays in the map with weight 0, because limb roots cap
        // their whole subtree: a Leg at 0 must zero out foot/bone/toe penalties too.
        public static readonly Dictionary<BodyPartDef, float> weights = new Dictionary<BodyPartDef, float>();

        // Spine and Pelvis share one setting and one penalty: each needs the other to work, so
        // the worse of the two counts and it is NOT doubled when both are gone. They are kept
        // OUT of the weights map (they are leaves, never anyone's limb root) and handled as a
        // pair in StatPart_MissingBodyParts.
        public static BodyPartDef spineDef;
        public static BodyPartDef pelvisDef;
        public static float spinePelvisFrac;

        // Defaults MUST stay in sync with 1.6/MissingParts/Patches/CCFB_MissingParts.xml
        // (XE only stores a value once the user saves the settings menu). Values are percent
        // numbers exactly as shown in the menu. Children of each limb sum exactly to their
        // parent (femur 15 + tibia 5 + foot 5 = leg 25; clavicle 5 + arm 10 = shoulder 15;
        // humerus 2.5 + radius 2.5 + hand 5 = arm 10; 5 fingers/toes = hand/foot 5).
        private static readonly (string key, string[] defNames, float pct)[] defaultTable =
        {
            ("SpinePelvis", new[] { "Spine", "Pelvis" }, 20f),
            ("Leg", new[] { "Leg" }, 25f),
            ("Femur", new[] { "Femur" }, 15f),
            ("Tibia", new[] { "Tibia" }, 5f),
            ("Foot", new[] { "Foot" }, 5f),
            ("Toe", new[] { "Toe" }, 1f),
            ("Shoulder", new[] { "Shoulder" }, 15f),
            ("Clavicle", new[] { "Clavicle" }, 5f),
            ("Arm", new[] { "Arm" }, 10f),
            ("Humerus", new[] { "Humerus" }, 2.5f),
            ("Radius", new[] { "Radius" }, 2.5f),
            ("Hand", new[] { "Hand" }, 5f),
            ("Finger", new[] { "Finger" }, 1f),
        };

        static MissingPartsBootstrap()
        {
            if (!GetBool("ToggleSectionMissingParts", false))
            {
                return;
            }
            maxReductionFrac = System.Math.Abs(GetFloat("MissingPartMaxReduction", 100f)) / 100f;

            bool anyPositive = false;
            foreach ((string key, string[] defNames, float pct) in defaultTable)
            {
                float frac = 0f;
                if (GetBool("ToggleMissingPart" + key, false))
                {
                    frac = System.Math.Abs(GetFloat("MissingPart" + key, pct)) / 100f;
                }
                if (key == "SpinePelvis")
                {
                    spineDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Spine");
                    pelvisDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Pelvis");
                    spinePelvisFrac = frac;
                }
                else
                {
                    foreach (string defName in defNames)
                    {
                        BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(defName);
                        if (def != null)
                        {
                            weights[def] = frac;
                        }
                    }
                }
                anyPositive |= frac > 0f;
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
