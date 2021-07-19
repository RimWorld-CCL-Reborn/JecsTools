using Verse;

namespace CompSlotLoadable
{
    public class SlotBonusProps_VampiricEffect
    {
        public float chance = 0.05f;
        public int woundLimit = 0;
        public FloatRange amountRange = new FloatRange(5f, 10f);
        public DamageDef damageDef;
        public float armorPenetration = 1f;
    }
}
