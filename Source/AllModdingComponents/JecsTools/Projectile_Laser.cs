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
            Map map = base.Map;
            IntVec3 targetPosition = hitThing?.PositionHeld ?? this.destination.ToIntVec3();
            if (destroy) this.Destroy(DestroyMode.Vanish);
            if (this.def.projectile.explosionEffect != null)
            {
                Effecter effecter = this.def.projectile.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(targetPosition, map, false),
                    new TargetInfo(targetPosition, map, false));
                effecter.Cleanup();
            }
            IntVec3 position = targetPosition;
            Map map2 = map;
            float explosionRadius = this.def.projectile.explosionRadius;
            DamageDef damageDef = this.def.projectile.damageDef;
            Thing launcher = this.launcher;
            int damageAmountBase = this.def.projectile.GetDamageAmount(1f);
            SoundDef soundExplode = this.def.projectile.soundExplode;
            ThingDef equipmentDef = this.equipmentDef;
            ThingDef def = this.def;
            ThingDef postExplosionSpawnThingDef = this.def.projectile.postExplosionSpawnThingDef;
            float postExplosionSpawnChance = this.def.projectile.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = this.def.projectile.postExplosionSpawnThingCount;
            ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
            GenExplosion.DoExplosion(position, map2, explosionRadius, damageDef, launcher, damageAmountBase, 0f,
                soundExplode, equipmentDef, def, null, postExplosionSpawnThingDef, postExplosionSpawnChance,
                postExplosionSpawnThingCount, this.def.projectile.applyDamageToExplosionCellsNeighbors,
                preExplosionSpawnThingDef, this.def.projectile.preExplosionSpawnChance,
                this.def.projectile.preExplosionSpawnThingCount, this.def.projectile.explosionChanceToStartFire,
                this.def.projectile.explosionDamageFalloff);
        }

        private int ticksToDetonation;

        public override void SpawnSetup(Map map, bool blabla)
        {
            base.SpawnSetup(map, blabla);
            drawingTexture = this.def.DrawMatSingle;
        }

        /// <summary>
        /// Get parameters from XML.
        /// </summary>
        public void GetParametersFromXml()
        {
            ThingDef_LaserProjectile additionalParameters = def as ThingDef_LaserProjectile;

            preFiringDuration = additionalParameters.preFiringDuration;
            postFiringDuration = additionalParameters.postFiringDuration;

            // Draw.
            preFiringInitialIntensity = additionalParameters.preFiringInitialIntensity;
            preFiringFinalIntensity = additionalParameters.preFiringFinalIntensity;
            postFiringInitialIntensity = additionalParameters.postFiringInitialIntensity;
            postFiringFinalIntensity = additionalParameters.postFiringFinalIntensity;
            startFireChance = additionalParameters.StartFireChance;
            this.canStartFire = additionalParameters.CanStartFire;
        }

        /// <summary>
        /// Save/load data from a savegame file (apparently not used for projectile for now).
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref tickCounter, "tickCounter", 0);

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
                else if (tickCounter == this.preFiringDuration)
                {
                    Fire();
                    GetPostFiringDrawingParameters();
                }
                // Post firing.
                else
                {
                    GetPostFiringDrawingParameters();
                }
                if (tickCounter == (this.preFiringDuration + this.postFiringDuration) && !this.Destroyed)
                {
                    this.Destroy(DestroyMode.Vanish);
                }
                if (this.launcher != null)
                {
                    if (this.launcher is Pawn)
                    {
                        Pawn launcherPawn = this.launcher as Pawn;
                        if ((((launcherPawn.Dead) == true) && !this.Destroyed))
                        {
                            this.Destroy(DestroyMode.Vanish);
                        }
                    }
                }
                tickCounter++;
            }
            catch
            {
                this.Destroy(DestroyMode.Vanish);
            }
        }

        /// <summary>
        /// Performs prefiring treatment: data initalization.
        /// </summary>
        public virtual void PerformPreFiringTreatment()
        {
            DetermineImpactExactPosition();
            Vector3 cannonMouthOffset = ((this.destination - this.origin).normalized * 0.9f);
            if (this.Def.graphicSettings.NullOrEmpty())
            {
                var drawingScale = new Vector3(1f, 1f,
                    (this.destination - this.origin).magnitude - cannonMouthOffset.magnitude);
                var drawingPosition = this.origin + (cannonMouthOffset / 2) + ((this.destination - this.origin) / 2) +
                                      Vector3.up * this.def.Altitude;
                drawingMatrix = new List<Matrix4x4>();
                var drawing = default(Matrix4x4);
                drawing.SetTRS(drawingPosition, this.ExactRotation, drawingScale);
                drawingMatrix.Add(drawing);
            }
            else
            {
                drawingMatrix = new List<Matrix4x4>();
                if (!this.Def.cycleThroughFiringPositions)
                {
                    foreach (var setting in this.Def.graphicSettings)
                    {
                        AddLaserGraphicUsing(setting);
                    }
                }
                else
                {
                    var curIndex = 0;
                    if (HarmonyPatches.AlternatingFireTracker.ContainsKey(this.launcher))
                    {
                        curIndex = (HarmonyPatches.AlternatingFireTracker[this.launcher] + 1) %
                                   this.Def.graphicSettings.Count;
                        HarmonyPatches.AlternatingFireTracker[this.launcher] = curIndex;
                    }
                    else
                    {
                        HarmonyPatches.AlternatingFireTracker.Add(this.launcher, curIndex);
                    }
                    AddLaserGraphicUsing(this.Def.graphicSettings[curIndex]);
                }
            }
        }

        private void AddLaserGraphicUsing(Projectile_LaserConfig setting)
        {
            var curCannonMouthOffset = ((this.destination - this.origin).normalized * 0.9f);
            var drawingScale = new Vector3(1f, 1f,
                (this.destination - this.origin).magnitude - curCannonMouthOffset.magnitude);
            var drawingPosition = this.origin + (curCannonMouthOffset / 2) + ((this.destination - this.origin) / 2) +
                                  Vector3.up * this.def.Altitude;
            float num = 0f;
            if ((this.destination - this.origin).MagnitudeHorizontalSquared() > 0.001f)
            {
                num = (this.destination - this.origin).AngleFlat();
            }
            drawingPosition += setting.offset.RotatedBy(num);
            var drawing = default(Matrix4x4);
            drawing.SetTRS(drawingPosition, this.ExactRotation, drawingScale);

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
                                   (float) tickCounter / (float) preFiringDuration;
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
                                   (((float) tickCounter - (float) preFiringDuration) / (float) postFiringDuration);
            }
        }

        /// <summary>
        /// Checks for colateral targets (cover, neutral animal, pawn) along the trajectory.
        /// </summary>
        protected void DetermineImpactExactPosition()
        {
            // We split the trajectory into small segments of approximatively 1 cell size.
            Vector3 trajectory = (this.destination - this.origin);
            int numberOfSegments = (int) trajectory.magnitude;
            Vector3 trajectorySegment = (trajectory / trajectory.magnitude);

            Vector3
                temporaryDestination = this.origin; // Last valid tested position in case of an out of boundaries shot.
            Vector3 exactTestedPosition = this.origin;
            IntVec3 testedPosition = exactTestedPosition.ToIntVec3();

            for (int segmentIndex = 1; segmentIndex <= numberOfSegments; segmentIndex++)
            {
                exactTestedPosition += trajectorySegment;
                testedPosition = exactTestedPosition.ToIntVec3();

                if (!exactTestedPosition.InBounds(this.Map))
                {
                    this.destination = temporaryDestination;
                    break;
                }

                if (!this.def.projectile.flyOverhead && segmentIndex >= 5)
                {
                    List<Thing> list = this.Map.thingGrid.ThingsListAt(base.Position);
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing current = list[i];

                        // Check impact on a wall.
                        if (current.def.Fillage == FillCategory.Full)
                        {
                            this.destination = testedPosition.ToVector3Shifted() +
                                               new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
                            this.hitThing = current;
                            break;
                        }

                        // Check impact on a pawn.
                        if (current.def.category == ThingCategory.Pawn)
                        {
                            Pawn pawn = current as Pawn;
                            float chanceToHitCollateralTarget = 0.45f;
                            if (pawn.Downed)
                            {
                                chanceToHitCollateralTarget *= 0.1f;
                            }
                            float targetDistanceFromShooter = (this.ExactPosition - this.origin).MagnitudeHorizontal();
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
                                this.destination = testedPosition.ToVector3Shifted() +
                                                   new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
                                this.hitThing = (Thing) pawn;
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
            ApplyDamage(this.hitThing);
        }

        /// <summary>
        /// Applies damage on a collateral pawn or an object.
        /// </summary>
        protected void ApplyDamage(Thing hitThing)
        {
            if (hitThing != null)
            {
                // Impact collateral target.
                this.Impact(hitThing);
            }
            else
            {
                this.ImpactSomething();
            }
        }

        /// <summary>
        /// Computes what should be impacted in the DestinationCell.
        /// </summary>
        protected void ImpactSomething()
        {
            // Check impact on a thick mountain.
            if (this.def.projectile.flyOverhead)
            {
                RoofDef roofDef = this.Map.roofGrid.RoofAt(this.DestinationCell);
                if (roofDef != null && roofDef.isThickRoof)
                {
                    SoundInfo info = SoundInfo.InMap(new TargetInfo(this.DestinationCell, this.Map, false),
                        MaintenanceType.None);
                    this.def.projectile.soundHitThickRoof.PlayOneShot(info);
                    return;
                }
            }

            // Impact the initial targeted pawn.
            if (this.usedTarget != null)
            {
                Pawn pawn = this.usedTarget.Thing as Pawn;
                if (pawn != null && pawn.Downed && (this.origin - this.destination).magnitude > 5f && Rand.Value < 0.2f)
                {
                    this.Impact(null);
                    return;
                }
                this.Impact(this.usedTarget.Thing);
                return;
            }
            else
            {
                // Impact a pawn in the destination cell if present.
                Thing thing = this.Map.thingGrid.ThingAt(this.DestinationCell, ThingCategory.Pawn);
                if (thing != null)
                {
                    this.Impact(thing);
                    return;
                }
                // Impact any cover object.
                foreach (Thing current in this.Map.thingGrid.ThingsAt(this.DestinationCell))
                {
                    if (current.def.fillPercent > 0f || current.def.passability != Traversability.Standable)
                    {
                        this.Impact(current);
                        return;
                    }
                }
                this.Impact(null);
                return;
            }
        }

        /// <summary>
        /// Impacts a pawn/object or the ground.
        /// </summary>
        protected override void Impact(Thing hitThing)
        {
            if (this.Def.createsExplosion)
            {
                this.Explode(hitThing, false);
                GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, this.def.projectile.damageDef,
                    this.launcher.Faction);
            }

            if (hitThing != null)
            {
                Map map = base.Map;
                BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(this.launcher,
                    hitThing, this.intendedTarget.Thing, this.equipmentDef, this.def, this.targetCoverDef);
                Find.BattleLog.Add(battleLogEntry_RangedImpact);
                
                    int damageAmountBase = this.def.projectile.GetDamageAmount(1f);
                    DamageDef damageDef = this.def.projectile.damageDef;
                    int amount = damageAmountBase;
                    float y = this.ExactRotation.eulerAngles.y;
                    Thing launcher = this.launcher;
                    ThingDef equipmentDef = this.equipmentDef;
                    DamageInfo dinfo = new DamageInfo(damageDef, amount, this.def.projectile.GetArmorPenetration(1f), y, launcher, null, equipmentDef,
                        DamageInfo.SourceCategory.ThingOrUnknown);
                    hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                
                //int damageAmountBase = this.def.projectile.DamageAmount;
                //DamageInfo dinfo = new DamageInfo(this.def.projectile.damageDef, damageAmountBase, this.ExactRotation.eulerAngles.y, this.launcher, null, equipmentDef);
                //hitThing.TakeDamage(dinfo);
                //hitThing.TakeDamage(dinfo);
                if (this.canStartFire && Rand.Range(0f, 1f) > startFireChance)
                {
                    hitThing.TryAttachFire(0.05f);
                }
                Pawn pawn = hitThing as Pawn;
                if (pawn != null)
                {
                    PostImpactEffects(this.launcher as Pawn, pawn);
                    MoteMaker.ThrowMicroSparks(this.destination, this.Map);
                    MoteMaker.MakeStaticMote(this.destination, this.Map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
                }
            }
            else
            {
                SoundInfo info = SoundInfo.InMap(new TargetInfo(base.Position, this.Map, false), MaintenanceType.None);
                SoundDefOf.BulletImpact_Ground.PlayOneShot(info);
                MoteMaker.MakeStaticMote(this.ExactPosition, this.Map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
                MoteMaker.ThrowMicroSparks(this.ExactPosition, this.Map);
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
            this.Comps_PostDraw();
            if (!drawingMatrix.NullOrEmpty())
            {
                foreach (var drawing in drawingMatrix)
                {
                    UnityEngine.Graphics.DrawMesh(MeshPool.plane10, drawing,
                        FadedMaterialPool.FadedVersionOf(drawingTexture, drawingIntensity), 0);
                }
            }
        }
    }
}