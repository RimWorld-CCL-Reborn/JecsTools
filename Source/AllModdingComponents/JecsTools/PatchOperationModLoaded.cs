using System.Linq;
using System.Xml;
using Verse;

namespace JecsTools
{
    //Original code from NoImageAvailable's Combat Extended (You're a legend, sir)
    //https://github.com/NoImageAvailable/CombatExtended/blob/master/Source/CombatExtended/CombatExtended/PatchOperationFindMod.cs
    //Using under ShareAlike license: https://creativecommons.org/licenses/by-nc-sa/4.0/
    public class PatchOperationModLoaded : PatchOperation
    {
#pragma warning disable 649
        private string modName;
#pragma warning restore 649
        
        protected override bool ApplyWorker(XmlDocument xml)
        {
            return !modName.NullOrEmpty() && ModsConfig.ActiveModsInLoadOrder.Any(mod => mod.Name == modName);
        }
    }
}