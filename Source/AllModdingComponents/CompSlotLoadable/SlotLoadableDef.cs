using System.Collections.Generic;
using Verse;

namespace CompSlotLoadable
{
    public class SlotLoadableDef : ThingDef
    {
        //These can be loaded into the slot.
        public List<ThingDef> slottableThingDefs;

        //Does the slot change the thing's color?
        public bool doesChangeColor = false;
        public ColorInt colorToChangeTo;
        
        //Or change the second color?
        public bool doesChangeSecondColor = false;
        public ColorInt secondColorToChangeTo;

        //Does it change the stats?
        public bool doesChangeStats = false;
    }
}
