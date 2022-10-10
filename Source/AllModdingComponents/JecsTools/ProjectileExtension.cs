using System;
using Verse;

namespace JecsTools
{
    /// <summary>
    /// Projectile Extension allows extra control over
    /// damage
    /// </summary>
    [Obsolete("Seems to have never worked properly due to incomplete implementation")]
    public class ProjectileExtension : DefModExtension
    {
        public bool passesWalls = false;
        public bool passesRoofs = false; // TODO: unused
        public bool damagesTargetsBetween = false; // TODO: unused
        public float damageMultiplierPerTarget = 1.0f; // TODO: unused
    }
}
