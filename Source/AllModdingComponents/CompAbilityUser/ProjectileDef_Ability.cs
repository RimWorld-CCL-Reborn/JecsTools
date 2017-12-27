using Verse;

namespace AbilityUser
{
    public class ProjectileDef_Ability : ThingDef
    {
        public int HealCapacity = 3;
        public float HealFailChance = 0.3f;
        public bool IsBeamProjectile = false;
    }
}