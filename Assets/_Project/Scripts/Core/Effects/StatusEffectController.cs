using System;
using System.Collections.Generic;

namespace QueteI1.StatusEffects
{
    /// <summary>
    /// Gère les effets actifs d'une entité : application (règles de cumul), passage du temps,
    /// expiration, retrait. Il ne connaît aucun effet concret ni aucune capacité : il appelle les
    /// composants et annonce ce qui se passe par des événements typés.
    /// </summary>
    public sealed class StatusEffectController
    {
        const float Epsilon = 1e-4f;

        readonly StatusEntity owner;
        readonly List<ActiveEffect> active = new List<ActiveEffect>();
        readonly List<ActiveEffect> iterationBuffer = new List<ActiveEffect>();
        readonly List<StatModifier> modifierBuffer = new List<StatModifier>();

        internal StatusEffectController(StatusEntity owner)
        {
            this.owner = owner;
        }

        public IReadOnlyList<ActiveEffect> Active => active;

        /// <summary>
        /// Une application vient d'être traitée, acceptée ou refusée. Levé une fois l'état à jour
        /// (stacks, durée) et avant que les composants de l'effet agissent.
        /// </summary>
        public event Action<ApplyResult> Applied;

        /// <summary>Un effet vient de quitter la liste (expiration, dissipation, mort, réinitialisation).</summary>
        public event Action<ActiveEffect, RemovalReason> Removed;

        /// <summary>La liste des effets actifs ou leurs stacks/durées ont changé.</summary>
        public event Action Changed;

        /// <summary>Première instance active de cet effet, quelle que soit sa source.</summary>
        public ActiveEffect Find(EffectDefinition definition)
        {
            foreach (var effect in active)
            {
                if (effect.Definition == definition)
                    return effect;
            }
            return null;
        }

        /// <summary>Instance active de cet effet posée par cette source.</summary>
        public ActiveEffect Find(EffectDefinition definition, EffectSource source)
        {
            foreach (var effect in active)
            {
                if (effect.Definition == definition && effect.Source.Equals(source))
                    return effect;
            }
            return null;
        }

        /// <summary>Composants des effets actifs qui implémentent la capacité <typeparamref name="T"/>.</summary>
        public CapabilityEnumerable<T> Capabilities<T>() where T : class => new CapabilityEnumerable<T>(owner, active);

        public ApplyResult Apply(EffectDefinition definition, EffectSource source)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (owner.IsDead)
                return Announce(new ApplyResult(ApplyOutcome.Rejected, definition, source, null, RejectReason.TargetDead));

            foreach (var filter in Capabilities<IApplicationFilter>())
            {
                if (filter.Component.Blocks(filter.Context, definition))
                    return Announce(new ApplyResult(ApplyOutcome.Rejected, definition, source, filter.Context.Effect, RejectReason.Immune));
            }

            if (definition.IsInstant)
            {
                var instant = new ActiveEffect(definition, source, owner.Log.Now);
                var result = Announce(new ApplyResult(ApplyOutcome.AppliedInstant, definition, source, null));
                var instantContext = new EffectContext(owner, instant);
                foreach (var component in definition.Components)
                    component?.OnApply(instantContext);
                return result;
            }

            var existing = definition.SeparatePerSource ? Find(definition, source) : Find(definition);
            if (existing == null)
            {
                var effect = new ActiveEffect(definition, source, owner.Log.Now);
                active.Add(effect);
                var result = Announce(new ApplyResult(ApplyOutcome.Applied, definition, source, effect));
                // Les stats sont à jour avant d'appeler les composants : un soin posé avec un bonus
                // de PV max voit déjà le nouveau maximum.
                owner.Stats.Recompute();
                var context = new EffectContext(owner, effect);
                foreach (var component in definition.Components)
                    component?.OnApply(context);
                Changed?.Invoke();
                return result;
            }

            if (definition.StackingRule == StackingRule.Reject)
                return Announce(new ApplyResult(ApplyOutcome.Rejected, definition, source, existing, RejectReason.AlreadyActive));

            float previousRemaining = existing.Remaining;
            existing.Remaining = definition.Duration;
            var existingContext = new EffectContext(owner, existing);

