using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QueteI1.StatusEffects.Content;
using UnityEngine;

namespace QueteI1.StatusEffects.Tests
{
    public sealed class StatusEffectControllerTests
    {
        const float Tolerance = 1e-3f;

        readonly List<EffectDefinition> created = new List<EffectDefinition>();
        SimulationClock clock;
        EffectEventLog log;
        StatusEntity entity;

        [SetUp]
        public void SetUp()
        {
            clock = new SimulationClock();
            log = new EffectEventLog(() => clock.Time, 1000);
            entity = new StatusEntity("Test", new BaseStats(100f, 4f, 10f, 0f), log);
            clock.Add(entity);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var definition in created)
                Object.DestroyImmediate(definition);
            created.Clear();
        }

        // ---------- Règles de cumul ----------

        [Test]
        public void Stack_AddsStacksUpToMax_ThenOnlyRefreshesDuration()
        {
            var slow = Slow(maxStacks: 3);

            var outcomes = Enumerable.Range(0, 4).Select(_ => entity.Effects.Apply(slow, "test").Outcome).ToList();

            CollectionAssert.AreEqual(new[]
            {
                ApplyOutcome.Applied, ApplyOutcome.StackAdded, ApplyOutcome.StackAdded, ApplyOutcome.RefreshedAtMaxStacks
            }, outcomes);
            Assert.AreEqual(3, entity.Effects.Find(slow).Stacks);
            Assert.AreEqual(1f, entity.Stats.Get(StatType.Speed), Tolerance); // 4 × (1 − 0.25 × 3)
        }

        [Test]
        public void Stack_NewStackResetsDuration()
        {
            var slow = Slow(maxStacks: 3);
            entity.Effects.Apply(slow, "test");
            Advance(3f, 0.1f);

            entity.Effects.Apply(slow, "test");

            Assert.AreEqual(5f, entity.Effects.Find(slow).Remaining, Tolerance);
        }

        [Test]
        public void Refresh_ResetsDurationWithoutAddingStacks()
        {
            var haste = Haste();
            entity.Effects.Apply(haste, "test");
            Advance(4f, 0.1f);

            var result = entity.Effects.Apply(haste, "test");

            Assert.AreEqual(ApplyOutcome.Refreshed, result.Outcome);
            Assert.AreEqual(1, result.Effect.Stacks);
            Assert.AreEqual(6f, result.Effect.Remaining, Tolerance);
            Assert.AreEqual(6f, entity.Stats.Get(StatType.Speed), Tolerance);
        }

        [Test]
        public void Reject_RefusesWhileActive_AndAcceptsAgainAfterExpiry()
        {
            var stun = Stun();
            entity.Effects.Apply(stun, "test");
            Advance(1f, 0.1f);

            var rejected = entity.Effects.Apply(stun, "test");

            Assert.AreEqual(ApplyOutcome.Rejected, rejected.Outcome);
            Assert.AreEqual(1.5f, entity.Effects.Find(stun).Remaining, Tolerance);
            Assert.IsTrue(log.Entries.Any(e => e.Type == EffectEventType.Rejected));

            Advance(1.5f, 0.1f);
            Assert.IsNull(entity.Effects.Find(stun));
            Assert.AreEqual(ApplyOutcome.Applied, entity.Effects.Apply(stun, "test").Outcome);
        }

        // ---------- Durées ----------

        [Test]
        public void ExpiredEffect_RemovesItsModifiers()
        {
            entity.Effects.Apply(Haste(), "test");
            Assert.AreEqual(6f, entity.Stats.Get(StatType.Speed), Tolerance);

            Advance(6.01f, 0.1f);

            Assert.AreEqual(0, entity.Effects.Active.Count);
            Assert.AreEqual(4f, entity.Stats.Get(StatType.Speed), Tolerance);
            Assert.IsTrue(log.Entries.Any(e => e.Type == EffectEventType.Expired));
        }

