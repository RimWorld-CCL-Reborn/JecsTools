using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace AbilityUser
{
    public class Verb_UseAbility : Verb_LaunchProjectile
    {
        private PawnAbility ability = null;
        public Action<Thing> timeSavingActionVariable = null;

        public PawnAbility Ability { get => ability; set => ability = value; }
        public List<LocalTargetInfo> TargetsAoE = new List<LocalTargetInfo>();

        public VerbProperties_Ability UseAbilityProps => (VerbProperties_Ability)this.verbProps;
        public ProjectileDef_Ability AbilityProjectileDef => this.UseAbilityProps.defaultProjectile as ProjectileDef_Ability;
        public CompAbilityUser AbilityUserComp => this.CasterPawn.TryGetComp<CompAbilityUser>();

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            float result =  this.verbProps.defaultProjectile.projectile.explosionRadius;
            if (this.UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties != null)
            {
                if (this.UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties.showRangeOnSelect)
                {
                    result = this.UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties.range;
                }
            }
            return result;
        }

        protected virtual void UpdateTargets()
        {
            this.TargetsAoE.Clear();
            if (this.UseAbilityProps.AbilityTargetCategory == AbilityTargetCategory.TargetAoE)
            {
                //Log.Message("AoE Called");
                if (this.UseAbilityProps.TargetAoEProperties == null)
                {
                    Log.Error("Tried to Cast AoE-Ability without defining a target class");
                }

                List<Thing> targets = new List<Thing>();

                //Handle TargetAoE start location.
                IntVec3 aoeStartPosition = this.caster.PositionHeld;
                if (!this.UseAbilityProps.TargetAoEProperties.startsFromCaster)
                {
                    aoeStartPosition = this.currentTarget.Cell;
                }
                
                //Handle friendly fire targets.
                if (!this.UseAbilityProps.TargetAoEProperties.friendlyFire)
                {
                    targets = this.caster.Map.listerThings.AllThings.Where(x => (x.Position.InHorDistOf(aoeStartPosition, this.UseAbilityProps.TargetAoEProperties.range)) && (this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType())) && !x.Faction.HostileTo(Faction.OfPlayer)).ToList<Thing>();
                }
                else if ((this.UseAbilityProps.TargetAoEProperties.targetClass == typeof(Plant)) || (this.UseAbilityProps.TargetAoEProperties.targetClass == typeof(Building)))
                {
                    targets = this.caster.Map.listerThings.AllThings.Where(x => (x.Position.InHorDistOf(aoeStartPosition, this.UseAbilityProps.TargetAoEProperties.range)) && (this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType()))).ToList<Thing>();
                    foreach (Thing targ in targets)
                    {
                        LocalTargetInfo tinfo = new LocalTargetInfo(targ);
                        this.TargetsAoE.Add(tinfo);
                    }
                    return;
                }
                else
                {
                    targets.Clear();
                    targets = this.caster.Map.listerThings.AllThings.Where(x =>
                        (x.Position.InHorDistOf(aoeStartPosition, this.UseAbilityProps.TargetAoEProperties.range)) &&
                        (this.UseAbilityProps.TargetAoEProperties.targetClass.IsAssignableFrom(x.GetType())) &&
                        (x.HostileTo(Faction.OfPlayer) || this.UseAbilityProps.TargetAoEProperties.friendlyFire)).ToList<Thing>();
                }

                int maxTargets = this.UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties.maxTargets;
                List<Thing> randTargets = new List<Thing>(targets.InRandomOrder<Thing>());
                for (int i = 0; i < maxTargets && i < randTargets.Count(); i++)
                {
                    TargetInfo tinfo = new TargetInfo(randTargets[i]);
                    if (this.UseAbilityProps.targetParams.CanTarget(tinfo))
                    {
                        this.TargetsAoE.Add(new LocalTargetInfo(randTargets[i]));
                    }
                }
            }
            else
            {
                this.TargetsAoE.Clear();
                this.TargetsAoE.Add(this.currentTarget);
            }
        }

        public virtual void PostCastShot(bool inResult, out bool outResult) => outResult = inResult;

        protected override bool TryCastShot()
        {
            //Log.Message("Cast");
            bool result = false;
            this.TargetsAoE.Clear();
            UpdateTargets();
            int burstShots = this.ShotsPerBurst;
            if (this.UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && this.TargetsAoE.Count > 1)
            {
                this.TargetsAoE.RemoveRange(0, this.TargetsAoE.Count - 1);
            }
            for (int i = 0; i < this.TargetsAoE.Count; i++)
            {
                //                for (int j = 0; j < burstshots; j++)
                //                {
                bool? attempt = TryLaunchProjectile(this.verbProps.defaultProjectile, this.TargetsAoE[i]);
                ////Log.Message(TargetsAoE[i].ToString());
                if (attempt != null)
                {
                    if (attempt == true) result = true;
                    if (attempt == false) result = false;
                }
                //                }
            }

            // here, might want to have this set each time so people don't force stop on last burst and not hit the cooldown?
            //this.burstShotsLeft = 0;
            //if (this.burstShotsLeft == 0)
            //{
            //}
            PostCastShot(result, out result);
            return result;
            //bool result = false;
            //this.TargetsAoE.Clear();
            //UpdateTargets();
            //int burstshots = this.ShotsPerBurst;
            //if (this.UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && this.TargetsAoE.Count > 1)
            //{
            //    this.TargetsAoE.RemoveRange(0, this.TargetsAoE.Count - 1);
            //}
            //for (int i = 0; i < this.TargetsAoE.Count; i++)
            //{
            //    for (int j = 0; j < burstshots; j++)
            //    {
            //        bool? attempt = TryLaunchProjectile(this.verbProps.projectileDef, this.TargetsAoE[i]);
            //        if (attempt != null)
            //        {
            //            if (attempt == true)
            //                result = true;
            //            if (attempt == false)
            //                result = false;
            //        }
            //    }
            //}
            //this.burstShotsLeft = 0;
            //PostCastShot(result, out result);
            //return result;
        }

        protected bool? TryLaunchProjectile(ThingDef projectileDef, LocalTargetInfo launchTarget)
        {
            bool flag = base.TryFindShootLineFromTo(this.caster.Position, launchTarget, out ShootLine shootLine);
            if (this.verbProps.stopBurstWithoutLos && !flag)
            {
                return false;
            }
            Vector3 drawPos = this.caster.DrawPos;
            Projectile_AbilityBase projectile = (Projectile_AbilityBase)GenSpawn.Spawn(projectileDef, shootLine.Source, this.caster.Map);
            projectile.extraDamages = this.UseAbilityProps.extraDamages;
            projectile.localSpawnThings = this.UseAbilityProps.thingsToSpawn;
            projectile.FreeIntercept = (this.canFreeInterceptNow && !projectile.def.projectile.flyOverhead);
            ShotReport shotReport = ShotReport.HitReportFor(this.caster, this, launchTarget);
            if (this.verbProps.soundCast != null)
             {
                this.verbProps.soundCast.PlayOneShot(new TargetInfo(this.caster.Position, this.caster.Map, false));
            }
            if (this.verbProps.soundCastTail != null)
            {
                this.verbProps.soundCastTail.PlayOneShotOnCamera();
            }
            if (!this.UseAbilityProps.AlwaysHits)
            {
                if (Rand.Value > shotReport.ChanceToNotGoWild_IgnoringPosture)
                {
                    if (DebugViewSettings.drawShooting)
                    {
                        MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, "ToWild", -1f);
                    }
                    shootLine.ChangeDestToMissWild();
                    if (launchTarget.HasThing)
                    {
                        projectile.ThingToNeverIntercept = launchTarget.Thing;
                    }
                    if (!projectile.def.projectile.flyOverhead)
                    {
                        projectile.InterceptWalls = true;
                    }
                    projectile.Launch(this.caster, Ability.Def, drawPos, shootLine.Dest, this.ownerEquipment, this.UseAbilityProps.hediffsToApply, this.UseAbilityProps.mentalStatesToApply, this.UseAbilityProps.thingsToSpawn);
                    return true;
                }
                if (Rand.Value > shotReport.ChanceToNotHitCover)
                {
                    if (DebugViewSettings.drawShooting)
                    {
                        MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, "ToCover", -1f);
                    }
                    if (launchTarget.Thing != null && launchTarget.Thing.def.category == ThingCategory.Pawn)
                    {
                        Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
                        if (!projectile.def.projectile.flyOverhead)
                        {
                            projectile.InterceptWalls = true;
                        }
                        projectile.Launch(this.caster, Ability.Def, drawPos, randomCoverToMissInto, this.ownerEquipment, this.UseAbilityProps.hediffsToApply, this.UseAbilityProps.mentalStatesToApply, this.UseAbilityProps.thingsToSpawn);
                        return true;
                    }
                }
            }
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, "ToHit", -1f);
            }
            if (!projectile.def.projectile.flyOverhead)
            {
                projectile.InterceptWalls = (!launchTarget.HasThing || launchTarget.Thing.def.Fillage == FillCategory.Full);
            }
            projectile.Launch(this.caster, Ability.Def, drawPos, launchTarget, null, this.UseAbilityProps.hediffsToApply, this.UseAbilityProps.mentalStatesToApply, this.UseAbilityProps.thingsToSpawn);
            return true;
        }

        protected override int ShotsPerBurst => this.verbProps.burstShotCount;

        public override void WarmupComplete()
        {
            this.burstShotsLeft = this.ShotsPerBurst;
            this.state = VerbState.Bursting;
            this.TryCastNextBurstShot();
        }
    }
}
