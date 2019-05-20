using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace JecsTools
{
    /// <summary>
    /// Projectile Extension allows extra control over
    /// damage 
    /// </summary>
    public class ProjectileExtension : DefModExtension
    {
        public bool passesWalls = false;
        public bool passesRoofs = false;
        public bool damagesTargetsBetween = false;
        public float damageMultiplierPerTarget = 1.0f;
    }
}