using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CompVehicle
{
    public class CompVehicleUtility
    {
        // ToDo: Rotate pawns to allow for some pawns to rest while others
        // use the vehicle.
        // RimWorld.Planet.CaravanPawnsNeedsUtility
        public static void TrySatisfyRestNeed(Pawn pawn, Need_Rest rest, Pawn Vehicle)
        {
            //Do not try to rest in the vehicle during combat.
            //It's too dangerous to sleep while driving.
            //if (Vehicle.Map?.attackTargetsCache?.GetPotentialTargetsFor(Vehicle)?.FirstOrDefault(x => !x.ThreatDisabled()) == null)
            //{
            //    float restEffectiveness = RestUtility.PawnHealthRestEffectivenessFactor(pawn);
            //    rest.TickResting(restEffectiveness);
            //}
        }


        // RimWorld.Planet.CaravanPawnsNeedsUtility
        public static void TrySatisfyFoodNeed(Pawn pawn, Need_Food food, Pawn vehicle)
        {
            if (food.CurCategory < HungerCategory.Hungry)
            {
                return;
            }
            Thing thing;
            Pawn pawn2;
            if (VirtualPlantsUtility.CanEatVirtualPlantsNow(pawn))
            {
                VirtualPlantsUtility.EatVirtualPlants(pawn);
            }
            else if (CompVehicleUtility.TryGetBestFood(vehicle, pawn, out thing, out pawn2))
            {
                food.CurLevel += thing.Ingested(pawn, food.NutritionWanted);
                if (thing.Destroyed)
                {
                    if (pawn2 != null)
                    {
                        vehicle.inventory.innerContainer.Remove(thing);
                        //caravan.RecacheImmobilizedNow();
                        //caravan.RecacheDaysWorthOfFood();
                    }
                    //if (!CompVehicleUtility.TryGetBestFood(vehicle, pawn, out thing, out pawn2))
                    //{
                    //    Messages.Message("MessageCaravanRunOutOfFood".Translate(new object[]
                    //    {
                    //caravan.LabelCap,
                    //pawn.Label
                    //    }), caravan, MessageSound.SeriousAlert);
                    //}
                }
            }
        }

        // RimWorld.Planet.CaravanInventoryUtility
        public static bool TryGetBestFood(Pawn vehicle, Pawn forPawn, out Thing food, out Pawn owner)
        {
            List<Thing> list = vehicle?.inventory?.innerContainer?.InnerListForReading;
            Thing thing = null;
            float num = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing2 = list[i];
                if (CompVehicleUtility.CanNowEatForNutrition(thing2, forPawn))
                {
                    float foodScore = CaravanPawnsNeedsUtility.GetFoodScore(thing2, forPawn);
                    if (thing == null || foodScore > num)
                    {
                        thing = thing2;
                        num = foodScore;
                    }
                }
            }
            if (thing != null)
            {
                food = thing;
                owner = forPawn; //CaravanInventoryUtility.GetOwnerOf(caravan, thing);
                return true;
            }
            food = null;
            owner = null;
            return false;
        }

        // RimWorld.Planet.CaravanPawnsNeedsUtility
        public static bool CanNowEatForNutrition(Thing food, Pawn pawn)
        {
            return food.IngestibleNow && CompVehicleUtility.CanNowEatForNutrition(food.def, pawn);
        }

        // RimWorld.Planet.CaravanPawnsNeedsUtility
        public static bool CanNowEatForNutrition(ThingDef food, Pawn pawn)
        {
            return CompVehicleUtility.CanEverEatForNutrition(food, pawn) && (pawn.needs.food.CurCategory >= HungerCategory.Starving || food.ingestible.preferability > FoodPreferability.DesperateOnly);
        }

        // RimWorld.Planet.CaravanPawnsNeedsUtility
        public static bool CanEverEatForNutrition(ThingDef food, Pawn pawn)
        {
            return food.IsNutritionGivingIngestible && pawn.RaceProps.CanEverEat(food) && food.ingestible.preferability > FoodPreferability.NeverForNutrition && (!pawn.IsTeetotaler() || !food.IsDrug);
        }

        // RimWorld.Planet.CaravanPawnsNeedsUtility
        public static void TrySatisfyChemicalNeed(Pawn pawn, Need_Chemical chemical, Pawn vehicle)
        {
            if (chemical.CurCategory >= DrugDesireCategory.Satisfied)
            {
                return;
            }
            Thing drug;
            Pawn drugOwner;
            if (CompVehicleUtility.TryGetBestDrug(vehicle, pawn, chemical, out drug, out drugOwner))
            {
                CompVehicleUtility.IngestDrug(pawn, drug, drugOwner, vehicle);
            }
        }

        // RimWorld.Planet.CaravanInventoryUtility
        public static bool TryGetBestDrug(Pawn vehicle, Pawn forPawn, Need_Chemical chemical, out Thing drug, out Pawn owner)
        {
            Hediff_Addiction addictionHediff = chemical.AddictionHediff;
            if (addictionHediff == null)
            {
                drug = null;
                owner = null;
                return false;
            }
            List<Thing> list = vehicle?.inventory?.innerContainer?.InnerListForReading;
            Thing thing = null;
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing2 = list[i];
                if (thing2.IngestibleNow && thing2.def.IsDrug)
                {
                    CompDrug compDrug = thing2.TryGetComp<CompDrug>();
                    if (compDrug != null && compDrug.Props.chemical != null)
                    {
                        if (compDrug.Props.chemical.addictionHediff == addictionHediff.def)
                        {
                            if (forPawn.drugs == null || forPawn.drugs.CurrentPolicy[thing2.def].allowedForAddiction || forPawn.story == null || forPawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0)
                            {
                                thing = thing2;
                                break;
                            }
                        }
                    }
                }
            }
            if (thing != null)
            {
                drug = thing;
                owner = forPawn;
                return true;
            }
            drug = null;
            owner = null;
            return false;
        }

        // RimWorld.Planet.CaravanPawnsNeedsUtility
        public static void IngestDrug(Pawn pawn, Thing drug, Pawn drugOwner, Pawn vehicle)
        {
            float num = drug.Ingested(pawn, 0f);
            Need_Food food = pawn.needs.food;
            if (food != null)
            {
                food.CurLevel += num;
            }
            if (drug.Destroyed && drugOwner != null)
            {
                vehicle.inventory.innerContainer.Remove(drug);
                //caravan.RecacheImmobilizedNow();
                //caravan.RecacheDaysWorthOfFood();
            }
        }


    }
}
