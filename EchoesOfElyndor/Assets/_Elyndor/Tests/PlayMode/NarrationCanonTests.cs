using System.Collections;
using System.Collections.Generic;
using Elyndor.Narration;
using Elyndor.Puzzles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Hält den freigegebenen Erzählkanon des Slice fest.
    ///
    /// Der wichtigste Test ist der über die verbotenen Begriffe. Kanon, der nur
    /// in einem Dokument steht, überlebt den ersten beiläufigen Textnachtrag
    /// nicht — ein Name, der „nur an einer Stelle" fällt, beantwortet eine
    /// Frage, die der Slice offen lassen soll.
    /// </summary>
    public sealed class NarrationCanonTests
    {
        private const string CatalogPath =
            "Assets/_Elyndor/Narration/FinsterwaldNarration.asset";

        private NarrationCatalog catalog;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return LoadScene();

            foreach (CataloguedExaminable examinable in
                     Object.FindObjectsByType<CataloguedExaminable>(
                         FindObjectsInactive.Include))
            {
                if (examinable.Catalog != null)
                {
                    catalog = examinable.Catalog;
                    break;
                }
            }

            Assert.That(
                catalog,
                Is.Not.Null,
                "Kein Erzaehlkatalog in der Szene gefunden.");
        }

        // ------------------------------------------------------------------
        // Schlüssel
        // ------------------------------------------------------------------

        [Test]
        public void JederSchluessel_HatGenauEinenNichtLeerenText()
        {
            var seen = new HashSet<string>();

            foreach (NarrationEntry entry in catalog.Entries)
            {
                Assert.That(
                    entry.Key,
                    Is.Not.Null.And.Not.Empty,
                    "Ein Eintrag ohne Schluessel ist nicht uebersetzbar.");
                Assert.That(
                    entry.Text,
                    Is.Not.Null.And.Not.Empty,
                    $"Der Schluessel '{entry.Key}' hat keinen Text.");
                Assert.That(
                    seen.Add(entry.Key),
                    Is.True,
                    $"Der Schluessel '{entry.Key}' kommt doppelt vor.");
            }
        }

        [Test]
        public void AlleErwartetenSchluessel_SindVorhanden()
        {
            foreach (string key in NarrationKeys.All)
            {
                Assert.That(
                    catalog.Find(key),
                    Is.Not.Null,
                    $"Der Schluessel '{key}' fehlt im Katalog.");
            }
        }

        [Test]
        public void JederInDerSzeneBenutzteSchluessel_Existiert()
        {
            foreach (CataloguedExaminable examinable in
                     Object.FindObjectsByType<CataloguedExaminable>(
                         FindObjectsInactive.Include))
            {
                Assert.That(
                    examinable.Catalog, Is.Not.Null, examinable.name);
                Assert.That(
                    examinable.Catalog.Find(examinable.NarrationKey),
                    Is.Not.Null,
                    $"'{examinable.name}' verweist auf den unbekannten " +
                    $"Schluessel '{examinable.NarrationKey}'.");
            }
        }

        // ------------------------------------------------------------------
        // Kanon
        // ------------------------------------------------------------------

        [Test]
        public void KeinVerbotenerBegriff_KommtImSliceVor()
        {
            foreach (NarrationEntry entry in catalog.Entries)
            {
                foreach (string forbidden in NarrationKeys.ForbiddenTerms)
                {
                    Assert.That(
                        entry.Text,
                        Does.Not.Contain(forbidden),
                        $"'{entry.Key}' enthaelt '{forbidden}'. Der Slice " +
                        "beantwortet diese Frage noch nicht.");
                }
            }
        }

        [Test]
        public void DieStimme_BleibtUnbekannt()
        {
            NarrationEntry voice =
                catalog.Find(NarrationKeys.UnknownVoiceBridge);

            Assert.That(
                voice.Text,
                Does.Contain("Unbekannte Stimme"),
                "Der Sprecher muss ausdruecklich unbekannt bleiben.");
            Assert.That(
                voice.Text,
                Does.Contain("Du suchst immer nach dem Weg, Aren"),
                "Die freigegebene Zeile fehlt.");
        }

        [Test]
        public void AusDerErinnerung_NimmtArenGenauEinenSatzMit()
        {
            NarrationEntry aftermath =
                catalog.Find(NarrationKeys.MemoryBridgeAftermath);

            Assert.That(
                aftermath.Text,
                Does.Contain("Ich war hier"),
                "Die belastbare Erkenntnis fehlt.");

            // Und ausdruecklich nicht mehr als das.
            foreach (string withheld in
                     new[] { "weil", "damals", "geloescht", "genommen hat" })
            {
                Assert.That(
                    aftermath.Text,
                    Does.Not.Contain(withheld),
                    $"Der Nachhall erklaert zu viel ('{withheld}').");
            }
        }

        [Test]
        public void DasZeichen_WirdNichtErklaert()
        {
            NarrationEntry mark = catalog.Find(NarrationKeys.InscriptionMark);

            Assert.That(
                mark.Text,
                Does.Contain("weiss nicht"),
                "Die Gravur muss ausdruecklich unerklaert bleiben.");
        }

        [Test]
        public void DasZeichen_TauchtMehrfachAuf()
        {
            int count = 0;

            foreach (CataloguedExaminable examinable in
                     Object.FindObjectsByType<CataloguedExaminable>(
                         FindObjectsInactive.Include))
            {
                if (examinable.NarrationKey == NarrationKeys.InscriptionMark)
                {
                    count++;
                }
            }

            Assert.That(
                count,
                Is.GreaterThanOrEqualTo(2),
                "Ein wiederkehrendes Zeichen muss wiederkehren.");
        }

        [Test]
        public void KeinTextIstEineTextwand()
        {
            foreach (NarrationEntry entry in catalog.Entries)
            {
                Assert.That(
                    entry.Text.Length,
                    Is.LessThan(400),
                    $"'{entry.Key}' ist ein Lore-Dump.");
            }
        }

        // ------------------------------------------------------------------
        // Optionalität
        // ------------------------------------------------------------------

        /// <summary>
        /// Optionale Lore heißt optional. Wer nichts davon anfasst, muss den
        /// Slice trotzdem spielen können.
        /// </summary>
        [UnityTest]
        public IEnumerator OhneJedeLore_BleibtDasRaetselLoesbar()
        {
            foreach (CataloguedExaminable examinable in
                     Object.FindObjectsByType<CataloguedExaminable>(
                         FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(examinable.gameObject);
            }

            foreach (MemoryEchoNarration narration in
                     Object.FindObjectsByType<MemoryEchoNarration>(
                         FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(narration.gameObject);
            }

            BridgePuzzle puzzle = Object.FindAnyObjectByType<BridgePuzzle>();
            Assert.That(puzzle, Is.Not.Null);

            puzzle.SetPlayerInZone(true);
            puzzle.NotifyEchoObserved();

            foreach (BridgeAnchor anchor in
                     Object.FindObjectsByType<BridgeAnchor>(
                         FindObjectsInactive.Include))
            {
                anchor.SetSetting(
                    BridgePuzzleRules.SettingForNotches(
                        BridgePuzzleRules.RequiredNotches(anchor.AnchorId)),
                    true);
            }

            Assert.That(puzzle.TryRelease(), Is.True);

            float deadline = Time.time + 10f;

            while (puzzle.State == BridgePuzzleState.BridgeDeploying &&
                   Time.time < deadline)
            {
                yield return null;
            }

            for (int i = 0; i < puzzle.RequiredPlanks; i++)
            {
                Assert.That(puzzle.TryPlacePlank(), Is.True);
            }

            Assert.That(
                puzzle.State, Is.EqualTo(BridgePuzzleState.Solved));

            PuzzleSessionState.Forget(puzzle.PuzzleId);
        }

        private static IEnumerator LoadScene()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(
                "Finsterwald", LoadSceneMode.Single);

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
        }
    }
}
