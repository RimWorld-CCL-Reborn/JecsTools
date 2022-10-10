using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbilityUser
{
    public class JobDriver_CastAbilityVerb : JobDriver
    {
        public AbilityContext Context => job.count == 1 ? AbilityContext.Player : AbilityContext.AI;
        public Verb_UseAbility Verb => pawn.CurJob.verbToUse as Verb_UseAbility;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            if (TargetA.HasThing)
            {
                // !GetActor().IsFighting() || - removed from below "If" by xen for melee casting fix - replaced with range check - !pawn.Position.InHorDistOf(TargetA.Cell, pawn.CurJob.verbToUse.verbProps.range)

                if (!pawn.Position.InHorDistOf(TargetA.Cell, pawn.CurJob.verbToUse.verbProps.range) ||
                    !Verb.UseAbilityProps.canCastInMelee)
                {
                    var getInRangeToil = Toils_Combat.GotoCastPosition(TargetIndex.A, false);
                    yield return getInRangeToil;
                }

            }

            if (Context == AbilityContext.Player)
            {
                Find.Targeter.targetingSource = Verb;
            }

            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
            yield return new Toil
            {
                initAction = delegate
                {
                    if (Verb?.UseAbilityProps?.isViolent == true)
                    {
                        CheckForAutoAttack(this.pawn);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield return new Toil
            {
                initAction = delegate { Verb.Ability.PostAbilityAttempt(); },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        //from the JobDriver_Wait in Vanilla RimWorld
        //Updated 10/9/2022
        public static void CheckForAutoAttack(Pawn searcher)
        {
            if (searcher.Downed)
            {
                return;
            }
            if (searcher.stances.FullBodyBusy)
            {
                return;
            }
<<<<<<< Updated upstream
            bool flag = searcher.story == null || !searcher.WorkTagIsDisabled(WorkTags.Violent);
            bool flag2 = searcher.RaceProps.ToolUser && searcher.Faction == Faction.OfPlayer &&
                         !searcher.WorkTagIsDisabled(WorkTags.Firefighting);
=======
            if (searcher.IsCarryingPawn(null))
            {
                return;
            }
            //this.collideWithPawns = false;
            var flag = searcher.story == null || !searcher.WorkTagIsDisabled(WorkTags.Violent);
            var flag2 = searcher.RaceProps.ToolUser && searcher.Faction == Faction.OfPlayer &&
                !searcher.WorkTagIsDisabled(WorkTags.Firefighting);
>>>>>>> Stashed changes
            if (flag || flag2)
            {
                Fire fire = null;
                for (int i = 0; i < 9; i++)
                {
                    IntVec3 c = searcher.Position + GenAdj.AdjacentCellsAndInside[i];
                    if (c.InBounds(searcher.Map))
                    {
                        List<Thing> thingList = c.GetThingList(searcher.MapHeld);
                        for (int j = 0; j < thingList.Count; j++)
                        {
                            if (flag)
                            {
<<<<<<< Updated upstream
                                Pawn pawn = thingList[j] as Pawn;
                                if (pawn != null && !pawn.Downed && searcher.HostileTo(pawn))
=======
                                if (thingList[j] is Pawn pawn &&
                                    !pawn.Downed && 
                                    searcher.HostileTo(pawn) &&
                                    !searcher.ThreatDisabledBecauseNonAggressiveRoamer(pawn) &&
                                    GenHostility.IsActiveThreatTo(pawn,searcher.Faction))
>>>>>>> Stashed changes
                                {
                                    searcher.meleeVerbs.TryMeleeAttack(pawn, null, false);
                                    //this.collideWithPawns = true;
                                    return;
                                }
                            }
                            if (flag2)
                            {
<<<<<<< Updated upstream
                                Fire fire2 = thingList[j] as Fire;
                                if (fire2 != null && (fire == null || fire2.fireSize < fire.fireSize || i == 8) &&
                                    (fire2.parent == null || fire2.parent != searcher))
                                {
=======
                                if (thingList[j] is Fire fire2 &&
                                fire2 != null && 
                                (fire == null || fire2.fireSize < fire.fireSize || i == 8) 
                                && (fire2.parent == null || fire2.parent != searcher))
                                    {
>>>>>>> Stashed changes
                                    fire = fire2;
                                }
                            }
                        }
                    }
                }
                if (fire != null && (!searcher.InMentalState || searcher.MentalState.def.allowBeatfire))
                {
                    searcher.natives.TryBeatFire(fire);
                    return;
                }
                if (flag && searcher.Faction != null &&
                    (searcher.drafter == null || searcher.drafter.FireAtWill))
                {
                    bool allowManualCastWeapons = !searcher.IsColonist;
                    Verb verb = searcher.TryGetAttackVerb(null, allowManualCastWeapons);
                    if (verb != null && !verb.verbProps.IsMeleeAttack)
                    {
<<<<<<< Updated upstream
                        TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns |
                                                          TargetScanFlags.NeedThreat;
                        if (verb.IsIncendiary())
=======
                        var targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns |
                                              TargetScanFlags.NeedThreat;
                        if (verb.IsIncendiary_Ranged())
>>>>>>> Stashed changes
                        {
                            targetScanFlags |= TargetScanFlags.NeedNonBurning;
                        }
                        Thing thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(searcher, targetScanFlags, null,
                            verb.verbProps.minRange, verb.verbProps.range);
                        if (thing != null)
                        {
                            searcher.TryStartAttack(thing);
<<<<<<< Updated upstream
                            return;
=======
                            //this.collideWithPawns = true;
>>>>>>> Stashed changes
                        }
                    }
                }
            }
        }
    }
}