        [Test]
        public void PermanentEffect_NeverExpires()
        {
            var blessing = Make("Bénédiction", DurationType.Permanent, 1f, StackingRule.Reject, 1,
                new StatModifierComponent(new ModifierEntry(StatType.Damage, ModifierOperation.Additive, 5f)));
            entity.Effects.Apply(blessing, "test");

            Advance(1000f, 1f);

            Assert.IsNotNull(entity.Effects.Find(blessing));
            Assert.AreEqual(15f, entity.Stats.Get(StatType.Damage), Tolerance);
        }

        [Test]
        public void InstantEffect_IsNeverStoredAsActive()
        {
            var strike = Make("Frappe", DurationType.Instant, 1f, StackingRule.Refresh, 1,
                new InstantHealthComponent(HealthChangeKind.Damage, 20f));

            var result = entity.Effects.Apply(strike, "test");

            Assert.AreEqual(ApplyOutcome.AppliedInstant, result.Outcome);
            Assert.AreEqual(0, entity.Effects.Active.Count);
            Assert.AreEqual(80f, entity.Health.Current, Tolerance);
        }

        // ---------- Effets périodiques ----------

        [TestCase(0.016f)]
        [TestCase(0.37f)]
        [TestCase(1f)]
        [TestCase(10f)]
        public void Periodic_TicksOncePerInterval_WhateverTheFrameRate(float frameTime)
        {
            entity.Effects.Apply(Poison(maxStacks: 1), "test");

            Advance(10f, frameTime);

            // 6 s, un tick par seconde, 2 dégâts par tick.
            Assert.AreEqual(100f - 6 * 2f, entity.Health.Current, Tolerance);
        }

        [Test]
        public void Periodic_DamageScalesWithStacks()
        {
            var poison = Poison(maxStacks: 5);
            for (int i = 0; i < 3; i++)
                entity.Effects.Apply(poison, "test");

            Advance(1f, 0.05f);

            Assert.AreEqual(100f - 3 * 2f, entity.Health.Current, Tolerance);
        }

        // Poison et régénération en même temps : le temps avance par pas fixes, donc les ticks
        // s'entrelacent de la même façon quelle que soit la durée d'une image.
        [TestCase(0.016f)]
        [TestCase(0.37f)]
        [TestCase(10f)]
        public void InteractingEffects_GiveTheSameResult_WhateverTheFrameRate(float frameTime)
        {
            entity.Health.ApplyDamage(90f, "test", ignoreDefense: true); // reste 10 PV
            entity.Effects.Apply(Poison(maxStacks: 1), "test"); // −2 PV/s pendant 6 s
            entity.Effects.Apply(Make("Régénération", DurationType.Timed, 5f, StackingRule.Refresh, 1,
                new PeriodicHealthComponent(HealthChangeKind.Heal, 6f, 1f), EffectPolarity.Buff), "test"); // +6 PV/s pendant 5 s

            Advance(10f, frameTime);

            // Si le poison consommait tout le temps avant la régénération, ses 5 premiers ticks tueraient l'entité.
            Assert.IsFalse(entity.IsDead);
            Assert.AreEqual(10f - 6 * 2f + 5 * 6f, entity.Health.Current, Tolerance);
        }

        // ---------- Effets à plusieurs composants ----------

        [Test]
        public void Components_SeeUpToDateStats_WhenTheEffectIsApplied()
        {
            // Le soin est placé avant le bonus de PV max : l'ordre des composants ne doit pas compter.
            var vitality = Make("Vitalité", DurationType.Timed, 10f, StackingRule.Refresh, 1, EffectPolarity.Buff,
                new InstantHealthComponent(HealthChangeKind.Heal, 50f),
                new StatModifierComponent(new ModifierEntry(StatType.MaxHealth, ModifierOperation.Additive, 50f)));

            entity.Effects.Apply(vitality, "test");

            Assert.AreEqual(150f, entity.Health.Max, Tolerance);
            Assert.AreEqual(150f, entity.Health.Current, Tolerance);
        }

