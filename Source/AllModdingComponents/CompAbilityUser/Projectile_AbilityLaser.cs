using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace AbilityUser
{
    public class Projectile_AbilityLaser : Projectile_AbilityBase
    {
        // Variables.
        public int tickCounter = 0;
        public Thing hitThing = null;

        // Draw variables.
        public Material preFiringTexture;
        public Material postFiringTexture;
        public Matrix4x4 drawingMatrix = default(Matrix4x4);
        public Vector3 drawingScale;
        public Vector3 drawingPosition;
        public float drawingIntensity = 0f;
        public Material drawingTexture;

        // Custom XML variables.
        public float preFiringInitialIntensity = 0f;
        public float preFiringFinalIntensity = 0f;
        public float postFiringInitialIntensity = 0f;
        public float postFiringFinalIntensity = 0f;
        public int preFiringDuration = 0;
        public int postFiringDuration = 0;
        public float startFireChance = 0;
        public bool canStartFire = false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.drawingTexture = this.def.DrawMatSingle;
        }

        /// <summary>
        /// Get parameters from XML.
        /// </summary>
        public void GetParametersFromXml()
        {
            ProjectileDef_AbilityLaser additionalParameters = this.def as ProjectileDef_AbilityLaser;

            this.preFiringDuration = additionalParameters.preFiringDuration;
            this.postFiringDuration = additionalParameters.postFiringDuration;

            // Draw.
            this.preFiringInitialIntensity = additionalParameters.preFiringInitialIntensity;
            this.preFiringFinalIntensity = additionalParameters.preFiringFinalIntensity;
            this.postFiringInitialIntensity = additionalParameters.postFiringInitialIntensity;
            this.postFiringFinalIntensity = additionalParameters.postFiringFinalIntensity;
            this.startFireChance = additionalParameters.StartFireChance;
            this.canStartFire = additionalParameters.CanStartFire;
        }

        /// <summary>
        /// Save/load data from a savegame file (apparently not used for projectile for now).
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.tickCounter, "tickCounter", 0);

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
            //  //Log.Message("Tickng Ma Lazor");
            // Directly call the Projectile base Tick function (we want to completely override the Projectile Tick() function).
            //((ThingWithComponents)this).Tick(); // Does not work...
            try
            {

                if (this.tickCounter == 0)
                {
                    GetParametersFromXml();
                    PerformPreFiringTreatment();
                }

                // Pre firing.
                if (this.tickCounter < this.preFiringDuration)
                {
                    GetPreFiringDrawingParameters();
                }
                // Firing.
                else if (this.tickCounter == this.preFiringDuration)
                {
                    Fire();
                    GetPostFiringDrawingParameters();
                }
                // Post firing.
                else
                {
                    GetPostFiringDrawingParameters();
                }
                if (this.tickCounter == (this.preFiringDuration + this.postFiringDuration) && !this.Destroyed)
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
                this.tickCounter++;
            }
            catch
            {
                if (!this.Destroyed) this.Destroy(DestroyMode.Vanish);
            }

        }

        /// <summary>
        /// Performs prefiring treatment: data initalization.
        /// </summary>
        public virtual void PerformPreFiringTreatment()
        {
            DetermineImpactExactPosition();
            Vector3 cannonMouthOffset = ((this.destination - this.origin).normalized * 0.9f);
            this.drawingScale = new Vector3(1f, 1f, (this.destination - this.origin).magnitude - cannonMouthOffset.magnitude);
            this.drawingPosition = this.origin + (cannonMouthOffset / 2) + ((this.destination - this.origin) / 2) + Vector3.up * this.def.Altitude;
            this.drawingMatrix.SetTRS(this.drawingPosition, this.ExactRotation, this.drawingScale);
        }

        /// <summary>
        /// Gets the prefiring drawing parameters.
        /// </summary>
        public virtual void GetPreFiringDrawingParameters()
        {
            if (this.preFiringDuration != 0)
            {
                this.drawingIntensity = this.preFiringInitialIntensity + (this.preFiringFinalIntensity - this.preFiringInitialIntensity) * this.tickCounter / this.preFiringDuration;
            }
        }

        /// <summary>
        /// Gets the postfiring drawing parameters.
        /// </summary>
        public virtual void GetPostFiringDrawingParameters()
        {
            if (this.postFiringDuration != 0)
            {
                this.drawingIntensity = this.postFiringInitialIntensity + (this.postFiringFinalIntensity - this.postFiringInitialIntensity) * ((this.tickCounter - (float)this.preFiringDuration) / this.postFiringDuration);
            }
        }

        /// <summary>
        /// Checks for colateral targets (cover, neutral animal, pawn) along the trajectory.
        /// </summary>
        protected void DetermineImpactExactPosition()
        {
            // We split the trajectory into small segments of approximatively 1 cell size.
            Vector3 trajectory = (this.destination - this.origin);
            int numberOfSegments = (int)trajectory.magnitude;
            Vector3 trajectorySegment = (trajectory / trajectory.magnitude);

            Vector3 temporaryDestination = this.origin; // Last valid tested position in case of an out of boundaries shot.
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
                    List<Thing> list = this.Map.thingGrid.ThingsListAt(this.Position);
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing current = list[i];

                        // Check impact on a wall.
                        if (current.def.Fillage == FillCategory.Full)
                        {
                            this.destination = testedPosition.ToVector3Shifted() + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
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
                                this.destination = testedPosition.ToVector3Shifted() + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
                                this.hitThing = pawn;
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
        public virtual void Fire() => ApplyDamage(this.hitThing);


        /// <summary>
        /// Impacts a pawn/object or the ground.
        /// </summary>
        public override void Impact_Override(Thing hitThing)
        {

            base.Impact_Override(hitThing);
            if (hitThing != null)
            {
                int damageAmountBase = this.def.projectile.damageAmountBase;
                DamageInfo dinfo = new DamageInfo(this.def.projectile.damageDef, damageAmountBase, this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef);
                hitThing.TakeDamage(dinfo);
                //hitThing.TakeDamage(dinfo);
                if (this.canStartFire && Rand.Range(0f, 1f) > this.startFireChance)
                {
                    hitThing.TryAttachFire(0.05f);
                }
                if (hitThing is Pawn pawn)
                {
                    PostImpactEffects(this.launcher as Pawn, pawn);
                    MoteMaker.ThrowMicroSparks(this.destination, this.Map);
                    MoteMaker.MakeStaticMote(this.destination, this.Map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
                }
            }
            else
            {
                SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Position, this.Map, false), MaintenanceType.None);
                SoundDefOf.BulletImpactGround.PlayOneShot(info);
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
            UnityEngine.Graphics.DrawMesh(MeshPool.plane10, this.drawingMatrix, FadedMaterialPool.FadedVersionOf(this.drawingTexture, this.drawingIntensity), 0);
        }
    }
}
