using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QueteI1.StatusEffects
{
    /// <summary>
    /// Détail du calcul d'une stat : permet d'expliquer pourquoi elle vaut sa valeur actuelle.
    /// </summary>
    public sealed class StatBreakdown
    {
        readonly List<StatModifier> additives = new List<StatModifier>();
        readonly List<StatModifier> multipliers = new List<StatModifier>();

        public StatType Stat { get; private set; }
        public float BaseValue { get; private set; }
        public float AdditiveTotal { get; private set; }
        public float Multiplier { get; private set; } = 1f;
        public float Unclamped { get; private set; }
        public float Final { get; private set; }

        public IReadOnlyList<StatModifier> Additives => additives;
        public IReadOnlyList<StatModifier> Multipliers => multipliers;
        public bool HasModifiers => additives.Count > 0 || multipliers.Count > 0;
        public bool IsClamped => !Mathf.Approximately(Unclamped, Final);

        internal void Begin(StatType stat, float baseValue)
        {
            Stat = stat;
            BaseValue = baseValue;
            AdditiveTotal = 0f;
            Multiplier = 1f;
            additives.Clear();
            multipliers.Clear();
        }

        internal void AddAdditive(StatModifier modifier)
        {
            additives.Add(modifier);
            AdditiveTotal += modifier.Value;
        }

        internal void AddMultiplier(StatModifier modifier)
        {
            multipliers.Add(modifier);
            Multiplier *= modifier.Value;
        }

        internal void End(float unclamped, float final)
        {
            Unclamped = unclamped;
            Final = final;
        }

        /// <summary>Ex. : « (4 + 2 Hâte) × 0.5 Ralentissement ×2 = 3 ».</summary>
        public string ToFormula()
        {
            if (!HasModifiers)
                return "aucun modificateur";

            var text = new StringBuilder();
            bool wrap = additives.Count > 0 && multipliers.Count > 0;
            if (wrap) text.Append('(');
            text.Append(TextFormat.Num(BaseValue));
            foreach (var modifier in additives)
            {
                text.Append(modifier.Value >= 0f ? " + " : " − ")
                    .Append(TextFormat.Num(Mathf.Abs(modifier.Value)))
                    .Append(' ').Append(modifier.SourceName);
            }
            if (wrap) text.Append(')');

            foreach (var modifier in multipliers)
                text.Append(" × ").Append(TextFormat.Num(modifier.Value)).Append(' ').Append(modifier.SourceName);

            text.Append(" = ").Append(TextFormat.Num(Unclamped));
            if (IsClamped)
                text.Append(" → borné à ").Append(TextFormat.Num(Final));
            return text.ToString();
        }
    }
}
