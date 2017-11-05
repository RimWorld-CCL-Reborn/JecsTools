using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace JecsTools
{
    public abstract class WorldObjectBlueprint : WorldObject
    {
        public abstract WorldObjectRecipeDef Recipe { get; }
        public abstract CaravanJobDef ConstructJobDef { get; }
        public bool resourcesSupplied = false;

        public virtual WorldObjectBlueprint NextBlueprint => null;
        public virtual WorldObjectBlueprint PrevBlueprint => null;

        private List<Thing> consumedResources = new List<Thing>();
        public List<Thing> ConsumedResources { get => consumedResources; set => consumedResources = value; }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.resourcesSupplied, "resourcesSupplied", false);
            Scribe_Collections.Look<Thing>(ref this.consumedResources, "consumedResources", LookMode.Reference);
        }

        public virtual void Finish()
        {
            if (!ConsumedResources.NullOrEmpty())
            {
                HashSet<Thing> iterable = new HashSet<Thing>(ConsumedResources);
                foreach (Thing t in iterable)
                {
                    ConsumedResources.Remove(t);
                    t.Destroy(DestroyMode.Vanish);
                }
            }
        }

        


        public virtual bool Cancel()
        {
            Caravan c = Find.WorldObjects.PlayerControlledCaravanAt(this.Tile);
            if (ConsumedResources != null && ConsumedResources.Count() > 0)
            {
                if (c != null)
                {
                    HashSet<Thing> temp = new HashSet<Thing>(ConsumedResources);
                    foreach (Thing t in temp)
                    {
                        Pawn pawn2 = CaravanInventoryUtility.FindPawnToMoveInventoryTo(t, c.PawnsListForReading, null, null);
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
                    bool didCancel = false;
                    string[] labels = new string[ConsumedResources.Count];
                    for (int i = 0; i < ConsumedResources.Count; i++)
                        labels[i] = ConsumedResources[i].Label;
                    Dialog_MessageBox window2 = Dialog_MessageBox.CreateConfirmation("ConfirmAbandonItemDialog".Translate(new object[]
                    {
                    string.Join(", ", labels) + " " + "JecsTools_WorldObjectConst_AbandonReason".Translate()
                    }), delegate
                    {
                        //Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(caravan, t);
                        //if (ownerOf == null)
                        //{
                        //    Log.Error("Could not find owner of " + t);
                        //    return;
                        //}
                        //ownerOf.inventory.innerContainer.Remove(t);
                        HashSet<Thing> temp = new HashSet<Thing>(ConsumedResources);
                        foreach (Thing t in temp)
                        {
                            t.Destroy(DestroyMode.Vanish);
                            ConsumedResources.Remove(t);
                        }
                        didCancel = true;
                    }, true, null);
                    Find.WindowStack.Add(window2);
                    return didCancel;
                }
            }
            return true;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption f in base.GetFloatMenuOptions(caravan))
                yield return f;
            if (ConstructJobDef != null)
            {
                if (this.CheckAndConsumeResources(caravan, Recipe, false))
                {
                    yield return new FloatMenuOption("CreateNewZone".Translate(Recipe.FinishedThing.label), () =>
                    {
                        Caravan_JobTracker t = Find.World.GetComponent<JecsTools.CaravanJobGiver>().Tracker(caravan);
                        WorldObjectBlueprint curBlueprint = this;
                        while (curBlueprint != null)
                        {
                            t.jobQueue.EnqueueLast(new JecsTools.CaravanJob(ConstructJobDef, curBlueprint));
                            curBlueprint = curBlueprint.NextBlueprint;
                        }
                    }, MenuOptionPriority.Default);
                }
                else
                {
                    yield return new FloatMenuOption("CreateNewZone".Translate(Recipe.FinishedThing.label) + " (" + "MissingMaterials".Translate() + ")",
                        null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    
                }
                yield return new FloatMenuOption("VisitSite".Translate(this.Label), delegate
                {
                    caravan.pather.StartPath(this.Tile, null, true);
                }, MenuOptionPriority.Default, null, null, 0f, null, this);
                if (Prefs.DevMode)
                {
                    yield return new FloatMenuOption("CreateNewZone".Translate(Recipe.FinishedThing.label) + " (Dev: instantly)", delegate
                    {
                        //this.caravan.Tile = this.<> f__this.Tile;
                        //this.caravan.pather.StopDead();
                        //new CaravanArrivalAction_VisitSettlement(this.<> f__this).Arrived(this.caravan);
                        this.Finish();
                    }, MenuOptionPriority.Default, null, null, 0f, null, this);
                    yield return new FloatMenuOption("VisitSite".Translate(Recipe.FinishedThing.label) + " (Dev: instantly)", delegate
                    {
                        caravan.Tile = this.Tile;
                        caravan.pather.StopDead();
                        //new CaravanArrivalAction_VisitSettlement(this.<> f__this).Arrived(this.caravan);
                        //this.FinishConstruction();
                    }, MenuOptionPriority.Default, null, null, 0f, null, this);
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            yield return new Command_Action
            {
                defaultLabel = "CommandRemoveWaypointLabel".Translate(),
                defaultDesc = "CommandRemoveWaypointDesc".Translate(),
                icon = TexCommand.RemoveRoutePlannerWaypoint,
                action = delegate
                {
                    Cancel();
                }
            };
            if (DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Dev: Instant build",
                    action = delegate
                    {
                        this.Finish();
                    }
                };
            }
        }


        public virtual bool CheckAndConsumeResources(Caravan c, WorldObjectRecipeDef recipe, bool consumeResources = true)
        {
            if (resourcesSupplied) return true;
            Dictionary<Thing, int> toBeConsumed = new Dictionary<Thing, int>();
            List<Thing> caravanInv = CaravanInventoryUtility.AllInventoryItems(c);

            if (caravanInv == null || caravanInv.Count() == 0)
            {
                return false;
            }

            if (!recipe.stuffCategories.NullOrEmpty() && recipe.costStuffCount > 0)
            {
                bool passed = false;
                foreach (StuffCategoryDef t in recipe.stuffCategories)
                {

                    List<Thing> yy = CaravanInventoryUtility.AllInventoryItems(c).FindAll(x => x?.def?.stuffProps?.categories?.Contains(t) ?? false);
                    if (!yy.NullOrEmpty())
                    {
                        int totalCount = yy.Sum(x => x.stackCount);
                        if (totalCount - recipe.costStuffCount >= 0)
                        {
                            totalCount -= totalCount - recipe.costStuffCount;
                            passed = true;
                            foreach (Thing y in yy)
                            {
                                if (totalCount > 0)
                                {
                                    int math = Math.Min(y.stackCount, totalCount);
                                    toBeConsumed.Add(y, math);
                                    Log.Message(y.ToString() + " x" + math);
                                }
                                totalCount -= y.stackCount;
                            }
                        }

                    }
                }
                if (!passed && consumeResources)
                {
                    string[] categories = new string[recipe.stuffCategories.Count];
                    for (int i = 0; i < recipe.stuffCategories.Count; i++)
                        categories[i] = recipe.stuffCategories[i].label;

                    Messages.Message("JecsTools_WorldObjectConst_NotEnoughStuff".Translate(new object[]
                    { string.Join(", ", categories), recipe.costStuffCount}), MessageTypeDefOf.RejectInput);
                    return false;
                }
            }
            if (consumeResources)
            {
                if (!ConsumeResources(c, toBeConsumed))
                {
                    return false;
                }
            }
                
            return true;
        }
        public bool ConsumeResources(Caravan c, Dictionary<Thing, int> toBeConsumed)
        {

            if (toBeConsumed != null && toBeConsumed.Count > 0)
            {

                foreach (KeyValuePair<Thing, int> pair in toBeConsumed)
                {
                    Thing t = pair.Key;
                    int count = pair.Value;
                    Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(c, t);
                    if (ownerOf == null)
                    {
                        Log.Error("Could not find owner of " + t);
                        return false;
                    }
                    if (count == t.stackCount)
                    {
                        ownerOf.inventory.innerContainer.Remove(t);
                        this.ConsumedResources.Add(t);
                        //t.Destroy(DestroyMode.Vanish);
                    }
                    else
                    {
                        this.ConsumedResources.Add(t.SplitOff(count)); //Destroy(DestroyMode.Vanish);
                    }
                    c.RecacheImmobilizedNow();
                    c.RecacheDaysWorthOfFood();
                }
            }
            resourcesSupplied = true;
            return true;
        }

        public override string GetInspectString()
        {
            StringBuilder s = new StringBuilder();
            s.Append(base.GetInspectString());
            if (Recipe != null)
            {
                if (!Recipe.costList.NullOrEmpty())
                {
                    foreach (ThingCountClass t in Recipe.costList)
                    {
                        string amtFilled = (resourcesSupplied) ? t.count.ToString() : "0";
                        s.AppendLine(t.thingDef.LabelCap + ": " + amtFilled + " / " + t.count);
                    }
                }
                if (Recipe.stuffCategories != null && Recipe.costStuffCount > 0)
                {
                    string[] categories = new string[Recipe.stuffCategories.Count];
                    for (int i =0; i< Recipe.stuffCategories.Count; i++)
                    {
                        categories[i] = Recipe.stuffCategories[i].LabelCap;
                    }
                    string amtFilled = (resourcesSupplied) ? Recipe.costStuffCount.ToString() : "0";
                    s.AppendLine(string.Join(", ", categories) + " " + "JecsTools_WorldObjectConst_ResourcesNeeded".Translate(amtFilled + " / " + Recipe.costStuffCount.ToString()));
                }
            }
            return s.ToString().TrimEndNewlines();
        }

    }
}
