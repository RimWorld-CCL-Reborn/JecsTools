using System.Collections.Generic;
using RimWorld;
using Verse;

namespace JecsTools
{
    public class WorldObjectRecipeDef : Def
    {
        public List<ThingCountClass> costList;
        public int costStuffCount = -1;
        public List<ResearchProjectDef> researchPrerequisites;
        public List<StuffCategoryDef> stuffCategories;
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