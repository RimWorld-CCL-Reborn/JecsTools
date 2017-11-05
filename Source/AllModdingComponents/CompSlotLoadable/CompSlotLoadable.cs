using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace CompSlotLoadable
{
    public class CompSlotLoadable : ThingComp
    {
        public bool GizmosOnEquip = true;

        private List<SlotLoadable> slots = new List<SlotLoadable>();
        public List<SlotLoadable> Slots => this.slots;
        private SlotLoadable colorChangingSlot = null;
        public SlotLoadable ColorChangingSlot
        {
            get
            {
                if (this.colorChangingSlot != null) return this.colorChangingSlot;
                if (this.Slots != null)
                {
                    if (this.Slots.Count > 0)
                    {
                        this.colorChangingSlot = this.Slots.FirstOrDefault((SlotLoadable x) => ((SlotLoadableDef)(x.def)).doesChangeColor);
                    }
                }
                return this.colorChangingSlot;

            }
        }


        private SlotLoadable secondColorChangingSlot = null;
        public SlotLoadable SecondColorChangingSlot
        {
            get
            {
                if (this.secondColorChangingSlot != null) return this.secondColorChangingSlot;
                if (this.Slots != null)
                {
                    if (this.Slots.Count > 0)
                    {
                        this.secondColorChangingSlot = this.Slots.FirstOrDefault((SlotLoadable x) => ((SlotLoadableDef)(x.def)).doesChangeSecondColor);
                    }
                }
                return this.colorChangingSlot;

            }
        }

        public List<SlotLoadableDef> SlotDefs
        {
            get
            {
                List<SlotLoadableDef> result = new List<SlotLoadableDef>();
                if (this.slots != null)
                {
                    if (this.slots.Count > 0)
                    {
                        foreach (SlotLoadable slot in this.slots)
                        {
                            result.Add(slot.def as SlotLoadableDef);
                        }
                    }
                }
                return result;
            }
        }

        private bool isInitialized = false;

        private bool isGathering = false;

        public Map GetMap
        {
            get
            {
                Map map = this.parent.Map;
                if (map == null)
                {
                    if (this.GetPawn != null) map = this.GetPawn.Map;
                }
                return map;
            }
        }

        public CompEquippable GetEquippable => this.parent.GetComp<CompEquippable>();

        public Pawn GetPawn => this.GetEquippable.verbTracker.PrimaryVerb.CasterPawn;

        public void Initialize()
        {
            //Log.Message("1");
            if (!this.isInitialized)
            {

                //Log.Message("2");
                this.isInitialized = true;
                if (this.Props != null)
                {

                    //Log.Message("3");
                    if (this.Props.slots != null)
                    {

                        //Log.Message("4");
                        if (this.Props.slots.Count > 0)
                        {

                            //Log.Message("5");
                            foreach (SlotLoadableDef slot in this.Props.slots)
                            {
                                SlotLoadable newSlot = new SlotLoadable(slot, this.parent);
                                //Log.Message("Added Slot");
                                this.slots.Add(newSlot);
                            }
                        }
                    }
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!this.isInitialized) Initialize();
        }

        private void TryCancel(string reason = "")
        {
            Pawn pawn = this.GetPawn;
            if (pawn != null)
            {
                if (pawn.CurJob.def == CompSlotLoadableDefOf.GatherSlotItem)
                {
                    pawn.jobs.StopAll();
                }
                this.isGathering = false;
                //Messages.Message("Cancelling sacrifice. " + reason, MessageSound.Negative);
            }
        }

        private void TryGiveLoadSlotJob(Thing itemToLoad)
        {
            if (this.GetPawn != null)
            {
                if (!this.GetPawn.Drafted)
                {
                    this.isGathering = true;

                    Job job = new Job(CompSlotLoadableDefOf.GatherSlotItem, itemToLoad)
                    {
                        count = 1
                    };
                    this.GetPawn.jobs.TryTakeOrderedJob(job);
                    //GetPawn.jobs.jobQueue.EnqueueFirst(job);
                    //GetPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
                else Messages.Message(string.Format(StringOf.IsDrafted, new object[]
                    {
                        this.GetPawn.Label
                    }), MessageTypeDefOf.RejectInput);
            }
        }

        public bool TryLoadSlot(Thing thing)
        {
            //Log.Message("TryLoadSlot Called");
            this.isGathering = false;
            if (this.slots != null)
            {
                if (this.slots.Count > 0)
                {
                    SlotLoadable loadSlot = this.slots.FirstOrDefault((SlotLoadable x) => x.IsEmpty() && x.CanLoad(thing.def));
                    if (loadSlot == null) loadSlot = this.slots.FirstOrDefault((SlotLoadable y) => y.CanLoad(thing.def));
                    if (loadSlot != null)
                    {
                        if (loadSlot.TryLoadSlot(thing, true))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void ProcessInput(SlotLoadable slot)
        {
            List<ThingDef> loadTypes = new List<ThingDef>();
            List<FloatMenuOption> floatList = new List<FloatMenuOption>();
            if (!this.isGathering)
            {
                Map map = this.GetMap;
                loadTypes = slot.SlottableTypes;
                if (slot.SlotOccupant == null)
                {
                    if (loadTypes != null)
                    {
                        if (loadTypes.Count != 0)
                        {
                            foreach (ThingDef current in loadTypes)
                            {
                                List<Thing> thingsWithDef = new List<Thing>(map.listerThings.AllThings.FindAll((Thing x) => x.def == current));
                                if (thingsWithDef != null)
                                {
                                    if (thingsWithDef.Count > 0)
                                    {
                                        Thing thingToLoad = thingsWithDef.FirstOrDefault((Thing x) => map.reservationManager.CanReserve(this.GetPawn, x));
                                        if (thingToLoad != null)
                                        {
                                            string text = "Load".Translate() + " " + thingToLoad.def.label;
                                            //Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, current);
                                            floatList.Add(new FloatMenuOption(text, delegate
                                            {
                                                this.TryGiveLoadSlotJob(thingToLoad);
                                            }, MenuOptionPriority.Default, null, null, 29f, null, null));
                                        }
                                        else
                                        {
                                            floatList.Add(new FloatMenuOption(string.Format(StringOf.Unavailable, new object[] { current.label }), delegate
                                            {
                                            }, MenuOptionPriority.Default));
                                        }
                                    }
                                    else
                                    {
                                        floatList.Add(new FloatMenuOption(string.Format(StringOf.Unavailable, new object[] { current.label }), delegate
                                        {
                                        }, MenuOptionPriority.Default));
                                    }
                                }
                                else
                                {
                                    floatList.Add(new FloatMenuOption(string.Format(StringOf.Unavailable, new object[] { current.label }), delegate
                                    {
                                    }, MenuOptionPriority.Default));
                                }
                            }
                        }
                        else
                        {
                            floatList.Add(new FloatMenuOption(StringOf.NoLoadOptions, delegate
                            {
                            }, MenuOptionPriority.Default));
                        }
                    }
                }
            }
            else
            {
                //TryCancel();
            }
            if (!slot.IsEmpty())
            {
                string text = string.Format(StringOf.Unload, new object[] { slot.SlotOccupant.Label });
                //Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, current);
                floatList.Add(new FloatMenuOption(text, delegate
                {
                    TryEmptySlot(slot);
                }, MenuOptionPriority.Default, null, null, 29f, null, null));
            }
            Find.WindowStack.Add(new FloatMenu(floatList));
        }

        public virtual void TryEmptySlot(SlotLoadable slot) => slot.TryEmptySlot();

        public virtual IEnumerable<Gizmo> EquippedGizmos()
        {
            if (this.slots != null)
            {
                if (this.slots.Count > 0)
                {
                    if (this.isGathering)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "DesignatorCancel".Translate(),
                            defaultDesc = "DesignatorCancelDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                            action = delegate
                            {
                                this.TryCancel();
                            }
                        };
                    }
                    foreach (SlotLoadable slot in this.slots)
                    {
                        if (slot.IsEmpty())
                        {
                            yield return new Command_Action
                            {
                                defaultLabel = slot.Label,
                                icon = Command.BGTex,
                                defaultDesc = SlotDesc(slot),
                                action = delegate
                                {
                                    this.ProcessInput(slot);
                                }
                            };
                        }
                        else
                        {
                            yield return new Command_Action
                            {
                                defaultLabel = slot.Label,
                                icon = slot.SlotIcon(),
                                defaultDesc = SlotDesc(slot),
                                defaultIconColor = slot.SlotColor(),
                                action = delegate
                                {
                                    this.ProcessInput(slot);
                                }
                            };
                        }
                    }
                }

            }
            yield break;
        }

        public virtual string SlotDesc(SlotLoadable slot)
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine(slot.GetDescription());
            if (!slot.IsEmpty())
            {
                s.AppendLine();
                s.AppendLine(string.Format(StringOf.CurrentlyLoaded, new object[] { slot.SlotOccupant.LabelCap }));
                if (((SlotLoadableDef)slot.def).doesChangeColor)
                {
                    s.AppendLine();
                    s.AppendLine(StringOf.Effects);
                    s.AppendLine("  " + StringOf.ChangesPrimaryColor);
                }
                if (((SlotLoadableDef)slot.def).doesChangeStats)
                {
                    CompSlottedBonus slotBonus = slot.SlotOccupant.TryGetComp<CompSlottedBonus>();
                    if (slotBonus != null)
                    {
                        if (slotBonus.Props != null)
                        {
                            if (slotBonus.Props.statModifiers != null && slotBonus.Props.statModifiers.Count > 0)
                            {
                                s.AppendLine();
                                s.AppendLine(StringOf.StatModifiers);

                                foreach (StatModifier mod in slotBonus.Props.statModifiers)
                                {
                                    float v = DetermineSlottableStatAugment(slot.SlotOccupant,mod.stat);
                                    string modstring = 	mod.stat.ValueToString(v, ToStringNumberSense.Offset);
                                    //Log.Message("Determined slot stat augment "+v+" and made string "+modstring);
                                    s.AppendLine("  " + mod.stat.LabelCap + " " + modstring);
                                    //s.AppendLine("\t" + mod.stat.LabelCap + " " + mod.ToStringAsOffset);
                                }
                                /*
                                //Log.Message("fix this to display statModifiers");
                                List<StatModifier> statMods = slot.SlotOccupant.def.statBases.FindAll(
                                    (StatModifier z) => z.stat.category == StatCategoryDefOf.Weapon ||
                                                        z.stat.category == StatCategoryDefOf.EquippedStatOffsets);
                                if (statMods != null && statMods.Count > 0)
                                {
                                    s.AppendLine();
                                    s.AppendLine("StatModifiers".Translate() + ":");
                                    foreach (StatModifier mod in statMods)
                                    {
                                        s.AppendLine("\t" + mod.stat.LabelCap + " " + mod.ToStringAsOffset);
                                    }
                                }
                                */
                            }
                            DamageDef damageDef = slotBonus.Props.damageDef;
                            if (damageDef != null)
                            {
                                s.AppendLine();
                                s.AppendLine(string.Format(StringOf.DamageType, new object[] { damageDef.LabelCap }));
                            }
                            SlotBonusProps_DefensiveHealChance defHealChance = slotBonus.Props.defensiveHealChance;
                            if (defHealChance != null)
                            {
                                string healText = StringOf.all;
                                if (defHealChance.woundLimit != 0) healText = defHealChance.woundLimit.ToString();
                                s.AppendLine("  " + string.Format(StringOf.DefensiveHealChance, new object[]
                                    {
                                        healText,
                                        defHealChance.chance.ToStringPercent()
                                    }));
                            }
                            SlotBonusProps_VampiricEffect vampChance = slotBonus.Props.vampiricHealChance;
                            if (vampChance != null)
                            {
                                string vampText = StringOf.all;
                                if (vampChance.woundLimit != 0) vampText = defHealChance.woundLimit.ToString();
                                s.AppendLine("  " + string.Format(StringOf.VampiricChance, new object[]
                                    {
                                        vampText,
                                        vampChance.chance.ToStringPercent()
                                    }));
                            }
                        }
                    }

                }
            }
            return s.ToString();
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref this.isInitialized, "isInitialized", false);
            Scribe_Values.Look<bool>(ref this.isGathering, "isGathering", false);
            Scribe_Collections.Look<SlotLoadable>(ref this.slots, "slots", LookMode.Deep, new object[0]);
            base.PostExposeData();
            if (this.slots == null)
            {
                this.slots = new List<SlotLoadable>();
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                //Scribe.writingForDebug = false;
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                //Scribe.writingForDebug = true;
            }
        }


        public CompProperties_SlotLoadable Props => (CompProperties_SlotLoadable)this.props;


        // Grab slots of the thing if they exists. Returns null if none
        public static List<SlotLoadable> GetSlots(Thing someThing) {
            List<SlotLoadable> retval = null;

            if (someThing is ThingWithComps thingWithComps)
            {
                ThingComp comp = thingWithComps.AllComps.FirstOrDefault((ThingComp x) => x is CompSlotLoadable);
                if (comp != null)
                {
                    CompSlotLoadable compSlotLoadable = comp as CompSlotLoadable;

                    if (compSlotLoadable.Slots != null && compSlotLoadable.Slots.Count > 0)
                    { retval = compSlotLoadable.Slots; }
                }
            }

            return retval;
        }


        // Get the thing's modificaiton to stat from it's slots
        public static float CheckThingSlotsForStatAugment(Thing slottedThing, StatDef stat) {
            float retval = 0.0f;
            List<SlotLoadable> slots = CompSlotLoadable.GetSlots(slottedThing);

            if ( slots != null ) {
                foreach ( SlotLoadable slot in slots ) {
                    if (!slot.IsEmpty())
                    {
                        Thing slottable = slot.SlotOccupant;
                        retval += DetermineSlottableStatAugment(slottable ,stat);
                    }
                }
            }
            return retval;
        }

        public static float DetermineSlottableStatAugment(Thing slottable, StatDef stat) {
            float retval =0.0f;
            CompSlottedBonus slotBonus = slottable.TryGetComp<CompSlottedBonus>();
            if (slotBonus != null)
            {
                if (slotBonus.Props != null)
                {
                    if (slotBonus.Props.statModifiers != null && slotBonus.Props.statModifiers.Count > 0)
                    {
                        foreach (StatModifier thisStat in slotBonus.Props.statModifiers)
                        {
                            //Log.Message("Check for modding "+stat+"  against "+thisStat.stat);
                            if (thisStat.stat == stat)
                            {
                                //Log.Message("adding in stat "+thisStat.stat+":"+thisStat.value+" to result "+retval);
                                retval += thisStat.value;

                                // apply stats parts from Slottable
                                if (stat.parts != null && stat.parts.Count > 0)
                                {
                                    StatRequest req = StatRequest.For(slottable);
                                    for (int i = 0; i < stat.parts.Count; i++)
                                    {
                                        //Log.Message("adding in parts "+stat.parts[i]);
                                        stat.parts[i].TransformValue(req, ref retval);
                                    }
                                    //Log.Message("added in parts of a stat for result "+retval);
                                }
                            }
                        }
                    }
                }
            }

            return retval;
        }


    }
}
