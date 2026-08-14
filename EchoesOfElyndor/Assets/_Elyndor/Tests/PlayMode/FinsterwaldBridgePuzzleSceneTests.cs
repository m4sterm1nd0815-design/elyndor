using System;
using System.Collections;
using System.Collections.Generic;
using Elyndor.Memory;
using Elyndor.Puzzles;
using Elyndor.UIFoundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Abnahme des Brückenrätsels im echten Finsterwald.
    ///
    /// Die Laufzeittests prüfen die Regeln an einem zusammengesetzten Rätsel.
    /// Hier geht es um die Fragen, die nur die Szene beantworten kann: steht
    /// alles da, wo es hingehört, hängt nichts aneinander vorbei, und lässt
    /// sich das Rätsel von Anfang bis Ende tatsächlich lösen.
    /// </summary>
    public sealed class FinsterwaldBridgePuzzleSceneTests
    {
        private const string SceneName = "Finsterwald";

        private readonly List<string> consoleErrors = new List<string>();

        private BridgePuzzle puzzle;

        [SetUp]
        public void SetUp()
        {
            consoleErrors.Clear();
            Application.logMessageReceived += CollectConsoleError;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= CollectConsoleError;

            if (puzzle != null)
            {
                PuzzleSessionState.Forget(puzzle.PuzzleId);
            }

            puzzle = null;
        }

        [UnityTest]
        public IEnumerator DasRaetsel_StehtVollstaendigInDerSzene()
        {
            yield return LoadScene();

            BridgePuzzle[] puzzles =
                UnityEngine.Object.FindObjectsByType<BridgePuzzle>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(
                puzzles.Length,
                Is.EqualTo(1),
                "Es muss genau ein Brueckenraetsel geben.");

            puzzle = puzzles[0];

            BridgeAnchor[] anchors =
                UnityEngine.Object.FindObjectsByType<BridgeAnchor>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(
                anchors.Length,
                Is.EqualTo(BridgePuzzleRules.AnchorCount),
                "Das Raetsel braucht genau drei Anker.");

            foreach (BridgeAnchorId id in
                     (BridgeAnchorId[])Enum.GetValues(typeof(BridgeAnchorId)))
            {
                Assert.That(
                    puzzle.NotchesOf(id),
                    Is.InRange(1, 3),
                    $"Anker {id} ist nicht am Raetsel angeschlossen.");
            }

            Assert.That(
                puzzle.IsConfigurationCorrect(),
                Is.False,
                "Das Raetsel darf nicht bereits geloest beginnen.");

            Assert.That(
                UnityEngine.Object.FindAnyObjectByType<WatchResonanceZone>(),
                Is.Not.Null,
                "Ohne Resonanzzone meldet die Watch nie Bereitschaft.");
            Assert.That(
                UnityEngine.Object.FindAnyObjectByType<FordReset>(),
                Is.Not.Null,
                "Die Furt braucht ihre schadenslose Ruecksetzung.");
        }

        /// <summary>
        /// Der wichtigste Aufbaufehler, den nur die Szene zeigt: Läge die
        /// Memory Site außerhalb der Resonanzzone, käme der Spieler nie über
        /// den ersten Schritt hinaus — er stünde am Echo, und das Rätsel bliebe
        /// stumm.
        /// </summary>
        [UnityTest]
        public IEnumerator DieMemorySite_LiegtInnerhalbDerResonanzzone()
        {
            yield return LoadScene();

            WatchResonanceZone zone =
                UnityEngine.Object.FindAnyObjectByType<WatchResonanceZone>();
            MemorySite site =
                UnityEngine.Object.FindAnyObjectByType<MemorySite>();

            Assert.That(zone, Is.Not.Null);
            Assert.That(site, Is.Not.Null);

            Collider volume = zone.GetComponent<Collider>();

            Assert.That(
                volume.bounds.Contains(site.transform.position),
                Is.True,
                "Die Memory Site der alten Bruecke muss in der Resonanzzone " +
                "liegen, sonst ist das Raetsel nicht anzustossen.");
        }

        [UnityTest]
        public IEnumerator DasRaetsel_LaesstSichVonAnfangBisEndeLoesen()
        {
            yield return LoadScene();

            puzzle = UnityEngine.Object.FindAnyObjectByType<BridgePuzzle>();
            Assert.That(puzzle, Is.Not.Null);

            // Schritt 1: Aren steht an der Bruecke und sieht das Echo.
            puzzle.SetPlayerInZone(true);
            puzzle.NotifyEchoObserved();

            Assert.That(
                puzzle.CanTurnAnchors,
                Is.True,
                "Nach dem Echo muessen die Anker drehbar sein.");

            // Schritt 2: die Anker auf den Lastverlauf stellen.
            SetAnchors(1, 2, 3);

            Assert.That(
                puzzle.IsConfigurationCorrect(),
                Is.True,
                "Sued 1, Seite 2, Nord 3 muss tragen.");
            Assert.That(puzzle.CheckTension(), Is.True);

            // Schritt 3: den Stamm freigeben.
            Assert.That(puzzle.TryRelease(), Is.True);

            float deadline = Time.time + 10f;

            while (puzzle.State == BridgePuzzleState.BridgeDeploying &&
                   Time.time < deadline)
            {
                yield return null;
            }

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.Securing),
                "Der Stamm ist nicht eingerastet.");

            // Schritt 4: die Bohlen legen.
            for (int i = 0; i < puzzle.RequiredPlanks; i++)
            {
                Assert.That(
                    puzzle.TryPlacePlank(),
                    Is.True,
                    $"Bohle {i + 1} liess sich nicht legen.");
            }

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.Solved),
                "Das Raetsel liess sich nicht zu Ende loesen.");

            yield return null;

            Assert.That(
                consoleErrors,
                Is.Empty,
                "Das Loesen hinterlaesst rote Meldungen:\n" +
                string.Join("\n", consoleErrors));
        }

        /// <summary>
        /// Die Töne hängen an der vorhandenen zentralen Bibliothek. Fehlte
        /// dort ein Clip, bliebe die Regel stumm — und stumm ist von „gibt es
        /// nicht" im Spiel nicht zu unterscheiden.
        /// </summary>
        [UnityTest]
        public IEnumerator DieSzene_HatAlleLesbarkeitstoeneVerdrahtet()
        {
            yield return LoadScene();

            Elyndor.Core.SfxLibrary library =
                UnityEngine.Object.FindAnyObjectByType<Elyndor.Core.SfxLibrary>();

            Assert.That(
                library,
                Is.Not.Null,
                "Ohne SfxLibrary bleibt jede Regel stumm.");

            string[] fields =
            {
                "enemyTelegraphClip",
                "enemyHitLightClip",
                "enemyHitHeavyClip",
                "enemyDefeatedClip",
                "blockedHitClip",
                "unblockedHitClip",
                "bridgeTensionHoldsClip",
                "bridgeTensionSlackClip"
            };

            foreach (string field in fields)
            {
                var info = typeof(Elyndor.Core.SfxLibrary).GetField(
                    field,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

                Assert.That(info, Is.Not.Null, $"Feld '{field}' fehlt.");
                Assert.That(
                    info.GetValue(library),
                    Is.Not.Null,
                    $"Der Hinweis '{field}' hat keinen Clip.");
            }
        }

        /// <summary>
        /// Ein Spieler muss die Stellung eines Ankers <em>zählen</em> können.
        ///
        /// Zuerst trug jeder Anker alle Kerbengruppen gleichzeitig, und aus
        /// jedem Blickwinkel waren Teile mehrerer Gruppen zu sehen — der Stein
        /// wirkte umwickelt, und zählen liess sich nichts. Sichtbar ist
        /// deshalb immer genau eine Gruppe, und ihre Kerbenzahl muss zur
        /// Stellung passen.
        /// </summary>
        [UnityTest]
        public IEnumerator JedeAnkerstellung_ZeigtIhreKerbenzahl()
        {
            yield return LoadScene();

            BridgeAnchor[] anchors =
                UnityEngine.Object.FindObjectsByType<BridgeAnchor>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(
                anchors.Length,
                Is.EqualTo(BridgePuzzleRules.AnchorCount));

            foreach (BridgeAnchor anchor in anchors)
            {
                for (int setting = 0;
                     setting < BridgePuzzleRules.SettingsPerAnchor;
                     setting++)
                {
                    anchor.SetSetting(setting, false);

                    int activeGroups = 0;
                    int visibleMarks = 0;

                    foreach (Transform child in anchor.transform)
                    {
                        if (!child.name.StartsWith("Kerben_"))
                        {
                            continue;
                        }

                        if (child.gameObject.activeSelf)
                        {
                            activeGroups++;
                            visibleMarks += child.childCount;
                        }
                    }

                    Assert.That(
                        activeGroups,
                        Is.EqualTo(1),
                        $"{anchor.AnchorId} zeigt in Stellung {setting} " +
                        $"{activeGroups} Kerbengruppen statt einer.");
                    Assert.That(
                        visibleMarks,
                        Is.EqualTo(anchor.Notches),
                        $"{anchor.AnchorId} zeigt in Stellung {setting} " +
                        $"{visibleMarks} Kerben, angezeigt wird aber " +
                        $"{anchor.Notches}.");
                }
            }
        }

        /// <summary>
        /// Die drei Anker müssen sich voneinander unterscheiden lassen, ohne
        /// dass man auf sie zeigt. Drei gleiche Zylinder wären ordentlich und
        /// unbrauchbar.
        /// </summary>
        [UnityTest]
        public IEnumerator DieDreiAnker_SindUnterscheidbar()
        {
            yield return LoadScene();

            BridgeAnchor[] anchors =
                UnityEngine.Object.FindObjectsByType<BridgeAnchor>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int a = 0; a < anchors.Length; a++)
            {
                for (int b = a + 1; b < anchors.Length; b++)
                {
                    Assert.That(
                        anchors[a].transform.localScale,
                        Is.Not.EqualTo(anchors[b].transform.localScale),
                        $"{anchors[a].AnchorId} und {anchors[b].AnchorId} " +
                        "haben dieselbe Form.");

                    Assert.That(
                        Vector3.Distance(
                            anchors[a].transform.position,
                            anchors[b].transform.position),
                        Is.GreaterThan(2f),
                        $"{anchors[a].AnchorId} und {anchors[b].AnchorId} " +
                        "stehen zu dicht beieinander, um raeumlich " +
                        "unterschieden zu werden.");
                }
            }
        }

        [UnityTest]
        public IEnumerator EinFehlversuch_KostetInDerSzeneNichts()
        {
            yield return LoadScene();

            puzzle = UnityEngine.Object.FindAnyObjectByType<BridgePuzzle>();
            GameObject player = GameObject.Find("Player");
            PlayerVitals vitals = player.GetComponent<PlayerVitals>();

            vitals.ResetToFull();
            float healthBefore = vitals.Health;

            puzzle.SetPlayerInZone(true);
            puzzle.NotifyEchoObserved();
            SetAnchors(3, 2, 1);

            Assert.That(puzzle.TryRelease(), Is.False);
            Assert.That(
                puzzle.State, Is.EqualTo(BridgePuzzleState.Recovering));

            float deadline = Time.time + 5f;

            while (puzzle.State == BridgePuzzleState.Recovering &&
                   Time.time < deadline)
            {
                yield return null;
            }

            Assert.That(
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.Configuring),
                "Nach dem Fehlversuch muss weiterprobiert werden koennen.");
            Assert.That(
                vitals.Health,
                Is.EqualTo(healthBefore).Within(0.001f),
                "Ein Fehlversuch am Raetsel darf nichts kosten.");
            Assert.That(consoleErrors, Is.Empty);
        }

        // ------------------------------------------------------------------

        private void SetAnchors(int south, int side, int north)
        {
            foreach (BridgeAnchor anchor in
                     UnityEngine.Object.FindObjectsByType<BridgeAnchor>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                int notches = anchor.AnchorId switch
                {
                    BridgeAnchorId.SouthDeep => south,
                    BridgeAnchorId.Side => side,
                    _ => north
                };

                anchor.SetSetting(
                    BridgePuzzleRules.SettingForNotches(notches), true);
            }
        }

        private static IEnumerator LoadScene()
        {
            AsyncOperation load =
                SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
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
