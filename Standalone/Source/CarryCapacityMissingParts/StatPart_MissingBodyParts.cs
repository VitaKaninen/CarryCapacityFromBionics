using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vita.CarryCapacityFromBionics.MissingParts
{
    // Subtracts carry capacity for missing body parts (topmost visible part only, exactly the
    // set the Health tab displays) and for damaged-but-attached weighted parts (scaled by lost
    // HP), then clamps to the configured minimum. Injected into the active mass-carry stat by
    // MissingPartsBootstrap; StatParts run in StatWorker.FinalizeValue, i.e. after the base
    // value and all implant statOffsets, so this composes with the rest of the mod.
    public class StatPart_MissingBodyParts : StatPart
    {
        // Per top-level limb, the summed penalty of its subtree is capped at the limb's own
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
            if (penalty <= 0f)
            {
                return;
            }
            // The floor only limits penalty damage; it never raises a pawn above the value
            // it would have without this feature.
            float floor = Mathf.Min(MissingPartsBootstrap.floorKg, val);
            val = Mathf.Max(val - penalty, floor);
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
            // Reconcile the floor against the value the StatPart actually received: the
            // unfinalized value (base + statOffsets; any parts before this one are no-ops
            // for pawns).
            float before = parentStat.Worker.GetValueUnfinalized(req);
            float floor = Mathf.Min(MissingPartsBootstrap.floorKg, before);
            float after = before - penalty;
            if (after < floor)
            {
                sb.AppendLine($"Raised to minimum: +{Fmt(floor - after)} kg");
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

        // Returns the total penalty in kg. When sb is non-null (explanation path), also
        // appends one line per contributing part and one per capped limb group.
        private static float ComputePenalty(Pawn pawn, StringBuilder sb)
        {
            Dictionary<BodyPartDef, float> weights = MissingPartsBootstrap.weights;
            HediffSet hediffSet = pawn.health.hediffSet;
            groupSums.Clear();

            // Missing parts: topmost visible ones only. Hidden child missing-parts and parts
            // replaced by bionics/prosthetics are already excluded by the game's cache.
            List<Hediff_MissingPart> missing = hediffSet.GetMissingPartsCommonAncestors();
            for (int i = 0; i < missing.Count; i++)
            {
                BodyPartRecord part = missing[i].Part;
                if (weights.TryGetValue(part.def, out float kg) && kg > 0f)
                {
                    AddContribution(part, kg, weights);
                    sb?.AppendLine($"Missing {part.Label}: -{Fmt(kg)} kg");
                }
            }

            // Damaged attached parts: weight scaled by the fraction of HP lost. Every gone
            // part carries its own missing-part hediff, so the direct check skips them all.
            List<BodyPartRecord> allParts = pawn.RaceProps.body.AllParts;
            for (int i = 0; i < allParts.Count; i++)
            {
                BodyPartRecord part = allParts[i];
                if (!weights.TryGetValue(part.def, out float kg) || kg <= 0f || hediffSet.PartIsMissing(part))
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
                float amount = kg * (1f - health / maxHealth);
                AddContribution(part, amount, weights);
                sb?.AppendLine($"Damaged {part.Label} ({(health / maxHealth).ToStringPercent()} HP): -{Fmt(amount)} kg");
            }

            float total = 0f;
            foreach (KeyValuePair<BodyPartRecord, float> group in groupSums)
            {
                float cap = weights[group.Key.def];
                if (group.Value > cap)
                {
                    total += cap;
                    sb?.AppendLine($"{group.Key.LabelCap} penalties capped at amputation (-{Fmt(cap)} kg): +{Fmt(group.Value - cap)} kg");
                }
                else
                {
                    total += group.Value;
                }
            }
            return total;
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
