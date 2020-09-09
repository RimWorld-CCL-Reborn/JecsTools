using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CompSlotLoadable
{
    public class SlotLoadable : Thing, IThingHolder
    {
        //Spawn methods
        public SlotLoadable()
        {
        }

        public SlotLoadable(Thing newOwner)
        {
            //Log.Message("Slot started");
            var def = this.def as SlotLoadableDef;
            slottableThingDefs = def.slottableThingDefs;
            owner = newOwner;
            ThingIDMaker.GiveIDTo(this);
            slot = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public SlotLoadable(SlotLoadableDef xmlDef, Thing newOwner)
        {
            //Log.Message("Slot Loaded");
            def = xmlDef;
            slottableThingDefs = xmlDef.slottableThingDefs;
            owner = newOwner;
            ThingIDMaker.GiveIDTo(this);
            slot = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public Texture2D SlotIcon() => SlotOccupant?.def?.uiIcon;

        public Color SlotColor() => SlotOccupant?.def?.graphic.Color ?? Color.white;

        public bool IsEmpty() => SlotOccupant == null;

        public bool CanLoad(ThingDef defType) => slottableThingDefs?.Contains(defType) ?? false;

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
                if (thingIDNumber == -1)
                    ThingIDMaker.GiveIDTo(this);
            Scribe_Deep.Look(ref slot, "slot", this);
            Scribe_Collections.Look(ref slottableThingDefs, "slottableThingDefs", LookMode.Undefined);
            Scribe_References.Look(ref owner, "owner");
            //Scribe_References.Look(ref this.slotOccupant, "slotOccupant");
        }

        #region Variables

        //Exposable Variables
        //private Thing slotOccupant;
        private ThingOwner slot;

        //Settable variables
        public List<ThingDef> slottableThingDefs;

        //
        //Spawn variables
        public Thing owner;
        private CompSlotLoadable parentComp;

        #endregion Variables

        #region IThingOwnerOwner

        public Map GetMap() => ParentMap;

        public ThingOwner GetInnerContainer() => slot;

        public IntVec3 GetPosition() => ParentLoc;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings() => slot;

        #endregion IThingOwnerOwner

        #region Properties

        //Get methods

        // XXX: Not sure of all the ways SlotLoadable is initialized (like whether client code does it itself),
        // but they should all have parent CompSlotLoadable, which is lazily initialized here from owner.
        public CompSlotLoadable ParentComp => parentComp ??= owner?.TryGetCompSlotLoadable();

        public Thing SlotOccupant
        {
            get
            {
                if (slot.Count == 0)
                    return null;
                if (slot.Count > 1)
                    Log.Error("ContainedThing used on a DropPodInfo holding > 1 thing.");
                return slot[0];
            }
            set
            {
                slot.Clear();
                if (value.holdingOwner != null)
                {
                    var takenThing = value.holdingOwner.Take(value, 1);
                    value.DeSpawn();
                    slot.TryAdd(takenThing, true);
                }
                else
                {
                    slot.TryAdd(value, true);
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
            get => slot;
            set => slot = value;
        }

        public Pawn Holder => ParentComp?.GetPawn is Pawn pawn && pawn.Spawned ? pawn : null;

        public Map ParentMap
        {
            get
            {
                //Does our parent have an equippable class?
                //Use that to find a pawn location if it's equipped.
                if (owner != null)
                {
                    if (Holder != null)
                        return Holder.Map;
                    return owner.Map;
                }
                return null;
            }
        }

        public IntVec3 ParentLoc
        {
            get
            {
                //Does our parent have an equippable class?
                //Use that to find a pawn location if it's equipped.
                if (owner != null)
                {
                    if (Holder != null)
                        return Holder.Position;
                    return owner.Position;
                }
                return IntVec3.Invalid;
            }
        }

        public List<ThingDef> SlottableTypes => slottableThingDefs;

        #endregion Properties

        #region Methods

        public virtual bool TryLoadSlot(Thing thingToLoad, bool emptyIfFilled = false)
        {
            //Log.Message("TryLoadSlot Called");
            if (SlotOccupant != null && emptyIfFilled || SlotOccupant == null)
            {
                TryEmptySlot();
                if (thingToLoad != null)
                    if (slottableThingDefs != null)
                        if (slottableThingDefs.Contains(thingToLoad.def))
                        {
                            SlotOccupant = thingToLoad;
                            //slot.TryAdd(thingToLoad, false);
                            if (((SlotLoadableDef)def).doesChangeColor)
                                owner.Notify_ColorChanged();
                            return true;
                        }
            }
            else
            {
                Messages.Message(string.Format(StringOf.ExceptionSlotAlreadyFilled, owner.Label), MessageTypeDefOf.RejectInput);
            }
            return false;
        }

        public virtual bool TryEmptySlot()
        {
            if (!CanEmptySlot()) return false;
            return slot.TryDropAll(ParentLoc, ParentMap, ThingPlaceMode.Near);
        }

        public virtual bool CanEmptySlot()
        {
            return true;
        }

        #endregion Methods
    }
}
