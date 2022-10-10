using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace JecsTools
{
    public abstract class WorldObjectBlueprint : WorldObject
    {
        private List<Thing> consumedResources = new List<Thing>();
        public bool resourcesSupplied;
        public abstract WorldObjectRecipeDef Recipe { get; }
        public abstract CaravanJobDef ConstructJobDef { get; }

        public virtual WorldObjectBlueprint NextBlueprint => null;
        public virtual WorldObjectBlueprint PrevBlueprint => null;

        public List<Thing> ConsumedResources
        {
            get => consumedResources;
            set => consumedResources = value;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref resourcesSupplied, nameof(resourcesSupplied));
            Scribe_Collections.Look(ref consumedResources, nameof(consumedResources), LookMode.Reference);
        }

        public virtual void Finish()
        {
            if (!ConsumedResources.NullOrEmpty())
                DestroyConsumedResources();
        }

        private void DestroyConsumedResources()
        {
            // XXX: Not sure if Destroy on each consumed resource can end up modifying ConsumedResources itself,
            // so to be safe, enumerating over a copy of the list.
            // Also not sure if any code relies on the "remove t from ConsumedResources, then Destroy t" behavior,
            // so keeping that behavior as well.
            foreach (var t in ConsumedResources.ToArray())
            {
                ConsumedResources.Remove(t);
                t.Destroy();
            }
        }

        public virtual bool Cancel()
        {
            if (!ConsumedResources.NullOrEmpty())
            {
                var c = Find.WorldObjects.PlayerControlledCaravanAt(Tile);
                if (c != null)
                {
                    foreach (var t in ConsumedResources.ToArray()) // using copy for enumeration - see DestroyConsumedResources comments
                    {
                        var pawn = CaravanInventoryUtility.FindPawnToMoveInventoryTo(t, c.PawnsListForReading, null);
                        if (pawn == null)
                        {
                            Log.Error("Could not find pawn to move bought thing to (bought by player). thing=" + t);
                            t.Destroy();
                        }
                        else if (!pawn.inventory.innerContainer.TryAdd(t))
                        {
                            Log.Error("Could not add item to inventory.");
                            t.Destroy();
                        }
                    }
                    return true;
                }
                else
                {
                    var didCancel = false;
                    var labels = new string[ConsumedResources.Count];
                    for (var i = 0; i < ConsumedResources.Count; i++)
                        labels[i] = ConsumedResources[i].Label;
                    var window = Dialog_MessageBox.CreateConfirmation(
                        "ConfirmAbandonItemDialog".Translate(string.Join(", ", labels) + " " +
                        "JecsTools_WorldObjectConst_AbandonReason".Translate()),
                        () =>
                        {
                            //var ownerOf = CaravanInventoryUtility.GetOwnerOf(caravan, t);
                            //if (ownerOf == null)
                            //{
                            //    Log.Error("Could not find owner of " + t);
                            //    return;
                            //}
                            //ownerOf.inventory.innerContainer.Remove(t);
                            DestroyConsumedResources();
                            didCancel = true;
                        }, true, null);
                    Find.WindowStack.Add(window);
                    return didCancel;
                }
            }
            return true;
        }

        // Based off Settlement/CaravanArrivalAction_VisitSettlement/CaravanArrivalActionUtility.
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (var f in base.GetFloatMenuOptions(caravan))
                yield return f;
            if (ConstructJobDef != null)
            {
                if (CheckAndConsumeResources(caravan, Recipe, false))
                    yield return new FloatMenuOption(
                        "CreateNewZone".Translate(Recipe.FinishedThing.label), () =>
                        {
                            var t = CaravanJobsUtility.GetCaravanJobGiver().Tracker(caravan);
                            var curBlueprint = this;
                            while (curBlueprint != null)
                            {
                                t.jobQueue.EnqueueLast(new CaravanJob(ConstructJobDef, curBlueprint));
                                curBlueprint = curBlueprint.NextBlueprint;
                            }
                        });
                else
                    yield return new FloatMenuOption(
                        "CreateNewZone".Translate(Recipe.FinishedThing.label) + " (" + "MissingMaterials".Translate() + ")",
                        null);
                yield return new FloatMenuOption("VisitSite".Translate(Label),
                    () => caravan.pather.StartPath(Tile, null, true), revalidateWorldClickTarget: this);
                if (Prefs.DevMode)
                {
                    yield return new FloatMenuOption(
                        "CreateNewZone".Translate(Recipe.FinishedThing.label) + " (Dev: instantly)", Finish);
                    //{
                    //    caravan.Tile = Tile;
                    //    caravan.pather.StopDead();
                    //    new CaravanArrivalAction_VisitSettlement(this).Arrived(caravan);
                    //});
                    yield return new FloatMenuOption(
                        "VisitSite".Translate(Recipe.FinishedThing.label) + " (Dev: instantly)", () =>
                        {
                            caravan.Tile = Tile;
                            caravan.pather.StopDead();
                            //new CaravanArrivalAction_VisitSettlement(this).Arrived(caravan);
                            //FinishConstruction();
                        });
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;
            yield return new Command_Action
            {
                defaultLabel = "CommandRemoveWaypointLabel".Translate(),
                defaultDesc = "CommandRemoveWaypointDesc".Translate(),
                icon = TexCommand.RemoveRoutePlannerWaypoint,
                action = () => Cancel(),
            };
            if (DebugSettings.godMode)
                yield return new Command_Action
                {
                    defaultLabel = "Dev: Instant build",
                    action = Finish,
                };
        }

        public void AddNumOfCheapestThings(List<Thing> allItems, Dictionary<Thing, int> toBeConsumed,
            ThingDefCountClass thingCount)
        {
            var matchingItemsPriceSorted = allItems.FindAll(thing =>
                thing.def == thingCount.thingDef &&
                (!toBeConsumed.TryGetValue(thing, out var value) || thing.stackCount > value));
            matchingItemsPriceSorted.Sort(CompareMarketValue);
                ;
            var remainingCount = thingCount.count;
            foreach (var thing in matchingItemsPriceSorted)
            {
                toBeConsumed.TryGetValue(thing, out var currentTaken);
                var numberToTake = Math.Min(thing.stackCount - currentTaken, remainingCount);
                if (currentTaken != 0)
                    toBeConsumed[thing] += numberToTake;
                else
                    toBeConsumed.Add(thing, numberToTake);
                remainingCount -= numberToTake;
                if (remainingCount <= 0)
                    break;
            }

            if (remainingCount > 0)
                Log.ErrorOnce(
                    "JecsTools.WorldObjectBluePrint.AddNumOfCheapestThings: ran out of items before finding required amount. This should be checked before",
                    9534712);
        }

        public void AddNumOfCheapestStuff(List<Thing> allItems, Dictionary<Thing, int> toBeConsumed,
            StuffCategoryCountClass stuffCount)
        {
            var matchingItemsPriceSorted = allItems.FindAll(thing =>
                ((thing.Stuff?.stuffProps ?? thing.def.stuffProps)?.categories?.Contains(stuffCount.stuffCatDef) ?? false) &&
                (!toBeConsumed.TryGetValue(thing, out var value) || thing.stackCount > value));
            matchingItemsPriceSorted.Sort(CompareMarketValue);
            var remainingCount = stuffCount.count;
            foreach (var thing in matchingItemsPriceSorted)
            {
                toBeConsumed.TryGetValue(thing, out var currentTaken);
                var numberToTake = Math.Min(thing.stackCount - currentTaken, remainingCount);
                if (currentTaken != 0)
                    toBeConsumed[thing] += numberToTake;
                else
                    toBeConsumed.Add(thing, numberToTake);
                remainingCount -= numberToTake;
                if (remainingCount <= 0)
                    break;
            }

            if (remainingCount > 0)
                Log.ErrorOnce(
                    "JecsTools.WorldObjectBluePrint.AddNumOfCheapestStuff: ran out of items before finding required amount. This should be checked before",
                    9534713);
        }

        private static int CompareMarketValue(Thing x, Thing y)
        {
            return x.GetStatValue(StatDefOf.MarketValue).CompareTo(y.GetStatValue(StatDefOf.MarketValue));
        }

        public virtual bool CheckAndConsumeResources(Caravan c, WorldObjectRecipeDef recipe,
            bool consumeResources = true)
        {
            if (resourcesSupplied)
                return true;
            var toBeConsumed = new Dictionary<Thing, int>();
            var caravanInv = CaravanInventoryUtility.AllInventoryItems(c);

            if (caravanInv.NullOrEmpty())
                return false;

            var passed = true;
            var allItems = CaravanInventoryUtility.AllInventoryItems(c);
            var missingResourcesMessage = new StringBuilder();

            var costList = recipe?.costList;
            if (costList != null)
            {
                foreach (var thingCount in costList)
                {
                    var thingsFound = 0;
                    foreach (var thing in allItems)
                    {
                        if (thing.def == thingCount.thingDef)
                            thingsFound += thing.stackCount;
                    }
                    if (thingsFound >= thingCount.count)
                        AddNumOfCheapestThings(allItems, toBeConsumed, thingCount);
                    else
                    {
                        missingResourcesMessage.AppendLine("JecsTools_WorldObjectConst_NotEnoughThings"
                            .Translate(thingsFound, thingCount.count, thingCount.thingDef.LabelCap));
                        passed = false;
                    }
                }
            }

            var stuffCostList = recipe?.stuffCostList;
            if (stuffCostList != null)
            {
                foreach (var stuffCount in stuffCostList)
                {
                    //Ensure I find stuffProps either under Stuff or def if any
                    var stuffFound = 0;
                    foreach (var thing in allItems)
                    {
                        if ((thing.Stuff?.stuffProps ?? thing.def.stuffProps)?.categories?.Contains(stuffCount.stuffCatDef) ?? false)
                            stuffFound += thing.stackCount - (toBeConsumed.TryGetValue(thing, out var value) ? value : 0);
                    }
                    if (stuffFound >= stuffCount.count)
                        AddNumOfCheapestStuff(allItems, toBeConsumed, stuffCount);
                    else
                    {
                        missingResourcesMessage.AppendLine("JecsTools_WorldObjectConst_NotEnoughStuff"
                            .Translate(stuffFound, stuffCount.count, stuffCount.stuffCatDef.LabelCap));
                        passed = false;
                    }
                }
            }

            if (!passed && consumeResources)
            {
                missingResourcesMessage.Insert(0, "JecsTools_WorldObjectConst_NotEnoughResources"
                                                      .Translate(recipe.LabelCap) + Environment.NewLine);
                Messages.Message(missingResourcesMessage.ToString(), MessageTypeDefOf.RejectInput);
                return false;
            }

            if (passed && consumeResources)
                if (!ConsumeResources(c, toBeConsumed))
                    return false;

            return passed;
        }

        public bool ConsumeResources(Caravan c, Dictionary<Thing, int> toBeConsumed)
        {
            foreach (var (t, count) in toBeConsumed)
            {
                var ownerOf = CaravanInventoryUtility.GetOwnerOf(c, t);
                if (ownerOf == null)
                {
                    Log.Error("Could not find owner of " + t);
                    return false;
                }
                if (count == t.stackCount)
                {
                    ownerOf.inventory.innerContainer.Remove(t);
                    ConsumedResources.Add(t);
                    //t.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    ConsumedResources.Add(t.SplitOff(count)); //Destroy(DestroyMode.Vanish);
                }
                c.RecacheImmobilizedNow();
                c.RecacheDaysWorthOfFood();
            }
            resourcesSupplied = true;
            return true;
        }

        public override string GetInspectString()
        {
            var s = new StringBuilder();
            s.Append(base.GetInspectString());
            if (Recipe != null)
            {
                if (Recipe.costList != null)
                    foreach (var t in Recipe.costList)
                    {
                        var amtFilled = resourcesSupplied ? t.count.ToString() : "0";
                        s.AppendLine(t.thingDef.LabelCap + ": " + amtFilled + " / " + t.count);
                    }
                if (Recipe.stuffCostList != null)
                    foreach (var stuffCat in Recipe.stuffCostList)
                    {
                        var amtFilled = resourcesSupplied ? stuffCat.count.ToString() : "0";
                        s.AppendLine(stuffCat.stuffCatDef.LabelCap + ": " + amtFilled + " / " + stuffCat.count);
                    }
            }
            return s.ToString().TrimEndNewlines();
        }
    }
}
