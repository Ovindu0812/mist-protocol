using NUnit.Framework;
using MistProtocol.Player;

namespace MistProtocol.Tests
{
    [TestFixture]
    public class WeaponDataTests
    {
        [Test]
        public void WeaponData_InitializesCorrectly()
        {
            var w = new WeaponData
            {
                name = "Test Rifle",
                magazineSize = 30,
                inMagazine = 30,
                reserveAmmo = 120,
                damage = 18f
            };

            Assert.AreEqual("Test Rifle", w.name);
            Assert.AreEqual(30, w.inMagazine);
            Assert.AreEqual(120, w.reserveAmmo);
            Assert.AreEqual(18f, w.damage);
        }

        [Test]
        public void WeaponData_AmmoConsumption_DecrementsMagazine()
        {
            var w = new WeaponData { magazineSize = 30, inMagazine = 30, reserveAmmo = 60 };
            w.inMagazine -= 5;
            Assert.AreEqual(25, w.inMagazine);

            int needed = w.magazineSize - w.inMagazine;
            Assert.AreEqual(5, needed);

            int take = System.Math.Min(needed, w.reserveAmmo);
            w.inMagazine += take;
            w.reserveAmmo -= take;

            Assert.AreEqual(30, w.inMagazine);
            Assert.AreEqual(55, w.reserveAmmo);
        }

        [Test]
        public void WeaponData_EmptyReserve_ReloadsPartially()
        {
            var w = new WeaponData { magazineSize = 30, inMagazine = 10, reserveAmmo = 8 };
            int needed = w.magazineSize - w.inMagazine; // 20
            int take = System.Math.Min(needed, w.reserveAmmo); // 8

            w.inMagazine += take;
            w.reserveAmmo -= take;

            Assert.AreEqual(18, w.inMagazine);
            Assert.AreEqual(0, w.reserveAmmo);
        }
    }
}
