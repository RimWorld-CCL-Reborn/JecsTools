using System;
using System.Collections.Generic;
using System.Linq;
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
            Scribe_Values.Look(ref resourcesSupplied, "resourcesSupplied", false);
            Scribe_Collections.Look(ref consumedResources, "consumedResources", LookMode.Reference);
        }

        public virtual void Finish()
        {
            if (!ConsumedResources.NullOrEmpty())
            {
                var iterable = new HashSet<Thing>(ConsumedResources);
                foreach (var t in iterable)
                {
                    ConsumedResources.Remove(t);
                    t.Destroy(DestroyMode.Vanish);
                }
            }
        }


        public virtual bool Cancel()
        {
            var c = Find.WorldObjects.PlayerControlledCaravanAt(Tile);
            if (ConsumedResources != null && ConsumedResources.Count() > 0)
                if (c != null)
                {
                    var temp = new HashSet<Thing>(ConsumedResources);
                    foreach (var t in temp)
                    {
                        var pawn2 = CaravanInventoryUtility.FindPawnToMoveInventoryTo(t, c.PawnsListForReading, null,
                            null);
                        if (pawn2 == null)
                        {
                            Log.Error("Could not find pawn to move bought thing to (bought by player). thing=" + t);
                            t.Destroy(DestroyMode.Vanish);
                        }
                        else if (!pawn2.inventory.innerContainer.TryAdd(t, true))
                        {
                            Log.Error("Could not add item to inventory.");
                            t.Destroy(DestroyMode.Vanish);
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
                    var window2 = Dialog_MessageBox.CreateConfirmation(
                        "ConfirmAbandonItemDialog".Translate(string.Join(", ", labels) + " " +
                                                             "JecsTools_WorldObjectConst_AbandonReason".Translate()),
                        delegate
                        {
                            //Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(caravan, t);
                            //if (ownerOf == null)
                            //{
                            //    Log.Error("Could not find owner of " + t);
                            //    return;
                            //}
                            //ownerOf.inventory.innerContainer.Remove(t);
                            var temp = new HashSet<Thing>(ConsumedResources);
                            foreach (var t in temp)
                            {
                                t.Destroy(DestroyMode.Vanish);
                                ConsumedResources.Remove(t);
                            }
                            didCancel = true;
                        }, true, null);
                    Find.WindowStack.Add(window2);
                    return didCancel;
                }
            return true;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (var f in base.GetFloatMenuOptions(caravan))
                yield return f;
            if (ConstructJobDef != null)
            {
                if (CheckAndConsumeResources(caravan, Recipe, false))
                    yield return new FloatMenuOption("CreateNewZone".Translate(Recipe.FinishedThing.label), () =>
                    {
                        var t = Find.World.GetComponent<CaravanJobGiver>().Tracker(caravan);
                        var curBlueprint = this;
                        while (curBlueprint != null)
                        {
                            t.jobQueue.EnqueueLast(new CaravanJob(ConstructJobDef, curBlueprint));
                            curBlueprint = curBlueprint.NextBlueprint;
                        }
                    }, MenuOptionPriority.Default);
                else
                    yield return new FloatMenuOption(
                        "CreateNewZone".Translate(Recipe.FinishedThing.label) + " (" + "MissingMaterials".Translate() +
                        ")",
                        null, MenuOptionPriority.Default, null, null, 0f, null, null);
                yield return new FloatMenuOption("VisitSite".Translate(Label),
                    delegate { caravan.pather.StartPath(Tile, null, true); }, MenuOptionPriority.Default, null, null,
                    0f, null, this);
                if (Prefs.DevMode)
                {
                    yield return new FloatMenuOption(
                        "CreateNewZone".Translate(Recipe.FinishedThing.label) + " (Dev: instantly)", delegate
                        {
                            //this.caravan.Tile = this.<> f__this.Tile;
                            //this.caravan.pather.StopDead();
                            //new CaravanArrivalAction_VisitSettlement(this.<> f__this).Arrived(this.caravan);
                            Finish();
                        }, MenuOptionPriority.Default, null, null, 0f, null, this);
                    yield return new FloatMenuOption(
                        "VisitSite".Translate(Recipe.FinishedThing.label) + " (Dev: instantly)", delegate
                        {
                            caravan.Tile = Tile;
                            caravan.pather.StopDead();
                            //new CaravanArrivalAction_VisitSettlement(this.<> f__this).Arrived(this.caravan);
                            //this.FinishConstruction();
                        }, MenuOptionPriority.Default, null, null, 0f, null, this);
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
                action = delegate { Cancel(); }
            };
            if (DebugSettings.godMode)
                yield return new Command_Action
                {
                    defaultLabel = "Dev: Instant build",
                    action = delegate { Finish(); }
                };
        }


        public virtual bool CheckAndConsumeResources(Caravan c, WorldObjectRecipeDef recipe,
            bool consumeResources = true)
        {
            if (resourcesSupplied) return true;
            var toBeConsumed = new Dictionary<Thing, int>();
            var caravanInv = CaravanInventoryUtility.AllInventoryItems(c);

            if (caravanInv == null || caravanInv.Count() == 0)
                return false;

            if (!recipe.stuffCategories.NullOrEmpty() && recipe.costStuffCount > 0)
            {
                var passed = false;
                foreach (var t in recipe.stuffCategories)
                {
                    var yy = CaravanInventoryUtility.AllInventoryItems(c)
                        .FindAll(x => x?.def?.stuffProps?.categories?.Contains(t) ?? false);
                    if (!yy.NullOrEmpty())
                    {
                        var totalCount = yy.Sum(x => x.stackCount);
                        if (totalCount - recipe.costStuffCount >= 0)
                        {
                            totalCount -= totalCount - recipe.costStuffCount;
                            passed = true;
                            foreach (var y in yy)
                            {
                                if (totalCount > 0)
                                {
                                    var math = Math.Min(y.stackCount, totalCount);
                                    toBeConsumed.Add(y, math);
                                    Log.Message(y + " x" + math);
                                }
                                totalCount -= y.stackCount;
                            }
                        }
                    }
                }
                if (!passed && consumeResources)
                {
                    var categories = new string[recipe.stuffCategories.Count];
                    for (var i = 0; i < recipe.stuffCategories.Count; i++)
                        categories[i] = recipe.stuffCategories[i].label;

                    Messages.Message(
                        "JecsTools_WorldObjectConst_NotEnoughStuff".Translate(string.Join(", ", categories),
                            recipe.costStuffCount), MessageTypeDefOf.RejectInput);
                    return false;
                }
            }
            if (consumeResources)
                if (!ConsumeResources(c, toBeConsumed))
                    return false;

            return true;
        }

        public bool ConsumeResources(Caravan c, Dictionary<Thing, int> toBeConsumed)
        {
            if (toBeConsumed != null && toBeConsumed.Count > 0)
                foreach (var pair in toBeConsumed)
                {
                    var t = pair.Key;
                    var count = pair.Value;
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
                if (!Recipe.costList.NullOrEmpty())
                    foreach (var t in Recipe.costList)
                    {
                        var amtFilled = resourcesSupplied ? t.count.ToString() : "0";
                        s.AppendLine(t.thingDef.LabelCap + ": " + amtFilled + " / " + t.count);
                    }
                if (Recipe.stuffCategories != null && Recipe.costStuffCount > 0)
                {
                    var categories = new string[Recipe.stuffCategories.Count];
                    for (var i = 0; i < Recipe.stuffCategories.Count; i++)
                        categories[i] = Recipe.stuffCategories[i].LabelCap;
                    var amtFilled = resourcesSupplied ? Recipe.costStuffCount.ToString() : "0";
                    s.AppendLine(string.Join(", ", categories) + " " +
                                 "JecsTools_WorldObjectConst_ResourcesNeeded".Translate(
                                     amtFilled + " / " + Recipe.costStuffCount));
                }
            }
            return s.ToString().TrimEndNewlines();
        }
    }
}