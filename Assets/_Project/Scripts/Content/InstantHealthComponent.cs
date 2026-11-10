using System;
using System.Collections.Generic;
using UnityEngine;

namespace QueteI1.StatusEffects.Content
{
    /// <summary>Soin ou dégâts immédiats, déclenchés à chaque application acceptée de l'effet.</summary>
    [Serializable]
    [EffectComponentInfo("Santé : instantané", "Soigne ou blesse une fois, à chaque application acceptée de l'effet.")]
    public sealed class InstantHealthComponent : EffectComponent
    {
        public HealthChangeKind kind = HealthChangeKind.Damage;
        [Min(0f)] public float amount = 20f;
        [Tooltip("Les dégâts ignorent la défense de la cible.")]
        public bool ignoreDefense;

        public InstantHealthComponent() { }

        public InstantHealthComponent(HealthChangeKind kind, float amount, bool ignoreDefense = false)
        {
            this.kind = kind;
            this.amount = amount;
            this.ignoreDefense = ignoreDefense;
        }

        public override void OnApply(EffectContext context) => Trigger(context);

        public override void OnStacksChanged(EffectContext context, int previousStacks) => Trigger(context);

        public override void OnRefresh(EffectContext context) => Trigger(context);

        void Trigger(EffectContext context) =>
            context.ChangeHealth(kind, amount, context.Definition.DisplayName, ignoreDefense);

        public override string Describe() => kind == HealthChangeKind.Heal
            ? $"Rend {TextFormat.Num(amount)} PV"
            : $"Inflige {TextFormat.Num(amount)} dégâts" + (ignoreDefense ? " (ignore la défense)" : "");

        public override void Validate(EffectDefinition owner, List<string> warnings)
        {
            if (amount <= 0f)
                warnings.Add("Santé instantanée : montant nul, le composant ne fait rien.");
        }
    }
}
