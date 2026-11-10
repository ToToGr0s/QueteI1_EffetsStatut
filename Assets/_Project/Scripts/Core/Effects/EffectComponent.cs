using System;
using System.Collections.Generic;

namespace QueteI1.StatusEffects
{
    /// <summary>
    /// Brique de comportement d'un effet (dégâts, modificateur de stat, blocage...).
    /// Un effet = une liste de composants configurés dans un asset <see cref="EffectDefinition"/>.
    /// Pour un nouveau type de comportement, on hérite de cette classe : le cœur n'a pas à changer.
    /// Les composants sont des données partagées : l'état propre à une instance passe par
    /// <see cref="ActiveEffect.GetState{T}"/>.
    /// </summary>
    [Serializable]
    public abstract class EffectComponent
    {
        /// <summary>Première application (ou application d'un effet instantané).</summary>
        public virtual void OnApply(EffectContext context) { }

        /// <summary>Un stack a été ajouté.</summary>
        public virtual void OnStacksChanged(EffectContext context, int previousStacks) { }

        /// <summary>La durée a été remise à zéro sans ajout de stack.</summary>
        public virtual void OnRefresh(EffectContext context) { }

        /// <summary>Passage du temps (secondes de simulation, déjà bornées à la durée restante).</summary>
        public virtual void OnTick(EffectContext context, float deltaTime) { }

        public virtual void OnRemove(EffectContext context, RemovalReason reason) { }

        /// <summary>Ajoute les modificateurs de stats de ce composant, stacks inclus.</summary>
        public virtual void CollectModifiers(EffectContext context, List<StatModifier> output) { }

        /// <summary>Phrase courte affichée dans l'inspecteur et l'outil de test.</summary>
        public abstract string Describe();

        /// <summary>Signale une configuration incohérente dans l'inspecteur.</summary>
        public virtual void Validate(EffectDefinition owner, List<string> warnings) { }
    }

    /// <summary>Nom et description affichés dans le menu « Ajouter un composant » de l'inspecteur.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class EffectComponentInfoAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public EffectComponentInfoAttribute(string name, string description = null)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Ce qu'un composant reçoit quand le système l'appelle, et la seule porte qu'il a sur l'entité :
    /// lire une stat, modifier les PV, dissiper des effets, écrire une note. Il ne voit pas le reste.
    /// </summary>
    public readonly struct EffectContext
    {
        internal readonly StatusEntity Target;
        public readonly ActiveEffect Effect;

        internal EffectContext(StatusEntity target, ActiveEffect effect)
        {
            Target = target;
            Effect = effect;
        }

        public EffectDefinition Definition => Effect.Definition;
        public int Stacks => Effect.Stacks;
        public EffectSource Source => Effect.Source;
        public bool TargetIsDead => Target.IsDead;

        public float GetStat(StatType stat) => Target.Stats.Get(stat);

        /// <summary>Soigne ou blesse la cible ; la source de l'effet est créditée.</summary>
        public void ChangeHealth(HealthChangeKind kind, float amount, string label, bool ignoreDefense = false) =>
            Target.Health.Apply(kind, amount, label, ignoreDefense, Effect.Source);

        /// <summary>Retire les autres effets de la cible qui correspondent ; renvoie leur nombre.</summary>
        public int Dispel(Predicate<ActiveEffect> match)
        {
            var self = Effect;
            return Target.Effects.RemoveWhere(effect => effect != self && match(effect), RemovalReason.Dispelled);
        }

        /// <summary>Ligne d'information dans le journal de la cible.</summary>
        public void Note(string message) => Target.Log.Add(Target.Name, EffectEventType.Info, message);
    }
}
