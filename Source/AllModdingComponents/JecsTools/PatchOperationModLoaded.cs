using System.Linq;
using System.Xml;
using Verse;

namespace JecsTools
{
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