        // ---------- Santé et défense ----------

        [Test]
        public void Heal_IsClampedToMaxHealth()
        {
            entity.Health.ApplyDamage(30f, "test");
            entity.Effects.Apply(Make("Soin", DurationType.Instant, 1f, StackingRule.Refresh, 1,
                new InstantHealthComponent(HealthChangeKind.Heal, 50f)), "test");

            Assert.AreEqual(100f, entity.Health.Current, Tolerance);
        }

        [Test]
        public void Defense_ReducesDirectDamage_ButNotIgnoreDefenseDamage()
        {
            entity.Effects.Apply(Make("Armure", DurationType.Timed, 10f, StackingRule.Refresh, 1,
                new StatModifierComponent(new ModifierEntry(StatType.Defense, ModifierOperation.Additive, 100f))), "test");

            entity.Health.ApplyDamage(20f, "test");
            Assert.AreEqual(90f, entity.Health.Current, Tolerance); // 20 × 100 / 200

            entity.Health.ApplyDamage(20f, "test", ignoreDefense: true);
            Assert.AreEqual(70f, entity.Health.Current, Tolerance);
        }

        [Test]
        public void LoweringMaxHealth_ClampsCurrentHealth()
        {
            var vitality = Make("Vitalité", DurationType.Permanent, 1f, StackingRule.Reject, 1,
                new StatModifierComponent(new ModifierEntry(StatType.MaxHealth, ModifierOperation.Additive, 50f)));
            entity.Effects.Apply(vitality, "test");
            entity.Health.Heal(50f, "test");
            Assert.AreEqual(150f, entity.Health.Current, Tolerance);

            entity.Effects.Remove(vitality, RemovalReason.Dispelled);

            Assert.AreEqual(100f, entity.Health.Max, Tolerance);
            Assert.AreEqual(100f, entity.Health.Current, Tolerance);
        }

        // ---------- Comportement ----------

        [Test]
        public void Stun_BlocksActions_UntilItExpires()
        {
            var target = new StatusEntity("Cible", new BaseStats(), log);
            entity.Effects.Apply(Stun(), "test");

            Assert.IsFalse(entity.CanPerform(ActionType.Move));
            Assert.IsFalse(entity.TryAttack(target));
            Assert.AreEqual(target.Health.Max, target.Health.Current, Tolerance);

            Advance(2.5f, 0.1f);

            Assert.IsTrue(entity.CanPerform(ActionType.Move));
            Assert.IsTrue(entity.TryAttack(target));
        }

        [Test]
        public void DeadEntity_LosesItsEffects_AndRejectsNewOnes()
        {
            var poison = Poison(maxStacks: 5);
            entity.Effects.Apply(poison, "test");

            entity.Health.ApplyDamage(500f, "test", ignoreDefense: true);

            Assert.IsTrue(entity.IsDead);
            Assert.AreEqual(0, entity.Effects.Active.Count);
            Assert.AreEqual(ApplyOutcome.Rejected, entity.Effects.Apply(poison, "test").Outcome);
        }

        [Test]
        public void LethalPoisonTick_DoesNotBreakTheUpdate()
        {
            entity.Health.ApplyDamage(99f, "test", ignoreDefense: true);
            entity.Effects.Apply(Poison(maxStacks: 1), "test");
            entity.Effects.Apply(Haste(), "test");

            Assert.DoesNotThrow(() => Advance(2f, 0.5f));
            Assert.IsTrue(entity.IsDead);
            Assert.AreEqual(0, entity.Effects.Active.Count);
        }

        [Test]
        public void Dispel_RemovesOnlyDebuffs()
        {
            var haste = Haste();
            var slow = Slow(maxStacks: 3);
            entity.Effects.Apply(haste, "test");
            entity.Effects.Apply(slow, "test");

            entity.Effects.Apply(Make("Purification", DurationType.Instant, 1f, StackingRule.Refresh, 1,
                new DispelComponent(EffectPolarity.Debuff)), "test");

            Assert.IsNotNull(entity.Effects.Find(haste));
            Assert.IsNull(entity.Effects.Find(slow));
        }

