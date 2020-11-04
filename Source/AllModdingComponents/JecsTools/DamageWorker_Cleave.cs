using RimWorld;
using Verse;

namespace JecsTools
{
    public class DamageWorker_Cleave : DamageWorker_AddInjury
    {
        public DamageDefCleave Def => def as DamageDefCleave;


        /// CALCULATION NOTES:
        /// Cleave calculation is determined by a few factors.
        /// 0) Only persue these calculations if 0 is set as target flag.
        /// 1) Pawns with weapons will use the mass to decide
        /// how many adjacent targets are hit with the cleave attack.
        /// 2) Pawns without weapons will use their body size.
        /// 3) Otherwise, only 1 additional attack.
        public virtual int NumToCleave(Thing t)
        {
            if (Def.cleaveTargets == 0)
            {
                if (t is Pawn p)
                {
                    if (p.equipment?.Primary is ThingWithComps w)
                        return (int)w.GetStatValue(StatDefOf.Mass);
                    return (int)p.BodySize;
                }
                return 1;
            }
            return Def.cleaveTargets;
        }

        private const int maxDist = 4;

        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            if (!dinfo.InstantPermanentInjury)
                if (dinfo.Instigator != null)
                {
                    int cleaveAttacks = NumToCleave(dinfo.Instigator);
                    if (victim?.PositionHeld != default(IntVec3))
                        for (var i = 0; i < 8; i++)
                        {
                            var c = victim.PositionHeld + GenAdj.AdjacentCells[i];
                            if (cleaveAttacks > 0 && (dinfo.Instigator.Position - c).LengthHorizontalSquared < maxDist)
                            {
                                var things = c.GetThingList(victim.Map);
                                for (var k = 0; cleaveAttacks > 0 && k < things.Count; k++)
                                {
                                    if (things[k] is Pawn pawn && pawn != dinfo.Instigator &&
                                        pawn.Faction != dinfo.Instigator.Faction)
                                    {
                                        --cleaveAttacks;
                                        pawn.TakeDamage(new DamageInfo(Def.cleaveDamage,
                                            (int)(dinfo.Amount * Def.cleaveFactor), Def.armorPenetration, -1,
                                            dinfo.Instigator));
                                    }
                                }
                            }
                        }
                }
            return base.Apply(dinfo, victim);
        }
    }
}