            if (definition.StackingRule == StackingRule.Stack && existing.Stacks < definition.MaxStacks)
            {
                int previousStacks = existing.Stacks;
                existing.Stacks++;
                var result = Announce(new ApplyResult(ApplyOutcome.StackAdded, definition, source, existing,
                    RejectReason.None, previousRemaining));
                owner.Stats.Recompute();
                foreach (var component in definition.Components)
                    component?.OnStacksChanged(existingContext, previousStacks);
                Changed?.Invoke();
                return result;
            }

            var outcome = definition.StackingRule == StackingRule.Stack ? ApplyOutcome.RefreshedAtMaxStacks : ApplyOutcome.Refreshed;
            var refreshed = Announce(new ApplyResult(outcome, definition, source, existing, RejectReason.None, previousRemaining));
            foreach (var component in definition.Components)
                component?.OnRefresh(existingContext);
            Changed?.Invoke();
            return refreshed;
        }

        /// <summary>
        /// Fait avancer le temps des effets actifs de <paramref name="deltaTime"/> et retire ceux qui expirent.
        /// Le découpage en pas fixes est fait par <see cref="SimulationClock"/>, commun à toutes les entités.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || active.Count == 0)
                return;

            // Copie : un composant peut retirer des effets pendant le pas (ex. la cible meurt).
            iterationBuffer.Clear();
            iterationBuffer.AddRange(active);
            bool expired = false;

            foreach (var effect in iterationBuffer)
            {
                if (!active.Contains(effect))
                    continue;

                // Un effet n'avance jamais au-delà de sa fin.
                float step = effect.IsPermanent ? deltaTime : Math.Min(deltaTime, effect.Remaining);
                effect.Elapsed += step;
                if (!effect.IsPermanent)
                    effect.Remaining -= step;

                var context = new EffectContext(owner, effect);
                foreach (var component in effect.Definition.Components)
                    component?.OnTick(context, step);

                if (active.Contains(effect) && !effect.IsPermanent && effect.Remaining <= Epsilon)
                {
                    effect.Remaining = 0f;
                    RemoveInternal(effect, RemovalReason.Expired);
                    expired = true;
                }
            }

            if (expired)
                Changed?.Invoke();
        }

        /// <summary>Retire cette instance précise.</summary>
        public bool Remove(ActiveEffect effect, RemovalReason reason)
        {
            if (effect == null || !active.Contains(effect))
                return false;

            RemoveInternal(effect, reason);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Retire toutes les instances de cet effet.</summary>
        public bool Remove(EffectDefinition definition, RemovalReason reason) =>
            RemoveWhere(effect => effect.Definition == definition, reason) > 0;

        public int RemoveWhere(Predicate<ActiveEffect> match, RemovalReason reason)
        {
            // Copie dédiée : RemoveWhere peut être appelé pendant Tick (mort), qui utilise iterationBuffer.
            int removed = 0;
            foreach (var effect in active.ToArray())
            {
                if (active.Contains(effect) && match(effect))
                {
                    RemoveInternal(effect, reason);
                    removed++;
                }
            }

            if (removed > 0)
                Changed?.Invoke();
            return removed;
        }

        public int RemoveAll(RemovalReason reason) => RemoveWhere(_ => true, reason);

        internal IReadOnlyList<StatModifier> CollectModifiers()
        {
            modifierBuffer.Clear();
            foreach (var effect in active)
            {
                var context = new EffectContext(owner, effect);
                foreach (var component in effect.Definition.Components)
                    component?.CollectModifiers(context, modifierBuffer);
            }
            return modifierBuffer;
        }

        void RemoveInternal(ActiveEffect effect, RemovalReason reason)
        {
            active.Remove(effect);
            Removed?.Invoke(effect, reason);

            // Les modificateurs de l'effet disparaissent tout de suite : ni les composants (OnRemove)
            // ni les effets suivants du même pas ne voient de stats périmées.
            owner.Stats.Recompute();
            var context = new EffectContext(owner, effect);
            foreach (var component in effect.Definition.Components)
                component?.OnRemove(context, reason);
        }

        ApplyResult Announce(ApplyResult result)
        {
            Applied?.Invoke(result);
            return result;
        }
    }
}
