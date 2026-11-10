using System.Collections.Generic;
using NUnit.Framework;

namespace QueteI1.StatusEffects.Tests
{
    public sealed class StatCalculatorTests
    {
        const float Tolerance = 1e-4f;

        [Test]
        public void AdditivesAreAppliedBeforeMultipliers()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Speed, ModifierOperation.Multiplicative, 0.5f, "Ralentissement"),
                new StatModifier(StatType.Speed, ModifierOperation.Additive, 2f, "Hâte")
            };

            var result = StatCalculator.Compute(StatType.Speed, 4f, modifiers);

            // (4 + 2) × 0.5 = 3, et non 4 × 0.5 + 2 = 4.
            Assert.AreEqual(3f, result.Final, Tolerance);
        }

        [Test]
        public void MultipliersFromDifferentSourcesAreMultiplied()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Damage, ModifierOperation.Multiplicative, 1.5f, "Rage"),
                new StatModifier(StatType.Damage, ModifierOperation.Multiplicative, 0.5f, "Faiblesse")
            };

            var result = StatCalculator.Compute(StatType.Damage, 10f, modifiers);

            Assert.AreEqual(7.5f, result.Final, Tolerance);
            Assert.AreEqual(0.75f, result.Multiplier, Tolerance);
        }

        [Test]
        public void ModifiersOfOtherStatsAreIgnored()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Defense, ModifierOperation.Additive, 50f, "Armure")
            };

            var result = StatCalculator.Compute(StatType.Speed, 4f, modifiers);

            Assert.AreEqual(4f, result.Final, Tolerance);
            Assert.IsFalse(result.HasModifiers);
        }

        [Test]
        public void ResultIsClampedToStatMinimum()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Defense, ModifierOperation.Additive, -30f, "Rage")
            };

            var result = StatCalculator.Compute(StatType.Defense, 10f, modifiers);

            Assert.AreEqual(-20f, result.Unclamped, Tolerance);
            Assert.AreEqual(0f, result.Final, Tolerance);
            Assert.IsTrue(result.IsClamped);
        }

        [Test]
        public void MaxHealthNeverDropsBelowOne()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.MaxHealth, ModifierOperation.Multiplicative, 0f, "Malédiction")
            };

            Assert.AreEqual(1f, StatCalculator.Compute(StatType.MaxHealth, 100f, modifiers).Final, Tolerance);
        }

        [Test]
        public void FormulaShowsEveryStep()
        {
            var modifiers = new List<StatModifier>
            {
                new StatModifier(StatType.Speed, ModifierOperation.Additive, 2f, "Hâte"),
                new StatModifier(StatType.Speed, ModifierOperation.Multiplicative, 0.5f, "Ralentissement")
            };

            string formula = StatCalculator.Compute(StatType.Speed, 4f, modifiers).ToFormula();

            Assert.AreEqual("(4 + 2 Hâte) × 0.5 Ralentissement = 3", formula);
        }
    }
}
