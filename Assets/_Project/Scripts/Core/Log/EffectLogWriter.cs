using UnityEngine;

namespace QueteI1.StatusEffects
{
    /// <summary>
    /// Écrit le journal à partir des événements typés d'une entité. C'est le seul endroit du cœur qui
    /// rédige des phrases : le contrôleur, les PV et les stats n'annoncent que des faits.
    /// </summary>
    public static class EffectLogWriter
    {
        public static void Attach(StatusEntity entity, EffectEventLog log)
        {
            string name = entity.Name;

            entity.Effects.Applied += result => log.Add(name, TypeOf(result.Outcome), Describe(result));
            entity.Effects.Removed += (effect, reason) => log.Add(name,
                reason == RemovalReason.Expired ? EffectEventType.Expired : EffectEventType.Removed, Describe(effect, reason));
            entity.Stats.StatChanged += (stat, before, after) => log.Add(name, EffectEventType.Stats,
                $"{StatTypeInfo.Label(stat)} : {TextFormat.Num(before)} → {TextFormat.Num(after)}");
            entity.Health.Changed += change => log.Add(name,
                change.Kind == HealthChangeKind.Heal ? EffectEventType.Heal : EffectEventType.Damage,
                Describe(change, entity.Health.Max));
            entity.Health.Died += () => log.Add(name, EffectEventType.Death, "est mort");
            entity.ActionDenied += (action, label) => log.Add(name, EffectEventType.Blocked, entity.IsDead
                ? $"{label} impossible : entité morte"
                : $"{label} bloqué par {entity.DescribeBlockers(action)}");
            entity.Revived += () => log.Add(name, EffectEventType.Info, "réinitialisé : PV au max, aucun effet actif");
        }

        static EffectEventType TypeOf(ApplyOutcome outcome) => outcome switch
        {
            ApplyOutcome.StackAdded => EffectEventType.Stacked,
            ApplyOutcome.Refreshed => EffectEventType.Refreshed,
            ApplyOutcome.RefreshedAtMaxStacks => EffectEventType.Refreshed,
            ApplyOutcome.Rejected => EffectEventType.Rejected,
            _ => EffectEventType.Applied
        };

        static string Describe(ApplyResult result)
        {
            var definition = result.Definition;
            var effect = result.Effect;
            string name = definition.DisplayName;

            switch (result.Outcome)
            {
                case ApplyOutcome.AppliedInstant:
                    return $"{name} (instantané) appliqué par {result.Source.Label}";
                case ApplyOutcome.Applied:
                    return $"{name} appliqué par {result.Source.Label} ({definition.RulesLabel})";
                case ApplyOutcome.StackAdded:
                    return $"{name} : stack {effect.Stacks}/{definition.MaxStacks}, durée remise à {effect.RemainingLabel}";
                case ApplyOutcome.RefreshedAtMaxStacks:
                    return $"{name} : déjà au maximum ({definition.MaxStacks}/{definition.MaxStacks}), " +
                           $"seule la durée est remise à {effect.RemainingLabel}";
                case ApplyOutcome.Refreshed:
                    string before = effect.IsPermanent ? effect.RemainingLabel : TextFormat.Seconds(result.PreviousRemaining);
                    return $"{name} rafraîchi : {before} → {effect.RemainingLabel}";
                default:
                    return $"{name} refusé : {Describe(result.RejectReason, effect)}";
            }
        }

        static string Describe(RejectReason reason, ActiveEffect effect) => reason switch
        {
            RejectReason.TargetDead => "la cible est morte",
            RejectReason.AlreadyActive => $"déjà actif ({effect.RemainingLabel} restantes)",
            RejectReason.Immune => $"bloqué par {effect.Definition.DisplayName}",
            _ => "raison inconnue"
        };

        static string Describe(ActiveEffect effect, RemovalReason reason)
        {
            string name = effect.Definition.DisplayName;
            string stacks = effect.Stacks > 1 ? $" ({effect.Stacks} stacks)" : "";
            return reason switch
            {
                RemovalReason.Expired => $"{name}{stacks} a expiré",
                RemovalReason.Dispelled => $"{name}{stacks} dissipé",
                RemovalReason.Death => $"{name}{stacks} retiré (mort de l'entité)",
                _ => $"{name}{stacks} retiré (réinitialisation)"
            };
        }

        static string Describe(HealthChange change, float maxHealth)
        {
            string result = $"→ {TextFormat.Num(change.After)}/{TextFormat.Num(maxHealth)} PV";
            string origin = change.Instigator.Entity != null ? $"{change.Label} de {change.Instigator.Label}" : change.Label;
            if (change.Kind == HealthChangeKind.Heal)
                return $"+{TextFormat.Num(change.Applied)} PV ({origin}) {result}";

            string detail = "";
            if (!Mathf.Approximately(change.AfterModifiers, change.Requested))
                detail += $", {TextFormat.Num(change.Requested)} → {TextFormat.Num(change.AfterModifiers)} par les effets";
            if (!change.DefenseApplied)
                detail += ", ignore la défense";
            else if (change.Defense > 0f)
                detail += $", {TextFormat.Num(change.AfterModifiers)} réduits par {TextFormat.Num(change.Defense)} de défense";
            return $"−{TextFormat.Num(change.Applied)} PV ({origin}{detail}) {result}";
        }
    }
}
