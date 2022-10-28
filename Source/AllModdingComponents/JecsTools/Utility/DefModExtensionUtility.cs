using Verse;

namespace JecsTools
{
    public static class DefModExtensionUtility
    {
        // Avoiding Def.GetModExtension<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.

        public static ProjectileExtension GetProjectileExtension(this Def def)
        {
            var modExtensions = def.modExtensions;
            if (modExtensions == null)
                return null;
            for (int i = 0, count = modExtensions.Count; i < count; i++)
            {
                if (modExtensions[i] is ProjectileExtension modExtension)
                    return modExtension;
            }
            return null;
        }

        public static BuildingExtension GetBuildingExtension(this Def def)
        {
            var modExtensions = def.modExtensions;
            if (modExtensions == null)
                return null;
            for (int i = 0, count = modExtensions.Count; i < count; i++)
            {
                if (modExtensions[i] is BuildingExtension modExtension)
                    return modExtension;
            }
            return null;
        }

        public static ApparelExtension GetApparelExtension(this Def def)
        {
            var modExtensions = def.modExtensions;
            if (modExtensions == null)
                return null;
            for (int i = 0, count = modExtensions.Count; i < count; i++)
            {
                if (modExtensions[i] is ApparelExtension modExtension)
                    return modExtension;
            }
            return null;
        }
        
        
        public static PawnKindGeneExtension GetPawnKindGeneExtension(this Def def)
        {
            var modExtensions = def.modExtensions;
            if (modExtensions == null)
                return null;
            for (int i = 0, count = modExtensions.Count; i < count; i++)
            {
                if (modExtensions[i] is PawnKindGeneExtension modExtension)
                    return modExtension;
            }
            return null;
        }
    }
}
