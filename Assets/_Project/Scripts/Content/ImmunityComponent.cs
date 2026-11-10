using System;
using System.Collections.Generic;

namespace QueteI1.StatusEffects.Content
{
    /// <summary>Tant que l'effet est actif, refuse tout nouvel effet d'une polarité donnée (immunité aux debuffs...).</summary>
    [Serializable]
    [EffectComponentInfo("Contrôle : immunité", "Refuse les nouveaux effets d'une polarité tant que l'effet est actif.")]
    public sealed class ImmunityComponent : EffectComponent, IApplicationFilter
    {
        public EffectPolarity polarity = EffectPolarity.Debuff;

        public ImmunityComponent() { }

        public ImmunityComponent(EffectPolarity polarity)
        {
            this.polarity = polarity;
        }

        // L'effet ne se bloque pas lui-même : il doit pouvoir être rafraîchi.
        public bool Blocks(EffectContext context, EffectDefinition incoming) =>
            incoming != context.Definition && incoming.Polarity == polarity;

        public override string Describe() =>
            polarity == EffectPolarity.Debuff ? "Immunise contre les debuffs" : $"Immunise contre les effets « {polarity} »";

        public override void Validate(EffectDefinition owner, List<string> warnings)
        {
            if (owner.IsInstant)
                warnings.Add("Immunité sur un effet instantané : elle ne bloquera jamais rien.");
        }
    }
}
