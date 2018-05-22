using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public class SwapCondition
    {
        public Gender swapWhenGender = Gender.None;
        public ThingDef swapTo = null;
    }
    public class ApparelExtension : DefModExtension
    {
        public List<string> coverage = new List<string>();
        public SwapCondition swapCondition = new SwapCondition();
    }
}