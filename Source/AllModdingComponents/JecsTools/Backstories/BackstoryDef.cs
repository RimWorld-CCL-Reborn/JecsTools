using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace JecsTools
{
    //Pulled from erdelf's Alien Races 2.0
    //Original credit and work belong to erdelf (https://github.com/erdelf)
    //Link -> https://github.com/RimWorld-CCL-Reborn/AlienRaces/blob/94bf6b6d7a91e9587bdc40e8a231b18515cb6bb7/Source/AlienRace/AlienRace/BackstoryDef.cs
    public class BackstoryDef : RimWorld.BackstoryDef
    {

        public static HashSet<BackstoryDef> checkBodyType = new HashSet<BackstoryDef>();

        public List<ChancedTraitEntry> forcedTraitsChance = new List<ChancedTraitEntry>();
        public List<ChancedTraitEntry> disallowedTraitsChance = new List<ChancedTraitEntry>();
        public WorkTags workAllows = WorkTags.AllWork;
        public float maleCommonality = 100f;
        public float femaleCommonality = 100f;
        public BackstoryDef linkedBackstory;
        public List<string> forcedHediffs = new List<string>();
        public List<SkillGain> passions = new List<SkillGain>();
        public IntRange bioAgeRange;
        public IntRange chronoAgeRange;
        public List<ThingDefCountRangeClass> forcedItems = new List<ThingDefCountRangeClass>();

        public bool CommonalityApproved(Gender g) => Rand.Range(min: 0, max: 100) < (g == Gender.Female ? this.femaleCommonality : this.maleCommonality);

        public bool Approved(Pawn p) => this.CommonalityApproved(p.gender) &&
                                        (this.bioAgeRange == default || (this.bioAgeRange.min < p.ageTracker.AgeBiologicalYears && p.ageTracker.AgeBiologicalYears < this.bioAgeRange.max)) &&
                                        (this.chronoAgeRange == default || (this.chronoAgeRange.min < p.ageTracker.AgeChronologicalYears && p.ageTracker.AgeChronologicalYears < this.chronoAgeRange.max));

        public override void ResolveReferences()
        {
            this.identifier = this.defName;
            base.ResolveReferences();

            this.forcedTraits = (this.forcedTraits ??= new List<BackstoryTrait>()).
                                Concat(this.forcedTraitsChance.Where(predicate: trait => Rand.Range(min: 0, max: 100) < trait.chance).ToList().ConvertAll(converter: trait => new BackstoryTrait { def = trait.defName, degree = trait.degree })).ToList();
            this.disallowedTraits = (this.disallowedTraits ??= new List<BackstoryTrait>()).
                                    Concat(this.disallowedTraitsChance.Where(predicate: trait => Rand.Range(min: 0, max: 100) < trait.chance).ToList().ConvertAll(converter: trait => new BackstoryTrait { def = trait.defName, degree = trait.degree })).ToList();
            this.workDisables = (this.workAllows & WorkTags.AllWork) != 0 ? this.workDisables : ~this.workAllows;

            if (this.bodyTypeGlobal == null && this.bodyTypeFemale == null && this.bodyTypeMale == null)
            {
                checkBodyType.Add(this);
                this.bodyTypeGlobal = DefDatabase<BodyTypeDef>.GetRandom();
            }
        }


        public class ChancedTraitEntry
        {
            public TraitDef defName;
            public int degree = 0;
            public float chance = 100;
            public float commonalityMale = -1f;
            public float commonalityFemale = -1f;
        }

        public struct BackstoryDefSkillListItem
        {
#pragma warning disable CS0649
            public string defName;
            public int amount;
#pragma warning restore CS0649
        }


        //public string baseDescription;
        //public BodyTypeDef bodyTypeGlobal;
        //public BodyTypeDef bodyTypeMale;
        //public BodyTypeDef bodyTypeFemale;
        //public string title;
        //public string titleFemale;
        //public string titleShort;
        //public string titleShortFemale;
        //public BackstorySlot slot = BackstorySlot.Adulthood;
        //public bool shuffleable = true;
        //public bool addToDatabase = true;
        //public List<WorkTags> workAllows = new List<WorkTags>();
        //public List<WorkTags> workDisables = new List<WorkTags>();
        //public List<WorkTags> requiredWorkTags = new List<WorkTags>();
        //public List<BackstoryDefSkillListItem> skillGains = new List<BackstoryDefSkillListItem>();
        //public List<string> spawnCategories = new List<string>();
        //public List<ChancedTraitEntry> forcedTraits = new List<ChancedTraitEntry>();
        //public List<ChancedTraitEntry> disallowedTraits = new List<ChancedTraitEntry>();
        //public float maleCommonality = 100f;
        //public float femaleCommonality = 100f;
        //public string linkedBackstory;
        ////public RelationSettings relationSettings = new RelationSettings();
        //public List<string> forcedHediffs = new List<string>();
        //public IntRange bioAgeRange;
        //public IntRange chronoAgeRange;
        //public List<ThingDefCountRangeClass> forcedItems = new List<ThingDefCountRangeClass>();
        //public Backstory backstory;
    }
}
