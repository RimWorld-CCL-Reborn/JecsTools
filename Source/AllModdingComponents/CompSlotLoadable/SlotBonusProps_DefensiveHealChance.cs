using Verse;

namespace CompSlotLoadable
{
    public class SlotBonusProps_DefensiveHealChance
    {
        public float chance = 0.05f;
        public int woundLimit = 0;
        // Default value matches the previously hard-coded behavior of always fully healing the hediff.
        public FloatRange amountRange = new FloatRange(float.PositiveInfinity, float.PositiveInfinity);
    }
}
