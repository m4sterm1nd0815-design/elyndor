using Elyndor.Combat;
using Elyndor.UIFoundation;
using NUnit.Framework;
using UnityEngine;

namespace Elyndor.Tests.EditMode
{
    /// <summary>
    /// Prueft den Schadensvertrag des Spielers.
    ///
    /// Hintergrund: Bis P1.5 rief jeder Angreifer <c>PlayerVitals.TakeDamage</c>
    /// direkt auf. Damit gab es keinen Ort, an dem ein Treffer haette wissen
    /// koennen, dass gerade geblockt wird — die Blockregel haette in jedem
    /// Gegner einzeln nachgebaut werden muessen und waere beim naechsten
    /// Angreifer wieder vergessen worden.
    ///
    /// Die Regel selbst: Block nimmt 70 % des Gesundheitsschadens weg, 30 %
    /// kommen durch. Block ist damit eine gueltige, aber keine dominante
    /// Antwort — die Rolle vermeidet vollstaendig, verlangt dafuer aber
    /// Timing.
    /// </summary>
    public sealed class PlayerDamageContractTests
    {
        /// <summary>
        /// Blockzustand ohne Waffe, Eingabe und Animator. Der echte
        /// <c>PlayerCombat</c> braucht all das; fuer den Vertrag zaehlt aber
        /// nur die eine Frage, ob gerade geblockt wird.
        /// </summary>
        private sealed class BlockStateStub : MonoBehaviour, IPlayerBlockState
        {
            public bool IsBlocking { get; set; }
        }

        private GameObject host;
        private PlayerVitals vitals;
        private PlayerDamageReceiver receiver;
        private BlockStateStub blockState;

        [SetUp]
        public void SetUp()
        {
            // Inaktiv aufbauen, damit jedes Awake eine vollstaendige
            // Komposition vorfindet.
            host = new GameObject("DamageContractTestHost");
            host.SetActive(false);

            vitals = host.AddComponent<PlayerVitals>();
            blockState = host.AddComponent<BlockStateStub>();
            receiver = host.AddComponent<PlayerDamageReceiver>();

            host.SetActive(true);

            vitals.SetHealth(100f);
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
        public void NormalerTreffer_KommtVollstaendigAn()
        {
            blockState.IsBlocking = false;

            PlayerDamageResult result = receiver.TakeHit(10f, Vector3.zero);

            Assert.That(
                result.Context,
                Is.EqualTo(PlayerDamageContext.Normal),
                "Ohne Block muss der Treffer als normal gelten.");
            Assert.That(
                result.AppliedAmount,
                Is.EqualTo(10f).Within(0.001f),
                "Ein ungeblockter Treffer darf nichts verlieren.");
            Assert.That(
                vitals.Health, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void GeblockterTreffer_LaesstDreissigProzentDurch()
        {
            blockState.IsBlocking = true;

            PlayerDamageResult result = receiver.TakeHit(10f, Vector3.zero);

            Assert.That(
                result.Context,
                Is.EqualTo(PlayerDamageContext.Blocked));
            Assert.That(
                result.AppliedAmount,
                Is.EqualTo(3f).Within(0.001f),
                "10 Schaden geblockt muessen 3 Restschaden ergeben.");
            Assert.That(
                vitals.Health,
                Is.EqualTo(97f).Within(0.001f),
                "Block darf nicht vollstaendig negieren — sonst waere Halten " +
                "immer die beste Antwort.");
            Assert.That(result.WasMitigated, Is.True);
        }

        [Test]
        public void BlockOhneAktivenBlock_MindertNicht()
        {
            // Genau die Lage nach einem Ausdauerzusammenbruch: der Block ist
            // beendet, der naechste Treffer kommt voll an.
            blockState.IsBlocking = true;
            receiver.TakeHit(10f, Vector3.zero);

            blockState.IsBlocking = false;
            PlayerDamageResult afterCollapse =
                receiver.TakeHit(10f, Vector3.zero);

            Assert.That(
                afterCollapse.Context,
                Is.EqualTo(PlayerDamageContext.Normal));
            Assert.That(
                afterCollapse.AppliedAmount,
                Is.EqualTo(10f).Within(0.001f),
                "Nach dem Zusammenbruch des Blocks darf nichts mehr " +
                "gemindert werden.");
        }

        [Test]
        public void OhneBlockzustand_KommtSchadenVollstaendigAn()
        {
            // Ein Ziel ohne PlayerCombat — etwa ein Testaufbau oder eine
            // spaetere Figur ohne Waffe — blockt nie.
            Object.DestroyImmediate(blockState);

            PlayerDamageResult result = receiver.TakeHit(10f, Vector3.zero);

            Assert.That(
                result.Context, Is.EqualTo(PlayerDamageContext.Normal));
            Assert.That(
                result.AppliedAmount, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void Blockminderung_IstEinstellbar()
        {
            receiver.Configure(0.5f);
            blockState.IsBlocking = true;

            PlayerDamageResult result = receiver.TakeHit(10f, Vector3.zero);

            Assert.That(
                result.AppliedAmount,
                Is.EqualTo(5f).Within(0.001f),
                "Die Minderung muss ein Balancingwert bleiben, keine " +
                "feste Zahl im Code.");
        }

        [Test]
        public void NullSchaden_VeraendertNichts()
        {
            float before = vitals.Health;

            PlayerDamageResult result = receiver.TakeHit(0f, Vector3.zero);

            Assert.That(result.AppliedAmount, Is.Zero);
            Assert.That(vitals.Health, Is.EqualTo(before).Within(0.001f));
        }

        [Test]
        public void NegativerSchaden_HeiltNicht()
        {
            float before = vitals.Health;

            receiver.TakeHit(-25f, Vector3.zero);

            Assert.That(
                vitals.Health,
                Is.EqualTo(before).Within(0.001f),
                "Ein negativer Schadenswert darf keine versteckte Heilung " +
                "werden.");
        }

        [Test]
        public void JederTreffer_WirdGemeldet()
        {
            int events = 0;
            PlayerDamageResult last = default;

            receiver.DamageTaken += result =>
            {
                events++;
                last = result;
            };

            blockState.IsBlocking = true;
            receiver.TakeHit(20f, new Vector3(1f, 0f, 2f));

            Assert.That(events, Is.EqualTo(1));
            Assert.That(last.RawAmount, Is.EqualTo(20f).Within(0.001f));
            Assert.That(last.AppliedAmount, Is.EqualTo(6f).Within(0.001f));
            Assert.That(
                last.SourcePosition,
                Is.EqualTo(new Vector3(1f, 0f, 2f)),
                "Feedback und VFX brauchen die Richtung des Treffers.");
        }
    }
}
