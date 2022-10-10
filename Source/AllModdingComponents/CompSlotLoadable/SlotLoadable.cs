using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CompSlotLoadable
{
    // Based off ActiveDropPodInfo.
    public class SlotLoadable : Thing, IThingHolder
    {
        //Spawn methods
        public SlotLoadable()
        {
        }

        public SlotLoadable(Thing newOwner)
        {
            //Log.Message("Slot started");
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

        public SlotLoadableDef Def => def as SlotLoadableDef;

        public Texture2D SlotIcon() => SlotOccupant?.def?.uiIcon;

        public Color SlotColor() => SlotOccupant?.def?.graphic.Color ?? Color.white;

        public bool IsEmpty() => SlotOccupant == null;

        public bool CanLoad(ThingDef defType) => SlottableTypes.Contains(defType);

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
                if (thingIDNumber == -1)
                    ThingIDMaker.GiveIDTo(this);
            Scribe_Deep.Look(ref slot, nameof(slot), this);
            Scribe_References.Look(ref owner, nameof(owner));

            // Only save slottableThingDefs if it isn't Def.slottableThingDefs (otherwise, save null) to save space.
            var defSlottableThingDefs = Def?.slottableThingDefs;
            var savedSlottableThingDefs = slottableThingDefs;
            if (defSlottableThingDefs != null && slottableThingDefs != null &&
                defSlottableThingDefs.SequenceEqual(slottableThingDefs))
                savedSlottableThingDefs = null;
            Scribe_Collections.Look(ref savedSlottableThingDefs, nameof(slottableThingDefs), LookMode.Def);
            if (slottableThingDefs != null)
                slottableThingDefs = savedSlottableThingDefs;
        }

        #region Variables

        //Exposable Variables
        private ThingOwner slot;
        private List<ThingDef> slottableThingDefs;

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
                    Log.Error("ContainedThing used on a SlotLoadable holding > 1 thing.");
                return slot[0];
            }
            set
            {
                slot.Clear();
                if (value.holdingOwner != null)
                {
                    var takenThing = value.holdingOwner.Take(value, 1);
                    value.DeSpawn();
                    slot.TryAdd(takenThing);
                }
                else
                {
                    slot.TryAdd(value);
                }
            }
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
                    if (Holder is Pawn holder)
                        return holder.Map;
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
                    if (Holder is Pawn holder)
                        return holder.Position;
                    return owner.Position;
                }
                return IntVec3.Invalid;
            }
        }

        public List<ThingDef> SlottableTypes
        {
            // def isn't available during constructor, so slottableThingDefs is lazily initialized here.
            get => slottableThingDefs ??= Def?.slottableThingDefs ?? new List<ThingDef>();
            set => slottableThingDefs = value;
        }

        #endregion Properties

        #region Methods

        public virtual bool TryLoadSlot(Thing thingToLoad, bool emptyIfFilled = false)
        {
            //Log.Message("TryLoadSlot Called");
            if (SlotOccupant == null || emptyIfFilled)
            {
                TryEmptySlot();
                if (thingToLoad != null && CanLoad(thingToLoad.def))
                {
                    SlotOccupant = thingToLoad;
                    //slot.TryAdd(thingToLoad, false);
                    if (Def?.doesChangeColor ?? false)
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
            return CanEmptySlot() && slot.TryDropAll(ParentLoc, ParentMap, ThingPlaceMode.Near);
        }

        public virtual bool CanEmptySlot()
        {
            return true;
        }

        #endregion Methods
    }
}
