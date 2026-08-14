using System.Collections.Generic;
using Elyndor.Puzzles;
using Elyndor.UIFoundation;
using NUnit.Framework;
using UnityEngine;

namespace Elyndor.Tests
{
    /// <summary>
    /// Laufzeittests des Brückenrätsels. Das Rätsel wird vollständig zur
    /// Laufzeit zusammengesetzt und von Hand getaktet — keine Szene, kein
    /// Prefab, keine Abhängigkeit von der Bildrate.
    ///
    /// Der Schwerpunkt liegt auf der Umkehrbarkeit. Ein Rätsel darf den Spieler
    /// nie in einen Zustand bringen, aus dem er nicht mehr herauskommt, und der
    /// Auftrag verlangt ausdrücklich schadensfreie Fehlversuche.
    /// </summary>
    public sealed class BridgePuzzleRuntimeTests
    {
        private const string PuzzleId = "runtime_test_bridge";
        private const float Step = 0.1f;

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<string> consoleErrors = new List<string>();

        private BridgePuzzle puzzle;
        private BridgeAnchor south;
        private BridgeAnchor side;
        private BridgeAnchor north;
        private BoxCollider walkway;
        private GameObject deployedPose;

        [SetUp]
        public void SetUp()
        {
            consoleErrors.Clear();
            Application.logMessageReceived += CollectConsoleError;
            PuzzleSessionState.Forget(PuzzleId);
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CollectConsoleError;
            PuzzleSessionState.Forget(PuzzleId);

            foreach (GameObject instance in spawned)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            spawned.Clear();
            puzzle = null;
        }

        // ------------------------------------------------------------------
        // Ablauf
        // ------------------------------------------------------------------

        [Test]
        public void DieWatch_MeldetSichNurInDerZone()
        {
            CreatePuzzle();

            Assert.That(puzzle.State, Is.EqualTo(BridgePuzzleState.Dormant));
            Assert.That(
                puzzle.WatchAvailable,
                Is.False,
                "Ausserhalb der Zone darf die Watch nichts melden.");

            puzzle.SetPlayerInZone(true);

            Assert.That(
                puzzle.State, Is.EqualTo(BridgePuzzleState.WatchAvailable));
            Assert.That(puzzle.WatchAvailable, Is.True);

            puzzle.SetPlayerInZone(false);

            Assert.That(puzzle.State, Is.EqualTo(BridgePuzzleState.Dormant));
        }

