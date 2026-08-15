using System.Collections;
using System.Collections.Generic;
using Elyndor.Companion;
using Elyndor.Puzzles;
using Elyndor.UIFoundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Prüft Link, den Begleiter.
    ///
    /// Der wichtigste Test ist der letzte: <b>Links Platzwahl darf nicht davon
    /// abhängen, wie die Anker stehen.</b> Täte sie es, könnte ein Spieler das
    /// Rätsel lösen, indem er der Eule zusieht — und das Rätsel wäre keines
    /// mehr. Die Garantie liegt in der Bauweise: <c>LinkCompanion</c> kennt
    /// weder Rätsel noch Anker. Der Test hält fest, dass das so bleibt.
    /// </summary>
    public sealed class LinkCompanionRuntimeTests
    {
        private const float Step = 0.1f;

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<string> consoleErrors = new List<string>();

        private LinkCompanion link;
        private GameObject player;

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

            foreach (GameObject instance in spawned)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            spawned.Clear();
            link = null;
        }

        // ------------------------------------------------------------------

        [Test]
        public void Link_SitztZuBeginnAufEinemSitzpunkt()
        {
            CreateLink(perchCount: 3);

            Assert.That(link.CurrentPerch, Is.Not.Null);
            Assert.That(link.State, Is.EqualTo(LinkState.Perched));
            Assert.That(
                link.transform.position,
                Is.EqualTo(link.CurrentPerch.transform.position));
        }

        [Test]
        public void Link_WechseltDenSitzpunkt_WennArenSichBewegt()
        {
            CreateLink(perchCount: 3);

            LinkPerch first = link.CurrentPerch;

            // Aren geht zu einem anderen Sitzpunkt hinueber.
            player.transform.position = new Vector3(20f, 0f, 0f);

            Tick(120);

            Assert.That(
                link.PerchChanges,
                Is.GreaterThan(0),
                "Link bleibt kleben, statt Aren zu folgen.");
            Assert.That(link.CurrentPerch, Is.Not.EqualTo(first));
            Assert.That(link.State, Is.EqualTo(LinkState.Perched));
        }

        [Test]
        public void OhneSitzpunkte_HaeltLinkSichInDerNaehe()
        {
            CreateLink(perchCount: 0);

            Tick(20);

            Assert.That(
                link.State,
                Is.EqualTo(LinkState.Fallback),
                "Ohne Sitzpunkt braucht Link eine sichere Rueckfalllage.");
            Assert.That(
                Vector3.Distance(link.transform.position, player.transform.position),
                Is.LessThan(8f),
                "Im Rueckfall darf Link nicht verschwinden.");
            Assert.That(consoleErrors, Is.Empty);
        }

        [Test]
        public void EinEinzigerSitzpunkt_FuehrtZuKeinemSoftlock()
        {
            CreateLink(perchCount: 1);

            Tick(200);

            Assert.That(
                link.State,
                Is.Not.EqualTo(LinkState.Flying),
                "Link bleibt im Flug haengen.");
            Assert.That(consoleErrors, Is.Empty);
        }

        [Test]
        public void Link_HatKeinenCollider()
        {
            CreateLink(perchCount: 2);

            Assert.That(
                link.GetComponentsInChildren<Collider>(true),
                Is.Empty,
                "Ein Begleiter mit Collider kann Aren den Weg versperren.");
        }

        [Test]
        public void ZuNaheSitzpunkte_WerdenNichtGewaehlt()
        {
            CreateLink(perchCount: 3);

            // Aren stellt sich direkt unter den zweiten Sitzpunkt.
            player.transform.position = PerchPosition(1);

            LinkPerch choice = link.ChoosePerch();

            Assert.That(
                choice,
                Is.Not.EqualTo(PerchAt(1)),
                "Eine Eule setzt sich einem nicht auf die Schulter.");
        }

        /// <summary>
        /// Der Kern der Sache: dieselbe Lage, verschiedene Ankerstellungen —
        /// dieselbe Wahl.
        /// </summary>
        [Test]
        public void DiePlatzwahl_HaengtNichtVonDenAnkernAb()
        {
            CreateLink(perchCount: 4);
            BridgePuzzle puzzle = CreatePuzzleWithAnchors();

            player.transform.position = new Vector3(8f, 0f, 4f);

            var choices = new List<LinkPerch>();

            for (int south = 1; south <= 3; south++)
            for (int side = 1; side <= 3; side++)
            for (int north = 1; north <= 3; north++)
            {
                SetNotches(puzzle, south, side, north);
                choices.Add(link.ChoosePerch());
            }

            Assert.That(
                choices.Count,
                Is.EqualTo(BridgePuzzleRules.TotalCombinations));

            foreach (LinkPerch choice in choices)
            {
                Assert.That(
                    choice,
                    Is.EqualTo(choices[0]),
                    "Links Wahl aendert sich mit der Ankerstellung — damit " +
                    "waere die Loesung an ihm ablesbar.");
            }
        }

        // ------------------------------------------------------------------
        // Szene
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator DieSzene_HatGenauEinenLink()
        {
            yield return LoadFinsterwald();

            LinkCompanion[] all = Object.FindObjectsByType<LinkCompanion>(FindObjectsInactive.Include);

            Assert.That(
                all.Length, Is.EqualTo(1), "Es darf genau eine Eule geben.");

            LinkPerch[] perches = Object.FindObjectsByType<LinkPerch>(FindObjectsInactive.Include);

            Assert.That(
                perches.Length,
                Is.GreaterThanOrEqualTo(3),
                "Link braucht Sitzpunkte an mehreren Orten des Slice.");

            Assert.That(
                all[0].GetComponentsInChildren<Collider>(true),
                Is.Empty,
                "Link darf in der Szene niemanden blockieren.");
        }

        /// <summary>
        /// Das Rätsel gehört dem Spieler, nicht der Eule. Ohne Link muss es
        /// vollständig lösbar bleiben.
        /// </summary>
        [UnityTest]
        public IEnumerator OhneLink_BleibtDasRaetselLoesbar()
        {
            yield return LoadFinsterwald();

            foreach (LinkCompanion companion in
                     Object.FindObjectsByType<LinkCompanion>(FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(companion.gameObject);
            }

            BridgePuzzle puzzle = Object.FindAnyObjectByType<BridgePuzzle>();
            Assert.That(puzzle, Is.Not.Null);

            puzzle.SetPlayerInZone(true);
            puzzle.NotifyEchoObserved();

            foreach (BridgeAnchor anchor in
                     Object.FindObjectsByType<BridgeAnchor>(FindObjectsInactive.Include))
            {
                int notches = BridgePuzzleRules.RequiredNotches(anchor.AnchorId);
                anchor.SetSetting(
                    BridgePuzzleRules.SettingForNotches(notches), true);
            }

            Assert.That(puzzle.IsConfigurationCorrect(), Is.True);
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
                puzzle.State,
                Is.EqualTo(BridgePuzzleState.Solved),
                "Ohne Link liess sich das Raetsel nicht loesen.");

            PuzzleSessionState.Forget(puzzle.PuzzleId);
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        private void CreateLink(int perchCount)
        {
            // Link sucht sich bei leerem Feld die Sitzpunkte der Szene. Das
            // ist im Spiel richtig, macht diesen Test aber davon abhaengig,
            // welche Szene ein vorheriger Test geladen hat.
            foreach (LinkPerch leftover in
                     Object.FindObjectsByType<LinkPerch>(
                         FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(leftover.gameObject);
            }

            player = new GameObject("LinkTestSpieler");
            spawned.Add(player);
            player.AddComponent<PlayerVitals>();
            player.transform.position = Vector3.zero;

            var perches = new LinkPerch[perchCount];

            for (int i = 0; i < perchCount; i++)
            {
                GameObject perchObject = new GameObject("Sitz_" + i);
                spawned.Add(perchObject);
                perchObject.transform.position = PerchPosition(i);
                perches[i] = perchObject.AddComponent<LinkPerch>();
            }

            GameObject root = new GameObject("LinkTest");
            root.SetActive(false);
            spawned.Add(root);

            link = root.AddComponent<LinkCompanion>();
            SetPrivateField(link, "player", player.transform);
            SetPrivateField(link, "perches", perches);

            root.SetActive(true);
            link.enabled = false;
        }

        private static Vector3 PerchPosition(int index) =>
            new Vector3(index * 9f, 3f, index * 4f);

        private LinkPerch PerchAt(int index)
        {
            foreach (GameObject candidate in spawned)
            {
                if (candidate != null && candidate.name == "Sitz_" + index)
                {
                    return candidate.GetComponent<LinkPerch>();
                }
            }

            return null;
        }

        private BridgePuzzle CreatePuzzleWithAnchors()
        {
            GameObject root = new GameObject("LinkTestRaetsel");
            root.SetActive(false);
            spawned.Add(root);

            BridgePuzzle puzzle = root.AddComponent<BridgePuzzle>();
            SetPrivateField(puzzle, "puzzleId", "link_test_bridge");

            var anchors = new Object[3];
            var ids = new[]
            {
                BridgeAnchorId.SouthDeep, BridgeAnchorId.Side,
                BridgeAnchorId.North
            };

            for (int i = 0; i < ids.Length; i++)
            {
                GameObject stone = new GameObject("Anker_" + ids[i]);
                stone.transform.SetParent(root.transform, false);
                stone.AddComponent<SphereCollider>().isTrigger = true;

                BridgeAnchor anchor = stone.AddComponent<BridgeAnchor>();
                SetPrivateField(anchor, "anchorId", (int)ids[i]);
                anchors[i] = anchor;
            }

            SetPrivateArray(puzzle, "anchors", anchors);
            root.SetActive(true);

            return puzzle;
        }

        private static void SetNotches(
            BridgePuzzle puzzle, int south, int side, int north)
        {
            foreach (BridgeAnchor anchor in
                     puzzle.GetComponentsInChildren<BridgeAnchor>(true))
            {
                int notches = anchor.AnchorId switch
                {
                    BridgeAnchorId.SouthDeep => south,
                    BridgeAnchorId.Side => side,
                    _ => north
                };

                anchor.SetSetting(
                    BridgePuzzleRules.SettingForNotches(notches), false);
            }
        }

        private void Tick(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                link.Tick(Step);
            }
        }

        private static IEnumerator LoadFinsterwald()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(
                "Finsterwald", LoadSceneMode.Single);

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
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
