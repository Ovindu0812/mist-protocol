using NUnit.Framework;
using MistProtocol.Player;
using UnityEngine;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class WeaponBallisticsTests
    {
        [Test]
        public void WeaponData_DefaultInitialization_ValidatesParameters()
        {
            var weapon = new WeaponData();
            
            Assert.AreEqual("Mist Rifle", weapon.name);
            Assert.AreEqual(30, weapon.magazineSize);
            Assert.AreEqual(30, weapon.inMagazine);
            Assert.AreEqual(180, weapon.reserveAmmo);
            Assert.AreEqual(16f, weapon.damage, 0.01f);
            Assert.AreEqual(80f, weapon.range, 0.01f);
            Assert.IsTrue(weapon.isAutomatic);
            Assert.AreEqual(1, weapon.pellets);
        }

        [Test]
        public void WeaponData_AmmoConsumption_DecrementsMagazineAccurately()
        {
            var weapon = new WeaponData
            {
                magazineSize = 30,
                inMagazine = 30,
                reserveAmmo = 90
            };

            // Simulate firing 5 rounds
            for (int i = 0; i < 5; i++)
            {
                weapon.inMagazine--;
            }

            Assert.AreEqual(25, weapon.inMagazine);

            // Simulate reload
            int needed = weapon.magazineSize - weapon.inMagazine;
            int toLoad = Mathf.Min(needed, weapon.reserveAmmo);
            weapon.inMagazine += toLoad;
            weapon.reserveAmmo -= toLoad;

            Assert.AreEqual(30, weapon.inMagazine);
            Assert.AreEqual(85, weapon.reserveAmmo);
        }

        [Test]
        public void WeaponData_RecoilPitchRecovery_RemainsWithinNormalBounds()
        {
            var weapon = new WeaponData { recoilPitch = 0.9f };
            float accumulatedRecoil = 0f;

            for (int i = 0; i < 3; i++)
            {
                accumulatedRecoil += weapon.recoilPitch;
            }

            Assert.AreEqual(2.7f, accumulatedRecoil, 0.01f);

            // Simulate recovery
            float recoveryRate = 5f;
            accumulatedRecoil = Mathf.Max(0f, accumulatedRecoil - recoveryRate * 0.5f);
            Assert.AreEqual(0.2f, accumulatedRecoil, 0.01f);
        }
    }
}
