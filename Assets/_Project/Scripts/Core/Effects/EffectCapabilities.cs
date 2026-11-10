using System.Collections.Generic;

namespace QueteI1.StatusEffects
{
    // Une capacité est une interface qu'un composant d'effet peut implémenter en plus de EffectComponent.
    // Le contrôleur n'en connaît aucune : il donne seulement accès aux composants actifs qui en ont une
    // (StatusEffectController.Capabilities<T>). C'est le code qui a besoin de la réponse qui la demande.
    // Pour une nouvelle capacité : déclarer l'interface, l'implémenter dans un composant, la lire là où elle sert.

    /// <summary>Empêche certaines actions de l'entité (lu par <see cref="StatusEntity.IsBlocked"/>).</summary>
    public interface IActionBlocker
    {
        ActionType BlockedActions { get; }
    }

    /// <summary>Modifie les dégâts que reçoit l'entité, avant la défense : bouclier, vulnérabilité.</summary>
    public interface IDamageModifier
    {
        float ModifyIncomingDamage(EffectContext context, float amount);
    }

    /// <summary>Refuse l'application de certains effets tant que celui-ci est actif : immunité.</summary>
    public interface IApplicationFilter
    {
        bool Blocks(EffectContext context, EffectDefinition incoming);
    }

    /// <summary>Un composant actif qui a la capacité demandée, avec le contexte de son effet.</summary>
    public readonly struct Capability<T> where T : class
    {
        public readonly T Component;
        public readonly EffectContext Context;

        internal Capability(T component, EffectContext context)
        {
            Component = component;
            Context = context;
        }
    }

    /// <summary>Parcours sans allocation des composants actifs qui implémentent <typeparamref name="T"/>.</summary>
    public readonly struct CapabilityEnumerable<T> where T : class
    {
        readonly StatusEntity owner;
        readonly List<ActiveEffect> effects;

        internal CapabilityEnumerable(StatusEntity owner, List<ActiveEffect> effects)
        {
            this.owner = owner;
            this.effects = effects;
        }

        public Enumerator GetEnumerator() => new Enumerator(owner, effects);

        public struct Enumerator
        {
            readonly StatusEntity owner;
            readonly List<ActiveEffect> effects;
            int effectIndex;
            int componentIndex;

            internal Enumerator(StatusEntity owner, List<ActiveEffect> effects)
            {
                this.owner = owner;
                this.effects = effects;
                effectIndex = 0;
                componentIndex = 0;
                Current = default;
            }

            public Capability<T> Current { get; private set; }

            public bool MoveNext()
            {
                while (effectIndex < effects.Count)
                {
                    var effect = effects[effectIndex];
                    var components = effect.Definition.Components;
                    while (componentIndex < components.Count)
                    {
                        if (components[componentIndex++] is T capability)
                        {
                            Current = new Capability<T>(capability, new EffectContext(owner, effect));
                            return true;
                        }
                    }

                    effectIndex++;
                    componentIndex = 0;
                }
                return false;
            }
        }
    }
}
