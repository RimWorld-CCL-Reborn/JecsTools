using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class HediffCompProperties_ExtraMeleeDamages : HediffCompProperties
    {
        public List<ExtraDamage> ExtraDamages = new List<ExtraDamage>();

        public HediffCompProperties_ExtraMeleeDamages()
        {
            compClass = typeof(HediffComp_ExtraMeleeDamages);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;
            for (var i = 0; i < ExtraDamages.Count; i++)
            {
                if (ExtraDamages[i]?.def == null)
                    yield return $"ExtraDamages[{i}] is null or has null def";
            }
        }
    }
}
