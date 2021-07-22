using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace JecsTools
{
    public class HediffCompProperties_Knockback : HediffCompProperties
    {
        [Obsolete("Use explosiveProps != null")]
        public bool explosiveKnockback = false;
        [Obsolete("Use explosiveProps.explosiveDamageType")]
        public DamageDef explosionDmg;
        [Obsolete("Use explosiveProps.explosiveRadius")]
        public float explosionSize = 2f;
        // Note: following CompProperties_Explosive are ignored:
        // explodeOnKilled
        // explosiveExpandPerStackcount
        // explosiveExpandPerFuel
        // startWickOnDamageTaken
        // startWickHitPointsPercent
        // wickTicks
        // wickScale (also never used in vanilla)
        // chanceNeverExplodeFromDamage
        // destroyThingOnExplosionSize
        // requiredDamageTypeToExplode
        // countdownTicks
        public CompProperties_Explosive explosiveProps;

        public float knockbackChance = 0.2f;
        public float knockbackSpeed = 30f;
        public SoundDef knockbackSound;
        public ThoughtDef knockbackThought;

        // original distance = knockDistance.RandomInRange
        // damage absorbed % (before armor calculations) =
        //    if absorbed flag set, 100%
        //    if absorbed flag unset, 100% - (post-PreApplyDamage dinfo.Amount / pre-PreApplyDamage dinfo.Amount)
        // distance =
        //    original distance *
        //    knockbackDistanceDamagePercentCurve.Evaluate(damage absorbed %)
        //    knockDistanceMassCurve.Evaluate(mass excluding pawn's inventory mass)
        public FloatRange knockDistance = new FloatRange(2f, 3f);
        public SimpleCurve knockDistanceAbsorbedPercentCurve = new SimpleCurve
        {
            new CurvePoint(1f, 0f), // 100% damage soaked/absorbed => 0% knockback distance
            new CurvePoint(0f, 1f), // 0% damage soaked/absorbed => 100% knockback distance
        };
        public SimpleCurve knockDistanceMassCurve = new SimpleCurve
        {
            new CurvePoint(0f, 2f),
            new CurvePoint(60f, 1f), // 60 is base pawn mass (typical humanoid pawn is a bit higher due to apparel/equipment)
            new CurvePoint(120f, 0.5f),
            new CurvePoint(240f, 0.25f), // 4 is largest vanilla body size, and 60*4 = 240
        };

        // distance % = actual distance traveled accounting for obstacles / distance
        // impact damage = knockImpactDamage.RandomInRange * knockImpactDamageDistancePercentCurve.Evaluate(distance %)
        public FloatRange knockImpactDamage = new FloatRange(8f, 10f);
        public SimpleCurve knockImpactDamageDistancePercentCurve = new SimpleCurve
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(0.5f, 0.75f),
            new CurvePoint(1f, 1f),
        };

        // XXX: Ideally knockImpactDamageType would just be a public field that's defaulted to DamageDefOf.Blunt, but:
        // a) DamageDefOf.Blunt isn't initialized yet when HediffCompProperties_Knockback are initialized.
        // b) There's no HediffCompProperties.ResolveReferences ala CompProperties.ResolveReferences to put late initialization logic.
        // c) HediffCompProperties.ConfigErrors is only called in dev mode, so late initialization cannot be put there.
        // So we're forced to use a public property + private field here.
        private DamageDef knockImpactDamageType;
        public DamageDef KnockImpactDamageType
        {
            get => knockImpactDamageType ??= DamageDefOf.Blunt;
            set => knockImpactDamageType = value;
        }

        public float stunChance = 0f;
        public int stunTicks = 60;

        public HediffCompProperties_Knockback()
        {
            compClass = typeof(HediffComp_Knockback);
        }

        public CompProperties_Explosive ExplosiveProps
        {
            get
            {
                if (explosiveProps == null &&
#pragma warning disable CS0618 // Type or member is obsolete
                    explosiveKnockback)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    explosiveProps = new CompProperties_Explosive
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        explosiveDamageType = explosionDmg,
                        explosiveRadius = explosionSize,
#pragma warning restore CS0618 // Type or member is obsolete
                        damageAmountBase = 0,
                    };
                }
                return explosiveProps;
            }
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;
            // Note: knockbackSound can be null - if it is, the explosion sound defaults to explosionDamageType.soundExplosion.
        }
    }
}
