using System.Collections.Generic;
using UnityEngine;

namespace QueteI1.StatusEffects
{
    /// <summary>
    /// Calcul d'une stat finale, toujours dans le même ordre :
    /// 1) base + somme des additifs
    /// 2) × produit des multiplicateurs
    /// 3) borne minimale de la stat (ex. vitesse ≥ 0, PV max ≥ 1).
    /// </summary>
    public static class StatCalculator
    {
        public static StatBreakdown Compute(StatType stat, float baseValue, IReadOnlyList<StatModifier> modifiers,
            StatBreakdown result = null)
        {
            result ??= new StatBreakdown();
            result.Begin(stat, baseValue);

            // Deux passes explicites : l'ordre de calcul se lit directement dans le code.
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].Stat == stat && modifiers[i].Operation == ModifierOperation.Additive)
                    result.AddAdditive(modifiers[i]);
            }
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].Stat == stat && modifiers[i].Operation == ModifierOperation.Multiplicative)
                    result.AddMultiplier(modifiers[i]);
            }

            float unclamped = (baseValue + result.AdditiveTotal) * result.Multiplier;
            result.End(unclamped, Mathf.Max(StatTypeInfo.Min(stat), unclamped));
            return result;
        }
    }
}
