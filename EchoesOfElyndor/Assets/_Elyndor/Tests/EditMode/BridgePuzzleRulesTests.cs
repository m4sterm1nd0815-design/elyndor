using System;
using Elyndor.Puzzles;
using NUnit.Framework;

namespace Elyndor.Tests.EditMode
{
    /// <summary>
    /// Prüft die Regeln des Brückenrätsels ohne Szene.
    ///
    /// Der wichtigste Test ist der über alle 27 Kombinationen: genau eine darf
    /// tragen. Ein Rätsel, bei dem zwei Konfigurationen funktionieren, ist
    /// nicht großzügig, sondern kaputt — der Spieler lernt dann nicht die
    /// Regel, sondern nur, dass Raten manchmal reicht.
    ///
    /// Was diese Tests <em>nicht</em> beantworten: ob ein Mensch die Lösung
    /// herleitet oder alle 27 durchprobiert. Das entscheidet der
    /// Usability-Test.
    /// </summary>
    public sealed class BridgePuzzleRulesTests
    {
        [TearDown]
        public void TearDown()
        {
            PuzzleSessionState.Forget("test_bridge");
        }

        [Test]
        public void GenauEineVonSiebenundzwanzigKombinationen_Traegt()
        {
            int accepted = 0;

            for (int south = 1; south <= 3; south++)
            for (int side = 1; side <= 3; side++)
            for (int north = 1; north <= 3; north++)
            {
                if (BridgePuzzleRules.IsCorrect(south, side, north))
                {
                    accepted++;

                    Assert.That(south, Is.EqualTo(1), "Süd-Tiefanker");
                    Assert.That(side, Is.EqualTo(2), "Seitenanker");
                    Assert.That(north, Is.EqualTo(3), "Nordanker");
                }
            }

            Assert.That(
                accepted,
                Is.EqualTo(1),
                "Es darf genau eine akzeptierte Konfiguration geben.");
        }

        [Test]
        public void DieNaheliegendeFalscheReihe_TraegtNicht()
        {
            // Wer die Kerben stumpf in Leserichtung 1-2-3 einstellt, statt dem
            // Lastverlauf zu folgen, liegt falsch. Genau dieser Irrtum ist im
            // Entwurf vorgesehen — er muss also auch wirklich falsch sein.
            Assert.That(
                BridgePuzzleRules.IsCorrect(1, 2, 3),
                Is.True,
                "Vorbedingung des Entwurfs: Sued 1, Seite 2, Nord 3.");

            Assert.That(
                BridgePuzzleRules.IsCorrect(3, 2, 1),
                Is.False,
                "Die umgekehrte Reihe darf nicht ebenfalls tragen.");
            Assert.That(
                BridgePuzzleRules.IsCorrect(2, 2, 2),
                Is.False);
        }

        [Test]
        public void Spannungsrueckmeldung_NenntNieDenFalschenAnker()
        {
            // Sie zaehlt nur, wie viele stimmen. Zwei verschiedene
            // Fehlstellungen mit derselben Anzahl richtiger Anker muessen
            // ununterscheidbar sein — sonst waere die Rueckmeldung ein
            // Loesungsverrat in Raten.
            int a = BridgePuzzleRules.CorrectAnchorCount(1, 2, 1);
            int b = BridgePuzzleRules.CorrectAnchorCount(1, 1, 3);

            Assert.That(a, Is.EqualTo(2));
            Assert.That(b, Is.EqualTo(2));
            Assert.That(
                a,
                Is.EqualTo(b),
                "Zwei verschiedene Fehler mit gleich vielen richtigen Ankern " +
                "muessen dieselbe Rueckmeldung ergeben.");
        }

        [Test]
        public void KerbenUndStellungen_SindUmkehrbar()
        {
            for (int setting = 0;
                 setting < BridgePuzzleRules.SettingsPerAnchor;
                 setting++)
            {
                int notches = BridgePuzzleRules.NotchesForSetting(setting);

                Assert.That(notches, Is.EqualTo(setting + 1));
                Assert.That(
                    BridgePuzzleRules.SettingForNotches(notches),
                    Is.EqualTo(setting));
            }
        }

        // ------------------------------------------------------------------
        // Zustandstabelle
        // ------------------------------------------------------------------

        [Test]
        public void KeinZustand_WechseltAufSichSelbst()
        {
            foreach (BridgePuzzleState state in AllStates())
            {
                Assert.That(
                    BridgePuzzleRules.CanTransition(state, state),
                    Is.False,
                    $"{state} wechselt auf sich selbst.");
            }
        }

        [Test]
        public void Geloest_IstEndgueltig()
        {
            foreach (BridgePuzzleState state in AllStates())
            {
                Assert.That(
                    BridgePuzzleRules.CanTransition(
                        BridgePuzzleState.Solved, state),
                    Is.False,
                    $"Aus Solved heraus wurde {state} zugelassen.");
            }
        }

