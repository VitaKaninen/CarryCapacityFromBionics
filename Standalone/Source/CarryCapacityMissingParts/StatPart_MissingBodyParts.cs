using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vita.CarryCapacityFromBionics.MissingParts
{
    // Reduces carry capacity for missing body parts (topmost visible part only, exactly the
    // set the Health tab displays) and for damaged-but-attached weighted parts (scaled by lost
    // HP). All values are percentages of the pawn's BASE capacity (body size x 35 kg), so the
    // kg amounts scale with body size. Injected into the active mass-carry stat by
    // MissingPartsBootstrap; StatParts run in StatWorker.FinalizeValue, i.e. after the base
    // value and all implant statOffsets, so this composes with the rest of the mod. Negative
    // final values are prevented by the stats' own minValue 0.
    public class StatPart_MissingBodyParts : StatPart
    {
        // Per top-level limb, the summed reduction of its subtree is capped at the limb's own
        // value: a mangled attached leg is never worse than a clean amputation. Group key is
        // the limb root (the highest TABLE ancestor, or the part itself) - roots stay in the
        // weights map even at 0, so a Leg set to 0 (or unticked) caps its whole subtree at 0.
        // Static scratch dict to avoid allocating on the hot path (stats are computed on the
        // main thread, same convention as vanilla's scratch collections in HediffSet).
        private static readonly Dictionary<BodyPartRecord, float> groupSums = new Dictionary<BodyPartRecord, float>();

        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn = GetPawn(req);
            if (pawn == null)
            {
                return;
            }
            float penalty = ComputePenalty(pawn, null);
            if (penalty > 0f)
            {
                val -= penalty;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn = GetPawn(req);
            if (pawn == null)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder();
            float penalty = ComputePenalty(pawn, sb);
            if (penalty <= 0f)
            {
                return null;
            }
            return sb.ToString().TrimEndNewlines();
        }

        private static Pawn GetPawn(StatRequest req)
        {
            if (req.HasThing && req.Thing is Pawn pawn
                && pawn.RaceProps != null && pawn.RaceProps.Humanlike
                && pawn.health?.hediffSet != null)
            {
                return pawn;
            }
            return null;
        }

        // Returns the total reduction in kg. When sb is non-null (explanation path), also
        // appends one line per contributing part, one per capped limb group, and one when the
        // total-reduction cap binds.
        private static float ComputePenalty(Pawn pawn, StringBuilder sb)
        {
            Dictionary<BodyPartDef, float> weights = MissingPartsBootstrap.weights;
            HediffSet hediffSet = pawn.health.hediffSet;
            float baseCapacity = pawn.BodySize * MassUtility.MassCapacityPerBodySize;
            groupSums.Clear();
            float total = 0f;

            // Spine/pelvis pair: one penalty, the worse of the two counts, never doubled.
            float pairFrac = MissingPartsBootstrap.spinePelvisFrac;
            if (pairFrac > 0f)
            {
                float spineFactor = PartLossFactor(pawn, hediffSet, MissingPartsBootstrap.spineDef, out bool spineMissing);
                float pelvisFactor = PartLossFactor(pawn, hediffSet, MissingPartsBootstrap.pelvisDef, out bool pelvisMissing);
                float factor = Mathf.Max(spineFactor, pelvisFactor);
                if (factor > 0f)
                {
                    float kg = pairFrac * baseCapacity * factor;
                    total += kg;
                    if (sb != null)
                    {
                        if (spineMissing && pelvisMissing)
                        {
                            sb.AppendLine($"Missing spine+pelvis: -{Fmt(kg)} kg");
                        }
                        else if (spineMissing || pelvisMissing)
                        {
                            sb.AppendLine($"Missing {(spineMissing ? "spine" : "pelvis")}: -{Fmt(kg)} kg");
                        }
                        else
                        {
                            string part = spineFactor >= pelvisFactor ? "spine" : "pelvis";
                            sb.AppendLine($"Damaged {part} ({(1f - factor).ToStringPercent()} HP): -{Fmt(kg)} kg");
                        }
                    }
                }
            }

            // Missing parts: topmost visible ones only. Hidden child missing-parts and parts
            // replaced by bionics/prosthetics are already excluded by the game's cache.
            List<Hediff_MissingPart> missing = hediffSet.GetMissingPartsCommonAncestors();
            for (int i = 0; i < missing.Count; i++)
            {
                BodyPartRecord part = missing[i].Part;
                if (weights.TryGetValue(part.def, out float frac) && frac > 0f)
                {
                    float kg = frac * baseCapacity;
                    AddContribution(part, kg, weights);
                    sb?.AppendLine($"Missing {part.Label}: -{Fmt(kg)} kg");
                }
            }

            // Damaged attached parts: scaled by the fraction of HP lost. Every gone part
            // carries its own missing-part hediff, so the direct check skips them all.
            List<BodyPartRecord> allParts = pawn.RaceProps.body.AllParts;
            for (int i = 0; i < allParts.Count; i++)
            {
                BodyPartRecord part = allParts[i];
                if (!weights.TryGetValue(part.def, out float frac) || frac <= 0f || hediffSet.PartIsMissing(part))
                {
                    continue;
                }
                float maxHealth = part.def.GetMaxHealth(pawn);
                if (maxHealth <= 0f)
                {
                    continue;
                }
                float health = hediffSet.GetPartHealth(part);
                if (health >= maxHealth)
                {
                    continue;
                }
                float kg = frac * baseCapacity * (1f - health / maxHealth);
                AddContribution(part, kg, weights);
                sb?.AppendLine($"Damaged {part.Label} ({(health / maxHealth).ToStringPercent()} HP): -{Fmt(kg)} kg");
            }

            foreach (KeyValuePair<BodyPartRecord, float> group in groupSums)
            {
                float cap = weights[group.Key.def] * baseCapacity;
                if (group.Value > cap)
                {
                    total += cap;
                    sb?.AppendLine($"{group.Key.LabelCap} penalties capped at -{Fmt(cap)} kg: +{Fmt(group.Value - cap)} kg");
                }
                else
                {
                    total += group.Value;
                }
            }

            // Total-reduction cap (setting, default 100% of base capacity).
            float reductionCap = MissingPartsBootstrap.maxReductionFrac * baseCapacity;
            if (total > reductionCap)
            {
                sb?.AppendLine($"Total reduction capped at {MissingPartsBootstrap.maxReductionFrac.ToStringPercent()} of base: +{Fmt(total - reductionCap)} kg");
                total = reductionCap;
            }
            return total;
        }

        // 0 = intact, 1 = missing, in between = damaged (share of HP lost).
        private static float PartLossFactor(Pawn pawn, HediffSet hediffSet, BodyPartDef def, out bool isMissing)
        {
            isMissing = false;
            if (def == null)
            {
                return 0f;
            }
            List<BodyPartRecord> records = pawn.RaceProps.body.GetPartsWithDef(def);
            for (int i = 0; i < records.Count; i++)
            {
                BodyPartRecord part = records[i];
                if (hediffSet.PartIsMissing(part))
                {
                    isMissing = true;
                    return 1f;
                }
                float maxHealth = part.def.GetMaxHealth(pawn);
                if (maxHealth > 0f)
                {
                    float health = hediffSet.GetPartHealth(part);
                    if (health < maxHealth)
                    {
                        return 1f - health / maxHealth;
                    }
                }
            }
            return 0f;
        }

        private static void AddContribution(BodyPartRecord part, float amount, Dictionary<BodyPartDef, float> weights)
        {
            BodyPartRecord root = part;
            for (BodyPartRecord cur = part.parent; cur != null; cur = cur.parent)
            {
                if (weights.ContainsKey(cur.def))
                {
                    root = cur;
                }
            }
            groupSums.TryGetValue(root, out float sum);
            groupSums[root] = sum + amount;
        }

        private static string Fmt(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
