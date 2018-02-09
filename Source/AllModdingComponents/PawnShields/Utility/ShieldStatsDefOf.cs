using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PawnShields
{
    /// <summary>
    /// Convenience class for getting Defs.
    /// </summary>
    [DefOf]
    public static class ShieldStatsDefOf
    {
        /// <summary>
        /// Shield stat category.
        /// </summary>
        public static StatCategoryDef Shield;

        /// <summary>
        /// Base melee block chance for shields.
        /// </summary>
        public static StatDef Shield_BaseMeleeBlockChance;

        /// <summary>
        /// Base ranged block chance for shields.
        /// </summary>
        public static StatDef Shield_BaseRangedBlockChance;

        /// <summary>
        /// Damage absorbtion factor for shields.
        /// </summary>
        public static StatDef Shield_DamageAbsorbed;

        /// <summary>
        /// Melee shield block chance base from pawn.
        /// </summary>
        public static StatDef MeleeShieldBlockChance;

        /// <summary>
        /// Ranged shield block chance base from pawn.
        /// </summary>
        public static StatDef RangedShieldBlockChance;
    }
}
