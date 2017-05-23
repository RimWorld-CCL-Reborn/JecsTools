using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CompOversizedWeapon
{
    internal class CompOversizedWeapon : ThingComp
    {
        public CompProperties_OversizedWeapon Props
        {
            get
            {
                return (CompProperties_OversizedWeapon)this.props;
            }
        }
    }
}
