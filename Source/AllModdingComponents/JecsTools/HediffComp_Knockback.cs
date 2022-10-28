//#define DEBUGLOG

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools
{
    public class HediffComp_Knockback : HediffComp
    {
        public static readonly SimpleCurve AlwaysOneCurve = new SimpleCurve { new CurvePoint(0f, 1f) };

        public HediffCompProperties_Knockback Props => (HediffCompProperties_Knockback)props;

        public override string CompTipStringExtra
        {
            get
            {
                var s = new StringBuilder();
                s.Append("JT_HI_Knockback".Translate(Props.knockbackChance.ToStringPercent()));
                var explosiveProps = Props.explosiveProps;
                if (explosiveProps != null)
                {
                    s.AppendLine().Append("JT_HI_KnockbackExplosive".Translate());
                    var extraInspectStringKey = explosiveProps.extraInspectStringKey;
                    if (extraInspectStringKey != null)
                        s.AppendLine().Append(extraInspectStringKey);
                }
                return s.ToString();
            }
        }

        public void ApplyKnockback(Pawn target, float damageAbsorbedPercent)
        {
            var instigator = Pawn;
            var props = Props;
            if (props != null && Rand.Chance(props.knockbackChance))
            {
                var explosiveProps = props.explosiveProps;
                if (explosiveProps != null)
                {
                    var map = target.Map;
                    var pos = target.PositionHeld;
                    DebugMessage($"ApplyKnockback({instigator})(target: {target}@{pos}@{map}, " +
                        $"damageAbsorbedPercent: {damageAbsorbedPercent}), " +
                        $"explosiveProps: ({Gen.GetNonNullFieldsDebugInfo(explosiveProps)})");
                    // Based off CompExplosive.Detonate.
                    var explosionEffect = explosiveProps.explosionEffect;
                    if (explosionEffect != null)
                    {
                        Effecter effecter = explosionEffect.Spawn();
                        var targetInfo = new TargetInfo(pos, map);
                        effecter.Trigger(targetInfo, targetInfo);
                        effecter.Cleanup();
                    }
                    GenExplosion.DoExplosion(
                        center: pos,
                        map, 
                        explosiveProps.
                        explosiveRadius, 
                        explosiveProps.explosiveDamageType,
                        instigator, 
                        explosiveProps.damageAmountBase,
                        explosiveProps.armorPenetrationBase,
                        explosiveProps.explosionSound,
                        weapon: null, 
                        projectile: null, 
                        intendedTarget: null,
                        explosiveProps.postExplosionSpawnThingDef, 
                        explosiveProps.postExplosionSpawnChance,
                        explosiveProps.postExplosionSpawnThingCount,
                        explosiveProps.postExplosionGasType,
                        explosiveProps.applyDamageToExplosionCellsNeighbors,
                        explosiveProps.preExplosionSpawnThingDef, 
                        explosiveProps.preExplosionSpawnChance,
                        explosiveProps.preExplosionSpawnThingCount, 
                        explosiveProps.chanceToStartFire,
                        explosiveProps.damageFalloff, 
                        direction: null, 
                        ignoredThings: null
                   );
                }

                if (damageAbsorbedPercent < 1f && target != instigator && !target.Dead && !target.Downed && target.Spawned)
                {
                    if (Rand.Chance(props.stunChance))
                        target.stances.stunner.StunFor(props.stunTicks, instigator);
                    PushEffect(instigator, target, damageAbsorbedPercent, props);
                }
            }
        }

        // Following Push* code is based off ProjectJedi's DamageWorker_ForcePush.

        private enum PushSearchState
        {
            NoCollision,
            Impassable,
            ClosedDoor,
            OutOfBounds,
        }

        public static Vector3 PushResult(Thing caster, Thing thingToPush, float distance,
            out float actualDistance, out bool collision)
        {
            collision = false;
            actualDistance = 0;
            var originLoc = thingToPush.TrueCenter();

            var map = caster.Map;
            if (map != thingToPush.Map)
            {
                DebugMessage($"PushResult(caster: {caster}@{caster.TrueCenter()}@{map}, thingToPush: {thingToPush}@{originLoc}" +
                    $", distance: {distance}, out actualDistance: {actualDistance}, out collision: {collision}): " +
                    $"not same map => {originLoc}");
                return originLoc;
            }

            var casterLoc = caster.TrueCenter();
            var direction = (originLoc - casterLoc).normalized;
            if (direction == Vector3.zero)
            {
                DebugMessage($"PushResult(caster: {caster}@{casterLoc}@{map}, thingToPush: {thingToPush}@{originLoc}" +
                    $", distance: {distance}, out actualDistance: {actualDistance}, out collision: {collision}): " +
                    $"indeterminate angle between thingToPush and caster => {originLoc}");
                return originLoc;
            }

            var originRect = thingToPush.OccupiedRect();
            var originCell = originLoc.ToIntVec3();
            var targetLoc = originLoc + direction * distance;
            var targetCell = targetLoc.ToIntVec3();
            var prevCell = originCell;
            var pathGrid = (thingToPush is Pawn pawnToPush ? map.pathing.For(pawnToPush) : map.pathing.Normal).pathGrid;
            var state = PushSearchState.NoCollision;

            // Using GenSight, which isn't ideal since it's IntVec3-based rather than Vector-based,
            // but it should be accurate enough.
            var cells = new List<IntVec3>(GenSight.PointsOnLineOfSight(originCell, targetCell));
            if (cells.Count == 0 || cells[cells.Count - 1] != targetCell)
                cells.Add(targetCell);
            foreach (var cell in cells)
            {
                DebugMessage($"... PushResult(caster: {caster}@{casterLoc}@{map}, thingToPush: {thingToPush}@{originLoc}" +
                    (originRect.Area > 1 ? $"(occupiedRect={originRect})" : "") + $" to {targetLoc}) @ {cell}");
                if (originRect.Contains(cell)) // skip target's own cells
                    continue;

                if (!cell.InBounds(map))
                {
                    state = PushSearchState.OutOfBounds;
                }
                else if (pathGrid.WalkableFast(cell) && !cell.Impassable(map))
                {
                    if (cell.GetEdifice(map) is Building_Door door && !door.Open)
                        state = PushSearchState.ClosedDoor;
                    // else leave state as PushSearchState.NoCollision
                }
                else
                {
                    state = PushSearchState.Impassable;
                }

                if (state != PushSearchState.NoCollision)
                {
                    collision = state != PushSearchState.OutOfBounds;
                    var cellDist = prevCell.DistanceTo(originCell);
                    var loc = ClampToCell(originLoc + direction * cellDist, prevCell);
                    actualDistance = Vector3.Distance(loc, originLoc);
                    DebugMessage($"PushResult(caster: {caster}@{casterLoc}@{map}, thingToPush: {thingToPush}@{originLoc}" +
                        (originRect.Area > 1 ? $"(occupiedRect={originRect})" : "") +
                        $" to {targetLoc}, distance: {distance}, out actualDistance: {actualDistance}, out collision: {collision}): " +
                        $"{state}@{cell}(prevCell={prevCell}) => clampToPrevCell({originLoc} + {direction} * {cellDist}) = " +
                        $"clampToPrevCell({originLoc + direction * cellDist}) = {loc}");
                    return loc;
                }
                prevCell = cell;
            }

            actualDistance = Vector3.Distance(targetLoc, originLoc);
            DebugMessage($"PushResult(caster: {caster}@{casterLoc}@{map}, thingToPush: {thingToPush}@{originLoc}" +
                (originRect.Area > 1 ? $"(occupiedRect={originRect})" : "") +
                $" to {targetLoc}, distance: {distance}, out actualDistance: {actualDistance}, out collision: {collision}): " +
                $"{state}@{prevCell} => {targetLoc}");
            return targetLoc;
        }

        // Clamps Vector3 loc's x & z to cell defined by IntVec3 cell.
        private static Vector3 ClampToCell(Vector3 loc, IntVec3 cell)
        {
            var x = cell.x;
            var z = cell.z;
            return new Vector3(Mathf.Clamp(loc.x, x, x + 1), loc.y, Mathf.Clamp(loc.z, z, z + 1));
        }

        public static void PushEffect(Thing caster, Thing target, float damageAbsorbedPercent,
            HediffCompProperties_Knockback props)
        {
            if (target is Pawn pawn && pawn.Spawned && !pawn.Downed && !pawn.Dead && pawn.MapHeld != null)
            {
                var origDistance = props.knockDistance.RandomInRange;
                if (origDistance == 0)
                    return;
                var distanceAbsorbedFactor = props.knockDistanceAbsorbedPercentCurve.Evaluate(damageAbsorbedPercent);
                var pawnMass = pawn.GetStatValue(StatDefOf.Mass);
                var inventoryMass = MassUtility.InventoryMass(pawn);
                var distanceMassFactor = props.knockDistanceMassCurve.Evaluate(pawnMass - inventoryMass);
                var distance = origDistance * distanceAbsorbedFactor * distanceMassFactor;
                DebugMessage($"PushEffect(caster: {caster}, target: {target}, damageAbsorbedPercent: {damageAbsorbedPercent}): " +
                    $"distanceAbsorbedFactor = absorbedPercentCurve({damageAbsorbedPercent}) = {distanceAbsorbedFactor}; " +
                    $"distanceMassFactor = massCurve({pawnMass}-{inventoryMass}={pawnMass - inventoryMass}) = {distanceMassFactor}; " +
                    $"distance = ({props.knockDistance} => {origDistance}) * {distanceAbsorbedFactor} * {distanceMassFactor} = {distance}");

                var destLoc = PushResult(caster, target, distance, out var actualDistance, out var collision);

                if (props.knockbackThought != null && (collision || actualDistance > 0f) && pawn.RaceProps.Humanlike)
                    pawn.needs.mood.thoughts.memories.TryGainMemory(props.knockbackThought);

                DamageInfo? impactDinfo = null;
                // Always calculate impactDinfo if we need a FlyingObject, in case something exists at destination
                // by the time the FlyingObject arrives at it.
                if (collision || actualDistance > 0f)
                {
                    var distancePercent = actualDistance / distance;
                    var origImpactDamage = props.knockImpactDamage.RandomInRange;
                    var impactDamageFactor = props.knockImpactDamageDistancePercentCurve.Evaluate(distancePercent);
                    var impactDamage = origImpactDamage * impactDamageFactor;
                    DebugMessage($"PushEffect(caster: {caster}, target: {target}, damageAbsorbedPercent: {damageAbsorbedPercent}): " +
                        $"impactDamageFactor = distanceCurve({actualDistance}/{distance}={distancePercent}) = {impactDamageFactor}; " +
                        $"impactDamage = ({props.knockImpactDamage} => {origImpactDamage}) * {impactDamageFactor} = {impactDamage}");
                    if (impactDamage > 0f)
                        impactDinfo = new DamageInfo(props.knockImpactDamageType, impactDamage);
                }

                if (actualDistance > 0f)
                {
                    var flyingObject =
                        (FlyingObject)GenSpawn.Spawn(MiscDefOf.JT_FlyingObject, target.PositionHeld, target.MapHeld);
                    flyingObject.Props.speed = props.knockbackSpeed;
                    flyingObject.Launch(caster, destLoc.ToIntVec3(), target, impactDinfo);
                    DebugMessage($"PushEffect(caster: {caster}, target: {target}, damageAbsorbedPercent: {damageAbsorbedPercent}): " +
                        $"flyingObject = {flyingObject}");
                }
                else if (impactDinfo is DamageInfo immediateImpactDinfo)
                {
                    DebugMessage($"PushEffect(caster: {caster}, target: {target}, damageAbsorbedPercent: {damageAbsorbedPercent}): " +
                        $"immediateImpactDinfo = {immediateImpactDinfo}");
                    target.TakeDamage(immediateImpactDinfo);
                }
            }
        }

        [Conditional("DEBUGLOG")]
        private static void DebugMessage(string s)
        {
            Log.Message(s);
        }
    }
}
