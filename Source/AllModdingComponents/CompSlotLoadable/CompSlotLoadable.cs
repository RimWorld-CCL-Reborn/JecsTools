using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace CompSlotLoadable
{
    public class CompSlotLoadable : ThingComp
    {
        public CompProperties_SlotLoadable Props => (CompProperties_SlotLoadable)props;

        private SlotLoadable colorChangingSlot;
        public bool GizmosOnEquip = true;

        private bool isGathering;

        private bool isInitialized;

        private CompEquippable compEquippable;

        private SlotLoadable secondColorChangingSlot;

        private List<SlotLoadable> slots = new List<SlotLoadable>();
        public List<SlotLoadable> Slots => slots;

        public SlotLoadable ColorChangingSlot => colorChangingSlot ??= slots.FirstOrDefault(x => ((SlotLoadableDef)x.def).doesChangeColor);

        public SlotLoadable SecondColorChangingSlot => secondColorChangingSlot ??= slots.FirstOrDefault(x => ((SlotLoadableDef)x.def).doesChangeSecondColor);

        public List<SlotLoadableDef> SlotDefs
        {
            get
            {
                var result = new List<SlotLoadableDef>(slots.Count);
                foreach (var slot in slots)
                    result.Add(slot.def as SlotLoadableDef);
                return result;
            }
        }

        public Map GetMap => parent.Map ?? GetPawn?.Map;

        public CompEquippable GetEquippable => compEquippable;

        public Pawn GetPawn => GetEquippable.PrimaryVerb.CasterPawn;

        // This is called during ThingWithComps.InitializeComps, after constructor is called and parent is set.
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
            // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
            // while `isinst` instruction against non-generic type operand like used below is fast.
            var comps = parent.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompEquippable compEquippable)
                {
                    this.compEquippable = compEquippable;
                    break;
                }
            }
        }

        // This is called on the first tick - not rolled into above Initialize since it's still needed in case subclasses implement it.
        public void Initialize()
        {
            if (!isInitialized)
            {
                isInitialized = true;
                var slots = Props?.slots;
                if (slots != null)
                    foreach (var slot in slots)
                    {
                        var newSlot = new SlotLoadable(slot, parent);
                        //Log.Message("Added Slot");
                        this.slots.Add(newSlot);
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
            var pawn = GetPawn;
            if (pawn != null)
                if (!pawn.Drafted)
                {
                    isGathering = true;

                    var job = JobMaker.MakeJob(CompSlotLoadableDefOf.GatherSlotItem, itemToLoad);
                    job.count = 1;
                    pawn.jobs.TryTakeOrderedJob(job);
                    //pawn.jobs.jobQueue.EnqueueFirst(job);
                    //pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
                else
                {
                    Messages.Message(string.Format(StringOf.IsDrafted, pawn.Label), MessageTypeDefOf.RejectInput);
                }
        }

        public bool TryLoadSlot(Thing thing)
        {
            //Log.Message("TryLoadSlot Called");
            isGathering = false;
            var loadSlot = slots.FirstOrDefault(x => x.IsEmpty() && x.CanLoad(thing.def)) ??
                slots.FirstOrDefault(y => y.CanLoad(thing.def));
            return loadSlot?.TryLoadSlot(thing, true) ?? false;
        }

        public void ProcessInput(SlotLoadable slot)
        {
            var floatList = new List<FloatMenuOption>();
            if (!isGathering)
            {
                var map = GetMap;
                if (slot.SlotOccupant == null && slot.SlottableTypes is List<ThingDef> loadTypes)
                {
                    if (loadTypes.Count > 0)
                    {
                        var pawn = GetPawn;
                        foreach (var current in loadTypes)
                        {
                            var thingToLoad = map.listerThings.ThingsOfDef(current)
                                .FirstOrDefault(x => map.reservationManager.CanReserve(pawn, x));
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
                                    string.Format(StringOf.Unavailable, current.label),
                                    delegate { }, MenuOptionPriority.Default));
                            }
                        }
                    }
                    else
                        floatList.Add(new FloatMenuOption(StringOf.NoLoadOptions, delegate { },
                            MenuOptionPriority.Default));
                }
            }
            if (!slot.IsEmpty())
            {
                var text = string.Format(StringOf.Unload, slot.SlotOccupant.Label);
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
                s.AppendLine(string.Format(StringOf.CurrentlyLoaded, slot.SlotOccupant.LabelCap));
                if (((SlotLoadableDef)slot.def).doesChangeColor)
                {
                    s.AppendLine();
                    s.AppendLine(StringOf.Effects);
                    s.AppendLine("  " + StringOf.ChangesPrimaryColor);
                }
                if (((SlotLoadableDef)slot.def).doesChangeStats)
                {
                    var slotBonusProps = slot.SlotOccupant.TryGetCompSlottedBonus()?.Props;
                    if (slotBonusProps != null)
                    {
                        if (!slotBonusProps.statModifiers.NullOrEmpty())
                        {
                            s.AppendLine();
                            s.AppendLine(StringOf.StatModifiers);

                            foreach (var mod in slotBonusProps.statModifiers)
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
                            if (statMods.Count > 0)
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
                        var damageDef = slotBonusProps.damageDef;
                        if (damageDef != null)
                        {
                            s.AppendLine();
                            s.AppendLine(string.Format(StringOf.DamageType, damageDef.LabelCap));
                        }
                        var defHealChance = slotBonusProps.defensiveHealChance;
                        if (defHealChance != null)
                        {
                            var healText = StringOf.all;
                            if (defHealChance.woundLimit != 0) healText = defHealChance.woundLimit.ToString();
                            s.AppendLine("  " + string.Format(StringOf.DefensiveHealChance, healText, defHealChance.chance.ToStringPercent()));
                        }
                        var vampChance = slotBonusProps.vampiricHealChance;
                        if (vampChance != null)
                        {
                            var vampText = StringOf.all;
                            if (vampChance.woundLimit != 0) vampText = defHealChance.woundLimit.ToString();
                            s.AppendLine("  " + string.Format(StringOf.VampiricChance, vampText, vampChance.chance.ToStringPercent()));
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
            slots ??= new List<SlotLoadable>();
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
