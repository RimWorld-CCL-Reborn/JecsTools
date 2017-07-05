using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;

namespace CompToggleDef
{
    public class CompToggleDef : ThingComp
    {

        // Can change into these
        public List<ThingDef> toggleDefs = null;
        protected string TDTag = "_TOGGLEDEF_";

        public string LabelKey
        {
            get { return ((CompProperties_ToggleDef)this.props).labelKey; }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            string tdbase;
            string tdkey;

            //            Log.Message("CompToggleDef Initialize entered");
            if (this.toggleDefs == null)
            {

                // find search strings
                if (this.parseToggleDef(out tdbase, out tdkey))
                {
                    // find all matching ThingDefs and put into toggleDefs
                    this.fillToggleDefs(tdbase, tdkey);
                }
                else
                {
                    Log.Warning("Failed to create toggleDefs");
                }
            }
        }

        public void fillToggleDefs(string tdbase, string tdkey)
        {
            // go through all and find the ones that match
            //            Log.Message("CompToggleDef.fillToggleDefs" + tdbase +" " + tdkey);
            this.toggleDefs = new List<ThingDef>();
            List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
            string tdbasematch = tdbase + this.TDTag;
            for (int i = 0; i < allDefsListForReading.Count; i++)
            {
                ThingDef adef = allDefsListForReading[i];
                if (adef.defName.StartsWith(tdbasematch))
                {
                    //                    Log.Message("  ... adding in "+adef.defName);
                    this.toggleDefs.Add(adef);
                }
            }
        }


        // return true on success
        public bool parseToggleDef(out string tdbase, out string tdkey)
        {
            string[] thematch = null;
            bool retval = false;
            if (this.parent != null)
            {
                if (this.parent.def != null)
                {
                    if (this.parent.def.defName != null)
                    {
                        thematch = this.parent.def.defName.Split(new string[] { this.TDTag }, StringSplitOptions.None);
                        if (thematch.Length == 2)
                        {
                            retval = true;
                        }
                        else Log.Warning("parsed defname of '" + this.parent.def.defName + "' failed to split on tag '" + this.TDTag + "'.");
                    }
                    else Log.Warning("Unable to parse defName because no this.parent.def.defName");
                }
                else Log.Warning("Unable to parse defName because no this.parent.def");
            }
            else Log.Warning("Unable to parse defName because no this.parent");

            if (retval == true) { tdbase = thematch[0]; tdkey = thematch[1]; }
            else { tdbase = null; tdkey = null; }
            return retval;
        }


    }
}