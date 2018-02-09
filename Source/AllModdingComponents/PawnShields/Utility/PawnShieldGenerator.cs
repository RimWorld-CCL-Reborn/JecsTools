using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace PawnShields
{
    /// <summary>
    /// Assists in generating shields for pawns.
    /// </summary>
    public static class PawnShieldGenerator
    {
        public static List<ThingStuffPair> allShieldPairs;
        public static List<ThingStuffPair> workingShields = new List<ThingStuffPair>();

        /*static PawnShieldGenerator()
        {
            //Initialise all shields.
            Reset();
        }*/

        /// <summary>
        /// Tries to generate a shield for the pawn.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="request"></param>
        public static void TryGenerateShieldFor(Pawn pawn, PawnGenerationRequest request)
        {
            //Shield stuff.
            ShieldPawnGeneratorProperties generatorProps = request.KindDef.GetModExtension<ShieldPawnGeneratorProperties>();

            //Abort early if there is no mention at all of shield properties.
            if (generatorProps == null)
                return;

            workingShields.Clear();

            //Initial filtering
            if (generatorProps.shieldTags == null || generatorProps.shieldTags.Count == 0)
            {
                return;
            }
            if (!pawn.RaceProps.ToolUser)
            {
                return;
            }
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return;
            }
            if (pawn.story != null && pawn.story.WorkTagIsDisabled(WorkTags.Violent))
            {
                return;
            }

            float randomInRange = generatorProps.shieldMoney.RandomInRange;
            for (int i = 0; i < allShieldPairs.Count; i++)
            {
                ThingStuffPair w = allShieldPairs[i];
                if (w.Price <= randomInRange)
                {
                    if (generatorProps.shieldTags.Any(tag => w.thing.weaponTags.Contains(tag)))
                    {
                        if (w.thing.generateAllowChance >= 1f || Rand.ValueSeeded(pawn.thingIDNumber ^ 28554824) <= w.thing.generateAllowChance)
                        {
                            workingShields.Add(w);
                        }
                    }
                }
            }
            if (workingShields.Count == 0)
            {
                return;
            }

            ThingStuffPair thingStuffPair;
            if (workingShields.TryRandomElementByWeight((ThingStuffPair w) => w.Commonality * w.Price, out thingStuffPair))
            {
                ThingWithComps thingWithComps = (ThingWithComps)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
                PawnGenerator.PostProcessGeneratedGear(thingWithComps, pawn);
                pawn.equipment.AddEquipment(thingWithComps);
            }
            workingShields.Clear();
        }

        /// <summary>
        /// Resets the shield generator.
        /// </summary>
        public static void Reset()
        {
            Predicate<ThingDef> isShield = (ThingDef td) => td.equipmentType != EquipmentType.Primary && td.canBeSpawningInventory && td.HasComp(typeof(CompShield));
            allShieldPairs = ThingStuffPair.AllWith(isShield);

            using (IEnumerator<ThingDef> enumerator = (from td in DefDatabase<ThingDef>.AllDefs
                                                       where isShield(td)
                                                       select td).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ThingDef thingDef = enumerator.Current;
                    float num = (from pa in allShieldPairs
                                 where pa.thing == thingDef
                                 select pa).Sum((ThingStuffPair pa) => pa.Commonality);
                    float num2 = thingDef.generateCommonality / num;
                    if (num2 != 1f)
                    {
                        for (int i = 0; i < allShieldPairs.Count; i++)
                        {
                            ThingStuffPair thingStuffPair = allShieldPairs[i];
                            if (thingStuffPair.thing == thingDef)
                            {
                                allShieldPairs[i] = new ThingStuffPair(thingStuffPair.thing, thingStuffPair.stuff, thingStuffPair.commonalityMultiplier * num2);
                            }
                        }
                    }
                }
            }
        }
    }
}
