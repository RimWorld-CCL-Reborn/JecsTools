using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace JecsTools
{
    public class Hediff_TransformedPart : Hediff_AddedPart
    {
        private readonly List<Hediff_MissingPart> temporarilyRemovedParts = new List<Hediff_MissingPart>();

        public override bool ShouldRemove
        {
            get
            {
                if (this.TryGetComp<HediffComp_Disappears>() is HediffComp_Disappears hdc_Disappears)
                    return hdc_Disappears.CompShouldRemove;
                return false;
            }
        }


        public override string TipStringExtra
        {
            get
            {
                var stringBuilder = new StringBuilder();
                if (base.TipStringExtra is string baseString && baseString != "")
                    stringBuilder.Append(baseString);
                if (def.comps.FirstOrDefault(x => x is HediffCompProperties_VerbGiver) is HediffCompProperties_VerbGiver
                        props &&
                    props?.tools?.Count() > 0)
                    for (var i = 0; i < props?.tools?.Count(); i++)
                        stringBuilder.AppendLine("Damage".Translate() + ": " + props.tools[i].power);
                return stringBuilder.ToString();
            }
        }

        /// Nothing should happen.
        public override void PostAdd(DamageInfo? dinfo)
        {
            if (Part == null)
            {
                Log.Error("Part is null. It should be set before PostAdd for " + def + ".");
                return;
            }
            pawn.health.RestorePart(Part, this, false);
            temporarilyRemovedParts.Clear();
            for (var i = 0; i < Part.parts.Count; i++)
            {
                var hediff_MissingPart =
                    (Hediff_MissingPart) HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, null);
                hediff_MissingPart.IsFresh = false;
                hediff_MissingPart.lastInjury = null;
                hediff_MissingPart.Part = Part.parts[i];
                pawn.health.hediffSet.AddDirect(hediff_MissingPart, null);
                temporarilyRemovedParts.Add(hediff_MissingPart);
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            pawn.health.RestorePart(Part, this, false);
            //for (int i = 0; i < base.Part.parts.Count; i++)
            //{

            //}
        }
    }
}