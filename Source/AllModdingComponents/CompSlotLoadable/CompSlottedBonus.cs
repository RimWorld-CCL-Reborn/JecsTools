using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CompSlotLoadable
{
    public class CompSlottedBonus : ThingComp
    {
        public CompProperties_SlottedBonus Props => (CompProperties_SlottedBonus)props;

        public virtual float GetStatOffset(StatDef stat)
        {
            var statModifiers = Props?.statModifiers;
            if (statModifiers != null)
            {
                var statOffset = statModifiers.GetStatOffsetFromList(stat);
                //Log.Message("adding in stat " + stat + " offset: " + statOffset);
                if (!stat.parts.NullOrEmpty())
                {
                    var statReq = StatRequest.For(parent);
                    for (var i = 0; i < stat.parts.Count; i++)
                    {
                        //Log.Message("adding in parts " + stat.parts[i]);
                        stat.parts[i].TransformValue(statReq, ref statOffset);
                    }
                    //Log.Message("added in parts of a stat for result " + statOffset);
                }
                return statOffset;
            }
            return 0f;
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            var props = Props;
            if (props != null)
            {
                if (props.damageDef != null)
                {
                    var damageTypeStr = props.damageDef.LabelCap;
                    yield return new StatDrawEntry(CompSlotLoadableDefOf.SlotLoadable, StringOf.OverrideDamageType, damageTypeStr,
                        string.Format(StringOf.OverrideDamageTypeExplanation, damageTypeStr), 99);
                    // TODO: armorPenStr should somehow be calculated via equivalent logic in ThingDef.SpecialDisplayStats.
                    var armorPenStr = props.armorPenetration.ToStringPercent();
                    yield return new StatDrawEntry(CompSlotLoadableDefOf.SlotLoadable, StringOf.OverrideArmorPenetration, armorPenStr,
                        string.Format("ArmorPenetrationExplanation".Translate(), armorPenStr), 98);
                }
                var statModifiers = props.statModifiers;
                if (statModifiers != null)
                {
                    var statReq = StatRequest.For(parent);
                    foreach (var mod in statModifiers)
                    {
                        var stat = mod.stat;
                        yield return new StatDrawEntry(CompSlotLoadableDefOf.SlotLoadable, stat,
                           stat.Worker.GetValue(statReq), statReq, ToStringNumberSense.Offset);
                    }
                }
                var defHealChance = props.defensiveHealChance;
                if (defHealChance != null)
                {
                    var chanceStr = defHealChance.chance.ToStringPercent();
                    var woundLimitStr = defHealChance.woundLimit == int.MaxValue ? StringOf.all : defHealChance.woundLimit.ToString();
                    yield return new StatDrawEntry(CompSlotLoadableDefOf.SlotLoadable, StringOf.DefensiveHeal,
                        string.Format(StringOf.DefensiveHealShort, chanceStr, woundLimitStr, defHealChance.amountRange),
                        string.Format(StringOf.DefensiveHealExplanation, chanceStr, woundLimitStr, defHealChance.amountRange),
                        90);
                }
                var vampHealChance = props.vampiricHealChance;
                if (vampHealChance != null)
                {
                    var chanceStr = vampHealChance.chance.ToStringPercent();
                    var woundLimitStr = vampHealChance.woundLimit == int.MaxValue ? StringOf.all : vampHealChance.woundLimit.ToString();
                    var armorPenStr = vampHealChance.armorPenetration.ToStringPercent();
                    yield return new StatDrawEntry(CompSlotLoadableDefOf.SlotLoadable, StringOf.VampiricHeal,
                        string.Format(StringOf.VampiricHealShort, chanceStr, woundLimitStr, vampHealChance.amountRange, vampHealChance.damageDef, armorPenStr),
                        string.Format(StringOf.VampiricHealExplanation, chanceStr, woundLimitStr, vampHealChance.amountRange, vampHealChance.damageDef, armorPenStr),
                        89);
                }
            }
        }
    }
}
