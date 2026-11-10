using System;
using System.Collections.Generic;
using UnityEngine;

namespace QueteI1.StatusEffects.Content
{
    /// <summary>
    /// Modifie les dégâts que reçoit la cible tant que l'effet est actif, avant la défense :
    /// un pourcentage positif donne une vulnérabilité, négatif un bouclier (−100 % = aucun dégât).
    /// </summary>
    [Serializable]
    [EffectComponentInfo("Santé : dégâts subis", "Augmente ou réduit les dégâts reçus tant que l'effet est actif.")]
    public sealed class DamageTakenComponent : EffectComponent, IDamageModifier
    {
        [Tooltip("Pourcentage par stack : +50 = dégâts subis +50 %, −100 = aucun dégât.")]
        public float percent = 50f;

        public DamageTakenComponent() { }

        public DamageTakenComponent(float percent)
        {
            this.percent = percent;
        }

        public float ModifyIncomingDamage(EffectContext context, float amount) =>
            amount * Mathf.Max(0f, 1f + percent / 100f * context.Stacks);

        public override string Describe() => $"Dégâts subis {TextFormat.Signed(percent)} %";

        public override void Validate(EffectDefinition owner, List<string> warnings)
        {
            if (owner.IsInstant)
                warnings.Add("Dégâts subis sur un effet instantané : il ne modifiera jamais rien.");
        }
    }
}
