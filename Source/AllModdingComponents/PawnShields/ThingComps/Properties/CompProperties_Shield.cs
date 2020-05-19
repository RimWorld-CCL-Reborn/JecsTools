using System;
using System.Collections.Generic;
using RimWorld;
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
        /// Does the shield take damage when blocking?
        /// </summary>
        public bool shieldTakeDamage = true;

        [Obsolete("use the Shield_DamageAbsorbed stat")]
        public float shieldTakeDamageFactor = 0.8f;

        /// <summary>
        /// Can the shield block melee attacks?
        /// </summary>
        public bool canBlockRanged = true;

        /// <summary>
        /// Can the shield block ranged attacks?
        /// </summary>
        public bool canBlockMelee = true;

        [Obsolete("use the Shield_BaseMeleeBlockChance stat")]
        public float meleeBlockChanceFactor = 1.0f;

        [Obsolete("use the Shield_BaseRangedBlockChance stat")]
        public float rangedBlockChanceFactor = 0.5f;

        /// <summary>
        /// Determines whether the shield can be automatically discarded by the shield wielder or not.
        /// </summary>
        public bool canBeAutoDiscarded = true;

        /// <summary>
        /// The hit points percentage threshold when the shield is automatically discarded by the shield wielder.
        /// </summary>
        public float healthAutoDiscardThreshold = 0.19f;

        /// <summary>
        /// If true the shield wielder gets fatigued from a blocked attack.
        /// </summary>
        public bool useFatigue = false;

        /// <summary>
        /// How much percent of the damage is converted to fatigue damage on a blocked attack.
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

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            yield return GetStatDrawEntry("ShieldHitPointAutoDiscardThreshold", canBeAutoDiscarded, healthAutoDiscardThreshold, 2);
            yield return GetStatDrawEntry("ShieldDamageToFatigueFactor", useFatigue, damageToFatigueFactor, 1);
        }

        private static StatDrawEntry GetStatDrawEntry(string baseKey, bool enabled, float value, int displayPriorityWithinCategory)
        {
            var valueString = (enabled ? value : 0f).ToStringPercent();
            var reportText = $"{(baseKey + "Ex").Translate()}\n\n{"StatsReport_FinalValue".Translate()}: {valueString}";
            if (!enabled)
                reportText += $" ({(baseKey + "Never").Translate()})";
            return new StatDrawEntry(ShieldStatsDefOf.Shield, baseKey.Translate(), valueString, reportText, displayPriorityWithinCategory);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);

            //Setup dictionary.
            if (sounds != null)
            {
                foreach (StuffedSound stuffedSound in sounds)
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
