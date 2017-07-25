using System;
using RimWorld;

namespace CompVehicle
{
    [Flags]
    public enum HandlingTypeFlags
    {
        None = 0,
        Movement = 1,
        Manipulation = 2,
        Weapons = 4,
    }
}
