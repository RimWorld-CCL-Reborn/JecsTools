using System;
using System.Collections.Generic;
using Verse;

namespace CompToggleDef
{
    public class CompToggleDef : ThingComp
    {
        protected string TDTag = "_TOGGLEDEF_";

        // Can change into these
        public List<ThingDef> toggleDefs;

        public string LabelKey => ((CompProperties_ToggleDef) props).labelKey;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            string tdbase;
            string tdkey;

            //Log.Message("CompToggleDef Initialize entered");
            if (toggleDefs == null)
                if (parseToggleDef(out tdbase, out tdkey))
                    fillToggleDefs(tdbase, tdkey);
                else
                    Log.Warning("Failed to create toggleDefs");
        }

        public void fillToggleDefs(string tdbase, string tdkey)
        {
            // go through all and find the ones that match
            //Log.Message("CompToggleDef.fillToggleDefs" + tdbase +" " + tdkey);
            toggleDefs = new List<ThingDef>();
            var allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
            var tdbasematch = tdbase + TDTag;
            for (var i = 0; i < allDefsListForReading.Count; i++)
            {
                var adef = allDefsListForReading[i];
                if (adef.defName.StartsWith(tdbasematch))
                    toggleDefs.Add(adef);
            }
        }


        // return true on success
        public bool parseToggleDef(out string tdbase, out string tdkey)
        {
            string[] thematch = null;
            var retval = false;
            if (parent != null)
                if (parent.def != null)
                    if (parent.def.defName != null)
                    {
                        thematch = parent.def.defName.Split(new[] {TDTag}, StringSplitOptions.None);
                        if (thematch.Length == 2)
                            retval = true;
                        else
                            Log.Warning("parsed defname of '" + parent.def.defName + "' failed to split on tag '" +
                                        TDTag + "'.");
                    }
                    else
                    {
                        Log.Warning("Unable to parse defName because no this.parent.def.defName");
                    }
                else Log.Warning("Unable to parse defName because no this.parent.def");
            else Log.Warning("Unable to parse defName because no this.parent");

            if (retval)
            {
                tdbase = thematch[0];
                tdkey = thematch[1];
            }
            else
            {
                tdbase = null;
                tdkey = null;
            }
            return retval;
        }
    }
}