namespace Drova_Modding_API.GlobalFields
{
    #pragma warning disable CS1591

    /// <summary>
    /// Readable IDs for <see cref="Il2CppDrova.GenericStatDesc"/> assets used by actor attribute maps.
    /// Use these constants with <c>CustomStatsModule.WithBaseStat(string readableId, ...)</c>.
    /// </summary>
    public static class StatReadableIds
    {
        public static class Attributes
        {
            public const string Dexterity = "AttributeStat_Dex";
            public const string Mind = "AttributeStat_Mind";
            public const string Strength = "AttributeStat_Strength";
        }

        public static class Armor
        {
            public const string Physical = "Stat_Resistance_Physical";
            public const string Magical = "Stat_Resistance_Magical";
        }

        public static class Resistances
        {
            public const string PhysicalResistance = Armor.Physical;
            public const string MagicalResistance = Armor.Magical;
        }

        public static class Stamina
        {
            public const string Regeneration = "Stat_Stamina_Reg";
            public const string Additional = "Stat_Stamina_Additional";
            public const string CostFactorGeneral = "Stat_Stamina_CostFactor_General";
            public const string CostFactorAttackCharge = "Stat_Stamina_CostFactor_AtkCharge";
        }

        public static class Flow
        {
            public const string PassiveFlowRegMultiplier = "Stat_Flow_PassiveFlowRegMultiplier";
            public const string AdditionalOrientationPoint = "Stat_Flow_OrientationPoint_Additional";
            public const string ReducedFlowCostsAdd = "Stat_Flow_ReducedFlowCosts_Add";
        }

        public static class Combat
        {
            public const string AmmoCostChance = "Stat_Ammo_CostChance";
            public const string CriticalChance = "Stat_CritChance";
            public const string PhysicalDamageFactor = "Stat_DamageFactor_Physical";
        }

        public static class Movement
        {
            public const string MovementSpeed = "Stat_MovementSpeed";
            public const string LookSpeed = "Stat_LookSpeed";
        }
    }

    #pragma warning restore CS1591
}
