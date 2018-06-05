using System;
using Verse;
using System.Xml;
using RimWorld;

namespace JecsTools
{
    //Based on Verse.ThingCountClass
    public sealed class StuffCategoryCountClass
    {
        public StuffCategoryDef stuffCatDef;

		public int count;

        public string Summary {
            get {
                return this.count + "x " + ((this.stuffCatDef == null) ? "null" : this.stuffCatDef.label);
            }
        }

        public StuffCategoryCountClass()
        {
        }

        public StuffCategoryCountClass(StuffCategoryDef stuffCatDef, int count)
        {
            this.stuffCatDef = stuffCatDef;
			this.count = count;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1) {
                Log.Error("Misconfigured StuffCategoryCount: " + xmlRoot.OuterXml);
                return;
            }
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "stuffCatDef", xmlRoot.Name);
            this.count = (int)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(int));
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
    }
}
