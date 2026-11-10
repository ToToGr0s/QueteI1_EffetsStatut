using System;
using System.Collections.Generic;

namespace QueteI1.StatusEffects
{
    public enum DurationType
    {
        /// <summary>Agit une fois à l'application, n'est jamais stocké comme effet actif.</summary>
        Instant,

        /// <summary>Reste actif pendant une durée puis expire.</summary>
        Timed,

        /// <summary>Reste actif jusqu'à être retiré (dissipation, mort, réinitialisation).</summary>
        Permanent
    }

    /// <summary>Que faire quand un effet déjà actif est appliqué de nouveau ?</summary>
    public enum StackingRule
    {
        /// <summary>Ajoute un stack (jusqu'au max) et remet la durée à zéro.</summary>
        Stack,

        /// <summary>Remet seulement la durée à zéro.</summary>
        Refresh,

        /// <summary>Refuse la nouvelle application.</summary>
        Reject
    }

    public enum EffectPolarity
    {
        Buff,
        Debuff,
        Neutral
    }

    public enum RemovalReason
    {
        Expired,
        Dispelled,
        Death,
        Cleared
    }

    public enum ApplyOutcome
    {
        AppliedInstant,
        Applied,
        StackAdded,
        RefreshedAtMaxStacks,
        Refreshed,
        Rejected
    }

    /// <summary>Actions qu'une entité peut tenter et qu'un effet peut bloquer.</summary>
    [Flags]
    public enum ActionType
    {
        None = 0,
        Move = 1 << 0,
        Attack = 1 << 1
    }

    public static class ActionTypeInfo
    {
        public static string Describe(ActionType actions)
        {
            if (actions == ActionType.None)
                return "rien";

            var parts = new List<string>();
            if ((actions & ActionType.Move) != 0) parts.Add("Déplacement");
            if ((actions & ActionType.Attack) != 0) parts.Add("Attaque");
            return string.Join(", ", parts);
        }
    }

    /// <summary>Pourquoi une application a été refusée.</summary>
    public enum RejectReason
    {
        None,
        TargetDead,
        AlreadyActive,
        Immune
    }

    /// <summary>Résultat d'une application d'effet, aussi transmis par <see cref="StatusEffectController.Applied"/>.</summary>
    public readonly struct ApplyResult
    {
        public readonly ApplyOutcome Outcome;
        public readonly EffectDefinition Definition;
        public readonly EffectSource Source;

        /// <summary>
        /// Instance concernée. Null pour un effet instantané ou une cible morte ; pour un refus,
        /// c'est l'effet déjà actif ou celui qui immunise.
        /// </summary>
        public readonly ActiveEffect Effect;

        public readonly RejectReason RejectReason;

        /// <summary>Durée qui restait avant un rafraîchissement ou un nouveau stack.</summary>
        public readonly float PreviousRemaining;

        public ApplyResult(ApplyOutcome outcome, EffectDefinition definition, EffectSource source, ActiveEffect effect,
            RejectReason rejectReason = RejectReason.None, float previousRemaining = 0f)
        {
            Outcome = outcome;
            Definition = definition;
            Source = source;
            Effect = effect;
            RejectReason = rejectReason;
            PreviousRemaining = previousRemaining;
        }

        public bool Accepted => Outcome != ApplyOutcome.Rejected;
    }
}
