using System;

namespace QueteI1.StatusEffects.Content
{
    /// <summary>Retire de la cible tous les effets actifs d'une polarité donnée (ex. purification des debuffs).</summary>
    [Serializable]
    [EffectComponentInfo("Contrôle : dissipe des effets", "Retire tous les effets actifs d'une polarité (buffs ou debuffs).")]
    public sealed class DispelComponent : EffectComponent
    {
        public EffectPolarity polarity = EffectPolarity.Debuff;

        public DispelComponent() { }

        public DispelComponent(EffectPolarity polarity)
        {
            this.polarity = polarity;
        }

        public override void OnApply(EffectContext context)
        {
            var target = polarity;
            if (context.Dispel(effect => effect.Definition.Polarity == target) == 0)
                context.Note($"{context.Definition.DisplayName} : aucun effet à dissiper");
        }

        public override string Describe() =>
            polarity == EffectPolarity.Debuff ? "Dissipe tous les debuffs" : $"Dissipe tous les effets « {polarity} »";
    }
}