        [Test]
        public void KeinZustand_IstEineSackgasse()
        {
            foreach (BridgePuzzleState state in AllStates())
            {
                if (BridgePuzzleRules.IsFinal(state))
                {
                    continue;
                }

                bool hasExit = false;

                foreach (BridgePuzzleState target in AllStates())
                {
                    if (BridgePuzzleRules.CanTransition(state, target))
                    {
                        hasExit = true;
                        break;
                    }
                }

                Assert.That(
                    hasExit,
                    Is.True,
                    $"{state} ist eine Sackgasse — dort waere der Spieler " +
                    "gefangen.");
            }
        }

        [Test]
        public void JederFehlversuch_FuehrtZurueckInDieKonfiguration()
        {
            Assert.That(
                BridgePuzzleRules.CanTransition(
                    BridgePuzzleState.Configuring,
                    BridgePuzzleState.Recovering),
                Is.True);
            Assert.That(
                BridgePuzzleRules.CanTransition(
                    BridgePuzzleState.Recovering,
                    BridgePuzzleState.Configuring),
                Is.True,
                "Aus der Sperre muss es zurueck in die Konfiguration gehen.");
        }

        [Test]
        public void UebergangszustaendeSchreiben_NurIhrenStabilenStand()
        {
            Assert.That(
                BridgePuzzleRules.IsStable(BridgePuzzleState.Recovering),
                Is.False);
            Assert.That(
                BridgePuzzleRules.IsStable(BridgePuzzleState.BridgeDeploying),
                Is.False);

            Assert.That(
                BridgePuzzleRules.StableFallback(BridgePuzzleState.Recovering),
                Is.EqualTo(BridgePuzzleState.Configuring));
            Assert.That(
                BridgePuzzleRules.StableFallback(
                    BridgePuzzleState.BridgeDeploying),
                Is.EqualTo(BridgePuzzleState.ReadyToRelease),
                "Ein Laden mitten in der Stammbewegung darf niemals " +
                "Zwischengeometrie wiederherstellen.");
        }

        [Test]
        public void WaehrendDerStammbewegung_IstDieEingabeGesperrt()
        {
            Assert.That(
                BridgePuzzleRules.AllowsAnchorTurning(
                    BridgePuzzleState.BridgeDeploying),
                Is.False);
            Assert.That(
                BridgePuzzleRules.AllowsRelease(
                    BridgePuzzleState.BridgeDeploying),
                Is.False);
            Assert.That(
                BridgePuzzleRules.AllowsAnchorTurning(
                    BridgePuzzleState.Recovering),
                Is.False,
                "In der Sperre darf nicht weitergedreht werden.");
        }

        [Test]
        public void DasEcho_BleibtNachDerLoesungBetrachtbar()
        {
            Assert.That(
                BridgePuzzleRules.AllowsWatch(BridgePuzzleState.Solved),
                Is.True,
                "Die geloeste Bruecke darf das Echo nicht abschalten.");
            Assert.That(
                BridgePuzzleRules.AllowsWatch(BridgePuzzleState.Dormant),
                Is.False,
                "Ausserhalb der Zone meldet die Watch keine Bereitschaft.");
        }

        [Test]
        public void BohlenLiegen_NurNachDemStamm()
        {
            foreach (BridgePuzzleState state in AllStates())
            {
                bool expected = state == BridgePuzzleState.Securing;

                Assert.That(
                    BridgePuzzleRules.AllowsPlanks(state),
                    Is.EqualTo(expected),
                    $"Bohlen in {state}.");
            }
        }

        // ------------------------------------------------------------------
        // Sitzungszustand
        // ------------------------------------------------------------------

        [Test]
        public void Sitzungszustand_HaeltDenStand()
        {
            Assert.That(
                PuzzleSessionState.GetBridgeState("test_bridge"),
                Is.EqualTo(BridgePuzzleState.Dormant),
                "Ohne gespeicherten Stand beginnt das Raetsel ruhend.");

            PuzzleSessionState.SetBridgeState(
                "test_bridge", BridgePuzzleState.Configuring);

            Assert.That(
                PuzzleSessionState.GetBridgeState("test_bridge"),
                Is.EqualTo(BridgePuzzleState.Configuring));
        }

        [Test]
        public void Sitzungszustand_SpeichertNieEinenUebergang()
        {
            PuzzleSessionState.SetBridgeState(
                "test_bridge", BridgePuzzleState.BridgeDeploying);

            Assert.That(
                PuzzleSessionState.GetBridgeState("test_bridge"),
                Is.EqualTo(BridgePuzzleState.ReadyToRelease),
                "Ein Speichern waehrend der Stammbewegung muss auf den " +
                "letzten stabilen Stand zurueckfallen.");

            PuzzleSessionState.SetBridgeState(
                "test_bridge", BridgePuzzleState.Recovering);

            Assert.That(
                PuzzleSessionState.GetBridgeState("test_bridge"),
                Is.EqualTo(BridgePuzzleState.Configuring));
        }

        private static BridgePuzzleState[] AllStates()
        {
            return (BridgePuzzleState[])Enum.GetValues(
                typeof(BridgePuzzleState));
        }
    }
}
