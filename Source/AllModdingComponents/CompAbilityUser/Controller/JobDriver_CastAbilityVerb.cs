using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbilityUser
{
    public class JobDriver_CastAbilityVerb : JobDriver
    {
        public AbilityContext Context => job.count == 1 ? AbilityContext.Player : AbilityContext.AI;

        public override bool TryMakePreToilReservations()
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            var verb = pawn.CurJob.verbToUse as Verb_UseAbility;
            if (TargetA.HasThing)
            {
                if (!GetActor().IsFighting() || !verb.UseAbilityProps.canCastInMelee)
                {
                    var getInRangeToil = Toils_Combat.GotoCastPosition(TargetIndex.A, false);
                    yield return getInRangeToil;
                }
            }

            if (Context == AbilityContext.Player)
                Find.Targeter.targetingVerb = verb;

            yield return new Toil
            {
                initAction = delegate { verb.Ability.PostAbilityAttempt(); },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
            yield return new Toil
            {
                initAction = delegate
                {
                    if (verb.UseAbilityProps.isViolent)
                    {
                    	CheckForAutoAttack(this.pawn);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

	    //from the JobDriver_Wait in Vanilla RimWorld
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
		    bool flag = searcher.story == null || !searcher.story.WorkTagIsDisabled(WorkTags.Violent);
		    bool flag2 = searcher.RaceProps.ToolUser && searcher.Faction == Faction.OfPlayer &&
		                 !searcher.story.WorkTagIsDisabled(WorkTags.Firefighting);
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
							    Pawn pawn = thingList[j] as Pawn;
							    if (pawn != null && !pawn.Downed && searcher.HostileTo(pawn))
							    {
								    searcher.meleeVerbs.TryMeleeAttack(pawn, null, false);
								    return;
							    }
						    }
						    if (flag2)
						    {
							    Fire fire2 = thingList[j] as Fire;
							    if (fire2 != null && (fire == null || fire2.fireSize < fire.fireSize || i == 8) &&
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
				    bool allowManualCastWeapons = !searcher.IsColonist;
				    Verb verb = searcher.TryGetAttackVerb(null,allowManualCastWeapons);
				    if (verb != null && !verb.verbProps.IsMeleeAttack)
				    {
					    TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns |
					                                      TargetScanFlags.NeedThreat;
					    if (verb.IsIncendiary())
					    {
						    targetScanFlags |= TargetScanFlags.NeedNonBurning;
					    }
					    Thing thing = (Thing) AttackTargetFinder.BestShootTargetFromCurrentPosition(searcher, null,
						    verb.verbProps.range, verb.verbProps.minRange, targetScanFlags);
					    if (thing != null)
					    {
						    searcher.TryStartAttack(thing);
						    return;
					    }
				    }
			    }
		    }
	    }
    }
}