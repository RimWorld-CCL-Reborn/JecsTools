using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class DamageWorker_Cleave : DamageWorker_AddInjury
    {

        public DamageDefCleave Def
        {
            get
            {
                return this.def as DamageDefCleave;
            }
        }


        /// CALCULATION NOTES:
        /// Cleave calculation is determined by a few factors.
        /// 
        /// 0) Only persue these calculations if 0 is set as target flag.
        /// 1) Pawns with weapons will use the mass to decide
        ///   how many adjacent targets are hit with the cleave attack.
        /// 2) Pawns without weapons will use their body size.
        /// 3) Otherwise, only 1 additional attack.
        public virtual int NumToCleave(Thing t)
        {
            if (Def.cleaveTargets == 0)
            {
                if (t is Pawn p)
                {
                    if (p?.equipment?.Primary is ThingWithComps w)
                    {
                        return (int)w.GetStatValue(StatDefOf.Mass);
                    }
                    return (int)p.BodySize;
                }
                return 1;
            }
            return Def.cleaveTargets;
        }

        public override float Apply(DamageInfo dinfo, Thing victim)
        {
            float maxDist;
            int cleaveAttacks;

            if (!dinfo.InstantOldInjury)
            {
                if (dinfo.Instigator != null)
                {
                    maxDist = 4;
                    cleaveAttacks = NumToCleave(dinfo.Instigator);
                    if (victim?.PositionHeld != default(IntVec3))
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            IntVec3 c = victim.PositionHeld + GenAdj.AdjacentCells[i];
                            if (cleaveAttacks > 0 && ((float)(dinfo.Instigator.Position - c).LengthHorizontalSquared < maxDist))
                            {
                                List<Thing> pawnsInCell = c.GetThingList(victim.Map).FindAll(x => x is Pawn && x != dinfo.Instigator && x?.Faction != dinfo.Instigator?.Faction);
                                for (int k = 0; cleaveAttacks > 0 && k < pawnsInCell.Count; k++)
                                {
                                    --cleaveAttacks;
                                    Pawn p = (Pawn)pawnsInCell[k];
                                    p.TakeDamage(new DamageInfo(Def.cleaveDamage, (int)(dinfo.Amount * Def.cleaveFactor), -1, dinfo.Instigator));
                                }
                            }
                        }
                    }
                }
            }
            return base.Apply(dinfo, victim);
        }
    }
}
