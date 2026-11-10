using System;
using UnityEngine;

namespace QueteI1.StatusEffects
{
    public enum HealthChangeKind
    {
        Damage,
        Heal
    }

    /// <summary>Trace d'une modification des PV : montants à chaque étape, origine, état final.</summary>
    public readonly struct HealthChange
    {
        public readonly HealthChangeKind Kind;

        /// <summary>Montant demandé au départ.</summary>
        public readonly float Requested;

        /// <summary>Montant après les effets qui modifient les dégâts reçus, avant la défense.</summary>
        public readonly float AfterModifiers;

        /// <summary>PV réellement perdus ou gagnés.</summary>
        public readonly float Applied;

        public readonly float After;

        /// <summary>Ce qui a causé le changement (« Poison ×2, tick 3 », « attaque »).</summary>
        public readonly string Label;

        /// <summary>Qui en est à l'origine.</summary>
        public readonly EffectSource Instigator;

        public readonly bool DefenseApplied;
        public readonly float Defense;

        public HealthChange(HealthChangeKind kind, float requested, float afterModifiers, float applied, float after,
            string label, EffectSource instigator, bool defenseApplied, float defense)
        {
            Kind = kind;
            Requested = requested;
            AfterModifiers = afterModifiers;
            Applied = applied;
            After = after;
            Label = label;
            Instigator = instigator;
            DefenseApplied = defenseApplied;
            Defense = defense;
        }
    }

    /// <summary>PV courants. Le maximum vient toujours de la stat finale MaxHealth.</summary>
    public sealed class HealthPool
    {
        readonly StatSheet stats;

        public float Current { get; private set; }
        public float Max => stats.Get(StatType.MaxHealth);
        public bool IsDead => Current <= 0f;

        public event Action<HealthChange> Changed;
        public event Action Died;

        /// <summary>
        /// Étape branchée avant la défense : reçoit des dégâts entrants et renvoie le montant à appliquer.
        /// <see cref="StatusEntity"/> y fait passer les effets actifs (bouclier, vulnérabilité).
        /// </summary>
        internal Func<float, float> IncomingDamageFilter;

        public HealthPool(StatSheet stats)
        {
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            Current = Max;
            stats.Recomputed += ClampToMax;
        }

        /// <summary>Réduction par la défense : 0 = dégâts pleins, 100 = dégâts divisés par 2.</summary>
        public static float Mitigate(float amount, float defense) => amount * 100f / (100f + Mathf.Max(0f, defense));

        public void Apply(HealthChangeKind kind, float amount, string label, bool ignoreDefense = false,
            EffectSource instigator = default)
        {
            if (kind == HealthChangeKind.Damage)
                ApplyDamage(amount, label, ignoreDefense, instigator);
            else
                Heal(amount, label, instigator);
        }

        /// <summary>Dégâts : d'abord les effets qui les modifient, puis la défense (sauf si elle est ignorée).</summary>
        public void ApplyDamage(float amount, string label, bool ignoreDefense = false, EffectSource instigator = default)
        {
            if (IsDead || amount <= 0f)
                return;

            float modified = IncomingDamageFilter != null ? Mathf.Max(0f, IncomingDamageFilter(amount)) : amount;
            float defense = stats.Get(StatType.Defense);
            float mitigated = ignoreDefense ? modified : Mitigate(modified, defense);
            float before = Current;
            Current = Mathf.Max(0f, Current - mitigated);
            Changed?.Invoke(new HealthChange(HealthChangeKind.Damage, amount, modified, before - Current, Current, label,
                instigator, !ignoreDefense, defense));

            if (IsDead)
                Died?.Invoke();
        }

        public void Heal(float amount, string label, EffectSource instigator = default)
        {
            if (IsDead || amount <= 0f)
                return;

            float before = Current;
            Current = Mathf.Min(Max, Current + amount);
            Changed?.Invoke(new HealthChange(HealthChangeKind.Heal, amount, amount, Current - before, Current, label,
                instigator, false, 0f));
        }

        public void ResetToMax() => Current = Max;

        // Si le PV max baisse (fin d'un buff), les PV courants ne peuvent pas le dépasser.
        // S'il monte, les PV courants ne bougent pas : le bonus donne de la marge, pas un soin.
        void ClampToMax()
        {
            if (Current > Max)
                Current = Max;
        }
    }
}
