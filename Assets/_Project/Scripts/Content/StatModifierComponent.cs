using System;
using System.Collections.Generic;
using UnityEngine;

namespace QueteI1.StatusEffects.Content
{
    [Serializable]
    public struct ModifierEntry
    {
        public StatType stat;
        public ModifierOperation operation;
        [Tooltip("Additif : valeur ajoutée par stack. Multiplicatif : pourcentage par stack (-25 = −25 %).")]
        public float value;

        public ModifierEntry(StatType stat, ModifierOperation operation, float value)
        {
            this.stat = stat;
            this.operation = operation;
            this.value = value;
        }
    }

    /// <summary>Modifie des stats tant que l'effet est actif (ralentissement, boost de dégâts...).</summary>
    [Serializable]
    [EffectComponentInfo("Stats : modificateur", "Modifie une ou plusieurs stats tant que l'effet est actif.")]
    public sealed class StatModifierComponent : EffectComponent
    {
        public List<ModifierEntry> modifiers = new List<ModifierEntry>();

        public StatModifierComponent() { }

        public StatModifierComponent(params ModifierEntry[] entries)
        {
            modifiers = new List<ModifierEntry>(entries);
        }

        // Stacks : un additif est multiplié par le nombre de stacks ; un pourcentage aussi
        // (3 stacks de −25 % = −75 %, soit un facteur 0.25). Le facteur ne descend jamais sous 0.
        public override void CollectModifiers(EffectContext context, List<StatModifier> output)
        {
            int stacks = context.Stacks;
            string source = stacks > 1 ? $"{context.Definition.DisplayName} ×{stacks}" : context.Definition.DisplayName;
            foreach (var entry in modifiers)
            {
                float value = entry.operation == ModifierOperation.Additive
                    ? entry.value * stacks
                    : Mathf.Max(0f, 1f + entry.value / 100f * stacks);
                output.Add(new StatModifier(entry.stat, entry.operation, value, source));
            }
        }

        public override string Describe()
        {
            var parts = new List<string>();
            foreach (var entry in modifiers)
            {
                string amount = entry.operation == ModifierOperation.Additive
                    ? TextFormat.Signed(entry.value)
                    : TextFormat.Signed(entry.value) + " %";
                parts.Add($"{StatTypeInfo.Label(entry.stat)} {amount}");
            }
            return parts.Count == 0 ? "Aucun modificateur" : string.Join(", ", parts);
        }

        public override void Validate(EffectDefinition owner, List<string> warnings)
        {
            if (modifiers.Count == 0)
                warnings.Add("Modificateur de stats : liste vide.");
            if (owner.IsInstant)
                warnings.Add("Modificateur de stats sur un effet instantané : il ne sera jamais pris en compte.");

            foreach (var entry in modifiers)
            {
                if (entry.operation == ModifierOperation.Multiplicative && entry.value * owner.MaxStacks <= -100f)
                    warnings.Add($"{StatTypeInfo.Label(entry.stat)} : {TextFormat.Num(entry.value)} % × {owner.MaxStacks} stack(s) " +
                                 "atteint −100 %, la stat tombe à sa borne minimale.");
            }
        }
    }
}
