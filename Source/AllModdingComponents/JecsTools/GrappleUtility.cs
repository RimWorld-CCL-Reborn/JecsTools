using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace JecsTools
{
    public static class GrappleUtility
    {

        public enum GrappleType
        {
            Humanoid,           //human grapples a human
            HumanoidXAnimal,    //human grapples animal
            AnimalXHumanoid,    //animal grapples human
            AnimalXAnimal,      //animal grapples animal
            None
        }

        public static bool TryGrapple(this Pawn grappler, Pawn victim)
        {
            //Null handling
            //---------------------------------------------------------
            if (!CanGrapple(grappler, victim))
            {
                return false;
            }

            BodyPartRecord grapplingPart;
            if (!TryGetGrapplingPart(grappler, out grapplingPart))
            {
                return false;
            }

            //Special Case Handling
            //---------------------------------------------------------
            if (CanGrappleNoContest(grappler, victim, true))
            {
                return true;
            }

            //Resolve Grapple Rolls
            //---------------------------------------------------------

            //Setup rolls
            float rollGrappler = Rand.Range(1, 10); //Introduces some random chance into the grapple.
            float rollVictim = Rand.Range(1, 10);

            //Setup modifiers
            float modifierGrappler = grappler.RaceProps.baseBodySize; //Boosts the chance of success/failure for both parties.
            float modifierVictim = victim.RaceProps.baseBodySize;
            ResolveModifiers(grappler, victim, ref modifierGrappler, ref modifierVictim);

            //Determine success of grapples
            if (rollGrappler + modifierGrappler > rollVictim + modifierVictim)
            {
                MoteMaker.ThrowText(grappler.DrawPos, grappler.Map, 
                    rollGrappler + " + " + modifierGrappler + " = " + (rollGrappler + modifierGrappler) 
                    + " vs " +
                    rollVictim + " + " + modifierVictim + " = " + (rollVictim + modifierVictim)
                    + " : " + "JTGrapple_Success".Translate(), -1f);

                Find.BattleLog.Add(
                    new BattleLogEntry_StateTransition(victim,
                    RulePackDef.Named("JT_GrappleSuccess"), grappler, null, grapplingPart.def)
                );
                return true;
            }
            MoteMaker.ThrowText(grappler.DrawPos, grappler.Map,
                rollGrappler + " + " + modifierGrappler + " = " + (rollGrappler + modifierGrappler)
                + " vs " +
                rollVictim + " + " + modifierVictim + " = " + (rollVictim + modifierVictim)
                + " : " + "JTGrapple_Failed".Translate(), -1f);
            Find.BattleLog.Add(
                new BattleLogEntry_StateTransition(victim,
                RulePackDef.Named("JT_GrappleFailed"), grappler, null, grapplingPart.def)
            );
            return false;
        }

        /// <summary>
        /// Stuns the target for time
        /// </summary>
        /// <param name="grappler"></param>
        /// <param name="victim"></param>
        /// <param name="ticks"></param>
        public static void ApplyGrappleEffect(Pawn grappler, Pawn victim, int ticks = 600)
        {
            victim.stances.stunner.StunFor(ticks);
            PawnUtility.ForceWait(victim, ticks, grappler);
        }

        /// <summary>
        /// Null Handling to weed out bad calls to grapple
        /// </summary>
        /// <param name="grappler"></param>
        /// <param name="victim"></param>
        /// <returns></returns>
        public static bool CanGrapple(Pawn grappler, Pawn victim)
        {
            //Null Handling
            //---------------------------------------------------------

            //If no grappler exists, the grapple fails.
            if (grappler == null || !grappler.Spawned || grappler.Dead)
                return false;

            //If no victim exist, the grapple fails.
            if (victim == null || !victim.Spawned || victim.Dead)
                return false;

            return true;
        }

        /// <summary>
        /// Checks for special cases where grappling can occur instantly without contest.
        /// E.g. grappling downed characters.
        /// </summary>
        /// <param name="grappler"></param>
        /// <param name="victim"></param>
        /// <param name="throwText"></param>
        /// <returns></returns>
        public static bool CanGrappleNoContest(Pawn grappler, Pawn victim, bool throwText = true)
        {
            if (!victim.Awake())
            {
                if (throwText) MoteMaker.ThrowText(grappler.DrawPos, grappler.Map, "JTGrapple_SleepingGrapple".Translate(), -1f);
                return true;
            }

            if (victim.Downed)
            {
                if (throwText) MoteMaker.ThrowText(grappler.DrawPos, grappler.Map, "JTGrapple_DownedGrapple".Translate(), -1f);
                return true;
            }

            if (victim.IsPrisonerOfColony && RestraintsUtility.InRestraints(victim))
            {
                if (throwText) MoteMaker.ThrowText(grappler.DrawPos, grappler.Map, "JTGrapple_PrisonerGrapple".Translate(), -1f);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns any part that is not missing and capable of manipulation.
        /// </summary>
        /// <param name="grappler"></param>
        /// <param name="bodyPartRec"></param>
        /// <returns></returns>
        public static bool TryGetGrapplingPart(Pawn grappler, out BodyPartRecord bodyPartRec)
        {
            BodyPartRecord result = null;
            if (grappler.health.hediffSet.GetNotMissingParts().ToList().FindAll(x => x.def.tags.Contains("ManipulationLimbSegment") || x.def.tags.Contains("ManipulationLimbCore")) is List<BodyPartRecord> recs && !recs.NullOrEmpty())
            {
                result = recs.RandomElement();
            }
            bodyPartRec = result;
            return bodyPartRec != null;
        }

        /// <summary>
        /// Sets up modifiers for grapple checks, similar to tabletop RPGs.
        /// If the characters are humanoid, use their melee skill in the check.
        /// </summary>
        /// <param name="grappler"></param>
        /// <param name="victim"></param>
        /// <param name="modifierGrappler"></param>
        /// <param name="modifierVictim"></param>
        public static void ResolveModifiers(Pawn grappler, Pawn victim, ref float modifierGrappler, ref float modifierVictim)
        {
            GrappleType grappleType = ResolveGrappleType(grappler, victim);
            switch (grappleType)
            {
                case GrappleType.Humanoid:
                    modifierGrappler
                        += grappler.skills.GetSkill(SkillDefOf.Melee).Level
                         + ResolveToolModifier(grappler)
                         + ResolveAdditionalModifiers(grappler);
                    modifierVictim
                        += victim.skills.GetSkill(SkillDefOf.Melee).Level
                         + ResolveToolModifier(victim)
                         + ResolveAdditionalModifiers(victim);
                    break;
                case GrappleType.HumanoidXAnimal:
                    modifierGrappler
                        += grappler.skills.GetSkill(SkillDefOf.Melee).Level
                         + ResolveToolModifier(grappler)
                         + ResolveAdditionalModifiers(grappler);
                    modifierVictim 
                        += ResolveToolModifier(victim);
                    break;
                case GrappleType.AnimalXHumanoid:
                    modifierGrappler
                        += ResolveToolModifier(grappler);
                    modifierVictim
                        += victim.skills.GetSkill(SkillDefOf.Melee).Level
                         + ResolveToolModifier(victim)
                         + ResolveAdditionalModifiers(victim);
                    break;
                case GrappleType.AnimalXAnimal:
                    modifierGrappler
                        += ResolveToolModifier(grappler);
                    modifierVictim
                        += ResolveToolModifier(victim);
                    break;
            }
        }
        
        /// <summary>
        /// Checks a character for melee tools to use in grapple calculations.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static float ResolveToolModifier(Pawn pawn)
        {
            return pawn?.def?.tools?.Max(x => x.power) ?? 0;
        }

        /// <summary>
        /// Checks humanoid characters for ability user components and their modifiers.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="modifier"></param>
        public static float ResolveAdditionalModifiers(Pawn pawn)
        {
            float result = 0f;
            try
            {
                ((Action)(() =>
                {
                    IEnumerable<AbilityUser.CompAbilityUser> abilityUsers = pawn.GetComps<AbilityUser.CompAbilityUser>();
                    foreach (AbilityUser.CompAbilityUser a in abilityUsers)
                    {
                        result += a.GrappleModifier;
                    }
                })).Invoke();

            }
            catch (TypeLoadException) { }

            return result;
        }

        /// <summary>
        /// Determines what kind of grapple is taking place.
        /// </summary>
        /// <param name="grappler"></param>
        /// <param name="victim"></param>
        /// <returns></returns>
        public static GrappleType ResolveGrappleType(Pawn grappler, Pawn victim)
        {
            GrappleType grappleType = GrappleType.None;

            if (grappler.RaceProps.Humanlike &&
                  victim.RaceProps.Humanlike)
                grappleType = GrappleType.Humanoid;

            else if (grappler.RaceProps.Humanlike &&
                       victim.RaceProps.Animal)
                grappleType = GrappleType.HumanoidXAnimal;

            else if (grappler.RaceProps.Animal &&
                       victim.RaceProps.Humanlike)
                grappleType = GrappleType.AnimalXHumanoid;

            else
                grappleType = GrappleType.AnimalXAnimal;

            return grappleType;
        }
    }
}
