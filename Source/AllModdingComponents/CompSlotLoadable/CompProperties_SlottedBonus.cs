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
    }
}