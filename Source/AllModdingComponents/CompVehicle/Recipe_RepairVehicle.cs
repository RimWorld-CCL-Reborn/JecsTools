using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using Verse;

namespace CompVehicle
{
    internal class Recipe_RepairVehicle : RecipeWorker
    {
        //public override string GetLabelWhenUsedOn(Pawn pawn, BodyPartRecord part)
        //{

        //    if (pawn?.GetComp<CompVehicle>() is CompVehicle compVehicle)
        //    {
        //        string result = base.GetLabelWhenUsedOn(pawn, part);
        //    }
        //    else return base.GetLabelWhenUsedOn(pawn, part);
        //}

        [DebuggerHidden]
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            var records = new List<BodyPartRecord>();

            var brokenParts = pawn.health.hediffSet.hediffs.FindAll(x => x is Hediff_Injury);
            if (brokenParts != null && brokenParts.Count > 0)
                foreach (var brokenPart in brokenParts)
                    if (brokenPart.Part != null)
                        if (!records.Contains(brokenPart.Part)) records.Add(brokenPart.Part);

            return records.AsEnumerable();
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients,
            Bill bill)
        {
            if (pawn != null)
                foreach (var rec in pawn.health.hediffSet.GetInjuredParts())
                foreach (var current in from injury in pawn.health.hediffSet.GetHediffs<Hediff_Injury>()
                    where injury.Part == rec
                    select injury)
                    if (rec == part) current.Heal((int) current.Severity + 1);
            //pawn.health.AddHediff(this.recipe.addsHediff, part, null);
            //ThoughtUtility.GiveThoughtsForPawnExecuted(pawn, PawnExecutionKind.GenericHumane);
        }
    }
}