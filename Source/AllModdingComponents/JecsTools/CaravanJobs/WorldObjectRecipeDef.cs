using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using RimWorld.Planet;

namespace JecsTools
{
    public class WorldObjectRecipeDef : Def
    {
        public virtual Def FinishedThing => finishedThing;
        private Def finishedThing;
        public List<ThingCountClass> costList;
        public List<StuffCategoryDef> stuffCategories;
        public int costStuffCount = -1;
        public int workToMake = -1;
        public List<ResearchProjectDef> researchPrerequisites;
        public List<string> tags = new List<string>();

        public virtual bool CanMake()
        {
            if (!researchPrerequisites.NullOrEmpty())
                foreach (ResearchProjectDef r in researchPrerequisites)
                    if (!r.IsFinished) return false;
            return true;
        }
    }
}
