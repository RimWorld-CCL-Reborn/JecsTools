// This Verb's main purpose is to treat bursts as over time and not at once, as in the parent class. This
// changes was in response to CompAbilityUser adding VerbTicks to its verbs.

namespace AbilityUser
{
    public class Verb_UseAbility_TrueBurst : Verb_UseAbility
    {
        //// Made it so burst is not burst per each target, but back to the regular burst-over-time.
        //protected override bool TryCastShot()
        //{
        //    this.ability.CooldownTicksLeft = (int)this.UseAbilityProps.SecondsToRecharge * GenTicks.TicksPerRealSecond;
        //    bool result = false;
        //    this.TargetsAoE.Clear();
        //    UpdateTargets();
        //    int burstShots = this.ShotsPerBurst;
        //    if (this.UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && this.TargetsAoE.Count > 1)
        //    {
        //        this.TargetsAoE.RemoveRange(0, this.TargetsAoE.Count - 1);
        //    }
        //    for (int i = 0; i < this.TargetsAoE.Count; i++)
        //    {
        //        //                for (int j = 0; j < burstshots; j++)
        //        //                {
        //        bool? attempt = TryLaunchProjectile(this.verbProps.projectileDef, this.TargetsAoE[i]);
        //        ////Log.Message(TargetsAoE[i].ToString());
        //        if (attempt != null)
        //        {
        //            if (attempt == true) result = true;
        //            if (attempt == false) result = false;
        //        }
        //        //                }
        //    }

        //    // here, might want to have this set each time so people don't force stop on last burst and not hit the cooldown?
        //    //this.burstShotsLeft = 0;
        //    //if (this.burstShotsLeft == 0)
        //    //{
        //    //}
        //    PostCastShot(result, out result);
        //    return result;
        //}
    }
}