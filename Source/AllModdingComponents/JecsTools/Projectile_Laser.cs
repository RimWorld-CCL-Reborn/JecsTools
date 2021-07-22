using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace JecsTools
{
    public class Projectile_LaserConfig
    {
        public Vector3 offset;
    }

    public class Projectile_Laser : Projectile
    {
        // Variables.
        public int tickCounter = 0;

        public Thing hitThing = null;


        // Custom XML variables.
        public float preFiringInitialIntensity = 0f;

        public float preFiringFinalIntensity = 0f;
        public float postFiringInitialIntensity = 0f;
        public float postFiringFinalIntensity = 0f;
        public int preFiringDuration = 0;
        public int postFiringDuration = 0;
        public float startFireChance = 0;
        public bool canStartFire = false;

        // Draw variables.
        public Material preFiringTexture;

        public Material postFiringTexture;

        public List<Matrix4x4> drawingMatrix = null;

        //public Vector3 drawingScale;
        //public Vector3 drawingPosition;
        public float drawingIntensity = 0f;

        public Material drawingTexture;

        protected virtual void Explode(Thing hitThing, bool destroy = false)
        {
            var map = Map;
            var targetPosition = hitThing?.PositionHeld ?? destination.ToIntVec3();
            if (destroy)
                Destroy();
            if (def.projectile.explosionEffect != null)
            {
                var effecter = def.projectile.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(targetPosition, map),
                    new TargetInfo(targetPosition, map));
                effecter.Cleanup();
            }
            GenExplosion.DoExplosion(targetPosition, map, def.projectile.explosionRadius, def.projectile.damageDef,
                launcher, def.projectile.GetDamageAmount(1f), 0f, def.projectile.soundExplode, equipmentDef, def, null,
                def.projectile.postExplosionSpawnThingDef, def.projectile.postExplosionSpawnChance,
                def.projectile.postExplosionSpawnThingCount, def.projectile.applyDamageToExplosionCellsNeighbors,
                def.projectile.preExplosionSpawnThingDef, def.projectile.preExplosionSpawnChance,
                def.projectile.preExplosionSpawnThingCount, def.projectile.explosionChanceToStartFire,
                def.projectile.explosionDamageFalloff);
        }

        public override void SpawnSetup(Map map, bool blabla)
        {
            base.SpawnSetup(map, blabla);
            drawingTexture = def.DrawMatSingle;
        }

        /// <summary>
        /// Get parameters from XML.
        /// </summary>
        public void GetParametersFromXml()
        {
            var additionalParameters = def as ThingDef_LaserProjectile;

            preFiringDuration = additionalParameters.preFiringDuration;
            postFiringDuration = additionalParameters.postFiringDuration;

            // Draw.
            preFiringInitialIntensity = additionalParameters.preFiringInitialIntensity;
            preFiringFinalIntensity = additionalParameters.preFiringFinalIntensity;
            postFiringInitialIntensity = additionalParameters.postFiringInitialIntensity;
            postFiringFinalIntensity = additionalParameters.postFiringFinalIntensity;
            startFireChance = additionalParameters.StartFireChance;
            canStartFire = additionalParameters.CanStartFire;
        }

        /// <summary>
        /// Save/load data from a savegame file (apparently not used for projectile for now).
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickCounter, nameof(tickCounter));

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                GetParametersFromXml();
            }
        }

        /// <summary>
        /// Main projectile sequence.
        /// </summary>
        public override void Tick()
        {
            //  Log.Message("Tickng Ma Lazor");
            // Directly call the Projectile base Tick function (we want to completely override the Projectile Tick() function).
            //((ThingWithComponents)this).Tick(); // Does not work...
            try
            {
                if (tickCounter == 0)
                {
                    GetParametersFromXml();
                    PerformPreFiringTreatment();
                }

                // Pre firing.
                if (tickCounter < preFiringDuration)
                {
                    GetPreFiringDrawingParameters();
                }
                // Firing.
                else if (tickCounter == preFiringDuration)
                {
                    Fire();
                    GetPostFiringDrawingParameters();
                }
                // Post firing.
                else
                {
                    GetPostFiringDrawingParameters();
                }
                if (tickCounter == (preFiringDuration + postFiringDuration) && !Destroyed)
                {
                    Destroy();
                }
                if (launcher != null)
                {
                    if (launcher is Pawn)
                    {
                        var launcherPawn = launcher as Pawn;
                        if (launcherPawn.Dead && !Destroyed)
                        {
                            Destroy();
                        }
                    }
                }
                tickCounter++;
            }
            catch
            {
                Destroy();
            }
        }

        /// <summary>
        /// Performs prefiring treatment: data initalization.
        /// </summary>
        public virtual void PerformPreFiringTreatment()
        {
            DetermineImpactExactPosition();
            var cannonMouthOffset = (destination - origin).normalized * 0.9f;
            if (Def.graphicSettings.NullOrEmpty())
            {
                var drawingScale = new Vector3(1f, 1f,
                    (destination - origin).magnitude - cannonMouthOffset.magnitude);
                var drawingPosition = origin + (cannonMouthOffset / 2) + ((destination - origin) / 2) +
                                      Vector3.up * def.Altitude;
                drawingMatrix = new List<Matrix4x4>();
                var drawing = Matrix4x4.TRS(drawingPosition, ExactRotation, drawingScale);
                drawingMatrix.Add(drawing);
            }
            else
            {
                drawingMatrix = new List<Matrix4x4>();
                if (!Def.cycleThroughFiringPositions)
                {
                    foreach (var setting in Def.graphicSettings)
                    {
                        AddLaserGraphicUsing(setting);
                    }
                }
                else
                {
                    if (HarmonyPatches.AlternatingFireTracker.TryGetValue(launcher, out var curIndex))
                    {
                        curIndex = (curIndex + 1) % Def.graphicSettings.Count;
                        HarmonyPatches.AlternatingFireTracker[launcher] = curIndex;
                    }
                    else
                    {
                        curIndex = 0; // technically unnecessary but good to be explicit
                        HarmonyPatches.AlternatingFireTracker.Add(launcher, curIndex);
                    }
                    AddLaserGraphicUsing(Def.graphicSettings[curIndex]);
                }
            }
        }

        private void AddLaserGraphicUsing(Projectile_LaserConfig setting)
        {
            var curCannonMouthOffset = (destination - origin).normalized * 0.9f;
            var drawingScale = new Vector3(1f, 1f,
                (destination - origin).magnitude - curCannonMouthOffset.magnitude);
            var drawingPosition = origin + (curCannonMouthOffset / 2) + ((destination - origin) / 2) +
                                  Vector3.up * def.Altitude;
            var num = 0f;
            if ((destination - origin).MagnitudeHorizontalSquared() > 0.001f)
            {
                num = (destination - origin).AngleFlat();
            }
            drawingPosition += setting.offset.RotatedBy(num);
            var drawing = Matrix4x4.TRS(drawingPosition, ExactRotation, drawingScale);

            drawingMatrix.Add(drawing);
        }

        public ThingDef_LaserProjectile Def
        {
            get => def as ThingDef_LaserProjectile;
        }

        /// <summary>
        /// Gets the prefiring drawing parameters.
        /// </summary>
        public virtual void GetPreFiringDrawingParameters()
        {
            if (preFiringDuration != 0)
            {
                drawingIntensity = preFiringInitialIntensity + (preFiringFinalIntensity - preFiringInitialIntensity) *
                                   tickCounter / preFiringDuration;
            }
        }

        /// <summary>
        /// Gets the postfiring drawing parameters.
        /// </summary>
        public virtual void GetPostFiringDrawingParameters()
        {
            if (postFiringDuration != 0)
            {
                drawingIntensity = postFiringInitialIntensity +
                                   (postFiringFinalIntensity - postFiringInitialIntensity) *
                                   ((tickCounter - (float)preFiringDuration) / postFiringDuration);
            }
        }

        /// <summary>
        /// Checks for colateral targets (cover, neutral animal, pawn) along the trajectory.
        /// </summary>
        protected void DetermineImpactExactPosition()
        {
            // We split the trajectory into small segments of approximatively 1 cell size.
            var trajectory = destination - origin;
            var numberOfSegments = (int)trajectory.magnitude;
            var trajectorySegment = trajectory / trajectory.magnitude;

            var temporaryDestination = origin; // Last valid tested position in case of an out of boundaries shot.
            var exactTestedPosition = origin;

            for (var segmentIndex = 1; segmentIndex <= numberOfSegments; segmentIndex++)
            {
                exactTestedPosition += trajectorySegment;
                var testedPosition = exactTestedPosition.ToIntVec3();

                if (!exactTestedPosition.InBounds(Map))
                {
                    destination = temporaryDestination;
                    break;
                }

                if (!def.projectile.flyOverhead && segmentIndex >= 5)
                {
                    var list = Map.thingGrid.ThingsListAt(Position);
                    for (var i = 0; i < list.Count; i++)
                    {
                        var current = list[i];

                        // Check impact on a wall.
                        if (current.def.Fillage == FillCategory.Full)
                        {
                            destination = testedPosition.ToVector3Shifted() +
                                               new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
                            hitThing = current;
                            break;
                        }

                        // Check impact on a pawn.
                        if (current.def.category == ThingCategory.Pawn)
                        {
                            var pawn = current as Pawn;
                            var chanceToHitCollateralTarget = 0.45f;
                            if (pawn.Downed)
                            {
                                chanceToHitCollateralTarget *= 0.1f;
                            }
                            var targetDistanceFromShooter = (ExactPosition - origin).MagnitudeHorizontal();
                            if (targetDistanceFromShooter < 4f)
                            {
                                chanceToHitCollateralTarget *= 0f;
                            }
                            else
                            {
                                if (targetDistanceFromShooter < 7f)
                                {
                                    chanceToHitCollateralTarget *= 0.5f;
                                }
                                else
                                {
                                    if (targetDistanceFromShooter < 10f)
                                    {
                                        chanceToHitCollateralTarget *= 0.75f;
                                    }
                                }
                            }
                            chanceToHitCollateralTarget *= pawn.RaceProps.baseBodySize;

                            if (Rand.Value < chanceToHitCollateralTarget)
                            {
                                destination = testedPosition.ToVector3Shifted() +
                                                   new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
                                hitThing = pawn;
                                break;
                            }
                        }
                    }
                }

                temporaryDestination = exactTestedPosition;
            }
        }

        /// <summary>
        /// Manages the projectile damage application.
        /// </summary>
        public virtual void Fire()
        {
            ApplyDamage(hitThing);
        }

        /// <summary>
        /// Applies damage on a collateral pawn or an object.
        /// </summary>
        protected void ApplyDamage(Thing hitThing)
        {
            if (hitThing != null)
            {
                // Impact collateral target.
                Impact(hitThing);
            }
            else
            {
                ImpactSomething();
            }
        }

        /// <summary>
        /// Computes what should be impacted in the DestinationCell.
        /// </summary>
        protected void ImpactSomething()
        {
            // Check impact on a thick mountain.
            if (def.projectile.flyOverhead)
            {
                var roofDef = Map.roofGrid.RoofAt(DestinationCell);
                if (roofDef != null && roofDef.isThickRoof)
                {
                    def.projectile.soundHitThickRoof.PlayOneShot(SoundInfo.InMap(new TargetInfo(DestinationCell, Map)));
                    return;
                }
            }

            // Impact the initial targeted pawn.
            if (usedTarget != null)
            {
                if (usedTarget.Thing is Pawn pawn && pawn.Downed && (origin - destination).magnitude > 5f && Rand.Value < 0.2f)
                    Impact(null);
                else
                    Impact(usedTarget.Thing);
            }
            else
            {
                // Impact a pawn in the destination cell if present.
                var thing = Map.thingGrid.ThingAt(DestinationCell, ThingCategory.Pawn);
                if (thing != null)
                {
                    Impact(thing);
                }
                else
                {
                    // Impact any cover object.
                    foreach (var current in Map.thingGrid.ThingsAt(DestinationCell))
                    {
                        if (current.def.fillPercent > 0f || current.def.passability != Traversability.Standable)
                        {
                            Impact(current);
                            return;
                        }
                    }
                    Impact(null);
                }
            }
        }

        /// <summary>
        /// Impacts a pawn/object or the ground.
        /// </summary>
        protected override void Impact(Thing hitThing)
        {
            if (Def.createsExplosion)
            {
                Explode(hitThing, false);
                GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, def.projectile.damageDef,
                    launcher.Faction);
            }

            if (hitThing != null)
            {
                var battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher,
                    hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
                Find.BattleLog.Add(battleLogEntry_RangedImpact);

                var dinfo = new DamageInfo(def.projectile.damageDef, def.projectile.GetDamageAmount(1f), def.projectile.GetArmorPenetration(1f), ExactRotation.eulerAngles.y, launcher, weapon: equipmentDef);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);

                if (canStartFire && Rand.Range(0f, 1f) > startFireChance)
                {
                    hitThing.TryAttachFire(0.05f);
                }
                if (hitThing is Pawn pawn)
                {
                    PostImpactEffects(launcher as Pawn, pawn);
                    FleckMaker.ThrowMicroSparks(destination, Map);
                    FleckMaker.Static(destination, Map, FleckDefOf.ShotHit_Dirt);
                }
            }
            else
            {
                SoundDefOf.BulletImpact_Ground.PlayOneShot(SoundInfo.InMap(new TargetInfo(Position, Map)));
                FleckMaker.Static(ExactPosition, Map, FleckDefOf.ShotHit_Dirt);
                FleckMaker.ThrowMicroSparks(ExactPosition, Map);
            }
        }

        /// <summary>
        /// JECRELL:: Added this to make derived classes work easily.
        /// </summary>
        /// <param name="launcher"></param>
        /// <param name="hitTarget"></param>
        public virtual void PostImpactEffects(Pawn launcher, Pawn hitTarget)
        {
        }

        /// <summary>
        /// Draws the laser ray.
        /// </summary>
        public override void Draw()
        {
            Comps_PostDraw();
            if (drawingMatrix != null)
            {
                foreach (var drawing in drawingMatrix)
                {
                    Graphics.DrawMesh(MeshPool.plane10, drawing,
                        FadedMaterialPool.FadedVersionOf(drawingTexture, drawingIntensity), 0);
                }
            }
        }
    }
}
