using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CompSlotLoadable
{
    public class CompSlotLoadable : ThingComp
    {
        private SlotLoadable colorChangingSlot;
        public bool GizmosOnEquip = true;

        private bool isGathering;

        private bool isInitialized;


        private SlotLoadable secondColorChangingSlot;

        private List<SlotLoadable> slots = new List<SlotLoadable>();
        public List<SlotLoadable> Slots => slots;

        public SlotLoadable ColorChangingSlot
        {
            get
            {
                if (colorChangingSlot != null) return colorChangingSlot;
                if (Slots != null)
                    if (Slots.Count > 0)
                        colorChangingSlot = Slots.FirstOrDefault(x => ((SlotLoadableDef) x.def).doesChangeColor);
                return colorChangingSlot;
            }
        }

        public SlotLoadable SecondColorChangingSlot
        {
            get
            {
                if (secondColorChangingSlot != null) return secondColorChangingSlot;
                if (Slots != null)
                    if (Slots.Count > 0)
                        secondColorChangingSlot =
                            Slots.FirstOrDefault(x => ((SlotLoadableDef) x.def).doesChangeSecondColor);
                return colorChangingSlot;
            }
        }

        public List<SlotLoadableDef> SlotDefs
        {
            get
            {
                var result = new List<SlotLoadableDef>();
                if (slots != null)
                    if (slots.Count > 0)
                        foreach (var slot in slots)
                            result.Add(slot.def as SlotLoadableDef);
                return result;
            }
        }

        public Map GetMap
        {
            get
            {
                var map = parent.Map;
                if (map == null)
                    if (GetPawn != null) map = GetPawn.Map;
                return map;
            }
        }

        public CompEquippable GetEquippable => parent.GetComp<CompEquippable>();

        public Pawn GetPawn => GetEquippable.verbTracker.PrimaryVerb.CasterPawn;


        public CompProperties_SlotLoadable Props => (CompProperties_SlotLoadable) props;

        public void Initialize()
        {
            //Log.Message("1");
            if (!isInitialized)
            {
                //Log.Message("2");
                isInitialized = true;
                if (Props != null)
                    if (Props.slots != null)
                        if (Props.slots.Count > 0)
                            foreach (var slot in Props.slots)
                            {
                                var newSlot = new SlotLoadable(slot, parent);
                                //Log.Message("Added Slot");
                                slots.Add(newSlot);
                            }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!isInitialized) Initialize();
        }

        private void TryCancel(string reason = "")
        {
            var pawn = GetPawn;
            if (pawn != null)
            {
                if (pawn.CurJob.def == CompSlotLoadableDefOf.GatherSlotItem)
                    pawn.jobs.StopAll();
                isGathering = false;
                //Messages.Message("Cancelling sacrifice. " + reason, MessageSound.Negative);
            }
        }

        private void TryGiveLoadSlotJob(Thing itemToLoad)
        {
            if (GetPawn != null)
                if (!GetPawn.Drafted)
                {
                    isGathering = true;

                    var job = new Job(CompSlotLoadableDefOf.GatherSlotItem, itemToLoad)
                    {
                        count = 1
                    };
                    GetPawn.jobs.TryTakeOrderedJob(job);
                    //GetPawn.jobs.jobQueue.EnqueueFirst(job);
                    //GetPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
                else
                {
                    Messages.Message(string.Format(StringOf.IsDrafted, new object[]
                    {
                        GetPawn.Label
                    }), MessageTypeDefOf.RejectInput);
                }
        }

        public bool TryLoadSlot(Thing thing)
        {
            //Log.Message("TryLoadSlot Called");
            isGathering = false;
            if (slots != null)
                if (slots.Count > 0)
                {
                    var loadSlot = slots.FirstOrDefault(x => x.IsEmpty() && x.CanLoad(thing.def));
                    if (loadSlot == null) loadSlot = slots.FirstOrDefault(y => y.CanLoad(thing.def));
                    if (loadSlot != null)
                        if (loadSlot.TryLoadSlot(thing, true))
                            return true;
                }
            return false;
        }

        public void ProcessInput(SlotLoadable slot)
        {
            var loadTypes = new List<ThingDef>();
            var floatList = new List<FloatMenuOption>();
            if (!isGathering)
            {
                var map = GetMap;
                loadTypes = slot.SlottableTypes;
                if (slot.SlotOccupant == null)
                    if (loadTypes != null)
                        if (loadTypes.Count != 0)
                            foreach (var current in loadTypes)
                            {
                                var thingsWithDef =
                                    new List<Thing>(map.listerThings.AllThings.FindAll(x => x.def == current));
                                if (thingsWithDef != null)
                                    if (thingsWithDef.Count > 0)
                                    {
                                        var thingToLoad = thingsWithDef.FirstOrDefault(x =>
                                            map.reservationManager.CanReserve(GetPawn, x));
                                        if (thingToLoad != null)
                                        {
                                            var text = "Load".Translate() + " " + thingToLoad.def.label;
                                            //Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, current);
                                            floatList.Add(new FloatMenuOption(text,
                                                delegate { TryGiveLoadSlotJob(thingToLoad); },
                                                MenuOptionPriority.Default, null, null, 29f, null, null));
                                        }
                                        else
                                        {
                                            floatList.Add(new FloatMenuOption(
                                                string.Format(StringOf.Unavailable, new object[] {current.label}),
                                                delegate { }, MenuOptionPriority.Default));
                                        }
                                    }
                                    else
                                    {
                                        floatList.Add(new FloatMenuOption(
                                            string.Format(StringOf.Unavailable, new object[] {current.label}),
                                            delegate { }, MenuOptionPriority.Default));
                                    }
                                else
                                    floatList.Add(new FloatMenuOption(
                                        string.Format(StringOf.Unavailable, new object[] {current.label}), delegate { },
                                        MenuOptionPriority.Default));
                            }
                        else
                            floatList.Add(new FloatMenuOption(StringOf.NoLoadOptions, delegate { },
                                MenuOptionPriority.Default));
            }
            if (!slot.IsEmpty())
            {
                var text = string.Format(StringOf.Unload, new object[] {slot.SlotOccupant.Label});
                //Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, current);
                floatList.Add(new FloatMenuOption(text, delegate { TryEmptySlot(slot); }, MenuOptionPriority.Default,
                    null, null, 29f, null, null));
            }
            Find.WindowStack.Add(new FloatMenu(floatList));
        }

        public virtual void TryEmptySlot(SlotLoadable slot)
        {
            slot.TryEmptySlot();
        }

        public virtual IEnumerable<Gizmo> EquippedGizmos()
        {
            if (slots != null)
                if (slots.Count > 0)
                {
                    if (isGathering)
                        yield return new Command_Action
                        {
                            defaultLabel = "Designator_Cancel".Translate(),
                            defaultDesc = "Designator_CancelDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                            action = delegate { TryCancel(); }
                        };
                    foreach (var slot in slots)
                        if (slot.IsEmpty())
                            yield return new Command_Action
                            {
                                defaultLabel = slot.Label,
                                icon = Command.BGTex,
                                defaultDesc = SlotDesc(slot),
                                action = delegate { ProcessInput(slot); }
                            };
                        else
                            yield return new Command_Action
                            {
                                defaultLabel = slot.Label,
                                icon = slot.SlotIcon(),
                                defaultDesc = SlotDesc(slot),
                                defaultIconColor = slot.SlotColor(),
                                action = delegate { ProcessInput(slot); }
                            };
                }
        }

        public virtual string SlotDesc(SlotLoadable slot)
        {
            var s = new StringBuilder();
            s.AppendLine(slot.def.description); //TODO
            if (!slot.IsEmpty())
            {
                s.AppendLine();
                s.AppendLine(string.Format(StringOf.CurrentlyLoaded, new object[] {slot.SlotOccupant.LabelCap}));
                if (((SlotLoadableDef) slot.def).doesChangeColor)
                {
                    s.AppendLine();
                    s.AppendLine(StringOf.Effects);
                    s.AppendLine("  " + StringOf.ChangesPrimaryColor);
                }
                if (((SlotLoadableDef) slot.def).doesChangeStats)
                {
                    var slotBonus = slot.SlotOccupant.TryGetComp<CompSlottedBonus>();
                    if (slotBonus != null)
                        if (slotBonus.Props != null)
                        {
                            if (slotBonus.Props.statModifiers != null && slotBonus.Props.statModifiers.Count > 0)
                            {
                                s.AppendLine();
                                s.AppendLine(StringOf.StatModifiers);

                                foreach (var mod in slotBonus.Props.statModifiers)
                                {
                                    var v = SlotLoadableUtility.DetermineSlottableStatAugment(slot.SlotOccupant, mod.stat);
                                    var modstring = mod.stat.ValueToString(v, ToStringNumberSense.Offset);
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
                            var damageDef = slotBonus.Props.damageDef;
                            if (damageDef != null)
                            {
                                s.AppendLine();
                                s.AppendLine(string.Format(StringOf.DamageType, new object[] {damageDef.LabelCap}));
                            }
                            var defHealChance = slotBonus.Props.defensiveHealChance;
                            if (defHealChance != null)
                            {
                                var healText = StringOf.all;
                                if (defHealChance.woundLimit != 0) healText = defHealChance.woundLimit.ToString();
                                s.AppendLine("  " + string.Format(StringOf.DefensiveHealChance, new object[]
                                {
                                    healText,
                                    defHealChance.chance.ToStringPercent()
                                }));
                            }
                            var vampChance = slotBonus.Props.vampiricHealChance;
                            if (vampChance != null)
                            {
                                var vampText = StringOf.all;
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
            return s.ToString();
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref isInitialized, "isInitialized", false);
            Scribe_Values.Look(ref isGathering, "isGathering", false);
            Scribe_Collections.Look(ref slots, "slots", LookMode.Deep);
            base.PostExposeData();
            if (slots == null)
                slots = new List<SlotLoadable>();
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                //Scribe.writingForDebug = false;
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                //Scribe.writingForDebug = true;
            }
        }


    }
}