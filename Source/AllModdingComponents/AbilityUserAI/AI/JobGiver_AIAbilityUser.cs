using System.Linq;
using AbilityUser;
using Verse;
using Verse.AI;

/*
 * Author: ChJees
 * Created: 2017-09-20
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Give jobs to cast abilities
    /// </summary>
    public class JobGiver_AIAbilityUser : ThinkNode_JobGiver
    {
        // Note: For vanilla RW (and this framework), this method is only called from TryIssueJobPackage of
        // ThinkNode_PrioritySorter and ThinkNode_FilterPriority for subnodes that are JobGiver_AIAbilityUser.
        // The InsertHookTest ThinkNodeDef that this framework defines in InsertHook_AbilityUserAI.xml doesn't
        // use either of them, so this only matters if another mod has a JobGiver_AIAbilityUser as a subnode of
        // ThinkNode_PrioritySorter/ThinkNode_FilterPriority (or custom ThinkNode that calls subnode.GetPriority).
        public override float GetPriority(Pawn pawn)
        {
            foreach (var abilityUser in pawn.GetCompAbilityUsers())
            {
                if (abilityUser.Initialized && abilityUser.AbilityData.Count > 0)
                    return 100f;
            }
            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            //Do we have at least one elegible profile?
            var profiles = pawn.EligibleAIProfiles();

            //var builder = new System.Text.StringBuilder("profiles = ");
            //foreach(AbilityUserAIProfileDef profile in profiles)
            //{
            //    builder.Append(profile.defName + ", ");
            //}
            //Log.Message(builder.ToString());

            foreach (var profile in profiles)
            {
                //Traverse the decision tree.
                //List<AbilityDecisionNode> currentNodes = new List<AbilityDecisionNode>();
                //List<AbilityDecisionNode> nextNodes = new List<AbilityDecisionNode>();

                //Seed root.
                //nextNodes.Add(profile.decisionTree);

                //Result AbilityAIDef to use.
                AbilityAIDef useThisAbility = null;

                if (profile.decisionTree != null)
                    useThisAbility = profile.decisionTree.RecursivelyGetAbility(pawn);

                //Debug
                /*int nodesTraversed = 0;
                AbilityDecisionNode lastNode = null;

                //Flat recursive iteration
                do
                {
                    //Add from next list to current list.
                    currentNodes.AddRange(nextNodes);
                    nextNodes.Clear();

                    //Check if we can continue traversing on the current level.
                    foreach (AbilityDecisionNode currentNode in currentNodes)
                    {
                        nodesTraversed++;

                        if (currentNode.CanContinueTraversing(pawn))
                            nextNodes.AddRange(currentNode.subNodes);

                        //Try picking an ability.
                        useThisAbility = currentNode.TryPickAbility(pawn);

                        //Found ability to use.
                        if (useThisAbility != null)
                        {
                            lastNode = currentNode;
                            break;
                        }
                    }

                    //Found ability to use.
                    if (useThisAbility != null)
                        break;

                    //Clear current set.
                    currentNodes.Clear();
                } while (nextNodes.Count > 0);*/

                //Debug
                //if (useThisAbility != null)
                //    Log.Message("JobGiver_AIAbilityUser.TryGiveJob for '" + pawn.ThingID + "' with ability: " + useThisAbility.defName + ", while traversing " + nodesTraversed + " nodes.");
                //else
                //    Log.Message("JobGiver_AIAbilityUser.TryGiveJob for '" + pawn.ThingID + "' with ability: No ability, while traversing " + nodesTraversed + " nodes.");

                if (useThisAbility != null)
                {
                    //Debug
                    /*Log.Message("Ability '" + useThisAbility.defName + "' picked for AI.\n" +
                        "lastNode=" + lastNode.GetType().Name + "\n" +
                        "lastNode.parent=" + lastNode?.parent?.GetType()?.Name);*/

                    //Get CompAbilityUser
                    var compAbilityUser = pawn.GetExactCompAbilityUser(profile.compAbilityUserClass);

                    if (compAbilityUser != null)
                    {
                        //Get Ability from Pawn.
                        var useAbility = compAbilityUser.AbilityData.AllPowers
                            .First(ability => ability.Def == useThisAbility.ability);

                        //Give job.
                        if (useAbility.CanCastPowerCheck(AbilityContext.AI, out var reason))
                        {
                            var target = useThisAbility.Worker.TargetAbilityFor(useThisAbility, pawn);
                            if (target.IsValid)
                                return useAbility.UseAbility(AbilityContext.AI, target);
                        }
                    }
                }
            }

            //No Job to give.
            //Report.
            //Log.Message("JobGiver_AIAbilityUser.TryGiveJob for '" + pawn.ThingID + "' is Invalid.");
            return null;
        }
    }
}
