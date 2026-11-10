using System;

namespace QueteI1.StatusEffects
{
    /// <summary>Statistiques qu'une entité possède et que les effets peuvent modifier.</summary>
    public enum StatType
    {
        MaxHealth,
        Speed,
        Damage,
        Defense
    }

    public static class StatTypeInfo
    {
        public static readonly StatType[] All = (StatType[])Enum.GetValues(typeof(StatType));

        public static string Label(StatType stat) => stat switch
        {
            StatType.MaxHealth => "PV max",
            StatType.Speed => "Vitesse",
            StatType.Damage => "Dégâts",
            StatType.Defense => "Défense",
            _ => stat.ToString()
        };

        /// <summary>Borne basse appliquée en dernière étape du calcul.</summary>
        public static float Min(StatType stat) => stat == StatType.MaxHealth ? 1f : 0f;
    }
}
