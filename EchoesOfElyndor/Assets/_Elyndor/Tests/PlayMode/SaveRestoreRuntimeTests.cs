using System.Collections;
using Elyndor.Memory;
using Elyndor.Persistence;
using Elyndor.Puzzles;
using Elyndor.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Wiederherstellen ist kein Nacherleben.
    ///
    /// Der Unterschied ist der ganze Punkt: wer die Bruecke gelöst und den
    /// Wald geweckt hat, soll die Welt beim naechsten Start <em>so</em>
    /// vorfinden — und nicht den Moment noch einmal geschenkt bekommen, in
    /// dem sie sich veraendert. Ein Ton, der bei jedem Betreten erneut
    /// erklingt, entwertet genau das Ereignis, das er begleiten soll.
    ///
    /// Die uebrige Speicherlogik prueft <c>SavePersistenceTests</c> ohne
    /// Szene. Hier laeuft der echte Finsterwald.
    /// </summary>
    public sealed class SaveRestoreRuntimeTests
    {
        private const string SceneName = "Finsterwald";
        private const string MemorySiteId = "finsterwald_bruecke_01";
        private const string PuzzleId = "finsterwald_bridge_memory_puzzle_v1";
        private const string RegionStateId = "finsterwald_bruecke_regeneration";

        private MemorySaveStore store;

        [SetUp]
        public void SetUp()
        {
            store = new MemorySaveStore();
            SaveService.Install(new SaveService(store));

            MemorySessionState.ForgetAll();
            PuzzleSessionState.ForgetAll();
            RegionRegenerationState.ForgetAll();
        }

        [TearDown]
        public void TearDown()
        {
            MemorySessionState.ForgetAll();
            PuzzleSessionState.ForgetAll();
            RegionRegenerationState.ForgetAll();

            // Die Suite laeuft weiter mit dem Speicher aus SaveIsolationSetup.
            SaveService.Install(new SaveService(new MemorySaveStore()));
        }

        private static IEnumerator LoadFinsterwald()
        {
            AsyncOperation load =
                SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DieWiederhergestellteWelt_FeuertKeineAntwortErneut()
        {
            // Vollstaendiger Fortschritt, so wie er nach einem echten
            // Durchlauf im Spielstand steht.
            MemorySessionState.MarkActivated(MemorySiteId);
            PuzzleSessionState.SetBridgeState(PuzzleId, BridgePuzzleState.Solved);
            RegionRegenerationState.MarkRegenerated(RegionStateId);

            // Neustart: alles Fluechtige weg, derselbe Spielstand wieder da.
            SaveService.Uninstall();
            MemorySessionState.ForgetAll();
            PuzzleSessionState.ForgetAll();
            RegionRegenerationState.ForgetAll();

            SaveService restarted = new SaveService(store);
            SaveService.Install(restarted);

            int answers = 0;

            void Count() => answers++;

            FinsterwaldRegeneration.AnyRegionRegenerated += Count;

            try
            {
                Assert.That(
                    restarted.LoadAndRestore(),
                    Is.EqualTo(LoadOutcome.Loaded),
                    "Der Spielstand liess sich nicht laden.");

                yield return LoadFinsterwald();
                yield return null;
                yield return null;

                FinsterwaldRegeneration regeneration =
                    Object.FindAnyObjectByType<FinsterwaldRegeneration>();

                Assert.That(
                    regeneration, Is.Not.Null, "Keine Regeneration in der Szene.");
                Assert.That(
                    regeneration.IsRegenerated,
                    Is.True,
                    "Der wiederhergestellte Wald steht wieder auf Anfang.");
                Assert.That(
                    answers,
                    Is.EqualTo(0),
                    "Die Wiederherstellung hat die Antwort des Waldes erneut " +
                    "ausgeloest — damit erklaenge der Ton bei jedem Start neu.");
            }
            finally
            {
                FinsterwaldRegeneration.AnyRegionRegenerated -= Count;
            }
        }

        [UnityTest]
        public IEnumerator EineHalbeLeistung_WecktDenWaldAuchNachDemLadenNicht()
        {
            // Nur die Erinnerung, kein geloestes Raetsel.
            MemorySessionState.MarkActivated(MemorySiteId);

            SaveService.Uninstall();
            MemorySessionState.ForgetAll();
            PuzzleSessionState.ForgetAll();
            RegionRegenerationState.ForgetAll();

            SaveService restarted = new SaveService(store);
            SaveService.Install(restarted);
            restarted.LoadAndRestore();

            yield return LoadFinsterwald();
            yield return null;

            FinsterwaldRegeneration regeneration =
                Object.FindAnyObjectByType<FinsterwaldRegeneration>();

            Assert.That(regeneration, Is.Not.Null);
            Assert.That(
                regeneration.MemorySeen,
                Is.True,
                "Die gesehene Erinnerung ging beim Laden verloren.");
            Assert.That(
                regeneration.PuzzleSolved,
                Is.False,
                "Das Raetsel gilt nach dem Laden als geloest.");
            Assert.That(
                regeneration.IsRegenerated,
                Is.False,
                "Der Wald hat auf die halbe Leistung geantwortet.");
        }

        [UnityTest]
        public IEnumerator DerFortschrittEinesDurchlaufs_LandetImSpielstand()
        {
            yield return LoadFinsterwald();

            MemorySessionState.MarkActivated(MemorySiteId);

            Assert.That(
                store.Exists,
                Is.True,
                "Nach einem Fortschrittsereignis wurde nichts geschrieben.");
            Assert.That(
                store.Read(),
                Does.Contain(MemorySiteId),
                "Der Spielstand kennt die gesehene Erinnerung nicht.");
        }
    }
}
