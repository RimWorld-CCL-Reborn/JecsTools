using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace PawnShields
{
    /// <summary>
    /// Component class for shields. Relevant logic is done here.
    /// </summary>
    public class CompShield : ThingComp
    {
        /// <summary>
        /// Shield properties.
        /// </summary>
        public CompProperties_Shield ShieldProps
        {
            get
            {
                return props as CompProperties_Shield;
            }
        }

        /// <summary>
        /// Determines whether the shield is broken or not.
        /// </summary>
        public virtual bool IsBroken
        {
            get
            {
                if (!ShieldProps.canBeAutoDiscarded)
                    return false;

                return ((float)parent.HitPoints / (float)parent.MaxHitPoints) <= ShieldProps.healthAutoDiscardThreshold;
            }
        }

        /// <summary>
        /// The blocking sound the shield use when it blocks something.
        /// </summary>
        public virtual SoundDef BlockSound
        {
            get
            {
                if (ShieldProps?.stuffedSounds != null && ShieldProps?.stuffedSounds?.Count > 0 && parent.Stuff?.stuffProps?.categories != null && ShieldProps.stuffedSounds.Count > 0 &&
                    ShieldProps.stuffedSounds.ContainsKey(parent.Stuff?.stuffProps?.categories.FirstOrDefault()))
                {
                    if (ShieldProps.stuffedSounds[parent.Stuff.stuffProps.categories.First()] is SoundDef def)
                        return def;
                }

                //Default sound
                return ShieldProps?.defaultSound ?? SoundDefOf.MetalHitImportant;
            }
        }

        /// <summary>
        /// Absorbs the attacker damage.
        /// </summary>
        /// <param name="defender">Defender doing the blocking.</param>
        /// <param name="dinfo">Describes the incoming damage.</param>
        /// <param name="ranged">Is this attack ranged or melee?</param>
        /// <returns>True if it absorbed damage successfully.</returns>
        public virtual bool AbsorbDamage(Pawn defender, DamageInfo dinfo, bool ranged)
        {
            bool absorbedDamage = false;

            //Check if we blocked the attack at all.
            if(ShieldProps.canBlockMelee && !ranged)
            {
                //Melee
                float baseStat = defender.GetStatValue(ShieldStatsDefOf.MeleeShieldBlockChance, true);
                float chance = baseStat * parent.GetStatValue(ShieldStatsDefOf.Shield_BaseMeleeBlockChance);

                //Log.Message("Melee block chance: " + chance.ToStringPercent());
                if (Rand.Chance(chance))
                    absorbedDamage = true;
            } else if (ShieldProps.canBlockRanged && ranged)
            {
                //Ranged
                float baseStat = defender.GetStatValue(ShieldStatsDefOf.RangedShieldBlockChance, true);
                float chance = baseStat * parent.GetStatValue(ShieldStatsDefOf.Shield_BaseRangedBlockChance);

                //Log.Message("Ranged block chance: " + chance.ToStringPercent());
                if (Rand.Chance(chance))
                    absorbedDamage = true;
            }

            if (!absorbedDamage)
                return false;

            //Fatigue damage.
            if(absorbedDamage && ShieldProps.useFatigue)
            {
                float finalDamage = (float)dinfo.Amount * ShieldProps.damageToFatigueFactor;
                HealthUtility.AdjustSeverity(defender, ShieldHediffDefOf.ShieldFatigue, finalDamage);
            }

            //Take damage from attack.
            if (ShieldProps.shieldTakeDamage)
            {
                int finalDamage = Mathf.CeilToInt((float)dinfo.Amount * parent.GetStatValue(ShieldStatsDefOf.Shield_DamageAbsorbed));
                DamageInfo shieldDamage = new DamageInfo(dinfo);
                shieldDamage.SetAmount(finalDamage);

                parent.TakeDamage(shieldDamage);
            }

            //Absorb damage if shield is still intact.
            if(parent.HitPoints > 0)
            {
                MakeBlockEffect(defender, dinfo, ranged);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Delegate for making the blocking effect on successful block while the shield is not broken.
        /// </summary>
        /// <param name="defender">Defender doing the blocking.</param>
        /// <param name="dinfo">Describes the incoming damage.</param>
        /// <param name="ranged">Is this attack ranged or melee?</param>
        public virtual void MakeBlockEffect(Pawn defender, DamageInfo dinfo, bool ranged)
        {
            MoteMaker.ThrowMicroSparks(defender.Position.ToVector3(), defender.Map);
        }

        /// <summary>
        /// Renders the shield on the pawn wielding it.
        /// </summary>
        /// <param name="loc">Origin location to draw at.</param>
        /// <param name="rot">Rotation to draw for.</param>
        /// <param name="thing">Shield Thing.</param>
        /// <param name="pawn">Shield bearer.</param>
        public virtual void RenderShield(Vector3 loc, Rot4 rot, Pawn pawn, Thing thing)
        {
            bool carryShieldOpenly = 
                (pawn.carryTracker == null || pawn.carryTracker.CarriedThing == null) && 
                (pawn.Drafted || (pawn.CurJob != null && pawn.CurJob.def.alwaysShowWeapon) || 
                (pawn.mindState.duty != null && pawn.mindState.duty.def.alwaysShowWeapon));

            if (!ShieldProps.renderProperties.renderWhenPeaceful && !carryShieldOpenly)
                return;

            if (ShieldProps.wieldedGraphic != null && ShieldProps.wieldedGraphic.Graphic.MatSingle != null)
            {
                if(rot == Rot4.North)
                {
                    float angle = -thing.def.equippedAngleOffset;
                    if (ShieldProps.renderProperties.flipRotation)
                        angle = -angle;

                    if(ShieldProps.useColoredVersion)
                        ShieldProps.wieldedGraphic.GraphicColoredFor(thing).Draw(loc, rot, thing, angle);
                    else
                        ShieldProps.wieldedGraphic.Graphic.Draw(loc, rot, thing, angle);
                }
                else if (rot == Rot4.South)
                {
                    float angle = thing.def.equippedAngleOffset;
                    if (ShieldProps.renderProperties.flipRotation)
                        angle = -angle;

                    if (ShieldProps.useColoredVersion)
                        ShieldProps.wieldedGraphic.GraphicColoredFor(thing).Draw(loc, rot, thing, angle);
                    else
                        ShieldProps.wieldedGraphic.Graphic.Draw(loc, rot, thing, angle);
                }
                else
                {
                    if (ShieldProps.useColoredVersion)
                        ShieldProps.wieldedGraphic.GraphicColoredFor(thing).Draw(loc, rot, thing);
                    else
                        ShieldProps.wieldedGraphic.Graphic.Draw(loc, rot, thing);
                }
            }
            else
            {
                //Default render.
                thing.Graphic.Draw(loc, rot, thing);
            }
        }

        /*public override void CompTick()
        {
            base.CompTick();

            Log.Message("Shield tick!");
        }*/
    }
}