        [Test]
        public void EinmalGesehenesEcho_GehtNichtWiederVerloren()
        {
            CreatePuzzle();
            puzzle.SetPlayerInZone(true);
            puzzle.NotifyEchoObserved();

            Assert.That(
                puzzle.State, Is.EqualTo(BridgePuzzleState.EchoObserved));

            puzzle.SetPlayerInZone(false);

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.EchoObserved),
                "Ein Schritt aus der Zone darf das Gesehene nicht " +
                "zuruecknehmen.");
        }

        [Test]
        public void EchoOhneGemeldeteZone_LaesstDasRaetselNichtHaengen()
        {
            CreatePuzzle();

            // Der Spieler betrachtet die Erinnerung, ohne dass die Zone es
            // vorher gemeldet hat — etwa weil die Auslöser in anderer
            // Reihenfolge greifen.
            puzzle.NotifyEchoObserved();

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.EchoObserved),
                "Wer das Echo sieht, muss danach die Anker drehen dürfen.");
            Assert.That(puzzle.CanTurnAnchors, Is.True);
        }

        [Test]
        public void DieErsteAnkerdrehung_OeffnetDieKonfiguration()
        {
            CreatePuzzle();
            ObserveEcho();

            south.Turn();

            Assert.That(
                puzzle.State, Is.EqualTo(BridgePuzzleState.Configuring));
        }

        [Test]
        public void FalscheStellung_VerkantetUndSetztUmkehrbarZurueck()
        {
            CreatePuzzle();
            ObserveEcho();

            SetNotches(3, 2, 1);

            Assert.That(
                puzzle.TryRelease(),
                Is.False,
                "Eine falsche Stellung darf den Stamm nicht freigeben.");
            Assert.That(
                puzzle.State, Is.EqualTo(BridgePuzzleState.Recovering));
            Assert.That(
                puzzle.CanTurnAnchors,
                Is.False,
                "Waehrend der Sperre wird nicht weitergedreht.");

            // Die Sperre dauert 1,2 s.
            Tick(15);

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.Configuring),
                "Nach der Sperre muss weiterprobiert werden koennen.");
            Assert.That(puzzle.CanTurnAnchors, Is.True);
            Assert.That(consoleErrors, Is.Empty);
        }

        [Test]
        public void RichtigeStellung_LaesstDenStammEinrasten()
        {
            CreatePuzzle();
            ObserveEcho();

            SetNotches(1, 2, 3);

            Assert.That(puzzle.TryRelease(), Is.True);
            Assert.That(
                puzzle.State, Is.EqualTo(BridgePuzzleState.BridgeDeploying));
            Assert.That(
                walkway.enabled,
                Is.False,
                "Waehrend der Bewegung traegt der Stamm noch nicht.");

            // Die Bewegung dauert 2,2 s.
            Tick(25);

            Assert.That(
                puzzle.State, Is.EqualTo(BridgePuzzleState.Securing));
        }

        [Test]
        public void ErstDieBohlen_MachenDieBrueckeBegehbar()
        {
            CreatePuzzle();
            SolveUpToSecuring();

            Assert.That(
                walkway.enabled,
                Is.False,
                "Der blosse Stamm darf noch nicht begehbar sein — sonst " +
                "waeren die Bohlen Zierde.");

            Assert.That(puzzle.TryPlacePlank(), Is.True);

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.Securing),
                "Eine Bohle allein reicht nicht.");
            Assert.That(walkway.enabled, Is.False);

            Assert.That(puzzle.TryPlacePlank(), Is.True);

            Assert.That(puzzle.State, Is.EqualTo(BridgePuzzleState.Solved));
            Assert.That(
                walkway.enabled,
                Is.True,
                "Die gesicherte Bruecke muss begehbar sein.");
            Assert.That(puzzle.IsSolved, Is.True);
        }

        [Test]
        public void DerStamm_LiegtErstAmEndeSeinerBewegung()
        {
            CreatePuzzle();
            ObserveEcho();
            SetNotches(1, 2, 3);

            Vector3 stowed = puzzle.transform.Find("Stamm").position;

            puzzle.TryRelease();
            Tick(5);

            Vector3 midway = puzzle.transform.Find("Stamm").position;

            Assert.That(
                midway,
                Is.Not.EqualTo(stowed),
                "Der Stamm muss sich sichtbar bewegen.");

            Tick(30);

            Assert.That(
                Vector3.Distance(
                    puzzle.transform.Find("Stamm").position,
                    deployedPose.transform.position),
                Is.LessThan(0.01f),
                "Am Ende muss der Stamm in der Aufnahme liegen.");
        }

        // ------------------------------------------------------------------
        // Umkehrbarkeit und Robustheit
        // ------------------------------------------------------------------

        [Test]
        public void AlleFalschenKombinationen_SindUmkehrbarUndOhneSoftlock()
        {
            CreatePuzzle();
            ObserveEcho();

            int rejected = 0;

            for (int s = 1; s <= 3; s++)
            for (int m = 1; m <= 3; m++)
            for (int n = 1; n <= 3; n++)
            {
                if (BridgePuzzleRules.IsCorrect(s, m, n))
                {
                    continue;
                }

                SetNotches(s, m, n);

                Assert.That(
                    puzzle.TryRelease(),
                    Is.False,
                    $"Kombination {s}-{m}-{n} haette nicht tragen duerfen.");

                rejected++;

                // Sperre abwarten; danach muss es weitergehen.
                Tick(15);

                Assert.That(
                    puzzle.State,
                    Is.EqualTo(BridgePuzzleState.Configuring),
                    $"Nach Kombination {s}-{m}-{n} bleibt das Raetsel " +
                    "haengen.");
            }

            Assert.That(
                rejected,
                Is.EqualTo(BridgePuzzleRules.TotalCombinations - 1),
                "Es muessen 26 Kombinationen abgelehnt worden sein.");

            // Und die richtige geht danach immer noch.
            SetNotches(1, 2, 3);
            Assert.That(puzzle.TryRelease(), Is.True);
            Assert.That(consoleErrors, Is.Empty);
        }

        [Test]
        public void NachDemDrehen_GiltEineGeprueftteSpannungNichtMehr()
        {
            CreatePuzzle();
            ObserveEcho();
            SetNotches(1, 2, 3);

            Assert.That(puzzle.CheckTension(), Is.True);

            south.Turn();

            Assert.That(
                puzzle.IsConfigurationCorrect(),
                Is.False,
                "Vorbedingung: die Stellung wurde verstellt.");
            Assert.That(
                puzzle.TryRelease(),
                Is.False,
                "Eine vorher geprueftte Spannung darf nach dem Drehen nicht " +
                "weitergelten.");
        }

        [Test]
        public void DerSeilbock_MeldetSpannungOhneDieLoesungZuNennen()
        {
            CreatePuzzle();
            ObserveEcho();

            SetNotches(1, 2, 1);
            Assert.That(puzzle.CheckTension(), Is.False);

            SetNotches(1, 2, 3);
            Assert.That(puzzle.CheckTension(), Is.True);

            // Die Pruefung selbst veraendert nichts.
            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.Configuring),
                "Der Seilbock darf den Zustand nicht weiterschalten.");
        }

        [Test]
        public void MehrfachesEcho_SchaltetNichtDoppeltWeiter()
        {
            CreatePuzzle();
            puzzle.SetPlayerInZone(true);

            puzzle.NotifyEchoObserved();
            puzzle.NotifyEchoObserved();
            puzzle.NotifyEchoObserved();

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.EchoObserved),
                "Mehrfaches Ansehen darf den Fortschritt nicht verdoppeln.");
            Assert.That(consoleErrors, Is.Empty);
        }

        [Test]
        public void DieGeloesteBruecke_UeberstehtEinenSzenenwechsel()
        {
            CreatePuzzle();
            SolveUpToSecuring();
            puzzle.TryPlacePlank();
            puzzle.TryPlacePlank();

            Assert.That(puzzle.State, Is.EqualTo(BridgePuzzleState.Solved));

            // Alles wegwerfen und neu aufbauen — wie nach einem Szenenwechsel.
            foreach (GameObject instance in spawned)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            spawned.Clear();
            CreatePuzzle();

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.Solved),
                "Die geloeste Bruecke muss geloest bleiben.");
            Assert.That(
                walkway.enabled,
                Is.True,
                "Und sie muss weiterhin begehbar sein.");
        }

        [Test]
        public void EinLadenWaehrendDerBewegung_StelltKeineZwischengeometrieHer()
        {
            CreatePuzzle();
            ObserveEcho();
            SetNotches(1, 2, 3);
            puzzle.TryRelease();
            Tick(5);

            Assert.That(
                puzzle.State, Is.EqualTo(BridgePuzzleState.BridgeDeploying));

            foreach (GameObject instance in spawned)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            spawned.Clear();
            CreatePuzzle();

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.ReadyToRelease),
                "Ein Laden mitten in der Bewegung muss auf den letzten " +
                "stabilen Stand zurueckfallen.");
            Assert.That(walkway.enabled, Is.False);
        }

        // ------------------------------------------------------------------
        // Furt
        // ------------------------------------------------------------------

        [Test]
        public void DieFurt_SetztOhneJedenSchadenZurueck()
        {
            GameObject player = new GameObject("FurtTestSpieler");
            player.SetActive(false);
            spawned.Add(player);
            PlayerVitals vitals = player.AddComponent<PlayerVitals>();
            SphereCollider playerCollider = player.AddComponent<SphereCollider>();
            playerCollider.radius = 0.5f;
            player.SetActive(true);
            vitals.ResetToFull();

            GameObject fordObject = new GameObject("Furt");
            spawned.Add(fordObject);
            BoxCollider volume = fordObject.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = new Vector3(6f, 4f, 3f);

            GameObject safe = new GameObject("SicheresUfer");
            spawned.Add(safe);
            safe.transform.position = new Vector3(0f, 0f, -6f);

            FordReset ford = fordObject.AddComponent<FordReset>();
            SetPrivateField(ford, "safeReturn", safe.transform);

            float healthBefore = vitals.Health;
            float staminaBefore = vitals.Stamina;

            player.transform.position = Vector3.zero;
            Physics.SyncTransforms();

            // Den Auslöser direkt aufrufen: der Test soll nicht davon
            // abhaengen, wann Unity Trigger auswertet.
            ford.SendMessage("OnTriggerEnter", playerCollider);

            Assert.That(ford.IsCarrying, Is.True);

            ford.Tick(1.5f);

            Assert.That(
                ford.ResetCount, Is.EqualTo(1), "Es wurde nicht zurueckgesetzt.");
            Assert.That(
                player.transform.position,
                Is.EqualTo(safe.transform.position),
                "Aren muss am sicheren Ufer stehen.");
            Assert.That(
                vitals.Health,
                Is.EqualTo(healthBefore).Within(0.001f),
                "Die Furt darf keinen Schaden machen — Tod und Respawn sind " +
                "noch nicht entschieden.");
            Assert.That(
                vitals.Stamina,
                Is.EqualTo(staminaBefore).Within(0.001f),
                "Und auch keine Ausdauer kosten.");
            Assert.That(consoleErrors, Is.Empty);
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        private void CreatePuzzle()
        {
            GameObject root = new GameObject("BrueckenraetselTest");
            root.SetActive(false);
            spawned.Add(root);

            puzzle = root.AddComponent<BridgePuzzle>();
            SetPrivateField(puzzle, "puzzleId", PuzzleId);

            south = CreateAnchor(root, BridgeAnchorId.SouthDeep, "Anker_Sued");
            side = CreateAnchor(root, BridgeAnchorId.Side, "Anker_Seite");
            north = CreateAnchor(root, BridgeAnchorId.North, "Anker_Nord");

            GameObject log = new GameObject("Stamm");
            log.transform.SetParent(root.transform, false);
            log.transform.position = new Vector3(5f, 1f, 0f);

            deployedPose = new GameObject("Zielpose");
            deployedPose.transform.SetParent(root.transform, false);
            deployedPose.transform.position = new Vector3(0f, 0f, 0f);

            GameObject surface = new GameObject("Lauffläche");
            surface.transform.SetParent(log.transform, false);
            walkway = surface.AddComponent<BoxCollider>();
            walkway.enabled = false;

            GameObject plankA = new GameObject("Bohle_1");
            plankA.transform.SetParent(root.transform, false);
            plankA.SetActive(false);

            GameObject plankB = new GameObject("Bohle_2");
            plankB.transform.SetParent(root.transform, false);
            plankB.SetActive(false);

            SetPrivateField(puzzle, "fallenLog", log.transform);
            SetPrivateField(puzzle, "deployedPose", deployedPose.transform);
            SetPrivateField(puzzle, "walkway", walkway);
            SetPrivateArray(puzzle, "anchors", new Object[] { south, side, north });
            SetPrivateArray(
                puzzle, "plankVisuals", new Object[] { plankA, plankB });

            root.SetActive(true);
        }

        private BridgeAnchor CreateAnchor(
            GameObject root, BridgeAnchorId id, string name)
        {
            GameObject stone = new GameObject(name);
            stone.transform.SetParent(root.transform, false);

            SphereCollider trigger = stone.AddComponent<SphereCollider>();
            trigger.isTrigger = true;

            BridgeAnchor anchor = stone.AddComponent<BridgeAnchor>();
            SetPrivateField(anchor, "anchorId", (int)id);
            SetPrivateField(anchor, "puzzle", puzzle);

            return anchor;
        }

        private void ObserveEcho()
        {
            puzzle.SetPlayerInZone(true);
            puzzle.NotifyEchoObserved();
        }

        private void SolveUpToSecuring()
        {
            ObserveEcho();
            SetNotches(1, 2, 3);
            puzzle.TryRelease();
            Tick(30);

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.Securing),
                "Vorbedingung: der Stamm liegt.");
        }

        private void SetNotches(int southNotches, int sideNotches, int northNotches)
        {
            south.SetSetting(
                BridgePuzzleRules.SettingForNotches(southNotches), true);
            side.SetSetting(
                BridgePuzzleRules.SettingForNotches(sideNotches), true);
            north.SetSetting(
                BridgePuzzleRules.SettingForNotches(northNotches), true);
        }

        private void Tick(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                puzzle.Tick(Step);
            }
        }

        private static void SetPrivateField(
            Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Feld '{fieldName}' fehlt.");
            field.SetValue(target, value);
        }

        private static void SetPrivateArray(
            Object target, string fieldName, Object[] values)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, $"Feld '{fieldName}' fehlt.");

            System.Array typed = System.Array.CreateInstance(
                field.FieldType.GetElementType()!, values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                typed.SetValue(values[i], i);
            }

            field.SetValue(target, typed);
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
