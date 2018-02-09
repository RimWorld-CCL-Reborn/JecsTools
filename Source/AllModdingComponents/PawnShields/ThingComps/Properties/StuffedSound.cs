using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Verse;

namespace PawnShields
{
    /// <summary>
    /// Links a StuffCategoryDef to a SoundDef.
    /// </summary>
    public class StuffedSound
    {
        /// <summary>
        /// Stuff category to link to.
        /// </summary>
        public StuffCategoryDef stuffCategory;

        /// <summary>
        /// Sound to link to.
        /// </summary>
        public SoundDef sound;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("Misconfigured StuffedSound: " + xmlRoot.OuterXml);
                return;
            }
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "stuffCategory", xmlRoot.Name);
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "sound", xmlRoot.FirstChild.Value);
        }
    }
}
