namespace CompSlotLoadable
{
    // TODO: These should be translation keys
    public static class StringOf
    {
        public static string all = "all";
        public static string ExceptionSlotAlreadyFilled = "{0}'s slot is already filled";
        public static string Unavailable = "{0} unavailable";
        public static string IsDrafted = "{0} is drafted.";
        public static string Unload = "Unload {0}";
        public static string CurrentlyLoaded = "Loaded {0}";
        public static string Effects = "Effects:";
        public static string ChangesPrimaryColor = "Changes Primary Color";
        public static string StatModifiers = "Stat Modifiers";
        public static string OverrideDamageType = "Override damage type";
        public static string OverrideDamageTypeExplanation = "Overrides attack's damage type with {0}";
        public static string OverrideArmorPenetration = "Override armor penetration";
        public static string NoLoadOptions = "No load options available.";
        public static string DefensiveHeal = "Defensive heal";
        public static string DefensiveHealShort = "{0} for {1} wounds, max {2} amount";
        public static string DefensiveHealExplanation = "When attacked, {0} chance to heal {1} wounds up to {2} total amount";
        public static string VampiricHeal = "Vampiric heal";
        public static string VampiricHealShort = "{0} for {1} wounds, max {2} amount\n({3} damage, {4} armor penetration)";
        public static string VampiricHealExplanation = "When attacking, {0} chance to vampiricly heal {1} wounds up to {2} total amount, " +
            "dealing additional {2} {3} damage with {4} armor penetration";
    }
}
