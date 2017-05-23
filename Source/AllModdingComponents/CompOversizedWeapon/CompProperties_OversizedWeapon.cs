using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CompOversizedWeapon
{
    internal class CompProperties_OversizedWeapon : CompProperties
    {
        //public SoundDef soundMiss;
        //public SoundDef soundHitPawn;
        //public SoundDef soundHitBuilding;
        //public SoundDef soundExtra;
        //public SoundDef soundExtraTwo;

        public CompProperties_OversizedWeapon()
        {
            this.compClass = typeof(CompOversizedWeapon);
        }
    }
}
