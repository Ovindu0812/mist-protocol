using NUnit.Framework;
using UnityEngine;
using MistProtocol.Core;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class HealthComponentTests
    {
        private GameObject _go;
        private Health _health;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject("Test_HealthObject");
            _health = _go.AddComponent<Health>();
            _health.SetMax(100f, refill: true);
        }

        [TearDown]
        public void Teardown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void Health_InitializesWithMaxHealth()
        {
            Assert.AreEqual(100f, _health.Max);
            Assert.AreEqual(100f, _health.Current);
            Assert.AreEqual(1f, _health.Normalized);
            Assert.IsFalse(_health.IsDead);
            Assert.IsTrue(_health.IsFullHealth);
        }

        [Test]
        public void Health_TakeDamage_ReducesCurrentHealth()
        {
            _health.TakeDamage(30f, Vector3.zero);
            Assert.AreEqual(70f, _health.Current);
            Assert.AreEqual(0.7f, _health.Normalized);
            Assert.IsFalse(_health.IsDead);
        }

        [Test]
        public void Health_TakeDamage_CannotDropBelowZero()
        {
            _health.TakeDamage(150f, Vector3.zero);
            Assert.AreEqual(0f, _health.Current);
            Assert.IsTrue(_health.IsDead);
        }

        [Test]
        public void Health_Heal_ReplenishesHealthClampedToMax()
        {
            _health.TakeDamage(50f, Vector3.zero);
            _health.Heal(30f);
            Assert.AreEqual(80f, _health.Current);

            _health.Heal(50f);
            Assert.AreEqual(100f, _health.Current);
            Assert.IsTrue(_health.IsFullHealth);
        }

        [Test]
        public void Health_Invulnerability_PreventsDamage()
        {
            _health.IsInvulnerable = true;
            _health.TakeDamage(50f, Vector3.zero);
            Assert.AreEqual(100f, _health.Current);
            Assert.IsFalse(_health.IsDead);
        }
    }
}
