using System;

namespace QueteI1.StatusEffects
{
    /// <summary>
    /// Qui applique un effet ou inflige des dégâts : une entité, ou un simple nom (outil de test, démo).
    /// Deux sources sont égales si c'est la même entité, ou à défaut le même nom.
    /// </summary>
    public readonly struct EffectSource : IEquatable<EffectSource>
    {
        public readonly string Name;
        public readonly StatusEntity Entity;

        public EffectSource(string name)
        {
            Name = name;
            Entity = null;
        }

        public EffectSource(StatusEntity entity)
        {
            Entity = entity;
            Name = entity?.Name;
        }

        public string Label => string.IsNullOrEmpty(Name) ? "inconnu" : Name;

        public bool Equals(EffectSource other) =>
            Entity != null || other.Entity != null ? ReferenceEquals(Entity, other.Entity) : Name == other.Name;

        public override bool Equals(object obj) => obj is EffectSource other && Equals(other);

        public override int GetHashCode() => Entity != null ? Entity.GetHashCode() : Name?.GetHashCode() ?? 0;

        public override string ToString() => Label;

        public static implicit operator EffectSource(string name) => new EffectSource(name);

        public static implicit operator EffectSource(StatusEntity entity) => new EffectSource(entity);
    }
}
