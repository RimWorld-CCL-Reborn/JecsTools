using System.Xml;
using RimWorld;
using Verse;

namespace JecsTools
{
    public class StuffCategoryCountClass : IExposable
    {
        public StuffCategoryDef stuffCatDef;

        public int count;

        public StuffCategoryCountClass()
        {
        }

        public StuffCategoryCountClass(StuffCategoryDef stuffCatDef, int count)
        {
            if (count < 0)
            {
                Log.Warning($"Tried to set StuffCategoryCountClass count to {count}. stuffDef={stuffCatDef}", false);
                count = 0;
            }
            this.stuffCatDef = stuffCatDef;
            this.count = count;
        }

        public string Summary => $"{count}x {stuffCatDef?.label ?? "null"}";

        public void ExposeData()
        {
            Scribe_Defs.Look(ref stuffCatDef, nameof(stuffCatDef));
            Scribe_Values.Look(ref count, nameof(count), 1);
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("Misconfigured StuffCategoryCountClass: " + xmlRoot.OuterXml, false);
            }
            else
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "stuffCatDef", xmlRoot.Name);
                count = (int)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(int));
            }
        }

        public override string ToString() => $"{count}x {stuffCatDef?.defName ?? "null"}";

        public override int GetHashCode() => stuffCatDef.shortHash + count << 16;

        public static implicit operator StuffCategoryCountClass(StuffDefCount t)
        {
            return new StuffCategoryCountClass(t.StuffDef, t.Count);
        }
    }
}
