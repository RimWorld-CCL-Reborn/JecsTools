using System;
using System.Collections.Generic;
using Verse;

namespace CompToggleDef
{
    public class CompProperties_ToggleDef : CompProperties
    {
        public string toggleDefTag = "_TOGGLEDEF_";

        private const string DefaultLabelKey = "TOGGLEDEF";

        public string labelKey = DefaultLabelKey; // needs a default value to avoid NRE

        // Can change into these
        [Unsaved]
        public List<ThingDef> toggleDefs;

        public CompProperties_ToggleDef()
        {
            compClass = typeof(CompToggleDef);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            var parsed = parentDef.defName.Split(new[] { toggleDefTag }, StringSplitOptions.None);
            if (parsed.Length == 2)
                toggleDefs = FillToggleDefs(parsed[0], parsed[1]);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;
            if (toggleDefs == null)
                yield return $"unable to parse defName '{parentDef.defName}' - must have format '<baseName>{toggleDefTag}<toggleKey>'";
            if (labelKey == DefaultLabelKey && !Translator.CanTranslate(labelKey))
                yield return $"{nameof(labelKey)} has default value '{DefaultLabelKey}' which lacks a translation entry";
        }

        private List<ThingDef> FillToggleDefs(string baseName, string toggleKey)
        {
            // go through all and find the ones that match
            var toggleDefs = new List<ThingDef>();
            var toggleBase = baseName + toggleDefTag;
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.defName.StartsWith(toggleBase))
                    toggleDefs.Add(def);
            }
            //Log.Message($"FillToggleDefs('{baseName}', '{toggleKey}') => " + toggleDefs.ToStringSafeEnumerable());
            return toggleDefs;
        }
    }
}
