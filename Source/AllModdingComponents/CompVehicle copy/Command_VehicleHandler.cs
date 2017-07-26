using System;
using UnityEngine;
using Verse;

namespace CompVehicle
{
    public class Command_VehicleHandler : Command
    {
        public Action action;

        public override float Width => NewWidth();
        public float NewWidth() => base.Width * 0.75f;

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            this.action();
        }
    }
}
