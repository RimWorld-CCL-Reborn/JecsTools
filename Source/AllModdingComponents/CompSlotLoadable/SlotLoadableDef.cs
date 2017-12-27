using System.Collections.Generic;
using Verse;

namespace CompSlotLoadable
{
    public class SlotLoadableDef : ThingDef
    {
        public ColorInt colorToChangeTo;

        //Does the slot change the thing's color?
        public bool doesChangeColor = false;

        //Or change the second color?
        public bool doesChangeSecondColor = false;

        //Does it change the stats?
        public bool doesChangeStats = false;

        public ColorInt secondColorToChangeTo;

        //These can be loaded into the slot.
        public List<ThingDef> slottableThingDefs;
    }
}