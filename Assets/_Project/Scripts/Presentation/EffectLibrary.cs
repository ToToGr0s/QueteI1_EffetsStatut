using System.Collections.Generic;
using UnityEngine;

namespace QueteI1.StatusEffects.Presentation
{
    /// <summary>Liste des effets proposés dans le menu de debug (ordre = touches 1, 2, 3...).</summary>
    [CreateAssetMenu(menuName = "Quête I1/Bibliothèque d'effets", fileName = "EffectLibrary", order = 1)]
    public sealed class EffectLibrary : ScriptableObject
    {
        [SerializeField] List<EffectDefinition> effects = new List<EffectDefinition>();

        public IReadOnlyList<EffectDefinition> Effects => effects;

        internal void SetEffects(IEnumerable<EffectDefinition> definitions) =>
            effects = new List<EffectDefinition>(definitions);
    }
}
