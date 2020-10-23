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
    public class BackstoryDef : Def
    {
        public string baseDescription;
        public BodyTypeDef bodyTypeGlobal;
        public BodyTypeDef bodyTypeMale;
        public BodyTypeDef bodyTypeFemale;
        public string title;
        public string titleFemale;
        public string titleShort;
        public string titleShortFemale;
        public BackstorySlot slot = BackstorySlot.Adulthood;
        public bool shuffleable = true;
        public bool addToDatabase = true;
        public List<WorkTags> workAllows = new List<WorkTags>();
        public List<WorkTags> workDisables = new List<WorkTags>();
        public List<WorkTags> requiredWorkTags = new List<WorkTags>();
        public List<BackstoryDefSkillListItem> skillGains = new List<BackstoryDefSkillListItem>();
        public List<string> spawnCategories = new List<string>();
        public List<ChancedTraitEntry> forcedTraits = new List<ChancedTraitEntry>();
        public List<ChancedTraitEntry> disallowedTraits = new List<ChancedTraitEntry>();
        public float maleCommonality = 100f;
        public float femaleCommonality = 100f;
        public string linkedBackstory;
        //public RelationSettings relationSettings = new RelationSettings();
        public List<string> forcedHediffs = new List<string>();
        public IntRange bioAgeRange;
        public IntRange chronoAgeRange;
        public List<ThingDefCountRangeClass> forcedItems = new List<ThingDefCountRangeClass>();
        public Backstory backstory;

        public class ChancedTraitEntry
        {
            public string defName;
            public int degree = 0;
            public float chance = 100;
            public float commonalityMale = -1f;
            public float commonalityFemale = -1f;
        }

        public bool CommonalityApproved(Gender g) => Rand.Range(0, 100) < (g == Gender.Female ? femaleCommonality : maleCommonality);

        public bool Approved(Pawn p) =>
            CommonalityApproved(p.gender) &&
            RangeIncludes(bioAgeRange, p.ageTracker.AgeBiologicalYears) &&
            RangeIncludes(chronoAgeRange, p.ageTracker.AgeChronologicalYears);

        private static bool RangeIncludes(IntRange range, int val) => range == default || (val >= range.min && val <= range.max);

        public override void ResolveReferences()
        {
            base.ResolveReferences();

            if (!addToDatabase || BackstoryDatabase.allBackstories.ContainsKey(defName) || title.NullOrEmpty() || spawnCategories.NullOrEmpty())
                return;

            static List<TraitEntry> ForcedTraits(BackstoryDef bs)
            {
                if (bs.forcedTraits.NullOrEmpty())
                    return null;
                return bs.forcedTraits
                    .Where(trait => Rand.Range(0, 100) < trait.chance)
                    .Select(trait => new TraitEntry(TraitDef.Named(trait.defName), trait.degree))
                    .ToList();
            }
            static List<TraitEntry> DisallowedTraits(BackstoryDef bs)
            {
                if (bs.disallowedTraits.NullOrEmpty())
                    return null;
                return bs.disallowedTraits
                    .Where(trait => Rand.Range(0, 100) < trait.chance)
                    .Select(trait => new TraitEntry(TraitDef.Named(trait.defName), trait.degree))
                    .ToList();
            }
            static WorkTags WorkDisables(BackstoryDef bs)
            {
                var wt = WorkTags.None;
                if (bs.workAllows.NullOrEmpty())
                {
                    if (bs.workDisables != null)
                    {
                        foreach (var tag in bs.workDisables)
                            wt |= tag;
                    }
                }
                else
                {
                    foreach (WorkTags tag in Enum.GetValues(typeof(WorkTags)))
                    {
                        if (!bs.workAllows.Contains(tag))
                            wt |= tag;
                    }
                }
                return wt;
            }
            static WorkTags RequiredWorkTags(BackstoryDef bs)
            {
                var wt = WorkTags.None;
                foreach (var tag in bs.requiredWorkTags)
                    wt |= tag;
                return wt;
            }

            backstory = new Backstory
            {
                slot = slot,
                shuffleable = shuffleable,
                spawnCategories = spawnCategories,
                forcedTraits = ForcedTraits(this),
                disallowedTraits = DisallowedTraits(this),
                workDisables = WorkDisables(this),
                identifier = defName,
                requiredWorkTags = RequiredWorkTags(this),
            };

            bsBodyTypeGlobalResolved(backstory) = bodyTypeGlobal;
            bsBodyTypeFemaleResolved(backstory) = bodyTypeFemale;
            bsBodyTypeMaleResolved(backstory) = bodyTypeMale;
            bsSkillGains(backstory) = skillGains.ToDictionary(i => i.defName, i => i.amount);

            UpdateTranslateableFields(this);

            backstory.ResolveReferences();
            backstory.PostLoad();

            backstory.identifier = defName;

            var errors = backstory.ConfigErrors(ignoreNoSpawnCategories: false);
            if (!errors.Any())
                BackstoryDatabase.AddBackstory(backstory);
            else
                Log.Error(defName + " has errors:\n" + string.Join("\n", errors));
        }

        private static readonly AccessTools.FieldRef<Backstory, BodyTypeDef> bsBodyTypeGlobalResolved =
            AccessTools.FieldRefAccess<Backstory, BodyTypeDef>("bodyTypeGlobalResolved");
        private static readonly AccessTools.FieldRef<Backstory, BodyTypeDef> bsBodyTypeFemaleResolved =
            AccessTools.FieldRefAccess<Backstory, BodyTypeDef>("bodyTypeFemaleResolved");
        private static readonly AccessTools.FieldRef<Backstory, BodyTypeDef> bsBodyTypeMaleResolved =
            AccessTools.FieldRefAccess<Backstory, BodyTypeDef>("bodyTypeMaleResolved");
        private static readonly AccessTools.FieldRef<Backstory, Dictionary<string, int>> bsSkillGains =
            AccessTools.FieldRefAccess<Backstory, Dictionary<string, int>>("skillGains");

        internal static void UpdateTranslateableFields(BackstoryDef bs)
        {
            if (bs.backstory == null)
                return;

            bs.backstory.baseDesc = bs.baseDescription.NullOrEmpty() ? "Empty." : bs.baseDescription;
            bs.backstory.SetTitle(newTitle: bs.title, newTitleFemale: bs.titleFemale);
            bs.backstory.SetTitleShort(newTitleShort: bs.titleShort.NullOrEmpty() ? bs.backstory.title : bs.titleShort,
                newTitleShortFemale: bs.titleShortFemale.NullOrEmpty() ? bs.backstory.titleFemale : bs.titleShortFemale);
        }

        public struct BackstoryDefSkillListItem
        {
#pragma warning disable CS0649
            public string defName;
            public int amount;
#pragma warning restore CS0649
        }
    }
}
