using Verse;

namespace PawnShields
{
    /// <summary>
    /// Utility functions and extensions for when dealing with shields.
    /// </summary>
    public static class ShieldUtility
    {
        // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast (~6x as fast for me).
        public static CompShield GetCompShield(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompShield comp)
                    return comp;
            }
            return null;
        }

        // Slightly faster than `thing.GetCompShield() != null`
        public static bool HasCompShield(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompShield)
                    return true;
            }
            return false;
        }

        // Avoiding ThingDef.GetCompProperties<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        public static CompProperties_Shield GetCompShieldProperties(this ThingDef def)
        {
            var allProps = def.comps;
            for (int i = 0, count = allProps.Count; i < count; i++)
            {
                if (allProps[i] is CompProperties_Shield props)
                    return props;
            }
            return null;
        }

        // Avoiding Def.GetModExtension<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        public static ShieldPawnGeneratorProperties GetShieldPawnGeneratorProperties(this Def def)
        {
            var modExtensions = def.modExtensions;
            if (modExtensions == null)
                return null;
            for (int i = 0, count = modExtensions.Count; i < count; i++)
            {
                if (modExtensions[i] is ShieldPawnGeneratorProperties modExtension)
                    return modExtension;
            }
            return null;
        }

        /// <summary>
        /// Attempts to get the first shield from the pawn.
        /// </summary>
        /// <param name="pawn">Pawn to get shield from.</param>
        /// <returns>Shield if pawn has any or null if pawn can't have equipment or there is no shield.</returns>
        public static ThingWithComps GetShield(this Pawn pawn)
        {
            if (pawn.equipment == null)
                return null;
            return pawn.equipment.GetShield();
        }

        /// <summary>
        /// Attempts to get the first shield from the equipment tracker.
        /// </summary>
        /// <param name="eqTracker">Equipment tracker to get shield from.</param>
        /// <returns>Shield if tracker has any or null if there is no shield.</returns>
        public static ThingWithComps GetShield(this Pawn_EquipmentTracker eqTracker)
        {
            // Note: Not using LINQ or List foreach since they're slower than index-based for loop (~5x or ~2x, respectively, for me).
            var allEquipment = eqTracker.AllEquipmentListForReading;
            for (int i = 0, count = allEquipment.Count; i < count; i++)
            {
                var equipment = allEquipment[i];
                if (equipment.HasCompShield())
                    return equipment;
            }
            return null;
        }
    }
}
