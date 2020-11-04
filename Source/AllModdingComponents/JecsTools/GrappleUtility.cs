using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public static class GrappleUtility
    {
        public enum GrappleType
        {
            Humanoid, //human grapples a human
            HumanoidXAnimal, //human grapples animal
            AnimalXHumanoid, //animal grapples human
            AnimalXAnimal, //animal grapples animal
            None
        }

        public interface IGrappleModifier
        {
            float Resolve(Pawn pawn);
        }

        public static bool TryGrapple(this Pawn grappler, Pawn victim, int grapplerBonusMod = 0, int victimBonusMod = 0)
        {
            //Null handling
            //---------------------------------------------------------
            if (!CanGrapple(grappler, victim))
                return false;

            if (!TryGetGrapplingPart(grappler, out var grapplingPart))
                return false;

            //Special Case Handling
            //---------------------------------------------------------
            if (CanGrappleNoContest(grappler, victim, true))
            {
                TryMakeBattleLog(victim, grappler, grapplingPart);
                return true;
            }

            //Resolve Grapple Rolls
            //---------------------------------------------------------
            return IsGrappleSuccessful(grappler, victim, grapplingPart, grapplerBonusMod, victimBonusMod);
        }

        public static bool IsGrappleSuccessful(Pawn grappler, Pawn victim, BodyPartRecord grapplingPart,
            int grapplerBonusMod = 0, int victimBonusMod = 0)
        {
            //Setup rolls
            float rollGrappler = Rand.Range(1, 10); //Introduces some random chance into the grapple.
            float rollVictim = Rand.Range(1, 10);

            //Setup modifiers
            var modifierGrappler =
                grappler.RaceProps.baseBodySize; //Boosts the chance of success/failure for both parties.
            var modifierVictim = victim.RaceProps.baseBodySize;
            ResolveModifiers(grappler, victim, ref modifierGrappler, ref modifierVictim);

            //Add bonus modiiers from parameters
            modifierGrappler += grapplerBonusMod;
            modifierVictim += victimBonusMod;

            //Throw a mental warning
            if (victim.mindState is Pawn_MindState mind)
                mind.Notify_DamageTaken(new DamageInfo(DamageDefOf.Bite, -1, 0f, -1, grappler));

            //Determine success of grapples
            if (rollGrappler + modifierGrappler > rollVictim + modifierVictim)
            {
                MoteMaker.ThrowText(grappler.DrawPos, grappler.Map,
                    $"{rollGrappler} + {modifierGrappler} = {rollGrappler + modifierGrappler} vs " +
                    $"{rollVictim} + {modifierVictim} = {rollVictim + modifierVictim} : " +
                    "JTGrapple_Success".Translate());

                TryMakeBattleLog(victim, grappler, grapplingPart);
                return true;
            }
            MoteMaker.ThrowText(grappler.DrawPos, grappler.Map,
                $"{rollGrappler} + {modifierGrappler} = {rollGrappler + modifierGrappler} vs " +
                $"{rollVictim} + {modifierVictim} = {rollVictim + modifierVictim} : " +
                "JTGrapple_Failed".Translate());
            return false;
        }

        /// <summary>
        ///     Stuns the target for time
        /// </summary>
        /// <param name="grappler"></param>
        /// <param name="victim"></param>
        /// <param name="ticks"></param>
        public static void ApplyGrappleEffect(Pawn grappler, Pawn victim, int ticks = 600)
        {
            victim.stances.stunner.StunFor(ticks, grappler);
            PawnUtility.ForceWait(victim, ticks, grappler);
        }

        /// <summary>
        ///     Null Handling to weed out bad calls to grapple
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
        ///     Checks for special cases where grappling can occur instantly without contest.
        ///     E.g. grappling downed characters.
        /// </summary>
        /// <param name="grappler"></param>
        /// <param name="victim"></param>
        /// <param name="throwText"></param>
        /// <returns></returns>
        public static bool CanGrappleNoContest(Pawn grappler, Pawn victim, bool throwText = true)
        {
            if (!victim.Awake())
            {
                if (throwText)
                    MoteMaker.ThrowText(grappler.DrawPos, grappler.Map, "JTGrapple_SleepingGrapple".Translate());
                return true;
            }

            if (victim.Downed)
            {
                if (throwText)
                    MoteMaker.ThrowText(grappler.DrawPos, grappler.Map, "JTGrapple_DownedGrapple".Translate());
                return true;
            }

            if (victim.IsPrisonerOfColony && RestraintsUtility.InRestraints(victim))
            {
                if (throwText)
                    MoteMaker.ThrowText(grappler.DrawPos, grappler.Map, "JTGrapple_PrisonerGrapple".Translate());
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Returns any part that is not missing and capable of manipulation.
        /// </summary>
        /// <param name="grappler"></param>
        /// <param name="bodyPartRec"></param>
        /// <returns></returns>
        public static bool TryGetGrapplingPart(Pawn grappler, out BodyPartRecord bodyPartRec)
        {
            var grapplingParts = new List<BodyPartRecord>();
            foreach (var part in grappler.health.hediffSet.GetNotMissingParts())
            {
                if (part.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbSegment) ||
                    part.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbCore))
                    grapplingParts.Add(part);
            }
            return grapplingParts.TryRandomElement(out bodyPartRec);
        }

        /// <summary>
        ///     Sets up modifiers for grapple checks, similar to tabletop RPGs.
        ///     If the characters are humanoid, use their melee skill in the check.
        /// </summary>
        /// <param name="grappler"></param>
        /// <param name="victim"></param>
        /// <param name="modifierGrappler"></param>
        /// <param name="modifierVictim"></param>
        public static void ResolveModifiers(Pawn grappler, Pawn victim, ref float modifierGrappler,
            ref float modifierVictim)
        {
            var grappleType = ResolveGrappleType(grappler, victim);
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
        ///     Checks a character for melee tools to use in grapple calculations.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static float ResolveToolModifier(Pawn pawn)
        {
            var maxPower = 0f;
            var tools = pawn.def?.tools;
            if (tools != null)
            {
                foreach (var tool in tools)
                {
                    if (maxPower < tool.power)
                        maxPower = tool.power;
                }
            }
            return maxPower;
        }

        private class CompAbilityUserGrappleModifier : IGrappleModifier
        {
            // This checks that the CompAbilityUser type is available - if this fails, this GrappleModifier won't be used.
            static CompAbilityUserGrappleModifier() => typeof(CompAbilityUser).ToString();

            public float Resolve(Pawn pawn)
            {
                var result = 0f;
                var abilityUsers = pawn.GetCompAbilityUsers();
                foreach (var a in abilityUsers)
                    result += a.GrappleModifier;
                return result;
            }
        }

        private static readonly IGrappleModifier[] additionalModifiers = GenTypes.AllTypes
            .Select(type =>
            {
                if (type.IsAbstract || !typeof(IGrappleModifier).IsAssignableFrom(type))
                    return null;
                try
                {
                    return (IGrappleModifier)Activator.CreateInstance(type);
                }
                catch
                {
                    Log.Message($"{typeof(GrappleUtility).FullName}: couldn't load or create instance of {type} - skipping");
                    return null;
                }
            })
            .Where(x => x != null)
            .ToArray();

        /// <summary>
        ///     Checks humanoid characters for ability user components and their modifiers.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="modifier"></param>
        public static float ResolveAdditionalModifiers(Pawn pawn)
        {
            var result = 0f;
            for (var i = 0; i < additionalModifiers.Length; i++)
                result += additionalModifiers[i].Resolve(pawn);
            return result;
        }

        /// <summary>
        ///     Determines what kind of grapple is taking place.
        /// </summary>
        /// <param name="grappler"></param>
        /// <param name="victim"></param>
        /// <returns></returns>
        public static GrappleType ResolveGrappleType(Pawn grappler, Pawn victim)
        {
            if (grappler.RaceProps.Humanlike && victim.RaceProps.Humanlike)
                return GrappleType.Humanoid;
            else if (grappler.RaceProps.Humanlike && victim.RaceProps.Animal)
                return GrappleType.HumanoidXAnimal;
            else if (grappler.RaceProps.Animal && victim.RaceProps.Humanlike)
                return GrappleType.AnimalXHumanoid;
            else
                return GrappleType.AnimalXAnimal;
        }

        /// <summary>
        ///     Makes a battle log using the RulePackDef of JT_GrappleSuccess.
        ///     It's pretty cool, because we can see the character's grapple attempt in combat logs.
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="grappler"></param>
        /// <param name="grapplingPart"></param>
        /// <returns></returns>
        public static bool TryMakeBattleLog(Pawn victim, Pawn grappler, BodyPartRecord grapplingPart)
        {
            Find.BattleLog.Add(
                new BattleLogEntry_StateTransition(victim, MiscDefOf.JT_GrappleSuccess, grappler, null, grapplingPart));
            return true;
        }
    }
}
