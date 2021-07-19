using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CompSlotLoadable
{
    public class CompProperties_SlottedBonus : CompProperties
    {
        public List<ThingDef> additionalProjectiles = new List<ThingDef>();

        public Color color = Color.white;

        public DamageDef damageDef = null;

        public float armorPenetration = 0f;

        public SlotBonusProps_DefensiveHealChance defensiveHealChance = null;

        public float muzzleFlashMod = 0.0f;

        public ThingDef projectileReplacer = null;

        public SoundDef soundCastReplacer = null;
        public List<StatModifier> statModifiers = null;

        public SlotBonusProps_VampiricEffect vampiricHealChance = null;

        public float weaponRangeMod = 0.0f;

        public CompProperties_SlottedBonus()
        {
            compClass = typeof(CompSlottedBonus);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            if (defensiveHealChance != null)
            {
                if (defensiveHealChance.woundLimit <= 0)
                    defensiveHealChance.woundLimit = int.MaxValue;
            }
            if (vampiricHealChance != null)
            {
                if (vampiricHealChance.woundLimit <= 0)
                    vampiricHealChance.woundLimit = 2;
                vampiricHealChance.damageDef ??= DamageDefOf.Burn;
            }
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;
            if (statModifiers != null)
            {
                for (var i = 0; i < statModifiers.Count; i++)
                {
                    if (statModifiers[i]?.stat == null)
                        yield return $"{nameof(statModifiers)}[{i}] is null or has null stat";
                }
            }
        }
    }
}
