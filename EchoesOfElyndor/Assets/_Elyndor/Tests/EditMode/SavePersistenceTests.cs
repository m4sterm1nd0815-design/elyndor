using System.IO;
using Elyndor.Memory;
using Elyndor.Persistence;
using Elyndor.Puzzles;
using Elyndor.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Der Fortschritt muss ein Programmende ueberleben — und ein kaputter
    /// Spielstand darf niemanden um sein Spiel bringen.
    ///
    /// Alle Tests hier arbeiten auf einem Speicher, der nur im
    /// Arbeitsspeicher lebt, oder auf einem eigenen temporaeren Ordner.
    /// **Kein Test fasst je einen echten Spielstand an.**
    /// </summary>
    public sealed class SavePersistenceTests
    {
        private const string SiteId = "test_memory_site";
        private const string PuzzleId = "test_bridge";
        private const string RegionId = "test_region";

        private string temporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            ClearSessionState();

            temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "elyndor_save_tests_" + Path.GetRandomFileName());
        }

        [TearDown]
        public void TearDown()
        {
            SaveService.Uninstall();
            ClearSessionState();

            if (temporaryDirectory != null && Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        private static void ClearSessionState()
        {
            MemorySessionState.ForgetAll();
            PuzzleSessionState.ForgetAll();
            RegionRegenerationState.ForgetAll();
        }

        /// <summary>
        /// Bildet einen Neustart nach: alles, was nur im Arbeitsspeicher
        /// steht, faellt weg, und der Dienst faengt mit demselben Speicher neu
        /// an. Genau so verhaelt sich ein zweiter Programmstart.
        /// </summary>
        private static SaveService RestartWith(ISaveStore store)
        {
            SaveService.Uninstall();
            ClearSessionState();

            SaveService service = new SaveService(store);
            SaveService.Install(service);
            service.LoadAndRestore();

            return service;
        }

        // ==================================================================

        [Test]
        public void OhneSpielstand_BeginntSauber()
        {
            MemorySaveStore store = new MemorySaveStore();
            SaveService service = new SaveService(store);
            SaveService.Install(service);

            Assert.That(
                service.LoadAndRestore(),
                Is.EqualTo(LoadOutcome.NoSave),
                "Ein fehlender Spielstand ist der Normalfall, kein Fehler.");
            Assert.That(
                MemorySessionState.ActivatedSiteIds, Is.Empty);
            Assert.That(
                RegionRegenerationState.RegeneratedRegionIds, Is.Empty);
        }

        [Test]
        public void GeseheneErinnerung_UeberlebtDenNeustart()
        {
            MemorySaveStore store = new MemorySaveStore();
            SaveService.Install(new SaveService(store));

            MemorySessionState.MarkActivated(SiteId);

            RestartWith(store);

            Assert.That(
                MemorySessionState.IsActivated(SiteId),
                Is.True,
                "Die gesehene Erinnerung ist nach dem Neustart vergessen.");
        }

        [Test]
        public void GeloestesRaetsel_UeberlebtDenNeustart()
        {
            MemorySaveStore store = new MemorySaveStore();
            SaveService.Install(new SaveService(store));

            PuzzleSessionState.SetBridgeState(PuzzleId, BridgePuzzleState.Solved);

            RestartWith(store);

            Assert.That(
                PuzzleSessionState.GetBridgeState(PuzzleId),
                Is.EqualTo(BridgePuzzleState.Solved),
                "Die fertige Bruecke ist nach dem Neustart wieder kaputt.");
        }

        [Test]
        public void RegenerierteRegion_UeberlebtDenNeustart()
        {
            MemorySaveStore store = new MemorySaveStore();
            SaveService.Install(new SaveService(store));

            RegionRegenerationState.MarkRegenerated(RegionId);

            RestartWith(store);

            Assert.That(
                RegionRegenerationState.IsRegenerated(RegionId),
                Is.True,
                "Der Wald hat seine Antwort vergessen.");
        }

        [Test]
        public void NurErinnerung_ErzeugtKeineRegeneration()
        {
            MemorySaveStore store = new MemorySaveStore();
            SaveService.Install(new SaveService(store));

            MemorySessionState.MarkActivated(SiteId);

            RestartWith(store);

            Assert.That(MemorySessionState.IsActivated(SiteId), Is.True);
            Assert.That(
                RegionRegenerationState.IsRegenerated(RegionId),
                Is.False,
                "Aus der halben Leistung wurde beim Laden eine ganze.");
        }

        [Test]
        public void NurRaetsel_ErzeugtKeineRegeneration()
        {
            MemorySaveStore store = new MemorySaveStore();
            SaveService.Install(new SaveService(store));

            PuzzleSessionState.SetBridgeState(PuzzleId, BridgePuzzleState.Solved);

            RestartWith(store);

            Assert.That(
                PuzzleSessionState.GetBridgeState(PuzzleId),
                Is.EqualTo(BridgePuzzleState.Solved));
            Assert.That(
                RegionRegenerationState.IsRegenerated(RegionId),
                Is.False,
                "Aus der halben Leistung wurde beim Laden eine ganze.");
        }

        [Test]
        public void BeideBedingungen_UeberlebenGemeinsam()
        {
            MemorySaveStore store = new MemorySaveStore();
            SaveService.Install(new SaveService(store));

            MemorySessionState.MarkActivated(SiteId);
            PuzzleSessionState.SetBridgeState(PuzzleId, BridgePuzzleState.Solved);
            RegionRegenerationState.MarkRegenerated(RegionId);

            RestartWith(store);

            Assert.That(MemorySessionState.IsActivated(SiteId), Is.True);
            Assert.That(
                PuzzleSessionState.GetBridgeState(PuzzleId),
                Is.EqualTo(BridgePuzzleState.Solved));
            Assert.That(
                RegionRegenerationState.IsRegenerated(RegionId), Is.True);
        }

        // ==================================================================
        // Wenn etwas schiefgeht
        // ==================================================================

        [Test]
        public void BeschaedigterSpielstand_StuerztNichtAb()
        {
            MemorySaveStore store = new MemorySaveStore("{ das ist kein JSON");
            SaveService service = new SaveService(store);
            SaveService.Install(service);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*"));

            LoadOutcome outcome = service.LoadAndRestore();

            Assert.That(
                outcome,
                Is.EqualTo(LoadOutcome.Corrupt),
                "Ein kaputter Stand wurde stillschweigend als gueltig " +
                "behandelt — das ist schlimmer als ein Absturz.");
            Assert.That(MemorySessionState.ActivatedSiteIds, Is.Empty);
        }

        [Test]
        public void BeschaedigterSpielstand_GreiftAufDieSicherungZurueck()
        {
            MemorySaveStore store = new MemorySaveStore();
            SaveService.Install(new SaveService(store));

            MemorySessionState.MarkActivated(SiteId);

            // Der gueltige Stand von eben wandert bei diesem Schreiben zur
            // Sicherung; danach ist nur noch Unfug im Hauptstand.
            store.Write("{ auch kein JSON");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*"));

            SaveService restarted = RestartWith(store);

            Assert.That(
                restarted.LastOutcome,
                Is.EqualTo(LoadOutcome.RecoveredFromBackup),
                "Die Sicherung wurde nicht genutzt.");
            Assert.That(
                MemorySessionState.IsActivated(SiteId),
                Is.True,
                "Die Sicherung hat den Fortschritt nicht zurueckgebracht.");
        }

        [Test]
        public void UnbekannteZukunftsversion_WirdNichtGeladen()
        {
            string fromTheFuture =
                "{\"saveVersion\":99,\"memorySiteIds\":[\"" + SiteId + "\"]}";

            MemorySaveStore store = new MemorySaveStore(fromTheFuture);
            SaveService service = new SaveService(store);
            SaveService.Install(service);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*"));

            Assert.That(
                service.LoadAndRestore(),
                Is.EqualTo(LoadOutcome.FromNewerVersion),
                "Ein Stand aus einer neueren Fassung wurde blind geladen.");
            Assert.That(
                MemorySessionState.IsActivated(SiteId),
                Is.False,
                "Daten einer unbekannten Fassung wurden uebernommen.");
            Assert.That(
                store.Read(),
                Is.EqualTo(fromTheFuture),
                "Der Stand der neueren Fassung wurde ueberschrieben — das " +
                "ist Datenverlust fuer den, der zurueckwechselt.");
        }

        [Test]
        public void NeuesSpiel_LaesstNichtsUebrig()
        {
            MemorySaveStore store = new MemorySaveStore();
            SaveService service = new SaveService(store);
            SaveService.Install(service);

            MemorySessionState.MarkActivated(SiteId);
            PuzzleSessionState.SetBridgeState(PuzzleId, BridgePuzzleState.Solved);
            RegionRegenerationState.MarkRegenerated(RegionId);

            service.ResetProgress();

            Assert.That(MemorySessionState.ActivatedSiteIds, Is.Empty);
            Assert.That(PuzzleSessionState.BridgeStates, Is.Empty);
            Assert.That(RegionRegenerationState.RegeneratedRegionIds, Is.Empty);
            Assert.That(store.Exists, Is.False, "Der Spielstand liegt noch da.");

            RestartWith(store);

            Assert.That(
                MemorySessionState.IsActivated(SiteId),
                Is.False,
                "Nach dem Zuruecksetzen kam der Fortschritt zurueck.");
        }

        // ==================================================================
        // Auf echten Dateien
        // ==================================================================

        [Test]
        public void AufDerPlatte_UeberlebtDerFortschritt()
        {
            FileSaveStore store = new FileSaveStore(temporaryDirectory);
            SaveService.Install(new SaveService(store));

            MemorySessionState.MarkActivated(SiteId);
            PuzzleSessionState.SetBridgeState(PuzzleId, BridgePuzzleState.Solved);

            Assert.That(
                File.Exists(store.SavePath),
                Is.True,
                "Es wurde nichts geschrieben.");

            // Ein zweiter Speicher auf denselben Pfad — wie ein zweiter
            // Programmstart, der dieselbe Datei vorfindet.
            RestartWith(new FileSaveStore(temporaryDirectory));

            Assert.That(MemorySessionState.IsActivated(SiteId), Is.True);
            Assert.That(
                PuzzleSessionState.GetBridgeState(PuzzleId),
                Is.EqualTo(BridgePuzzleState.Solved));
        }

        [Test]
        public void DerSpielstand_LiegtNichtImProjektordner()
        {
            FileSaveStore store = new FileSaveStore();

            Assert.That(
                store.SavePath,
                Does.StartWith(Application.persistentDataPath),
                "Der Spielstand landet ausserhalb des vorgesehenen Ortes.");
            Assert.That(
                store.SavePath,
                Does.Not.Contain(Application.dataPath),
                "Der Spielstand landet im Projektordner.");
        }

        [Test]
        public void EinAbbruchBeimSchreiben_KostetNichtDenLetztenStand()
        {
            FileSaveStore store = new FileSaveStore(temporaryDirectory);
            SaveService.Install(new SaveService(store));

            MemorySessionState.MarkActivated(SiteId);

            string valid = File.ReadAllText(store.SavePath);

            // Ein abgebrochener Schreibvorgang hinterlaesst die halbe
            // Datei unter dem temporaeren Namen — nicht unter dem gueltigen.
            File.WriteAllText(store.SavePath + ".tmp", "{ halb geschrieben");

            Assert.That(
                File.ReadAllText(store.SavePath),
                Is.EqualTo(valid),
                "Der gueltige Stand wurde beim Schreiben angetastet.");
        }
    }
}
