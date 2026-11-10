using System;
using UnityEngine;

namespace QueteI1.StatusEffects
{
    /// <summary>Stats de base d'une entité, avant tout modificateur.</summary>
    [Serializable]
    public sealed class BaseStats
    {
        [Min(1f)] public float maxHealth = 100f;
        [Min(0f)] public float speed = 4f;
        [Min(0f)] public float damage = 10f;
        [Min(0f)] public float defense;

        public BaseStats() { }

        public BaseStats(float maxHealth, float speed, float damage, float defense)
        {
            this.maxHealth = maxHealth;
            this.speed = speed;
            this.damage = damage;
            this.defense = defense;
        }

        public float Get(StatType stat) => stat switch
        {
            StatType.MaxHealth => maxHealth,
            StatType.Speed => speed,
            StatType.Damage => damage,
            StatType.Defense => defense,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };
    }
}
