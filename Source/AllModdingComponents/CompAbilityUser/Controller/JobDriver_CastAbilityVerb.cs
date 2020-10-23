﻿using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbilityUser
{
    // Based off JobDriver_Wait.
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
                initAction = () =>
                {
                    if (Verb.UseAbilityProps.isViolent)
                    {
                        CheckForAutoAttack(pawn);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
            };

            yield return new Toil
            {
                initAction = Verb.Ability.PostAbilityAttempt,
                defaultCompleteMode = ToilCompleteMode.Instant,
            };
        }

        //from the JobDriver_Wait in Vanilla RimWorld
        public static void CheckForAutoAttack(Pawn searcher)
        {
            if (searcher.Downed || searcher.stances.FullBodyBusy)
            {
                return;
            }
            var flag = searcher.story == null || !searcher.WorkTagIsDisabled(WorkTags.Violent);
            var flag2 = searcher.RaceProps.ToolUser && searcher.Faction == Faction.OfPlayer &&
                !searcher.WorkTagIsDisabled(WorkTags.Firefighting);
            if (flag || flag2)
            {
                Fire fire = null;
                for (var i = 0; i < 9; i++)
                {
                    var c = searcher.Position + GenAdj.AdjacentCellsAndInside[i];
                    if (c.InBounds(searcher.Map))
                    {
                        var thingList = c.GetThingList(searcher.MapHeld);
                        for (var j = 0; j < thingList.Count; j++)
                        {
                            if (flag)
                            {
                                if (thingList[j] is Pawn pawn && !pawn.Downed && searcher.HostileTo(pawn))
                                {
                                    searcher.meleeVerbs.TryMeleeAttack(pawn, null, false);
                                    return;
                                }
                            }
                            if (flag2)
                            {
                                if (thingList[j] is Fire fire2 && (fire == null || fire2.fireSize < fire.fireSize || i == 8) &&
                                    (fire2.parent == null || fire2.parent != searcher))
                                {
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
                    var allowManualCastWeapons = !searcher.IsColonist;
                    var verb = searcher.TryGetAttackVerb(null, allowManualCastWeapons);
                    if (verb != null && !verb.verbProps.IsMeleeAttack)
                    {
                        var targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns |
                                              TargetScanFlags.NeedThreat;
                        if (verb.IsIncendiary())
                        {
                            targetScanFlags |= TargetScanFlags.NeedNonBurning;
                        }
                        var thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(searcher, targetScanFlags, null,
                            verb.verbProps.minRange, verb.verbProps.range);
                        if (thing != null)
                        {
                            searcher.TryStartAttack(thing);
                        }
                    }
                }
            }
        }
    }
}
