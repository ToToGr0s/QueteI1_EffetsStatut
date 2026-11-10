using System.Collections.Generic;
using UnityEngine;

namespace QueteI1.StatusEffects
{
    /// <summary>
    /// Définition d'un effet (donnée, asset). Elle décrit ce que fait l'effet ; l'état en jeu
    /// (stacks, durée restante) vit dans <see cref="ActiveEffect"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Quête I1/Effet de statut", fileName = "NouvelEffet", order = 0)]
    public sealed class EffectDefinition : ScriptableObject
    {
        [SerializeField] string displayName = "Nouvel effet";
        [SerializeField, TextArea(2, 4)] string description;
        [SerializeField] Color color = Color.white;
        [SerializeField] EffectPolarity polarity = EffectPolarity.Debuff;

        [SerializeField] DurationType durationType = DurationType.Timed;
        [SerializeField, Min(0.1f)] float duration = 5f;

        [SerializeField] StackingRule stackingRule = StackingRule.Refresh;
        [SerializeField, Min(1)] int maxStacks = 1;
        [Tooltip("Chaque source garde sa propre instance (ses stacks, son chrono) au lieu de partager celle de l'effet.")]
        [SerializeField] bool separatePerSource;

        [SerializeReference] List<EffectComponent> components = new List<EffectComponent>();

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Color Color => color;
        public EffectPolarity Polarity => polarity;
        public DurationType DurationType => durationType;
        public bool IsInstant => durationType == DurationType.Instant;
        public float Duration => duration;
        public StackingRule StackingRule => stackingRule;
        public int MaxStacks => stackingRule == StackingRule.Stack ? Mathf.Max(1, maxStacks) : 1;
        public bool SeparatePerSource => separatePerSource && !IsInstant;
        public IReadOnlyList<EffectComponent> Components => components;

        public string DurationLabel => durationType switch
        {
            DurationType.Instant => "Instantané",
            DurationType.Permanent => "Permanent",
            _ => TextFormat.Num(duration) + " s"
        };

        public string StackingLabel
        {
            get
            {
                if (IsInstant)
                    return "—";
                return stackingRule switch
                {
                    StackingRule.Stack => $"Cumul ×{MaxStacks}",
                    StackingRule.Refresh => "Rafraîchit",
                    _ => "Refusé si actif"
                };
            }
        }

        public string RulesLabel => IsInstant ? DurationLabel : $"{DurationLabel} · {StackingLabel}";

        public void CollectWarnings(List<string> warnings)
        {
            if (components.Count == 0)
                warnings.Add("Aucun composant : l'effet ne fera rien.");
            if (stackingRule == StackingRule.Stack && maxStacks < 2 && !IsInstant)
                warnings.Add("Règle « Stack » avec un maximum de 1 : se comporte comme « Refresh ».");

            foreach (var component in components)
            {
                if (component == null)
                    warnings.Add("Composant vide (script supprimé ou renommé ?).");
                else
                    component.Validate(this, warnings);
            }
        }

        /// <summary>Configuration par code, réservée à l'outillage (générateur d'assets, tests).</summary>
        internal void Configure(string displayName, DurationType durationType, float duration,
            StackingRule stackingRule, int maxStacks, IEnumerable<EffectComponent> components,
            EffectPolarity polarity = EffectPolarity.Debuff, Color? color = null, string description = null,
            bool separatePerSource = false)
        {
            this.displayName = displayName;
            this.durationType = durationType;
            this.duration = duration;
            this.stackingRule = stackingRule;
            this.maxStacks = maxStacks;
            this.components = new List<EffectComponent>(components);
            this.polarity = polarity;
            this.color = color ?? Color.white;
            this.description = description;
            this.separatePerSource = separatePerSource;
        }
    }
}
