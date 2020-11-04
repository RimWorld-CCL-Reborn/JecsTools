using System.Collections.Generic;
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

        public SlotLoadable ColorChangingSlot =>
            colorChangingSlot ??= slots.Find(slot => slot.Def?.doesChangeColor ?? false);

        public SlotLoadable SecondColorChangingSlot =>
            secondColorChangingSlot ??= slots.Find(slot => slot.Def?.doesChangeSecondColor ?? false);

        public List<SlotLoadableDef> SlotDefs => slots.ConvertAll(slot => slot.Def);

        public Map GetMap => parent.Map ?? GetPawn?.Map;

        public CompEquippable GetEquippable => compEquippable;

        public Pawn GetPawn => compEquippable.PrimaryVerb.CasterPawn;

        // Caching comps needs to happen after all comps are created. Ideally, this would be done right after
        // ThingWithComps.InitializeComps(). This requires overriding two hooks: PostPostMake and PostExposeData.

        public override void PostPostMake()
        {
            base.PostPostMake();
            CacheComps();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isInitialized, nameof(isInitialized));
            Scribe_Values.Look(ref isGathering, nameof(isGathering));
            Scribe_Collections.Look(ref slots, nameof(slots), LookMode.Deep);
            slots ??= new List<SlotLoadable>();
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                //Scribe.writingForDebug = false;
                CacheComps();
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                //Scribe.writingForDebug = true;
            }
        }

        private void CacheComps()
        {
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
            if (!isInitialized)
                Initialize();
        }

        private void TryCancel()
        {
            var pawn = GetPawn;
            if (pawn != null)
            {
                if (pawn.CurJob.def == CompSlotLoadableDefOf.GatherSlotItem)
                    pawn.jobs.StopAll();
                isGathering = false;
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
            var loadSlot = slots.Find(slot => slot.IsEmpty() && slot.CanLoad(thing.def)) ??
                slots.Find(slot => slot.CanLoad(thing.def));
            return loadSlot?.TryLoadSlot(thing, true) ?? false;
        }

        public void ProcessInput(SlotLoadable slot)
        {
            var floatList = new List<FloatMenuOption>();
            var slotOccupant = slot.SlotOccupant;
            if (!isGathering)
            {
                var map = GetMap;
                if (slotOccupant == null)
                {
                    var loadTypes = slot.SlottableTypes;
                    if (loadTypes.Count > 0)
                    {
                        var pawn = GetPawn;
                        foreach (var current in loadTypes)
                        {
                            var thingToLoad = map.listerThings.ThingsOfDef(current)
                                .Find(x => map.reservationManager.CanReserve(pawn, x));
                            if (thingToLoad != null)
                            {
                                //bool extraPartOnGUI(Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, current);
                                floatList.Add(new FloatMenuOption("Load".Translate() + " " + thingToLoad.def.label,
                                    () => TryGiveLoadSlotJob(thingToLoad), extraPartWidth: 29f));
                            }
                            // Commenting following out since all the unavailable options can clutter the menu.
                            //else
                            //{
                            //    floatList.Add(new FloatMenuOption(string.Format(StringOf.Unavailable, current.label),
                            //        () => { }));
                            //}
                        }
                    }
                    if (floatList.Count == 0)
                        floatList.Add(new FloatMenuOption(StringOf.NoLoadOptions, () => { }));
                }
            }
            if (slotOccupant != null)
            {
                //bool extraPartOnGUI(Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, current);
                floatList.Add(new FloatMenuOption(string.Format(StringOf.Unload, slotOccupant.Label),
                    () => TryEmptySlot(slot), extraPartWidth: 29f));
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
                        icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
                        action = TryCancel,
                    };
                foreach (var slot in slots)
                    if (slot.IsEmpty())
                        yield return new Command_Action
                        {
                            defaultLabel = slot.Label,
                            icon = Command.BGTex,
                            defaultDesc = SlotDesc(slot),
                            action = () => ProcessInput(slot),
                        };
                    else
                        yield return new Command_Action
                        {
                            defaultLabel = slot.Label,
                            icon = slot.SlotIcon(),
                            defaultDesc = SlotDesc(slot),
                            defaultIconColor = slot.SlotColor(),
                            action = () => ProcessInput(slot),
                        };
            }
        }

        public virtual string SlotDesc(SlotLoadable slot)
        {
            var s = new StringBuilder();
            s.AppendLine(slot.def.description); //TODO
            var slotOccupant = slot.SlotOccupant;
            if (slotOccupant != null)
            {
                s.AppendLine();
                s.AppendLine(string.Format(StringOf.CurrentlyLoaded, slotOccupant.LabelCap));
                if (slot.Def?.doesChangeColor ?? false)
                {
                    s.AppendLine();
                    s.AppendLine(StringOf.Effects);
                    s.AppendLine("  " + StringOf.ChangesPrimaryColor);
                }
                if (slot.Def?.doesChangeStats ?? false)
                {
                    var slotBonusProps = slotOccupant.TryGetCompSlottedBonus()?.Props;
                    if (slotBonusProps != null)
                    {
                        if (!slotBonusProps.statModifiers.NullOrEmpty())
                        {
                            s.AppendLine();
                            s.AppendLine(StringOf.StatModifiers);

                            foreach (var mod in slotBonusProps.statModifiers)
                            {
                                var v = SlotLoadableUtility.DetermineSlottableStatAugment(slotOccupant, mod.stat);
                                var modstring = mod.stat.ValueToString(v, ToStringNumberSense.Offset);
                                //Log.Message("Determined slot stat augment "+v+" and made string "+modstring);
                                s.AppendLine("  " + mod.stat.LabelCap + " " + modstring);
                                //s.AppendLine("\t" + mod.stat.LabelCap + " " + mod.ToStringAsOffset);
                            }
                            /*
                            // TODO: Fix this to display statModifiers
                            var statMods = slotOccupant.def.statBases.FindAll(
                                z => z.stat.category == StatCategoryDefOf.Weapon ||
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
                            var healText = defHealChance.woundLimit == int.MaxValue ? StringOf.all : defHealChance.woundLimit.ToString();
                            s.AppendLine("  " + string.Format(StringOf.DefensiveHealChance, healText, defHealChance.chance.ToStringPercent()));
                        }
                        var vampChance = slotBonusProps.vampiricHealChance;
                        if (vampChance != null)
                        {
                            var vampText = vampChance.woundLimit == int.MaxValue ? StringOf.all : vampChance.woundLimit.ToString();
                            s.AppendLine("  " + string.Format(StringOf.VampiricChance, vampText, vampChance.chance.ToStringPercent()));
                        }
                    }
                }
            }
            return s.ToString();
        }
    }
}