        // ---------- Événements typés ----------

        [Test]
        public void TypedEvents_AnnounceApplicationsAndRemovals()
        {
            var results = new List<ApplyResult>();
            var removals = new List<(ActiveEffect effect, RemovalReason reason)>();
            entity.Effects.Applied += results.Add;
            entity.Effects.Removed += (effect, reason) => removals.Add((effect, reason));
            var stun = Stun();

            entity.Effects.Apply(stun, "test");
            entity.Effects.Apply(stun, "test");
            Advance(2.5f, 0.1f);

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(ApplyOutcome.Applied, results[0].Outcome);
            Assert.AreSame(stun, results[0].Definition);
            Assert.AreEqual(ApplyOutcome.Rejected, results[1].Outcome);
            Assert.AreEqual(RejectReason.AlreadyActive, results[1].RejectReason);
            Assert.AreEqual(1, removals.Count);
            Assert.AreSame(stun, removals[0].effect.Definition);
            Assert.AreEqual(RemovalReason.Expired, removals[0].reason);
        }

        // ---------- Capacités : dégâts reçus, immunité ----------

        [Test]
        public void DamageModifier_ChangesIncomingDamage_WithOrWithoutDefense()
        {
            entity.Effects.Apply(Make("Vulnérabilité", DurationType.Timed, 10f, StackingRule.Refresh, 1,
                new DamageTakenComponent(50f)), "test");

            entity.Health.ApplyDamage(20f, "test");
            Assert.AreEqual(70f, entity.Health.Current, Tolerance); // 20 × 1.5

            entity.Health.ApplyDamage(20f, "test", ignoreDefense: true);
            Assert.AreEqual(40f, entity.Health.Current, Tolerance);
        }

        [Test]
        public void DamageModifier_AtMinus100Percent_BlocksAllDamage()
        {
            entity.Effects.Apply(Make("Bouclier", DurationType.Timed, 10f, StackingRule.Refresh, 1,
                new DamageTakenComponent(-100f), EffectPolarity.Buff), "test");

            entity.Health.ApplyDamage(500f, "test", ignoreDefense: true);

            Assert.AreEqual(100f, entity.Health.Current, Tolerance);
            Assert.IsFalse(entity.IsDead);
        }

        [Test]
        public void Immunity_RejectsNewDebuffs_ButNotBuffsNorItself()
        {
            var immunity = Make("Immunité", DurationType.Timed, 10f, StackingRule.Refresh, 1,
                new ImmunityComponent(EffectPolarity.Debuff), EffectPolarity.Buff);
            entity.Effects.Apply(immunity, "test");

            var rejected = entity.Effects.Apply(Poison(maxStacks: 1), "test");

            Assert.AreEqual(ApplyOutcome.Rejected, rejected.Outcome);
            Assert.AreEqual(RejectReason.Immune, rejected.RejectReason);
            Assert.AreSame(immunity, rejected.Effect.Definition);
            Assert.AreEqual(ApplyOutcome.Applied, entity.Effects.Apply(Haste(), "test").Outcome);
            Assert.AreEqual(ApplyOutcome.Refreshed, entity.Effects.Apply(immunity, "test").Outcome);
        }

        // ---------- Sources ----------

        [Test]
        public void SharedInstance_ByDefault_WhateverTheSource()
        {
            var poison = Poison(maxStacks: 5);

            entity.Effects.Apply(poison, "A");
            entity.Effects.Apply(poison, "B");

            Assert.AreEqual(1, entity.Effects.Active.Count);
            Assert.AreEqual(2, entity.Effects.Find(poison).Stacks);
        }

