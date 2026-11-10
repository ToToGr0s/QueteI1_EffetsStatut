using System;
using System.Collections.Generic;
using UnityEngine;

namespace QueteI1.StatusEffects
{
    /// <summary>
    /// Stats d'une entité : base + modificateurs fournis par les effets actifs.
    /// Les valeurs finales sont mises en cache et recalculées dès que les effets changent.
    /// </summary>
    public sealed class StatSheet
    {
        static readonly IReadOnlyList<StatModifier> NoModifiers = Array.Empty<StatModifier>();

        readonly BaseStats baseStats;
        readonly Dictionary<StatType, StatBreakdown> breakdowns = new Dictionary<StatType, StatBreakdown>();
        Func<IReadOnlyList<StatModifier>> modifierSource = () => NoModifiers;

        /// <summary>Stat, ancienne valeur finale, nouvelle valeur finale.</summary>
        public event Action<StatType, float, float> StatChanged;

        /// <summary>Levé après chaque recalcul complet (ex. pour borner les PV au nouveau max).</summary>
        public event Action Recomputed;

        public StatSheet(BaseStats baseStats)
        {
            this.baseStats = baseStats ?? throw new ArgumentNullException(nameof(baseStats));
            foreach (var stat in StatTypeInfo.All)
                breakdowns[stat] = new StatBreakdown();
            Recompute(false);
        }

        public float GetBase(StatType stat) => baseStats.Get(stat);

        public float Get(StatType stat) => breakdowns[stat].Final;

        public StatBreakdown GetBreakdown(StatType stat) => breakdowns[stat];

        internal void SetModifierSource(Func<IReadOnlyList<StatModifier>> source)
        {
            modifierSource = source ?? (() => NoModifiers);
            Recompute(false);
        }

        public void Recompute() => Recompute(true);

        void Recompute(bool notify)
        {
            var modifiers = modifierSource();
            foreach (var stat in StatTypeInfo.All)
            {
                var breakdown = breakdowns[stat];
                float previous = breakdown.Final;
                StatCalculator.Compute(stat, baseStats.Get(stat), modifiers, breakdown);
                if (notify && !Mathf.Approximately(previous, breakdown.Final))
                    StatChanged?.Invoke(stat, previous, breakdown.Final);
            }
            if (notify)
                Recomputed?.Invoke();
        }
    }
}
