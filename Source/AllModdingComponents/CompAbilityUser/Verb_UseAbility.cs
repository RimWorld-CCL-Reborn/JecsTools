using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbilityUser
{
    public class Verb_UseAbility : Verb_LaunchProjectile
    {
        public VerbProperties_Ability useAbilityProps
        {
            get
            {
                return (VerbProperties_Ability)verbProps;
            }
        }

        public ProjectileDef_Ability abilityProjectileDef
        {
            get
            {
                return this.useAbilityProps.projectileDef as ProjectileDef_Ability;
            }
        }

        public PawnAbility ability = null;
        public List<LocalTargetInfo> TargetsAoE = new List<LocalTargetInfo>();

        //public Need_ForcePool soul
        //{
        //    get
        //    {
        //        return this.CasterPawn.needs.TryGetNeed<Need_Soul>();
        //    }
        //    set
        //    {

        //    }
        //}


        public CompAbilityUser abilityUserComp
        {
            get
            {
                return this.CasterPawn.TryGetComp<CompAbilityUser>();
            }
        }

        public override float HighlightFieldRadiusAroundTarget()
        {
            float result =  this.verbProps.projectileDef.projectile.explosionRadius;
            if (this.useAbilityProps.abilityDef.MainVerb.TargetAoEProperties != null)
            {
                if (this.useAbilityProps.abilityDef.MainVerb.TargetAoEProperties.showRangeOnSelect)
                {
                    result = (float)this.useAbilityProps.abilityDef.MainVerb.TargetAoEProperties.range;
                }
            }
            return result;
        }

        protected virtual void UpdateTargets()
        {
            this.TargetsAoE.Clear();
            if (this.useAbilityProps.AbilityTargetCategory == AbilityTargetCategory.TargetAoE)
            {
                ////Log.Message("AoE Called");
                if (useAbilityProps.TargetAoEProperties == null)
                {
                    Log.Error("Tried to Cast AoE-Ability without defining a target class");
                }

                List<Thing> targets = new List<Thing>();

                //Handle TargetAoE start location.
                IntVec3 aoeStartPosition = this.caster.PositionHeld;
                if (!useAbilityProps.TargetAoEProperties.startsFromCaster)
                {
                    aoeStartPosition = this.currentTarget.Cell;
                }


                //this.TargetsAoE.Add(new LocalTargetInfo(this.currentTarget.Cell));

                //Handle friendly fire targets.
                if (!useAbilityProps.TargetAoEProperties.friendlyFire)
                {
                    targets = this.caster.Map.listerThings.AllThings.Where(x => (x.Position.InHorDistOf(aoeStartPosition, useAbilityProps.TargetAoEProperties.range)) && (x.GetType() == this.useAbilityProps.TargetAoEProperties.targetClass) && !x.Faction.HostileTo(Faction.OfPlayer)).ToList<Thing>();
                }
                else if ((this.useAbilityProps.TargetAoEProperties.targetClass == typeof(Plant)) || (this.useAbilityProps.TargetAoEProperties.targetClass == typeof(Building)))
                {
                    targets = this.caster.Map.listerThings.AllThings.Where(x => (x.Position.InHorDistOf(aoeStartPosition, useAbilityProps.TargetAoEProperties.range)) && (x.GetType() == this.useAbilityProps.TargetAoEProperties.targetClass)).ToList<Thing>();
                    foreach (Thing targ in targets)
                    {
                        LocalTargetInfo tinfo = new LocalTargetInfo(targ);
                        TargetsAoE.Add(tinfo);
                    }
                    return;
                }
                else
                {
                    ////Log.Message("Expected call");
                    targets.Clear();
                    targets = this.caster.Map.listerThings.AllThings.Where(x =>
                        (x.Position.InHorDistOf(aoeStartPosition, useAbilityProps.TargetAoEProperties.range)) &&
                        (x.GetType() == this.useAbilityProps.TargetAoEProperties.targetClass) &&
                        (x.HostileTo(Faction.OfPlayer) || this.useAbilityProps.TargetAoEProperties.friendlyFire)).ToList<Thing>();
                }

                int maxTargets = this.useAbilityProps.abilityDef.MainVerb.TargetAoEProperties.maxTargets;
                foreach (Thing targ in targets.InRandomOrder<Thing>())
                {
                    TargetInfo tinfo = new TargetInfo(targ);
                    if (this.useAbilityProps.targetParams.CanTarget(tinfo) && maxTargets > 0)
                    {
                        maxTargets--;
                        TargetsAoE.Add(new LocalTargetInfo(targ));
                        ////Log.Message(targ.Label);
                    }
                }
            }
            else
            {
                this.TargetsAoE.Clear();
                this.TargetsAoE.Add(this.currentTarget);
            }
        }

        public virtual void PostCastShot(bool inResult, out bool outResult)
        {
            outResult = inResult;
        }

        protected override bool TryCastShot()
        {
            bool result = false;
            this.TargetsAoE.Clear();
            UpdateTargets();
            int burstshots = this.ShotsPerBurst;
            if (this.useAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && this.TargetsAoE.Count > 1)
            {
                this.TargetsAoE.RemoveRange(0, TargetsAoE.Count - 1);
            }
            for (int i = 0; i < TargetsAoE.Count; i++)
            {
                for (int j = 0; j < burstshots; j++)
                {
                    bool? attempt = TryLaunchProjectile(this.verbProps.projectileDef, TargetsAoE[i]);
                    ////Log.Message(TargetsAoE[i].ToString());
                    if (attempt != null)
                    {
                        if (attempt == true)
                            result = true;
                        if (attempt == false)
                            result = false;
                    }
                }
                ability.TicksUntilCasting = (int)this.useAbilityProps.SecondsToRecharge * GenTicks.TicksPerRealSecond;
            }
            this.burstShotsLeft = 0;
            PostCastShot(result, out result);
            return result;
        }

        protected bool? TryLaunchProjectile(ThingDef projectileDef, LocalTargetInfo launchTarget)
        {
            ShootLine shootLine;
            bool flag = base.TryFindShootLineFromTo(this.caster.Position, launchTarget, out shootLine);
            if (this.verbProps.stopBurstWithoutLos && !flag)
            {
                return false;
            }
            Vector3 drawPos = this.caster.DrawPos;
            Projectile_AbilityBase projectile = (Projectile_AbilityBase)GenSpawn.Spawn(projectileDef, shootLine.Source, this.caster.Map);
            projectile.extraDamages = useAbilityProps.extraDamages;
            projectile.localSpawnThings = useAbilityProps.thingsToSpawn;
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
            if (!this.useAbilityProps.AlwaysHits)
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
                    //              //Log.Message("LaunchingIntoWild");
                    projectile.Launch(this.caster, drawPos, shootLine.Dest, this.ownerEquipment, this.useAbilityProps.hediffsToApply, this.useAbilityProps.mentalStatesToApply, this.useAbilityProps.thingsToSpawn);
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
                        //            //Log.Message("LaunchingINtoCover");
                        projectile.Launch(this.caster, drawPos, randomCoverToMissInto, this.ownerEquipment, this.useAbilityProps.hediffsToApply, this.useAbilityProps.mentalStatesToApply, this.useAbilityProps.thingsToSpawn);
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
            projectile.Launch(this.caster, drawPos, launchTarget, null, this.useAbilityProps.hediffsToApply, this.useAbilityProps.mentalStatesToApply, this.useAbilityProps.thingsToSpawn);
            return true;
        }

        protected override int ShotsPerBurst
        {
            get
            {
                return this.verbProps.burstShotCount;
            }
        }

        public override void WarmupComplete()
        {
            this.burstShotsLeft = this.ShotsPerBurst;
            this.state = VerbState.Bursting;
            this.TryCastNextBurstShot();
        }
    }
}
