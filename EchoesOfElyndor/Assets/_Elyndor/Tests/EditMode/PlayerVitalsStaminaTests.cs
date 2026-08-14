using Elyndor.UIFoundation;
using NUnit.Framework;
using UnityEngine;

namespace Elyndor.Tests.EditMode
{
    /// <summary>
    /// Prueft die Ausdauerbuchhaltung.
    ///
    /// Hintergrund: Ausdauer wurde bisher nur vom Sprint verbraucht, und der
    /// Zeitpunkt des letzten Verbrauchs lag in <c>PlayerMovement</c>. Sobald
    /// auch Rolle, Angriff und Block verbrauchen, waere die Regeneration schon
    /// wieder angelaufen, waehrend ein anderes System gerade zahlt. Deshalb
    /// liegt der Zeitpunkt jetzt in <see cref="PlayerVitals"/> — und genau das
    /// halten diese Tests fest.
    /// </summary>
    public sealed class PlayerVitalsStaminaTests
    {
        private GameObject host;
        private PlayerVitals vitals;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("VitalsTestHost");
            vitals = host.AddComponent<PlayerVitals>();
            vitals.SetStamina(100f);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null)
            {
                Object.DestroyImmediate(host);
                host = null;
            }
        }

        [Test]
        public void AusreichendeAusdauer_WirdAbgebucht()
        {
            bool spent = vitals.TrySpendStamina(20f);

            Assert.That(spent, Is.True);
            Assert.That(vitals.Stamina, Is.EqualTo(80f).Within(0.001f));
        }

        [Test]
        public void ZuWenigAusdauer_WirdNichtTeilweiseAbgebucht()
        {
            vitals.SetStamina(5f);

            bool spent = vitals.TrySpendStamina(20f);

            Assert.That(spent, Is.False, "Der Verbrauch muss abgelehnt werden.");
            Assert.That(
                vitals.Stamina,
                Is.EqualTo(5f).Within(0.001f),
                "Ein abgelehnter Verbrauch darf nichts abbuchen — sonst gaebe " +
                "es halb bezahlte Rollen und Schlaege.");
        }

        [Test]
        public void ExaktAusreichendeAusdauer_WirdAbgebucht()
        {
            vitals.SetStamina(20f);

            Assert.That(vitals.TrySpendStamina(20f), Is.True);
            Assert.That(vitals.Stamina, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ErfolgreicherVerbrauch_MerktSichDenZeitpunkt()
        {
            float before = vitals.LastStaminaSpendTime;

            vitals.TrySpendStamina(10f);

            Assert.That(
                vitals.LastStaminaSpendTime,
                Is.GreaterThan(before),
                "Ohne diesen Zeitpunkt wuerde die Regeneration sofort wieder " +
                "anlaufen.");
        }

        [Test]
        public void AbgelehnterVerbrauch_VerschiebtDenZeitpunktNicht()
        {
            vitals.SetStamina(1f);
            vitals.TrySpendStamina(1f);
            float after = vitals.LastStaminaSpendTime;

            bool spent = vitals.TrySpendStamina(50f);

            Assert.That(spent, Is.False);
            Assert.That(
                vitals.LastStaminaSpendTime,
                Is.EqualTo(after),
                "Ein fehlgeschlagener Versuch darf die Regeneration nicht " +
                "weiter hinausschieben.");
        }

        [Test]
        public void NullVerbrauch_VerschiebtDenZeitpunktNicht()
        {
            float before = vitals.LastStaminaSpendTime;

            bool spent = vitals.TrySpendStamina(0f);

            Assert.That(spent, Is.True, "Nichts zu zahlen ist immer moeglich.");
            Assert.That(
                vitals.LastStaminaSpendTime,
                Is.EqualTo(before),
                "Ein Verbrauch von 0 ist kein Verbrauch. Sonst wuerde ein " +
                "Frame ohne Eingabe die Regeneration blockieren.");
        }

        [Test]
        public void AusdauerWirdNieNegativ()
        {
            vitals.SetStamina(10f);
            vitals.ApplyStamina(-999f);

            Assert.That(vitals.Stamina, Is.EqualTo(0f).Within(0.001f));
            Assert.That(vitals.Stamina01, Is.EqualTo(0f).Within(0.001f));
        }
    }
}
