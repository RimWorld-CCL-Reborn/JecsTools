using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PawnShields
{
    /// <summary>
    /// Assists in generating shields for pawns. Based off PawnWeaponGenerator.
    /// </summary>
    public static class PawnShieldGenerator
    {
        public static List<ThingStuffPair> allShieldPairs;
        public static List<ThingStuffPair> workingShields = new List<ThingStuffPair>();

        private const float WeaponSelectFactor_NobleByIdeo = 100f;

        private const float WeaponSelectFactor_DespisedByIdeo = 0.001f;

        //static PawnShieldGenerator()
        //{
        //    //Initialise all shields.
        //    Reset();
        //}

        /// <summary>
        /// Tries to generate a shield for the pawn.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="request"></param>
        public static void TryGenerateShieldFor(Pawn pawn, PawnGenerationRequest request)
        {
            workingShields.Clear();

            // Same conditions as weapon generation, except using PawnKindDef ShieldPawnGeneratorProperties.shieldTags instead of pawn.kindDef.weaponTags
            var generatorProps = request.KindDef.GetShieldPawnGeneratorProperties();
            if (generatorProps == null || generatorProps.shieldTags.NullOrEmpty())
                return;
            if (!pawn.RaceProps.ToolUser ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ||
                pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return;
            }

            var generatorPropsShieldMoney = generatorProps.shieldMoney;
            var randomInRange = generatorPropsShieldMoney.RandomInRange;
            foreach (var w in allShieldPairs)
            {
                if (w.Price <= randomInRange &&
                    !w.thing.weaponTags.NullOrEmpty() &&
                    generatorProps.shieldTags.Any(tag => w.thing.weaponTags.Contains(tag)) &&
                    (!w.thing.IsRangedWeapon || !pawn.WorkTagIsDisabled(WorkTags.Shooting)) &&
                    (w.thing.generateAllowChance >= 1f ||
                        Rand.ChanceSeeded(w.thing.generateAllowChance, pawn.thingIDNumber ^ w.thing.shortHash ^ 0x1B3B648)))
                {
                    workingShields.Add(w);
                }
            }
            if (workingShields.Count == 0)
            {
                //Log.Warning("No working shields found for " + pawn.Label + "::" + pawn.KindLabel);
                return;
            }

            if (workingShields.TryRandomElementByWeight(w => w.Commonality * w.Price * GetWeaponCommonalityFromIdeo(pawn, w), out var thingStuffPair))
            {
                var thingWithComps = (ThingWithComps)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
                PawnGenerator.PostProcessGeneratedGear(thingWithComps, pawn);
                var compEquippable = thingWithComps.GetCompShield();
                if (compEquippable != null)
                {
                    if (pawn.Ideo != null)
                    {
                        compEquippable.parent.StyleDef = pawn.Ideo.GetStyleFor(thingWithComps.def);
                    }
                }
                var biocodeWeaponChance = (request.BiocodeWeaponChance > 0f) ? request.BiocodeWeaponChance : pawn.kindDef.biocodeWeaponChance;
                if (Rand.Value < biocodeWeaponChance)
                {
                    BiocodeForPawn(pawn, thingWithComps);
                }
                pawn.equipment.AddEquipment(thingWithComps);
            }

            workingShields.Clear();
        }

        private static float GetWeaponCommonalityFromIdeo(Pawn pawn, ThingStuffPair pair)
        {
            if (pawn.Ideo == null)
            {
                return 1f;
            }
            return pawn.Ideo.GetDispositionForWeapon(pair.thing) switch
            {
                IdeoWeaponDisposition.Noble => WeaponSelectFactor_NobleByIdeo,
                IdeoWeaponDisposition.Despised => WeaponSelectFactor_DespisedByIdeo,
                _ => 1f,
            };
        }

        // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast (~6x as fast for me).
        private static void BiocodeForPawn(Pawn pawn, ThingWithComps thingWithComps)
        {
            var comps = thingWithComps.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompBiocodable compBiocodable)
                {
                    compBiocodable.CodeFor(pawn);
                }
            }
        }

        /// <summary>
        /// Resets the shield generator.
        /// </summary>
        public static void Reset()
        {
            static bool IsShield(ThingDef td) => td.equipmentType != EquipmentType.Primary && td.HasComp(typeof(CompShield));

            allShieldPairs = ThingStuffPair.AllWith(IsShield);

            foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!IsShield(thingDef)) continue;
                var sum = 0f;
                foreach (var pa in allShieldPairs)
                {
                    if (pa.thing == thingDef)
                    {
                        sum += pa.Commonality;
                    }
                }
                if (sum == 0f)
                    continue;
                var avg = thingDef.generateCommonality / sum;
                if (avg == 1f)
                    continue;
                for (var i = 0; i < allShieldPairs.Count; i++)
                {
                    var thingStuffPair = allShieldPairs[i];
                    if (thingStuffPair.thing == thingDef)
                    {
                        allShieldPairs[i] = new ThingStuffPair(thingStuffPair.thing, thingStuffPair.stuff,
                            thingStuffPair.commonalityMultiplier * avg);
                    }
                }
            }
        }
    }
}
