using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace JecsTools
{
    public class Hediff_TransformedPart : Hediff_AddedPart 
    {
        public override bool ShouldRemove
        {
            get
            {
                return false;
            }
        }

        public override string TipStringExtra
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(base.TipStringExtra);
                if (this.def.comps.FirstOrDefault(x => x is HediffCompProperties_VerbGiver) is HediffCompProperties_VerbGiver props)
                {
                    for (int i = 0; i < props.verbs.Count(); i++)
                    {
                        stringBuilder.AppendLine("Damage".Translate() + ": " + props.verbs[i].meleeDamageBaseAmount);
                    }
                }
                return stringBuilder.ToString();
            }
        }

        private List<Hediff_MissingPart> temporarilyRemovedParts = new List<Hediff_MissingPart>();

        /// Nothing should happen.
        public override void PostAdd(DamageInfo? dinfo)
        {
            if (base.Part == null)
            {
                Log.Error("Part is null. It should be set before PostAdd for " + this.def + ".");
                return;
            }
            this.pawn.health.RestorePart(base.Part, this, false);
            temporarilyRemovedParts.Clear();
            for (int i = 0; i < base.Part.parts.Count; i++)
            {
                Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, this.pawn, null);
                hediff_MissingPart.IsFresh = true;
                hediff_MissingPart.lastInjury = HediffDefOf.SurgicalCut;
                hediff_MissingPart.Part = base.Part.parts[i];
                this.pawn.health.hediffSet.AddDirect(hediff_MissingPart, null);
                temporarilyRemovedParts.Add(hediff_MissingPart);
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            this.pawn.health.RestorePart(base.Part, this, false);
            //for (int i = 0; i < base.Part.parts.Count; i++)
            //{
                
            //}
        }
    }
}
