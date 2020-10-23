// This Verb's main purpose is to treat bursts as over time and not at once, as in the parent class. This
// changes was in response to CompAbilityUser adding VerbTicks to its verbs.

namespace AbilityUser
{
    public class Verb_UseAbility_TrueBurst : Verb_UseAbility
    {
        //// Made it so burst is not burst per each target, but back to the regular burst-over-time.
        //protected override bool TryCastShot()
        //{
        //    ability.CooldownTicksLeft = (int)UseAbilityProps.SecondsToRecharge * GenTicks.TicksPerRealSecond;
        //    var result = false;
        //    TargetsAoE.Clear();
        //    UpdateTargets();
        //    //var burstShots = ShotsPerBurst;
        //    if (UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && TargetsAoE.Count > 1)
        //    {
        //        TargetsAoE.RemoveRange(0, TargetsAoE.Count - 1);
        //    }
        //    for (var i = 0; i < TargetsAoE.Count; i++)
        //    {
        //        //for (var j = 0; j < burstshots; j++)
        //        //{
        //        var attempt = TryLaunchProjectile(verbProps.projectileDef, TargetsAoE[i]);
        //        ////Log.Message(TargetsAoE[i].ToString());
        //        if (attempt.HasValue)
        //            result = attempt.GetValueOrDefault();
        //        //}
        //    }

        //    // here, might want to have this set each time so people don't force stop on last burst and not hit the cooldown?
        //    //burstShotsLeft = 0;
        //    //if (burstShotsLeft == 0)
        //    //{
        //    //}
        //    PostCastShot(result, out result);
        //    return result;
        //}
    }
}
