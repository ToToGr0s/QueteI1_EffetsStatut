using System.Collections.Generic;
using UnityEngine;

namespace QueteI1.StatusEffects
{
    /// <summary>Instance d'un effet posé sur une entité : source, stacks, durée restante, état des composants.</summary>
    public sealed class ActiveEffect
    {
        readonly Dictionary<EffectComponent, object> componentState = new Dictionary<EffectComponent, object>();

        internal ActiveEffect(EffectDefinition definition, EffectSource source, float appliedAt)
        {
            Definition = definition;
            Source = source;
            AppliedAt = appliedAt;
            Stacks = 1;
            Remaining = definition.Duration;
        }

        public EffectDefinition Definition { get; }

        /// <summary>Qui a posé l'effet (la première application, si plusieurs sources partagent l'instance).</summary>
        public EffectSource Source { get; }

        public float AppliedAt { get; }
        public int Stacks { get; internal set; }
        public float Remaining { get; internal set; }
        public float Elapsed { get; internal set; }

        public bool IsPermanent => Definition.DurationType == DurationType.Permanent;
        public float RemainingNormalized => IsPermanent ? 1f : Mathf.Clamp01(Remaining / Definition.Duration);
        public string RemainingLabel => IsPermanent ? "permanent" : TextFormat.Seconds(Remaining);

        /// <summary>État privé d'un composant pour cette instance (ex. minuteur d'un effet périodique).</summary>
        public T GetState<T>(EffectComponent component) where T : class, new()
        {
            if (componentState.TryGetValue(component, out var state))
                return (T)state;

            var created = new T();
            componentState[component] = created;
            return created;
        }
    }
}
