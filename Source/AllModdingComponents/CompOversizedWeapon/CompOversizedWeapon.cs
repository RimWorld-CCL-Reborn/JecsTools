using Verse;

namespace CompOversizedWeapon
{
    internal class CompOversizedWeapon : ThingComp
    {
        public CompProperties_OversizedWeapon Props => props as CompProperties_OversizedWeapon;

        public CompOversizedWeapon()
        {
            if (!(props is CompProperties_OversizedWeapon))
                props = new CompProperties_OversizedWeapon();
        }
    }
}