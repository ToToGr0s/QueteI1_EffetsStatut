using System;
using System.Collections.Generic;

namespace QueteI1.StatusEffects
{
    /// <summary>
    /// Entité qui peut recevoir des effets : stats, PV, effets actifs et actions.
    /// Classe C# pure (testable sans scène) ; <c>EntityBehaviour</c> la relie à un GameObject.
    /// Elle ne rédige aucun texte : elle lève des événements typés, que <see cref="EffectLogWriter"/>
    /// transforme en lignes de journal.
    /// </summary>
    public sealed class StatusEntity
    {
        public StatusEntity(string name, BaseStats baseStats, EffectEventLog log)
        {
            Name = name;
            Log = log ?? throw new ArgumentNullException(nameof(log));
            Stats = new StatSheet(baseStats);
            Effects = new StatusEffectController(this);
            Stats.SetModifierSource(Effects.CollectModifiers);
            Health = new HealthPool(Stats) { IncomingDamageFilter = ModifyIncomingDamage };

            // Le journal s'abonne avant la gestion de la mort : « est mort » s'écrit avant le retrait des effets.
            EffectLogWriter.Attach(this, log);
            Health.Died += OnDied;
        }

        public string Name { get; }
        public StatSheet Stats { get; }
        public HealthPool Health { get; }
        public StatusEffectController Effects { get; }
        public EffectEventLog Log { get; }
        public bool IsDead => Health.IsDead;

        /// <summary>Une action a été tentée et refusée (entité morte ou action bloquée par un effet).</summary>
        public event Action<ActionType, string> ActionDenied;

        /// <summary>L'entité vient d'être réinitialisée.</summary>
        public event Action Revived;

        public void Tick(float deltaTime) => Effects.Tick(deltaTime);

        /// <summary>Un effet actif bloque-t-il cette action ? (capacité <see cref="IActionBlocker"/>)</summary>
        public bool IsBlocked(ActionType action)
        {
            foreach (var blocker in Effects.Capabilities<IActionBlocker>())
            {
                if ((blocker.Component.BlockedActions & action) != 0)
                    return true;
            }
            return false;
        }

        /// <summary>Noms des effets qui bloquent l'action.</summary>
        public string DescribeBlockers(ActionType action)
        {
            var names = new List<string>();
            foreach (var blocker in Effects.Capabilities<IActionBlocker>())
            {
                string name = blocker.Context.Definition.DisplayName;
                if ((blocker.Component.BlockedActions & action) != 0 && !names.Contains(name))
                    names.Add(name);
            }
            return string.Join(", ", names);
        }

        public bool CanPerform(ActionType action) => !IsDead && !IsBlocked(action);

        /// <summary>Vérifie qu'une action est possible ; sinon lève <see cref="ActionDenied"/>.</summary>
        public bool TryPerform(ActionType action, string label)
        {
            if (CanPerform(action))
                return true;

            ActionDenied?.Invoke(action, label);
            return false;
        }

        public bool TryAttack(StatusEntity target)
        {
            if (target == null || !TryPerform(ActionType.Attack, "Attaque"))
                return false;

            target.Health.ApplyDamage(Stats.Get(StatType.Damage), "attaque", false, this);
            return true;
        }

        /// <summary>Retire tous les effets et remet les PV au maximum.</summary>
        public void Revive()
        {
            Effects.RemoveAll(RemovalReason.Cleared);
            Health.ResetToMax();
            Revived?.Invoke();
        }

        // Dégâts entrants : chaque effet actif qui a la capacité IDamageModifier les transforme à son tour.
        float ModifyIncomingDamage(float amount)
        {
            foreach (var modifier in Effects.Capabilities<IDamageModifier>())
                amount = modifier.Component.ModifyIncomingDamage(modifier.Context, amount);
            return amount;
        }

        void OnDied() => Effects.RemoveAll(RemovalReason.Death);
    }
}
