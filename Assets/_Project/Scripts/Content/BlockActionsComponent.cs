using System;
using System.Collections.Generic;

namespace QueteI1.StatusEffects.Content
{
    /// <summary>Empêche certaines actions tant que l'effet est actif (étourdissement, enracinement...).</summary>
    [Serializable]
    [EffectComponentInfo("Comportement : bloque des actions", "Empêche l'entité d'effectuer certaines actions tant que l'effet est actif.")]
    public sealed class BlockActionsComponent : EffectComponent, IActionBlocker
    {
        public ActionType blockedActions = ActionType.Move | ActionType.Attack;

        public BlockActionsComponent() { }

        public BlockActionsComponent(ActionType blockedActions)
        {
            this.blockedActions = blockedActions;
        }

        public ActionType BlockedActions => blockedActions;

        public override string Describe() => "Bloque : " + ActionTypeInfo.Describe(blockedActions);

        public override void Validate(EffectDefinition owner, List<string> warnings)
        {
            if (blockedActions == ActionType.None)
                warnings.Add("Blocage d'actions : aucune action sélectionnée.");
            if (owner.IsInstant)
                warnings.Add("Blocage d'actions sur un effet instantané : il ne bloquera jamais rien.");
        }
    }
}
