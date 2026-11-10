namespace QueteI1.StatusEffects
{
    /// <summary>Étape du calcul à laquelle un modificateur intervient.</summary>
    public enum ModifierOperation
    {
        /// <summary>Étape 1 : ajouté à la valeur de base.</summary>
        Additive = 0,

        /// <summary>Étape 2 : multiplie le résultat de l'étape 1.</summary>
        Multiplicative = 1
    }

    /// <summary>
    /// Modificateur déjà résolu (stacks pris en compte), produit par un effet actif.
    /// Additif : quantité ajoutée. Multiplicatif : facteur (0.75 = −25 %).
    /// </summary>
    public readonly struct StatModifier
    {
        public readonly StatType Stat;
        public readonly ModifierOperation Operation;
        public readonly float Value;
        public readonly string SourceName;

        public StatModifier(StatType stat, ModifierOperation operation, float value, string sourceName)
        {
            Stat = stat;
            Operation = operation;
            Value = value;
            SourceName = sourceName;
        }
    }
}
