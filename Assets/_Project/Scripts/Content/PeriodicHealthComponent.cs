using System;
using System.Collections.Generic;
using UnityEngine;

namespace QueteI1.StatusEffects.Content
{
    /// <summary>Soin ou dégâts à intervalles réguliers (poison, régénération).</summary>
    [Serializable]
    [EffectComponentInfo("Santé : périodique", "Soigne ou blesse à intervalles réguliers pendant la durée de l'effet.")]
    public sealed class PeriodicHealthComponent : EffectComponent
    {
        const float MinInterval = 0.05f;
        const float Tolerance = 1e-4f;

        public HealthChangeKind kind = HealthChangeKind.Damage;
        [Min(0f)] public float amountPerTick = 4f;
        [Min(MinInterval)] public float interval = 1f;
        [Tooltip("Multiplie le montant de chaque tick par le nombre de stacks.")]
        public bool scaleWithStacks = true;
        [Tooltip("Les dégâts ignorent la défense de la cible.")]
        public bool ignoreDefense = true;

        sealed class TickState
        {
            public float timer;
            public int count;
        }

        public PeriodicHealthComponent() { }

        public PeriodicHealthComponent(HealthChangeKind kind, float amountPerTick, float interval,
            bool scaleWithStacks = true, bool ignoreDefense = true)
        {
            this.kind = kind;
            this.amountPerTick = amountPerTick;
            this.interval = interval;
            this.scaleWithStacks = scaleWithStacks;
            this.ignoreDefense = ignoreDefense;
        }

        // Le premier tick a lieu après un intervalle complet. Le minuteur n'est pas remis à zéro
        // quand l'effet est rafraîchi ou cumulé : le rythme des ticks reste régulier.
        public override void OnTick(EffectContext context, float deltaTime)
        {
            var state = context.Effect.GetState<TickState>(this);
            float period = Mathf.Max(MinInterval, interval);
            state.timer += deltaTime;

            while (state.timer >= period - Tolerance && !context.TargetIsDead)
            {
                state.timer -= period;
                state.count++;
                int multiplier = scaleWithStacks ? context.Stacks : 1;
                string label = multiplier > 1
                    ? $"{context.Definition.DisplayName} ×{multiplier}, tick {state.count}"
                    : $"{context.Definition.DisplayName}, tick {state.count}";
                context.ChangeHealth(kind, amountPerTick * multiplier, label, ignoreDefense);
            }
        }

        public override string Describe()
        {
            string what = kind == HealthChangeKind.Heal
                ? $"Rend {TextFormat.Num(amountPerTick)} PV"
                : $"Inflige {TextFormat.Num(amountPerTick)} dégâts";
            return $"{what} toutes les {TextFormat.Num(interval)} s" + (scaleWithStacks ? " (× stacks)" : "");
        }

        public override void Validate(EffectDefinition owner, List<string> warnings)
        {
            if (owner.IsInstant)
                warnings.Add("Santé périodique sur un effet instantané : aucun tick ne se produira.");
            else if (owner.DurationType == DurationType.Timed && interval > owner.Duration)
                warnings.Add("Santé périodique : l'intervalle dépasse la durée, aucun tick ne se produira.");
        }
    }
}
