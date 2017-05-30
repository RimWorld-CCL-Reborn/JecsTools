using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CompSlotLoadable
{
    public class CompProperties_SlottedBonus : CompProperties
    {
        public List<StatModifier> statModifiers = null;

        public DamageDef damageDef = null;

        public SoundDef soundCastReplacer = null;

        public float weaponRangeMod = 0.0f;

        public float muzzleFlashMod = 0.0f;

        public ThingDef projectileReplacer = null;

        public List<ThingDef> additionalProjectiles = new List<ThingDef>();

        public Color color = Color.white;

        public SlotBonusProps_DefensiveHealChance defensiveHealChance = null;

        public SlotBonusProps_VampiricEffect vampiricHealChance = null;

        public CompProperties_SlottedBonus()
        {
            this.compClass = typeof(CompSlottedBonus);
        }
    }
}
