using System.Xml;
using RimWorld;
using Verse;

namespace JecsTools
{
    public class StuffCategoryCountClass: IExposable
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
                Log.Warning(string.Concat(new object[]
                {
                    "Tried to set StuffCategoryCountClass count to ",
                    count,
                    ". stuffDef=",
                    stuffCatDef
                }), false);
                count = 0;
            }
            this.stuffCatDef = stuffCatDef;
            this.count = count;
        }

        public string Summary
        {
            get
            {
                return this.count + "x " + ((this.stuffCatDef == null) ? "null" : this.stuffCatDef.label);
            }
        }

        public void ExposeData()
        {
            Scribe_Defs.Look<StuffCategoryDef>(ref this.stuffCatDef, "stuffCatDef");
            Scribe_Values.Look<int>(ref this.count, "count", 1, false);
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
                this.count = (int)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(int));
            }
        }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                "(",
                this.count,
                "x ",
                (this.stuffCatDef == null) ? "null" : this.stuffCatDef.defName,
                ")"
            });
        }

        public override int GetHashCode()
        {
            return (int)this.stuffCatDef.shortHash + this.count << 16;
        }

        public static implicit operator StuffCategoryCountClass(StuffDefCount t)
        {
            return new StuffCategoryCountClass(t.StuffDef, t.Count);
        }
    }
}