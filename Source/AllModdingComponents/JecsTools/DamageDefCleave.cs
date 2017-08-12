using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace JecsTools
{
    public class DamageDefCleave : DamageDef
    {
        public int cleaveTargets = 0; //Number of bonus targets
        public float cleaveFactor = 0.7f; //Damage factor for the cleave attack
        public DamageDef cleaveDamage = null;
    }
}
