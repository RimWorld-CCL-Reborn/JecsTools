using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace CompOverlays
{
    public class GraphicOverlay
    {
        public GraphicData graphicData;
        public Vector3 offset = Vector3.zero;
    }

    public class CompProperties_Overlays : CompProperties
    {
        public List<GraphicOverlay> overlays = new List<GraphicOverlay>();

        public bool fuelRequired = false;

        public CompProperties_Overlays()
        {
            this.compClass = typeof(CompOverlays);
        }
    }
}
