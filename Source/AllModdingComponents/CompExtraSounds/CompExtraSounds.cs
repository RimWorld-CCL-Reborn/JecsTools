using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CompExtraSounds
{
    internal class CompExtraSounds : ThingComp
    {
        public CompProperties_ExtraSounds Props
        {
            get
            {
                return (CompProperties_ExtraSounds)this.props;
            }
        }
    }
}
