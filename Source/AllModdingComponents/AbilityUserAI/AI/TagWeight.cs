using System.Xml;
using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-23
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Represents the weight of a tag.
    /// </summary>
    public class TagWeight
    {
        /// <summary>
        ///     Tag name.
        /// </summary>
        public string tag;

        /// <summary>
        ///     Tag weight.
        /// </summary>
        public float weight;

        public TagWeight()
        {
            tag = "";
            weight = 0.0f;
        }

        public TagWeight(string tag, float weight)
        {
            this.tag = tag;
            this.weight = weight;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("Misconfigured weight: " + xmlRoot.OuterXml);
                return;
            }

            tag = xmlRoot.Name;
            weight = (float) ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(float));
        }

        public override string ToString()
        {
            return string.Concat("(", weight, "x ", tag == null ? "null" : tag, ")");
        }

        public override int GetHashCode()
        {
            return (tag.GetHashCode() + (int) weight) << 16;
        }
    }
}