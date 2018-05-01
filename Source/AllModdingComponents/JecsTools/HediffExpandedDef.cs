using Verse;

namespace JecsTools
{
    /// <summary>
    /// A simple hediff for translations.
    /// </summary>
    public class HediffExpandedDef : HediffDef
    {
        /// <summary>
        /// Determines if the description should be shown for a hediff.
        /// </summary>
        public bool showDescription;
     
        ///Reminder: the string description already exists in the Def class we inherit in this class
        //public string description;
        
        /// <summary>
        /// Text key that appears before the list of modifiers.
        /// </summary>
        public string preListText;
        
        /// <summary>
        /// Text key that appears after the list of modifiers.
        /// </summary>
        public string postListText;
        
    }
}