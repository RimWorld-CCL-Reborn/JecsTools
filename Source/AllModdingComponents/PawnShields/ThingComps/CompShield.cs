using RimWorld;
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
        public CompProperties_Shield ShieldProps => props as CompProperties_Shield;

        /// <summary>
        /// Determines whether the shield is broken or not.
        /// </summary>
        public virtual bool IsBroken
        {
            get
            {
                if (!ShieldProps.canBeAutoDiscarded)
                    return false;
                return (parent.HitPoints / (float)parent.MaxHitPoints) <= ShieldProps.healthAutoDiscardThreshold;
            }
        }

        /// <summary>
        /// The blocking sound the shield use when it blocks something.
        /// </summary>
        public virtual SoundDef BlockSound
        {
            get
            {
                var categories = parent.Stuff?.stuffProps?.categories;
                if (!categories.NullOrEmpty())
                {
                    var stuffedSounds = ShieldProps?.stuffedSounds;
                    if (stuffedSounds != null)
                    {
                        if (stuffedSounds.TryGetValue(categories[0], out var soundDef))
                            return soundDef;
                    }
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
            var absorbedDamage = false;

            //Check if we blocked the attack at all.
            if (ShieldProps.canBlockMelee && !ranged)
            {
                var chance = defender.GetStatValue(ShieldStatsDefOf.MeleeShieldBlockChance, true);
                //Log.Message("Melee block chance: " + chance.ToStringPercent());
                if (Rand.Chance(chance))
                    absorbedDamage = true;
            }
            else if (ShieldProps.canBlockRanged && ranged)
            {
                var chance = defender.GetStatValue(ShieldStatsDefOf.RangedShieldBlockChance, true);
                //Log.Message("Ranged block chance: " + chance.ToStringPercent());
                if (Rand.Chance(chance))
                    absorbedDamage = true;
            }

            if (!absorbedDamage)
                return false;

            //Fatigue damage.
            if (absorbedDamage && ShieldProps.useFatigue)
            {
                var finalDamage = (float)dinfo.Amount * ShieldProps.damageToFatigueFactor;
                HealthUtility.AdjustSeverity(defender, ShieldHediffDefOf.ShieldFatigue, finalDamage);
            }

            //Take damage from attack.
            if (ShieldProps.shieldTakeDamage)
            {
                var finalDamage = Mathf.CeilToInt((float)dinfo.Amount * parent.GetStatValue(ShieldStatsDefOf.Shield_DamageAbsorbed));
                var shieldDamage = new DamageInfo(dinfo);
                shieldDamage.SetAmount(finalDamage);

                parent.TakeDamage(shieldDamage);
            }

            //Absorb damage if shield is still intact.
            if (parent.HitPoints > 0)
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
            FleckMaker.ThrowMicroSparks(defender.Position.ToVector3(), defender.Map);
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
            var carryShieldOpenly =
                (pawn.carryTracker == null || pawn.carryTracker.CarriedThing == null) &&
                (pawn.Drafted || (pawn.CurJob != null && pawn.CurJob.def.alwaysShowWeapon) ||
                (pawn.mindState.duty != null && pawn.mindState.duty.def.alwaysShowWeapon));

            if (!ShieldProps.renderProperties.renderWhenPeaceful && !carryShieldOpenly)
                return;

            if (ShieldProps.wieldedGraphic != null && ShieldProps.wieldedGraphic.Graphic.MatSingle != null)
            {
                if (rot == Rot4.North)
                {
                    var angle = -thing.def.equippedAngleOffset;
                    if (ShieldProps.renderProperties.flipRotation)
                        angle = -angle;

                    if (ShieldProps.useColoredVersion)
                        ShieldProps.wieldedGraphic.GraphicColoredFor(thing).Draw(loc, rot, thing, angle);
                    else
                        ShieldProps.wieldedGraphic.Graphic.Draw(loc, rot, thing, angle);
                }
                else if (rot == Rot4.South)
                {
                    var angle = thing.def.equippedAngleOffset;
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
    }
}
