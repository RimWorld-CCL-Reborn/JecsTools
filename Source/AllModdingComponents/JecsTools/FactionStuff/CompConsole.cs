using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace JecsTools
{
    // See comments on JecsToolsFactionDialogMaker.
    [Obsolete("Hasn't worked properly since RW B19")]
    public class CompConsole : ThingComp
    {
        public CompProperties_Console Props => this.props as CompProperties_Console;

        private CompPowerTrader compPowerTrader;

        public bool CanUseCommsNow =>
            (!parent.Spawned || !parent.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.SolarFlare)) &&
            (!Props.usesPower || (compPowerTrader?.PowerOn ?? false));

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
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                CacheComps();
        }

        private void CacheComps()
        {
            // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
            // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
            // while `isinst` instruction against non-generic type operand like used below is fast.
            var comps = parent.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompPowerTrader compPowerTrader)
                {
                    this.compPowerTrader = compPowerTrader;
                    break;
                }
            }
        }

        private void UseAct(Pawn myPawn, ICommunicable commTarget)
        {
            var job = JobMaker.MakeJob(JobDefMaker.JecsTools_UseConsole, this.parent);
            job.commTarget = commTarget;
            myPawn.jobs.TryTakeOrderedJob(job);
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.OpeningComms, KnowledgeAmount.Total);
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn myPawn)
        {
            foreach (var g in base.CompFloatMenuOptions(myPawn))
                yield return g;

            if (!myPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Some))
            {
                yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
                yield break;
            }
            if (parent.Spawned && parent.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.SolarFlare))
            {
                yield return new FloatMenuOption("CannotUseSolarFlare".Translate(), null);
                yield break;
            }
            if (Props.usesPower && (!compPowerTrader?.PowerOn ?? false))
            {
                yield return new FloatMenuOption("CannotUseNoPower".Translate(), null);
                yield break;
            }
            if (!myPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
            {
                yield return new FloatMenuOption("CannotUseReason".Translate("IncapableOfCapacity".Translate(PawnCapacityDefOf.Talking.label)), null);
                yield break;
            }
            if (myPawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
            {
                yield return new FloatMenuOption("CannotPrioritizeWorkTypeDisabled".Translate(SkillDefOf.Social.LabelCap), null);
                yield break;
            }
            if (!this.CanUseCommsNow)
            {
                Log.Error(myPawn + " could not use " + parent.Label + " for unknown reason.");
                yield return new FloatMenuOption("Cannot use now", null);
                yield break;
            }

            foreach (var localCommTarget in myPawn.Map.passingShipManager.passingShips.Cast<ICommunicable>().Concat(
                Find.FactionManager.AllFactionsInViewOrder.Cast<ICommunicable>()))
            {
                if (localCommTarget == null) continue;
                var text = "CallOnRadio".Translate(localCommTarget.GetCallLabel());
                if (localCommTarget is Faction faction)
                {
                    if (faction.IsPlayer)
                        continue;
                    if (!LeaderIsAvailableToTalk(faction))
                    {
                        string str;
                        if (faction.leader != null)
                            str = "LeaderUnavailable".Translate(faction.leader.LabelShort);
                        else
                            str = "LeaderUnavailableNoLeader".Translate();
                        yield return new FloatMenuOption(text + " (" + str + ")", null);
                        continue;
                    }
                }

                void Action()
                {
                    if (localCommTarget is TradeShip && !Building_OrbitalTradeBeacon.AllPowered(parent.Map).Any())
                    {
                        Messages.Message("MessageNeedBeaconToTradeWithShip".Translate(), parent, MessageTypeDefOf.RejectInput);
                        return;
                    }
                    UseAct(myPawn, localCommTarget);
                }

                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, Action, MenuOptionPriority.InitiateSocial), myPawn, parent, "ReservedBy");
            }
        }

        public static bool LeaderIsAvailableToTalk(Faction fac)
        {
            return fac.leader != null && (!fac.leader.Spawned || (!fac.leader.Downed && !fac.leader.IsPrisoner && fac.leader.Awake() && !fac.leader.InMentalState));
        }
    }
}
