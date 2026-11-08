# Supported and planned third-party mods
### Supported third-party mods (folder ↔ packageId)
- FSFABE — `FrozenSnowFox.AdvancedBionicsExpansion`
- FSFVBE — `FrozenSnowFox.VanillaBionicsExpansion`
- RBSE — `rah.rbse` **and** `rah.rbsehc`
- EPOE — `ykara.epoe` **and** `ykara.elstrages.epoe`
- EPOEForked — `vat.epoeforked`
- EPOEForkedRoyalty — `vat.epoeforkedroyalty` (+ Royalty)
- ArchotechExpanded — `teok25.archotechexpanded` **and** `teok25.archotechexpanded.prosthetics`
- AdvancedArchotechArm — `NightKosh.AdvancedArchotechArm`
- VOID — `RH2.Faction.VOID`
- SoS2 — `kentington.saveourship2`
- II (Integrated Implants) — `lts.I`, plus suffixed folders mirroring that mod's own conditional
  loading: `II_NoAEP` (needs Archotech Expanded Prosthetics ABSENT), `II_NoMSE2` (needs Medical
  System Expansion 2 ABSENT), and donor-mod extra-arm folders `II_EPOEForked`,
  `II_EPOEForkedRoyalty`, `II_FSFABE`, `II_FSFABE_Royalty`, `II_RBSE` (each needs lts.I + the
  donor mod (+DLC) + MSE2 absent). All inject into the single "Integrated Implants" section.
  Left/right extra-arm hediffs are separate defs sharing one settings key (menu shows one row).
  Implants the mod buffs natively via VEF_MassCarryCapacity (StrengthEnhancer, SkeletalBracing,
  LTS_ManipulationTail, HulkificationSurgery) are NOT patched normally (double-dip), but go
  through the **native-VEF override** `CCFB_ImplantNativeVEF` in `II_NativeVEF` /
  `II_NativeVEF_Anomaly` (gated lts.I (+Anomaly), no VEF gate) at the author's values 25/25/25/50.
  Fully skipped: ghoul claws (ghouls don't haul/caravan), venom tail (no carrying-relevant caps).
- YAPE (Yet Another Prosthetic Expansion - Core) — `MrKociak.YetAnotherProstheticExpansionModCore`.
  YAPE ships its own copies of SoS2's archotech parts (same defNames `SoSArchotechSpine`/`Skin`);
  `YAPE_NoSoS2` (gated YAPE + SoS2 ABSENT) patches those with the keys **shared** with the SoS2
  section, so exactly one folder ever patches them and one menu row renders.
- MedP (Medieval Prosthetics) — `NunahurAlShamikh.MedP`. Ambroisen tier is eff 1.0
  (natural-equivalent) → 0-value rows, default off.
- Astral (Astraltech Bionic Implants) — `AnthonyTherrien.AstraltechBionics`. Replacement parts
  (arm/leg/spine/pelvis/torso, eff 2.0–2.5) plus eff-1.0 booster implants valued from their
  Moving/Manipulation capMods (muscle-stimulator precedent). Organs/senses skipped.
- NeoP (Neolithic Prosthetics) — `Uga.Booga`, plus suffixed folders mirroring its conditional
  loading: `NeoP_Odyssey` (Odyssey DLC), `NeoP_VAE` (Vanilla Animals Expanded),
  `NeoP_VAE_NoOdyssey` (VAE + Odyssey ABSENT), `NeoP_AlphaAnimals`. Variant defs share keys
  (BeaverTail/O_BeaverTail/AlphaBeaverTail; GorillaArm/OD_GorillaArm/OD_ArmGorilla;
  CharmOfPanda/OD_CharmOfPanda; KangarooLeg/O_KangarooLeg). SewnInBag (15), MuffaloSpine (50) and
  SyntheticMuffaloSpine (75) are native-VEF → `CCFB_ImplantNativeVEF` in `NeoP_NativeVEF`, defaults
  = author's values (SewnInBag's native 15 exists only in NeoP 1.6/1.7; 1.5 has only in-hands
  CarryingCapacity). Many NeoP parts (Tyranno arm, mantis hands, Gallimimus leg, muffalo spines,
  CharmOfPanda) are defined inside NeoP's `Patches/` files, not `Defs/` — a Defs-only scan misses them.
- IP (Industrial Prosthetics) — `Mey.Prosthetics`. 46 hediffs → 16 keys: the
  Combat/Labor/Construct/Social/Military variants of a tier share one key (L/R convention).

## Planned future support (per VitaKaninen, 2026-06)
- YAPE - Animals (ws 2808876573) and A Dog Said... Animal Prosthetics 2 (ws 3238353862) —
  deferred animal wave (animal mass capacity = caravan capacity, worth doing; needs a per-leg
  weight convention for quadrupeds first).
  ADSAP2 scan (2026-11-08): packageId `SamBucher.ADogSaidAnimalProsthetics2`, supports 1.4–1.6
  (1.7 falls back to its `1.6/`). No native carry stats (no double-dip). Carrying-relevant
  hediffs ≈29: core `1.6/Defs` (Bionic/SimpleProsthetic Leg+Arm, BionicSpineAnimal eff 1.0,
  WoodenPaw/Hoof); `ModCompat/CommonSubparts` (paw/hoof/foot/toe/hand/finger/claw, gated
  `vat.epoeforked,mse2.core` = ANY); `ModCompat/MSE2` (forearm, internal support, RG foot claw);
  `ModCompat/EPOE-Forked` (SimpleSpineAnimal); `ModCompat/KenshiFauna` (`el.kenshifauna`: pincer,
  tremor leg). Wings folder is 1.4-only. EPOE-Forked patches lower some partEfficiency values
  (WoodenHoof 0.75→0.6). Values: body-size scaling (user decision 2026-11-08).
  **Blocker, verified by IL dump of 1.7.4995 Assembly-CSharp:** `MassUtility.Capacity` returns 0
  early unless `CanEverCarryAnything` (= adult non-subhuman ToolUser, else `packAnimal`). Both our
  transpiler and VEF's (same `Stloc_0` injection) sit after that return, so hediff statOffsets
  never reach non-pack animals (dogs). PUAH (Mehni, GitHub) then sees `WillBeOverEncumbered…` =
  true and falls back to a vanilla single-stack haul. Animal support for non-pack haulers needs new
  C# (e.g. a Capacity postfix for animals), not just XML.
  **Resolved by Caravan Packs** (`VFME.CaravanPacks`, ws 2255432128 up to 1.5 and fork ws 3609057140
  for 1.6 — same packageId, patch XML byte-identical, no DLL): it only adds
  `<packAnimal>true</packAnimal>` to species (Husky, LabradorRetriever, Thrumbo, Rhinoceros, bears,
  wolves…). That gives them vanilla capacity, so our bonus and PUAH inventory hauling work through
  the normal path, so no capacity-granting C# is needed. User decision 2026-11-08: no missing-part
  penalty for animals.
- EXCLUDED by decision (2026-06-10, not updated past RimWorld 1.5): Vanilla Archotech
  Implants (ws 2715093425, 1.3), [SMP] Simple Archotech Implants (ws 2462646185, 1.5),
  Cybernetic Organism and Neural Network (ws 2045064990, 1.4). If they ever update to 1.6,
  candidate hediffs + proposed values are in `.claude/new_mods_proposal.md`. Note for VAI:
  it defines `ArchotechSpine` — the same defName Archotech Expanded already patches — so its
  folder would need `IfModNotActive` on both teok25 packageIds.
