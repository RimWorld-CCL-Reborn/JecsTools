using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace PawnShields
{
    /// <summary>
    /// Shield properties.
    /// </summary>
    public class CompProperties_Shield : CompProperties
    {
        /// <summary>
        /// Graphic when shield is worn.
        /// </summary>
        public GraphicData wieldedGraphic;

        /// <summary>
        /// If true it will attempt to use a colored version of the shield. e.g stuff
        /// </summary>
        public bool useColoredVersion = true;

        /// <summary>
        /// Shield rendering properties.
        /// </summary>
        public ShieldRenderProperties renderProperties = new ShieldRenderProperties();

        /// <summary>
        /// Do the shield take damage when blocking?
        /// </summary>
        public bool shieldTakeDamage = true;

        /// <summary>
        /// How much of the damage the shield absorbs upon successful block.
        /// </summary>
        [Obsolete]
        public float shieldTakeDamageFactor = 0.8f;

        /// <summary>
        /// Can the shield block melee attacks?
        /// </summary>
        public bool canBlockRanged = true;

        /// <summary>
        /// Can the shield block ranged attacks?
        /// </summary>
        public bool canBlockMelee = true;

        /// <summary>
        /// Melee block chance factor the shield provides which is multiplied by the relevant statistic. 1.0 is equivalent to unchanged.
        /// </summary>
        [Obsolete]
        public float meleeBlockChanceFactor = 1.0f;

        /// <summary>
        /// Ranged block chance factor the shield provides which is multiplied by the relevant statistic. 1.0 is equivalent to unchanged.
        /// </summary>
        [Obsolete]
        public float rangedBlockChanceFactor = 0.5f;

        /// <summary>
        /// Determines whether the shield can be automatically discarded by a pawn or not.
        /// </summary>
        public bool canBeAutoDiscarded = true;

        /// <summary>
        /// The % threshold when the shield is automatically discarded by the pawn.
        /// </summary>
        public float healthAutoDiscardThreshold = 0.19f;

        /// <summary>
        /// If true the shield user gets fatigued when blocking with the shield.
        /// </summary>
        public bool useFatigue = false;

        /// <summary>
        /// How much damage is converted to fatigue damage on successful block.
        /// </summary>
        public float damageToFatigueFactor = 0.05f;

        /// <summary>
        /// The stuff the shield is made out of can be linked to here.
        /// </summary>
        public List<StuffedSound> sounds = new List<StuffedSound>();

        /// <summary>
        /// Default blocking sound if no sounds are defined.
        /// </summary>
        public SoundDef defaultSound = null;
//SoundDefOf.MetalHitImportant;

        /// <summary>
        /// Helps fetching sounds easier.
        /// </summary>
        [Unsaved]
        public Dictionary<StuffCategoryDef, SoundDef> stuffedSounds = new Dictionary<StuffCategoryDef, SoundDef>();

        /*public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            yield return new StatDrawEntry(ShieldStatsDefOf.Shield, ShieldStatsDefOf.Shield_BaseMeleeBlockChance);
            yield return new StatDrawEntry(ShieldStatsDefOf.Shield, ShieldStatsDefOf.Shield_BaseRangedBlockChance);
            yield return new StatDrawEntry(ShieldStatsDefOf.Shield, ShieldStatsDefOf.Shield_DamageAbsorbed);
        }*/

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);

            //Setup dictionary.
            if(sounds != null)
            {
                foreach(StuffedSound stuffedSound in sounds)
                {
                    stuffedSounds[stuffedSound.stuffCategory] = stuffedSound.sound;
                }
            }
        }

        public CompProperties_Shield()
        {
            compClass = typeof(CompShield);
        }
    }
}
