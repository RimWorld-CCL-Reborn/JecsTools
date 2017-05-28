using System;
using UnityEngine;
using Verse;

namespace CompVehicle
{
    public class Command_VehicleHandler : Command
    {
        public Action action;

        public override float Width => newWidth();
        public float newWidth()
        {
            return base.Width * 0.75f;
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            this.action();
        }
    }
}
