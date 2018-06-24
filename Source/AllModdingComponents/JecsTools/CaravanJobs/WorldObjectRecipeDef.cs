using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class WorldObjectRecipeDef : Def
    {
        public List<ThingDefCountClass> costList = new List<ThingDefCountClass>();
        public List<ResearchProjectDef> researchPrerequisites = new List<ResearchProjectDef>();
        public List<StuffCategoryCountClass> stuffCostList = new List<StuffCategoryCountClass>();
        public List<string> tags = new List<string>();
        public int workToMake = -1;
        public virtual Def FinishedThing { get; }

        public virtual bool CanMake()
        {
            if (!researchPrerequisites.NullOrEmpty())
                foreach (var r in researchPrerequisites)
                    if (!r.IsFinished) return false;
            return true;
        }
    }
}