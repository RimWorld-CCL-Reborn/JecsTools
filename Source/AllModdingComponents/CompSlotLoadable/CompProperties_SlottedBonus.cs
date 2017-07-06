using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;
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

        public CompProperties_SlottedBonus() => this.compClass = typeof(CompSlottedBonus);
    }
}
