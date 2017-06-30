using System.Collections.Generic;

namespace CompSlotLoadable
{
    public class SlotLoadable : Thing, IThingHolder
    {
        #region Variables

        //Exposable Variables
        //private Thing slotOccupant;
        private ThingOwner slot;

        //Settable variables
        public List<ThingDef> slottableThingDefs;
        //
        //Spawn variables
        public Thing owner;

        #endregion Variables
        
        //Spawn methods
        public SlotLoadable()
        {

        }

        public SlotLoadable(Thing newOwner)
        {
            Log.Message("Slot started");
            SlotLoadableDef def = this.def as SlotLoadableDef;
            this.slottableThingDefs = def.slottableThingDefs;
            this.owner = newOwner;
            ThingIDMaker.GiveIDTo(this);
            this.slot = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public SlotLoadable(SlotLoadableDef xmlDef, Thing newOwner)
        {
            Log.Message("Slot Loaded");
            this.def = xmlDef;
            this.slottableThingDefs = xmlDef.slottableThingDefs;
            this.owner = newOwner;
            ThingIDMaker.GiveIDTo(this);
            this.slot = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public Texture2D SlotIcon()
        {
            if (this.SlotOccupant != null)
            {
                if (this.SlotOccupant.def != null)
                {
                    return this.SlotOccupant.def.uiIcon;
                }
            }
            return null;
        }

        public Color SlotColor()
        {
            if (this.SlotOccupant != null)
            {
                if (this.SlotOccupant.def != null)
                {
                    return this.SlotOccupant.def.graphic.Color;
                }
            }
            return Color.white;
        }

        public bool IsEmpty()
        {
            if (this.SlotOccupant != null) return false;
            return true;
        }

        public bool CanLoad(ThingDef defType)
        {
            if (this.slottableThingDefs != null)
            {
                if (this.slottableThingDefs.Count > 0)
                {
                    if (this.slottableThingDefs.Contains(defType))
                    {
                        //Log.Message("Can Load: " + defType.ToString());
                        return true;
                    }
                }
            }
            return false;
        }

        #region IThingOwnerOwner

        public Map GetMap() => this.ParentMap;

        public ThingOwner GetInnerContainer() => this.slot;

        public IntVec3 GetPosition() => this.ParentLoc;

        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());

        public ThingOwner GetDirectlyHeldThings() => this.slot;

        #endregion IThingOwnerOwner

        #region Properties
        //Get methods



        public Thing SlotOccupant
        {
            get
            {
                if (this.slot.Count == 0)
                {
                    return null;
                }
                if (this.slot.Count > 1)
                {
                    Log.Error("ContainedThing used on a DropPodInfo holding > 1 thing.");
                }
                return this.slot[0];
            }
            set
            {
                this.slot.Clear();
                if (value.holdingOwner != null) {
                    Thing takenThing = value.holdingOwner.Take(value, 1);
                    value.DeSpawn();
                    this.slot.TryAdd(takenThing, true);
                }
                else
                {
                    this.slot.TryAdd(value, true);
                }
            }

            //get
            //{
            //    return slotOccupant;
            //}
            //set
            //{
            //    slotOccupant = value;
            //}
        }
        public ThingOwner Slot
        {
            get => this.slot;
            set => this.slot = value;
        }

        public Pawn Holder
        {
            get
            {
                Pawn result = null;
                if (this.owner != null)
                {
                    CompEquippable eq = this.owner.TryGetComp<CompEquippable>();
                    if (eq != null)
                    {
                        if (eq.PrimaryVerb != null)
                        {
                            Pawn pawn = eq.PrimaryVerb.CasterPawn;
                            if (pawn != null)
                            {
                                if (pawn.Spawned)
                                {
                                    result = pawn;
                                }
                            }
                        }
                    }
                }
                return result;
            }
        }
        
        public Map ParentMap
        {
            get
            {
                Map result = null;
                //Does our parent have an equippable class?
                //Use that to find a pawn location if it's equipped.
                if (this.owner != null)
                {
                    if (this.Holder != null)
                    {
                        return this.Holder.Map;
                    }
                    return this.owner.Map;
                }
                return result;
            }
        }

        public IntVec3 ParentLoc
        {
            get
            {
                IntVec3 result = IntVec3.Invalid;
                //Does our parent have an equippable class?
                //Use that to find a pawn location if it's equipped.
                if (this.owner != null)
                {
                    if (this.Holder != null)
                    {
                        return this.Holder.Position;
                    }
                    return this.owner.Position;
                }
                return result;
            }
        }

        public List<ThingDef> SlottableTypes => this.slottableThingDefs;

        #endregion Properties

        #region Methods

        public virtual bool TryLoadSlot(Thing thingToLoad, bool emptyIfFilled = false)
        {
            //Log.Message("TryLoadSlot Called");
            if ((this.SlotOccupant != null && emptyIfFilled) || this.SlotOccupant == null)
            {
                TryEmptySlot();
                if (thingToLoad != null)
                {
                    if (this.slottableThingDefs != null)
                    {
                        if (this.slottableThingDefs.Contains(thingToLoad.def))
                        {
                            this.SlotOccupant = thingToLoad;
                            //slot.TryAdd(thingToLoad, false);
                            if (((SlotLoadableDef)this.def).doesChangeColor)
                            {
                                this.owner.Notify_ColorChanged();
                            }
                            return true;
                        }
                    }
                }
            }
            else
            {
                Messages.Message(string.Format(StringOf.ExceptionSlotAlreadyFilled, new object[]{
                    this.owner.Label
                }), MessageSound.RejectInput);
            }
            return false;
        }

        public virtual bool TryEmptySlot()
        {
            if (!CanEmptySlot()) return false;
            return this.slot.TryDropAll(this.ParentLoc, this.ParentMap, ThingPlaceMode.Near);
        }

        public virtual bool CanEmptySlot() => true;

        #endregion Methods

        public override void ExposeData()
        {

            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (this.thingIDNumber == -1)
                {
                    ThingIDMaker.GiveIDTo(this);
                }
            }
            Scribe_Deep.Look<ThingOwner>(ref this.slot, "slot", new object[]
            {
                this
            });
            Scribe_Collections.Look<ThingDef>(ref this.slottableThingDefs, "slottableThingDefs", LookMode.Undefined, new object[0]);
            Scribe_References.Look<Thing>(ref this.owner, "owner");
            //Scribe_References.Look<Thing>(ref this.slotOccupant, "slotOccupant");
        }
    }
}
