using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CompOverlays
{
    public class GraphicOverlay
    {
        public GraphicData graphicData;
        
        public bool usesStuff = false;
        public Vector3 offset = Vector3.zero;
        
    }

    public class CompProperties_Overlays : CompProperties
    {
        public bool fuelRequired = false;
        public List<GraphicOverlay> overlays = new List<GraphicOverlay>();

        public CompProperties_Overlays()
        {
            compClass = typeof(CompOverlays);
        }
    }
}