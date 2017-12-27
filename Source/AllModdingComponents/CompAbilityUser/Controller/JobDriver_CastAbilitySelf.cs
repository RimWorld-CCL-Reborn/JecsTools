using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbilityUser
{
    public class JobDriver_CastAbilitySelf : JobDriver
    {
        public AbilityContext Context => job.count == 1 ? AbilityContext.Player : AbilityContext.AI;

        private List<CompAbilityUser> CompAbilityUsers
        {
            get
            {
                var results = new List<CompAbilityUser>();
                var allCompAbilityUsers = pawn.GetComps<CompAbilityUser>();
                if (allCompAbilityUsers.TryRandomElement(out var comp))
                    foreach (var compy in allCompAbilityUsers)
                        results.Add(compy);
                return results;
            }
        }

        public override bool TryMakePreToilReservations()
        {
            return true;
        }

        public void EvaluateCell(IntVec3 c, CastPositionRequest req,
            float maxRangeFromTargetSquared,
            float maxRangeFromLocusSquared,
            float maxRangeFromCasterSquared,
            float rangeFromCasterToCellSquared,
            int inRadiusMark
        )
        {
            /////////////// EVALUATE CELL METHOD
            if (maxRangeFromTargetSquared > 0.01f && maxRangeFromTargetSquared < 250000f &&
                (c - TargetA.Cell).LengthHorizontalSquared > maxRangeFromTargetSquared)
            {
                if (DebugViewSettings.drawCastPositionSearch)
                    req.caster.Map.debugDrawer.FlashCell(c, 0f, "range target");
                return;
            }
            if (maxRangeFromLocusSquared > 0.01 && (c - req.locus).LengthHorizontalSquared > maxRangeFromLocusSquared)
            {
                if (DebugViewSettings.drawCastPositionSearch)
                    req.caster.Map.debugDrawer.FlashCell(c, 0.1f, "range home");
                return;
            }
            if (maxRangeFromCasterSquared > 0.01f)
            {
                rangeFromCasterToCellSquared = (c - req.caster.Position).LengthHorizontalSquared;
                if (rangeFromCasterToCellSquared > maxRangeFromCasterSquared)
                {
                    if (DebugViewSettings.drawCastPositionSearch)
                        req.caster.Map.debugDrawer.FlashCell(c, 0.2f, "range caster");
                    return;
                }
            }
            if (!c.Walkable(req.caster.Map))
                return;
            if (req.maxRegionsRadius > 0 && c.GetRegion(req.caster.Map).mark != inRadiusMark)
            {
                if (DebugViewSettings.drawCastPositionSearch)
                    req.caster.Map.debugDrawer.FlashCell(c, 0.64f, "reg radius");
                return;
            }
            if (!req.caster.Map.reachability.CanReach(req.caster.Position, c, PathEndMode.OnCell,
                TraverseParms.For(req.caster, Danger.Some, TraverseMode.ByPawn, false)))
                if (DebugViewSettings.drawCastPositionSearch)
                    req.caster.Map.debugDrawer.FlashCell(c, 0.4f, "can't reach");
        }

        // Verse.AI.CastPositionFinder
        public bool TryFindCastPosition(CastPositionRequest req, out IntVec3 dest)
        {
            ByteGrid avoidGrid = null;
            var inRadiusMark = 0;
            if (pawn.CurJob.verbToUse == null)
            {
                Log.Error(pawn + " tried to find casting position without a verb.");
                dest = IntVec3.Invalid;
                return false;
            }
            if (req.maxRegionsRadius > 0)
            {
                var region = req.caster.PositionHeld.GetRegion(pawn.Map);
                if (region == null)
                {
                    Log.Error("TryFindCastPosition requiring region traversal but root region is null.");
                    dest = IntVec3.Invalid;
                    return false;
                }
                inRadiusMark = Rand.Int;
                RegionTraverser.MarkRegionsBFS(region, null, req.maxRegionsRadius, inRadiusMark);
                if (req.maxRangeFromLocus > 0.01f)
                {
                    var region2 = req.locus.GetRegion(pawn.Map);
                    if (region2 == null)
                    {
                        Log.Error("locus " + req.locus + " has no region");
                        dest = IntVec3.Invalid;
                        return false;
                    }
                    if (region2.mark != inRadiusMark)
                    {
                        Log.Error(string.Concat(req.caster, " can't possibly get to locus ", req.locus,
                            " as it's not in a maxRegionsRadius of ", req.maxRegionsRadius,
                            ". Overriding maxRegionsRadius."));
                        req.maxRegionsRadius = 0;
                    }
                }
            }
            var cellRect = CellRect.WholeMap(req.caster.Map);
            if (req.maxRangeFromCaster > 0.01f)
            {
                var numSolo = Mathf.CeilToInt(req.maxRangeFromCaster);
                var otherRect = new CellRect(pawn.PositionHeld.x - numSolo, pawn.PositionHeld.z - numSolo,
                    numSolo * 2 + 1, numSolo * 2 + 1);
                cellRect.ClipInsideRect(otherRect);
            }
            var num2 = Mathf.CeilToInt(req.maxRangeFromTarget);
            var otherRect2 = new CellRect(TargetA.Cell.x - num2, TargetA.Cell.z - num2, num2 * 2 + 1, num2 * 2 + 1);
            cellRect.ClipInsideRect(otherRect2);
            if (req.maxRangeFromLocus > 0.01f)
            {
                var numThree = Mathf.CeilToInt(req.maxRangeFromLocus);
                var otherRect3 = new CellRect(TargetA.Cell.x - numThree, TargetA.Cell.z - numThree, numThree * 2 + 1,
                    numThree * 2 + 1);
                cellRect.ClipInsideRect(otherRect3);
            }
            var bestSpot = IntVec3.Invalid;
            var bestSpotPref = 0.001f;
            var maxRangeFromCasterSquared = req.maxRangeFromCaster * req.maxRangeFromCaster;
            var maxRangeFromTargetSquared = req.maxRangeFromTarget * req.maxRangeFromTarget;
            var maxRangeFromLocusSquared = req.maxRangeFromLocus * req.maxRangeFromLocus;
            var rangeFromTarget = (req.caster.Position - TargetA.Cell).LengthHorizontal;
            float rangeFromTargetSquared = (req.caster.Position - TargetA.Cell).LengthHorizontalSquared;
            var rangeFromCasterToCellSquared = 0f;
            var optimalRangeSquared = pawn.CurJob.verbToUse.verbProps.range * 0.8f *
                                      (pawn.CurJob.verbToUse.verbProps.range * 0.8f);
            /////////////////// Evaluate Cell method

            var c = req.caster.PositionHeld;
            EvaluateCell(c, req, maxRangeFromTargetSquared, maxRangeFromLocusSquared, maxRangeFromCasterSquared,
                rangeFromCasterToCellSquared, inRadiusMark);
            var num = -1f;
            /////////////////// CAST POSITION PREFERENCE
            var flag = true;
            var list = req.caster.Map.thingGrid.ThingsListAtFast(c);
            for (var i = 0; i < list.Count; i++)
            {
                var thing = list[i];
                if (thing is Fire fire && fire.parent == null)
                {
                    num = -1f;
                    goto MainSequenceTwo;
                }
                if (thing.def.passability == Traversability.PassThroughOnly)
                    flag = false;
            }
            num = 0.3f;
            if (req.caster.kindDef.aiAvoidCover)
                num += 8f - CoverUtility.TotalSurroundingCoverScore(c, req.caster.Map);
            if (req.wantCoverFromTarget)
                num += CoverUtility.CalculateOverallBlockChance(c, TargetLocA, req.caster.Map);
            var numTwo = (req.caster.Position - c).LengthHorizontal;
            if (rangeFromTarget > 100f)
            {
                numTwo -= rangeFromTarget - 100f;
                if (numTwo < 0f)
                    numTwo = 0f;
            }
            num *= Mathf.Pow(0.967f, num2);
            var num3 = 1f;
            float rangeFromTargetToCellSquared = (c - TargetLocA).LengthHorizontalSquared;
            //rangeFromCasterToCellSquared = (req.target.Position - c).LengthHorizontalSquared;
            var num4 = Mathf.Abs(rangeFromTargetToCellSquared - optimalRangeSquared) / optimalRangeSquared;
            num4 = 1f - num4;
            num4 = 0.7f + 0.3f * num4;
            num3 *= num4;
            if (rangeFromTargetToCellSquared < 25f)
                num3 *= 0.5f;
            num *= num3;
            if (rangeFromCasterToCellSquared > rangeFromTargetSquared)
                num *= 0.4f;
            if (!flag)
                num *= 0.2f;
            ///////////////////////////////////////////////
            MainSequenceTwo:
            if (avoidGrid != null)
            {
                var b = avoidGrid[c];
                num *= Mathf.Max(0.1f, (37f - b) / 37f);
            }
            if (DebugViewSettings.drawCastPositionSearch)
                req.caster.Map.debugDrawer.FlashCell(c, num / 4f, num.ToString("F3"));
            if (num < bestSpotPref)
                goto MainSequence;
            if (!pawn.CurJob.verbToUse.CanHitTargetFrom(c, TargetLocA))
            {
                if (DebugViewSettings.drawCastPositionSearch)
                    req.caster.Map.debugDrawer.FlashCell(c, 0.6f, "can't hit");
                goto MainSequence;
            }
            if (req.caster.Map.pawnDestinationReservationManager.IsReserved(c))
            {
                if (DebugViewSettings.drawCastPositionSearch)
                    req.caster.Map.debugDrawer.FlashCell(c, num * 0.9f, "resvd");
                goto MainSequence;
            }
            bestSpot = c;
            bestSpotPref = num;
            /////////////////////////////////////////////////////
            MainSequence:
            if (bestSpotPref >= 1.0)
            {
                dest = req.caster.Position;
                return true;
            }

            var slope = -1f / CellLine.Between(TargetLocA, req.caster.Position).Slope;
            var cellLine = new CellLine(TargetLocA, slope);
            var flagTwo = cellLine.CellIsAbove(req.caster.Position);
            var iterator = cellRect.GetIterator();
            while (!iterator.Done())
            {
                var current = iterator.Current;
                if (cellLine.CellIsAbove(current) == flagTwo && cellRect.Contains(current))
                    EvaluateCell(current, req, maxRangeFromTargetSquared, maxRangeFromLocusSquared,
                        maxRangeFromCasterSquared, rangeFromCasterToCellSquared, inRadiusMark);
                iterator.MoveNext();
            }
            if (bestSpot.IsValid && bestSpotPref > 0.33f)
            {
                dest = bestSpot;
                return true;
            }
            var iterator2 = cellRect.GetIterator();
            while (!iterator2.Done())
            {
                var current2 = iterator2.Current;
                if (cellLine.CellIsAbove(current2) != flag && cellRect.Contains(current2))
                    EvaluateCell(current2, req, maxRangeFromTargetSquared, maxRangeFromLocusSquared,
                        maxRangeFromCasterSquared, rangeFromCasterToCellSquared, inRadiusMark);
                iterator2.MoveNext();
            }
            if (bestSpot.IsValid)
            {
                dest = bestSpot;
                return true;
            }
            dest = req.caster.PositionHeld;
            return false;
        }


        // Verse.AI.Toils_Combat
        public Toil GotoCastPosition(TargetIndex targetInd, bool closeIfDowned = false)
        {
            var toil = new Toil();
            toil.initAction = delegate
            {
                var actor = toil.actor;
                var curJob = actor.jobs.curJob;
                Thing thing = null;
                Pawn pawn = null;
                thing = curJob.GetTarget(targetInd).Thing;
                IntVec3 intVec;
                if (thing != null)
                {
                    pawn = thing as Pawn;
                    if (!CastPositionFinder.TryFindCastPosition(new CastPositionRequest
                    {
                        caster = toil.actor,
                        target = thing,
                        verb = curJob.verbToUse,
                        maxRangeFromTarget = closeIfDowned && pawn != null && pawn.Downed
                            ? Mathf.Min(curJob.verbToUse.verbProps.range, pawn.RaceProps.executionRange)
                            : curJob.verbToUse.verbProps.range,
                        wantCoverFromTarget = false
                    }, out intVec))
                    {
                        toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                        return;
                    }
                }
                else
                {
                    if (!TryFindCastPosition(new CastPositionRequest
                    {
                        caster = toil.actor,
                        target = null,
                        verb = curJob.verbToUse,
                        maxRangeFromTarget = closeIfDowned && pawn != null && pawn.Downed
                            ? Mathf.Min(curJob.verbToUse.verbProps.range, pawn.RaceProps.executionRange)
                            : curJob.verbToUse.verbProps.range,
                        wantCoverFromTarget = false
                    }, out intVec))
                    {
                        toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                        return;
                    }
                }
                toil.actor.pather.StartPath(intVec, PathEndMode.OnCell);
                actor.Map.pawnDestinationReservationManager.Reserve(actor, curJob, intVec);
            };
            toil.FailOnDespawnedOrNull(targetInd);
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }


        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            var verb = pawn.CurJob.verbToUse as Verb_UseAbility;
            //Toil getInRangeToil = GotoCastPosition(TargetIndex.A, false);
            //yield return getInRangeToil;

            Find.Targeter.targetingVerb = verb;
            yield return new Toil
            {
                initAction = delegate { verb.Ability.PostAbilityAttempt(); },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
        }
    }
}