        [Test]
        public void SeparatePerSource_KeepsOneInstancePerSource()
        {
            var poison = ScriptableObject.CreateInstance<EffectDefinition>();
            poison.Configure("Poison", DurationType.Timed, 6f, StackingRule.Stack, 5,
                new EffectComponent[] { new PeriodicHealthComponent(HealthChangeKind.Damage, 2f, 1f) }, separatePerSource: true);
            created.Add(poison);
            var other = new StatusEntity("Autre", new BaseStats(), log);

            entity.Effects.Apply(poison, other);
            entity.Effects.Apply(poison, other);
            entity.Effects.Apply(poison, "piège");

            Assert.AreEqual(2, entity.Effects.Active.Count);
            Assert.AreEqual(2, entity.Effects.Find(poison, other).Stacks);
            Assert.AreEqual(1, entity.Effects.Find(poison, "piège").Stacks);
        }

        [Test]
        public void Damage_CarriesItsInstigator()
        {
            var attacker = new StatusEntity("Attaquant", new BaseStats(), log);
            HealthChange last = default;
            entity.Health.Changed += change => last = change;

            attacker.TryAttack(entity);
            Assert.AreSame(attacker, last.Instigator.Entity);

            // Les dégâts d'un effet sont crédités à celui qui l'a posé.
            entity.Effects.Apply(Poison(maxStacks: 1), attacker);
            Advance(1f, 0.1f);
            Assert.AreEqual("Poison, tick 1", last.Label);
            Assert.AreSame(attacker, last.Instigator.Entity);
        }

        // ---------- Horloge ----------

        [Test]
        public void Clock_AdvancesAllEntitiesInTheSameFixedSteps()
        {
            var other = new StatusEntity("Autre", new BaseStats(), log);
            clock.Add(other);
            var haste = Haste();
            entity.Effects.Apply(haste, "test");
            other.Effects.Apply(haste, "test");

            clock.Advance(0.05f); // deux pas de 20 ms ; les 10 ms restants attendent l'appel suivant

            Assert.AreEqual(0.04f, clock.Time, 1e-5f);
            Assert.AreEqual(6f - 0.04f, entity.Effects.Find(haste).Remaining, 1e-4f);
            Assert.AreEqual(6f - 0.04f, other.Effects.Find(haste).Remaining, 1e-4f);
        }

        // ---------- Outils ----------

        void Advance(float duration, float frameTime)
        {
            while (duration > 1e-6f)
            {
                float step = Mathf.Min(frameTime, duration);
                clock.Advance(step);
                duration -= step;
            }
        }

        EffectDefinition Slow(int maxStacks) => Make("Ralentissement", DurationType.Timed, 5f, StackingRule.Stack, maxStacks,
            new StatModifierComponent(new ModifierEntry(StatType.Speed, ModifierOperation.Multiplicative, -25f)));

        EffectDefinition Haste() => Make("Hâte", DurationType.Timed, 6f, StackingRule.Refresh, 1,
            new StatModifierComponent(new ModifierEntry(StatType.Speed, ModifierOperation.Additive, 2f)), EffectPolarity.Buff);

        EffectDefinition Stun() => Make("Étourdissement", DurationType.Timed, 2.5f, StackingRule.Reject, 1,
            new BlockActionsComponent(ActionType.Move | ActionType.Attack));

        EffectDefinition Poison(int maxStacks) => Make("Poison", DurationType.Timed, 6f, StackingRule.Stack, maxStacks,
            new PeriodicHealthComponent(HealthChangeKind.Damage, 2f, 1f));

        EffectDefinition Make(string name, DurationType durationType, float duration, StackingRule rule, int maxStacks,
            EffectComponent component, EffectPolarity polarity = EffectPolarity.Debuff) =>
            Make(name, durationType, duration, rule, maxStacks, polarity, component);

        EffectDefinition Make(string name, DurationType durationType, float duration, StackingRule rule, int maxStacks,
            EffectPolarity polarity, params EffectComponent[] components)
        {
            var definition = ScriptableObject.CreateInstance<EffectDefinition>();
            definition.Configure(name, durationType, duration, rule, maxStacks, components, polarity);
            created.Add(definition);
            return definition;
        }
    }
}
