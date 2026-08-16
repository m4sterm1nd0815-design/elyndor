using System;
using System.Collections;
using System.Collections.Generic;
using Elyndor.Core;
using Elyndor.Interaction;
using Elyndor.Memory;
using Elyndor.Persistence;
using Elyndor.Puzzles;
using Elyndor.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Das Brueckenraetsel im <b>fortgesetzten</b> Spiel.
    ///
    /// Die uebrigen Suiten beginnen alle bei null: leere Sitzungszustaende,
    /// frische Szene, Aren sieht die Erinnerung zum ersten Mal. Genau darin lag
    /// die Luecke, die ein Mensch gefunden hat und keine Maschine: nach einem
    /// echten zweiten Programmstart war der Stamm nicht mehr freizugeben.
    ///
    /// Zwei Ursachen, beide nur im fortgesetzten Spiel sichtbar:
    ///
    /// <list type="number">
    /// <item><description>
    /// Der Spielstand kam zu spaet. <c>SaveBootstrap</c> lief auf
    /// <c>AfterSceneLoad</c>, die Welt liest ihren Stand aber im <c>Awake</c>.
    /// Die Bruecke stand nach jedem Programmstart wieder auf Anfang, obwohl
    /// die Datei den Fortschritt enthielt.
    /// </description></item>
    /// <item><description>
    /// Die Memory Site stellt sich als bereits benutzt wieder her und laesst
    /// sich — richtigerweise — kein zweites Mal aktivieren. Ihr Ereignis
    /// oeffnet aber das Raetsel. Ohne es blieb das Raetsel fuer immer in
    /// <c>WatchAvailable</c>: kein Anker drehbar, keine Freigabe moeglich.
    /// </description></item>
    /// </list>
    ///
    /// Deshalb bildet diese Suite den Programmstart in seiner echten
    /// Reihenfolge nach: Sitzungszustaende leeren, Spielstand
    /// wiederherstellen, <em>dann</em> die Szene laden. Bedient wird ueber den
    /// <see cref="InteractionDetector"/> und die ausgelieferte Tastenbelegung —
    /// ein Objekt, das nicht von selbst zum Ziel wird, ist fuer einen Spieler
    /// nicht vorhanden, und genau das war der gemeldete Fehler.
    /// </summary>
    public sealed class FinsterwaldBridgeResumeRuntimeTests : InputTestFixture
    {
        private const string SceneName = "Finsterwald";
        private const string MemorySiteId = "finsterwald_bruecke_01";
        private const string PuzzleId = "finsterwald_bridge_memory_puzzle_v1";
        private const string RegionStateId = "finsterwald_bruecke_regeneration";

        /// <summary>Zeitschranken in Sekunden — nie in Frames, siehe QA_GATES.md.</summary>
        private const float ArcTimeout = 20f;
        private const float SettleTimeout = 5f;

        /// <summary>Notbremse gegen eine stehende Uhr.</summary>
        private const int FrameBrake = 100000;

        private readonly List<string> consoleErrors = new List<string>();

        private Keyboard keyboard;

        private MemorySaveStore store;

        private GameObject player;
        private CharacterController playerBody;
        private InteractionDetector detector;

        private BridgePuzzle puzzle;
        private MemorySite memorySite;

        public override void Setup()
        {
            base.Setup();

            // Beide Geraete, obwohl nur die Tastatur gedrueckt wird: die
            // ausgelieferte Belegung kennt beide, und ein fehlendes Geraet
            // waere ein Unterschied zum Spiel, den dieser Test nicht haben
            // soll.
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();

            store = new MemorySaveStore();
            SaveService.Install(new SaveService(store));

            ForgetSessionProgress();

            consoleErrors.Clear();
            Application.logMessageReceived += CollectConsoleError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= CollectConsoleError;

            ForgetSessionProgress();

            // Die Suite laeuft weiter mit dem Speicher aus SaveIsolationSetup.
            SaveService.Install(new SaveService(new MemorySaveStore()));

            base.TearDown();
        }

        private static void ForgetSessionProgress()
        {
            MemorySessionState.Forget(MemorySiteId);
            PuzzleSessionState.Forget(PuzzleId);
            RegionRegenerationState.Forget(RegionStateId);
        }

        // ==================================================================
        // Der gemeldete Fehler
        // ==================================================================

        /// <summary>
        /// Der gemeldete Fall: die Erinnerung wurde gestern gesehen, das
        /// Raetsel noch nicht geloest. Heute muss der Stamm freizugeben sein.
        /// </summary>
        [UnityTest]
        public IEnumerator EineGesternGeseheneErinnerung_SperrtDenStammNichtAus()
        {
            MemorySessionState.MarkActivated(MemorySiteId);

            yield return RestartAndLoad();

            Assert.That(
                memorySite.IsActivated,
                Is.True,
                "Vorbedingung: die Erinnerung gilt als gesehen.");

            yield return EnterResonanceZone();

            Assert.That(
                puzzle.CanTurnAnchors,
                Is.True,
                "Nach dem Neustart sind die Anker gesperrt, obwohl Aren die " +
                "Erinnerung bereits gesehen hat. Die Memory Site laesst sich " +
                "kein zweites Mal aktivieren — damit gibt es keinen Weg mehr, " +
                "das Raetsel zu oeffnen.");

            yield return SetAnchorsToSolution();
            yield return InteractWith(FindPart(BridgePartRole.TensionCheck));

            BridgePuzzlePart release = FindPart(BridgePartRole.Release);

            Assert.That(
                release,
                Is.Not.Null,
                "Der Seilbock meldet, dass der Stamm tragen koennte, aber die " +
                "Stammfreigabe ist nicht bedienbar.");

            yield return InteractWith(release);

            yield return WaitUntil(
                () => puzzle.CanPlacePlanks,
                "Der Stamm wird nach dem Neustart nicht freigegeben.");

            yield return PlacePlanks();

            Assert.That(
                puzzle.IsSolved,
                Is.True,
                "Das fortgesetzte Raetsel laesst sich nicht abschliessen.");
            Assert.That(
                consoleErrors,
                Is.Empty,
                "Der Durchlauf hat rote Meldungen erzeugt:\n" +
                string.Join("\n", consoleErrors));
        }

        /// <summary>
        /// Die Ankerstellungen gehoeren zum erreichten Stand. Ohne sie meldet
        /// der Seilbock nach dem Neustart Spannung fuer eine Stellung, die es
        /// nicht mehr gibt.
        /// </summary>
        [UnityTest]
        public IEnumerator EinHalbGeloestesRaetsel_KommtMitSeinenAnkernZurueck()
        {
            yield return LoadScene();
            yield return EnterResonanceZone();
            yield return ActivateMemorySite();
            yield return SetAnchorsToSolution();
            yield return InteractWith(FindPart(BridgePartRole.TensionCheck));

            Assert.That(
                puzzle.IsConfigurationCorrect(),
                Is.True,
                "Vorbedingung: die Anker stehen richtig.");

            // Hier endet die erste Sitzung — ohne Freigabe.
            yield return RestartAndLoad();

            Assert.That(
                puzzle.IsConfigurationCorrect(),
                Is.True,
                "Die Ankerstellung ist nach dem Neustart verloren. Der " +
                "Spielstand meldet Fortschritt, die Steine stehen wieder auf " +
                "Anfang.");
            Assert.That(
                puzzle.CanRelease,
                Is.True,
                "Der erreichte Stand laesst den Stamm nicht freigeben.");

            yield return EnterResonanceZone();
            yield return InteractWith(FindPart(BridgePartRole.Release));

            yield return WaitUntil(
                () => puzzle.CanPlacePlanks,
                "Der Stamm wird nach dem Neustart nicht freigegeben.");

            Assert.That(consoleErrors, Is.Empty);
        }

        /// <summary>
        /// Der Fall aus <c>SAVE_ACCEPTANCE.md</c>, Punkt 4.2: die geloeste
        /// Bruecke muss nach einem echten Programmstart begehbar bleiben — und
        /// sich nicht noch einmal ankuendigen.
        /// </summary>
        [UnityTest]
        public IEnumerator DieGeloesteBruecke_IstNachDemNeustartBegehbarUndStill()
        {
            yield return LoadScene();
            yield return EnterResonanceZone();
            yield return ActivateMemorySite();
            yield return SolvePuzzle();

            Assert.That(
                puzzle.IsSolved, Is.True, "Vorbedingung: die Bruecke steht.");

            string solvedText = SolvedText(puzzle);
            int announcements = 0;

            void Count(string text, float duration)
            {
                if (text == solvedText)
                {
                    announcements++;
                }
            }

            NarrationEvents.MessageRequested += Count;

            try
            {
                yield return RestartAndLoad();

                Collider walkway = Walkway();

                Assert.That(
                    puzzle.IsSolved,
                    Is.True,
                    "Die geloeste Bruecke ist nach dem Neustart wieder " +
                    "kaputt.");
                Assert.That(
                    walkway, Is.Not.Null, "Die Lauffläche fehlt am Stamm.");
                Assert.That(
                    walkway.enabled,
                    Is.True,
                    "Die Bruecke ist nach dem Neustart nicht begehbar.");
                Assert.That(
                    announcements,
                    Is.EqualTo(0),
                    "Die Wiederherstellung hat die Loesungsmeldung erneut " +
                    "ausgegeben. Wiederherstellen ist kein Nacherleben.");
            }
            finally
            {
                NarrationEvents.MessageRequested -= Count;
            }
        }

        /// <summary>
        /// Ein wiederhergestellter Stand darf beim blossen Betreten der Zone
        /// nicht kleiner werden — sonst schriebe der naechste Speichervorgang
        /// den Fortschritt weg, den er sichern soll.
        /// </summary>
        [UnityTest]
        public IEnumerator EinWiederhergestellterStand_WirdNichtKleinerGeschrieben()
        {
            MemorySessionState.MarkActivated(MemorySiteId);
            PuzzleSessionState.SetBridgeState(
                PuzzleId, BridgePuzzleState.Solved);

            yield return RestartAndLoad();
            yield return EnterResonanceZone();

            Assert.That(
                PuzzleSessionState.GetBridgeState(PuzzleId),
                Is.EqualTo(BridgePuzzleState.Solved),
                "Das Betreten der Zone hat den gespeicherten Fortschritt " +
                "ueberschrieben.");
            Assert.That(
                store.Read(),
                Does.Contain(BridgePuzzleState.Solved.ToString()),
                "Der Spielstand auf der Platte kennt die geloeste Bruecke " +
                "nicht mehr.");
        }

        /// <summary>
        /// Auch im fortgesetzten Spiel gibt eine falsche Reihenfolge nichts
        /// frei. Der Fix darf das Raetsel nicht nebenbei aufweichen.
        /// </summary>
        [UnityTest]
        public IEnumerator EineFalscheStellung_GibtAuchNachDemNeustartNichtsFrei()
        {
            MemorySessionState.MarkActivated(MemorySiteId);

            yield return RestartAndLoad();
            yield return EnterResonanceZone();

            // Die verdrehte Reihenfolge, die der Plan als nachvollziehbaren
            // Irrtum vorsieht.
            yield return SetAnchors(3, 2, 1);

            Assert.That(
                puzzle.IsConfigurationCorrect(),
                Is.False,
                "Vorbedingung: die Stellung traegt nicht.");

            yield return InteractWith(FindPart(BridgePartRole.Release));

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.Recovering),
                "Eine falsche Stellung hat den Stamm freigegeben, statt ihn " +
                "verkanten zu lassen.");

            yield return WaitUntil(
                () => puzzle.State == BridgePuzzleState.Configuring,
                "Nach dem Fehlversuch bleibt das Raetsel haengen.");

            Collider walkway = Walkway();

            Assert.That(walkway, Is.Not.Null);
            Assert.That(
                walkway.enabled,
                Is.False,
                "Die Bruecke ist begehbar, ohne dass der Stamm liegt.");
        }

        /// <summary>
        /// Ein Spielstand aus einer Fassung ohne gemerkte Ankerstellungen
        /// behauptet <c>ReadyToRelease</c>, ohne dass die Steine dazu passen.
        /// Dann gilt der schwaechere, wahre Zustand — sonst meldete das
        /// Raetsel Bereitschaft und verkantete beim ersten Griff.
        /// </summary>
        [UnityTest]
        public IEnumerator EinAlterStandOhneAnker_BehauptetKeineSpannung()
        {
            MemorySessionState.MarkActivated(MemorySiteId);
            PuzzleSessionState.SetBridgeState(
                PuzzleId, BridgePuzzleState.ReadyToRelease);

            // Ausdruecklich ohne Ankerstellungen: genau so sieht ein
            // Spielstand aus, der vor diesem Feld geschrieben wurde.
            yield return RestartAndLoad();

            Assert.That(
                puzzle.IsConfigurationCorrect(),
                Is.False,
                "Vorbedingung: die Steine stehen auf ihrer Ausgangsstellung.");
            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.Configuring),
                "Das Raetsel behauptet eine tragende Ankerstellung, die es " +
                "nicht gibt.");
        }

        // ==================================================================
        // Programmstart, Szene und Bedienung
        // ==================================================================

        /// <summary>
        /// Bildet einen zweiten Programmstart nach — in der Reihenfolge, die
        /// das Spiel selbst hat: alles Fluechtige faellt weg, der Spielstand
        /// wird wiederhergestellt, und <em>erst danach</em> baut sich die Szene
        /// auf. Genau diese Reihenfolge fehlte, als der Fehler gemeldet wurde.
        /// </summary>
        private IEnumerator RestartAndLoad()
        {
            SaveService.Uninstall();

            MemorySessionState.ForgetAll();
            PuzzleSessionState.ForgetAll();
            RegionRegenerationState.ForgetAll();

            SaveService restarted = new SaveService(store);
            SaveService.Install(restarted);

            LoadOutcome outcome = restarted.LoadAndRestore();

            Assert.That(
                outcome,
                Is.EqualTo(LoadOutcome.Loaded).Or.EqualTo(LoadOutcome.NoSave),
                "Der Spielstand liess sich nicht laden.");

            yield return LoadScene();
        }

        private IEnumerator LoadScene()
        {
            AsyncOperation load =
                SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;

            player = GameObject.Find("Player");
            Assert.That(player, Is.Not.Null, "Player fehlt in der Szene.");

            playerBody = player.GetComponent<CharacterController>();
            detector = player.GetComponent<InteractionDetector>();

            Assert.That(
                detector,
                Is.Not.Null,
                "Ohne InteractionDetector gibt es keinen echten Bedienweg.");

            puzzle = UnityEngine.Object.FindAnyObjectByType<BridgePuzzle>();
            Assert.That(puzzle, Is.Not.Null, "Kein Brueckenraetsel in der Szene.");

            memorySite = null;

            foreach (MemorySite site in
                     UnityEngine.Object.FindObjectsByType<MemorySite>(
                         FindObjectsInactive.Include))
            {
                if (site.SiteId == MemorySiteId)
                {
                    memorySite = site;
                }
            }

            Assert.That(
                memorySite,
                Is.Not.Null,
                $"Die Memory Site '{MemorySiteId}' fehlt.");

            yield return null;
        }

        /// <summary>
        /// Die begehbare Fläche, so wie das Rätsel sie kennt. Ueber das Feld
        /// und nicht ueber den Objektnamen: der Name ist Kulisse, das Feld ist
        /// die Verdrahtung, an der die Begehbarkeit tatsaechlich haengt.
        /// </summary>
        private Collider Walkway()
        {
            return GetPrivateField(puzzle, "walkway") as Collider;
        }

        private static string SolvedText(BridgePuzzle target)
        {
            return GetPrivateField(target, "solvedText") as string;
        }

        private static object GetPrivateField(
            UnityEngine.Object target, string fieldName)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Feld '{fieldName}' fehlt.");

            return field.GetValue(target);
        }

        private IEnumerator EnterResonanceZone()
        {
            WatchResonanceZone zone =
                UnityEngine.Object.FindAnyObjectByType<WatchResonanceZone>();

            Assert.That(zone, Is.Not.Null, "Keine Resonanzzone in der Szene.");

            Teleport(zone.transform.position);

            yield return WaitUntil(
                () => puzzle.PlayerInZone,
                "Aren steht in der Resonanzzone, das Raetsel merkt es nicht.");
        }

        private IEnumerator ActivateMemorySite()
        {
            yield return InteractWith(memorySite);

            yield return WaitUntil(
                () => memorySite.IsActivated,
                "Die Memory Site laesst sich nicht aktivieren.");
        }

        private IEnumerator SolvePuzzle()
        {
            yield return SetAnchorsToSolution();
            yield return InteractWith(FindPart(BridgePartRole.TensionCheck));
            yield return InteractWith(FindPart(BridgePartRole.Release));

            yield return WaitUntil(
                () => puzzle.CanPlacePlanks,
                "Der Stamm kommt nach der Freigabe nicht zur Ruhe.");

            yield return PlacePlanks();
        }

        private IEnumerator PlacePlanks()
        {
            int guard = 0;

            while (!puzzle.IsSolved && guard < 10)
            {
                BridgePuzzlePart plank = FindPart(BridgePartRole.Plank);

                if (plank == null)
                {
                    break;
                }

                yield return InteractWith(plank);
                guard++;
            }
        }

        private IEnumerator SetAnchorsToSolution()
        {
            // Die Loesung wird bei den Regeln erfragt, nicht hier wiederholt.
            yield return SetAnchors(
                BridgePuzzleRules.RequiredNotches(BridgeAnchorId.SouthDeep),
                BridgePuzzleRules.RequiredNotches(BridgeAnchorId.Side),
                BridgePuzzleRules.RequiredNotches(BridgeAnchorId.North));
        }

        private IEnumerator SetAnchors(int south, int side, int north)
        {
            foreach (BridgeAnchor anchor in AllAnchors())
            {
                int wanted = anchor.AnchorId switch
                {
                    BridgeAnchorId.SouthDeep => south,
                    BridgeAnchorId.Side => side,
                    _ => north
                };

                int turns = 0;

                while (anchor.Notches != wanted && turns < 6)
                {
                    yield return InteractWith(anchor);
                    turns++;
                }

                Assert.That(
                    anchor.Notches,
                    Is.EqualTo(wanted),
                    $"Anker {anchor.AnchorId} laesst sich nicht auf " +
                    $"{wanted} Kerben stellen.");
            }
        }

        private static IEnumerable<BridgeAnchor> AllAnchors()
        {
            return UnityEngine.Object.FindObjectsByType<BridgeAnchor>(
                FindObjectsInactive.Include);
        }

        private BridgePuzzlePart FindPart(BridgePartRole role)
        {
            foreach (BridgePuzzlePart part in
                     UnityEngine.Object.FindObjectsByType<BridgePuzzlePart>(
                         FindObjectsInactive.Include))
            {
                if (part.Role == role && part.CanInteract(player))
                {
                    return part;
                }
            }

            return null;
        }

        /// <summary>
        /// Stellt Aren an ein Objekt, wartet bis der Detector es von selbst als
        /// Ziel fuehrt, und drueckt dann die Interaktionstaste — derselbe Weg,
        /// den ein Spieler nimmt, inklusive der Frage, ob das Objekt ueberhaupt
        /// erreichbar ist.
        /// </summary>
        private IEnumerator InteractWith(Component target)
        {
            Assert.That(
                target, Is.Not.Null, "Es gibt kein Objekt zum Bedienen.");

            IInteractable wanted = target as IInteractable;

            Assert.That(
                wanted,
                Is.Not.Null,
                $"'{target.name}' ist gar kein Interaktionsobjekt.");

            Teleport(target.transform.position);

            bool reached = false;
            float deadline = Time.time + SettleTimeout;
            int frames = 0;

            while (Time.time < deadline && frames < FrameBrake)
            {
                yield return null;
                frames++;

                IInteractable current = detector.CurrentTarget;

                if (current != null && ReferenceEquals(current, wanted))
                {
                    reached = true;
                    break;
                }
            }

            Assert.That(
                reached,
                Is.True,
                $"'{target.name}' wird nicht zum Interaktionsziel — fuer " +
                "einen Spieler waere es damit unbedienbar.");

            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);
            yield return null;
        }

        private void Teleport(Vector3 position)
        {
            bool wasEnabled = playerBody != null && playerBody.enabled;

            if (playerBody != null)
            {
                playerBody.enabled = false;
            }

            player.transform.position = position;

            if (playerBody != null)
            {
                playerBody.enabled = wasEnabled;
            }

            Physics.SyncTransforms();
        }

        private IEnumerator WaitUntil(Func<bool> condition, string message)
        {
            float deadline = Time.time + ArcTimeout;
            int frames = 0;

            while (Time.time < deadline && frames < FrameBrake)
            {
                if (condition())
                {
                    yield break;
                }

                frames++;
                yield return null;
            }

            if (condition())
            {
                yield break;
            }

            Assert.Fail($"{message} (nach {ArcTimeout:F0} s)");
        }

        private void CollectConsoleError(
            string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error ||
                type == LogType.Exception ||
                type == LogType.Assert)
            {
                consoleErrors.Add($"{type}: {condition}");
            }
        }
    }
}
