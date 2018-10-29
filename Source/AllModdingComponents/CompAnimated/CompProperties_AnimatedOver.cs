using System.Collections.Generic;
using Verse;

namespace CompAnimated
{
    public class CompProperties_AnimatedOver : CompProperties_Animated
    {
        public float yOffset = 0f, xOffset = 0f, xScale = 1f, yScale = 1f;
        public CompProperties_AnimatedOver()
        {
            compClass = typeof(CompAnimatedOver);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (xScale <= 0f) 
            {
                xScale = 0f;
                yield return "xScale must be positive";
            }
            
            if (yScale <= 0f)
            {
                yScale = 0f;
                yield return "yScale must be positive";
            }
        }
    }
}