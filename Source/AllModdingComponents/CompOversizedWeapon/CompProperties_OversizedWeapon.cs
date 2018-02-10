using UnityEngine;
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

        public Vector3 offset = new Vector3(0, 0, 0);

        public CompProperties_OversizedWeapon()
        {
            compClass = typeof(CompOversizedWeapon);
        }
    }
}