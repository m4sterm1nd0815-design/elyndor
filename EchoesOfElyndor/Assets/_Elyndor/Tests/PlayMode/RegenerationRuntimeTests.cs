using System.Collections;
using System.Collections.Generic;
using Elyndor.Memory;
using Elyndor.Puzzles;
using Elyndor.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Prüft, dass der Wald erst auf ein Verstehen reagiert — und dann genau
    /// einmal.
    ///
    /// Zwei Bedingungen, nicht eine: Rätsel und Memory Site laufen im Code
    /// getrennt, also werden beide verlangt. Ein Wald, der auf einen Schritt
    /// in einen Trigger reagiert, hat nichts verstanden.
    /// </summary>
    public sealed class RegenerationRuntimeTests
    {
        private const string StateId = "test_region_regeneration";

        /// <summary>
        /// Je Test eine eigene ID: MemorySessionState laesst sich nicht
        /// leeren, und ein Test darf nicht davon abhaengen, was ein anderer
        /// vorher als gesehen markiert hat.
        /// </summary>
        private string siteId;

        private readonly List<GameObject> spawned = new List<GameObject>();

        private FinsterwaldRegeneration regeneration;
        private BridgePuzzle puzzle;
        private GameObject shoot;
        private Collider path;

        [SetUp]
        public void SetUp()
        {
            siteId = "regen_test_site_" + System.Guid.NewGuid().ToString("N");
            RegionRegenerationState.Forget(StateId);
            PuzzleSessionState.Forget("regen_test_bridge");
        }

        [TearDown]
        public void TearDown()
        {
            RegionRegenerationState.Forget(StateId);
            PuzzleSessionState.Forget("regen_test_bridge");

            foreach (GameObject instance in spawned)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            spawned.Clear();
        }

        // ------------------------------------------------------------------

        [Test]
        public void OhneBeideBedingungen_PassiertNichts()
        {
            Create();

            regeneration.Tick();

            Assert.That(regeneration.IsRegenerated, Is.False);
            Assert.That(shoot.activeSelf, Is.False);

            // Nur das Raetsel: reicht nicht.
            SolvePuzzle();
            regeneration.Tick();

            Assert.That(
                regeneration.IsRegenerated,
                Is.False,
                "Das geloeste Raetsel allein darf den Wald nicht veraendern.");

            // Nur die Erinnerung: reicht auch nicht.
            RegionRegenerationState.Forget(StateId);
            MemorySessionState.MarkActivated(siteId);

            regeneration.Tick();

            Assert.That(
                regeneration.IsRegenerated,
                Is.True,
                "Mit beiden Bedingungen muss der Wald antworten.");
        }

        [Test]
        public void MitBeidenBedingungen_KehrtDasLebenZurueck()
        {
            Create();
            SolvePuzzle();
            MemorySessionState.MarkActivated(siteId);

            Assert.That(regeneration.ConditionsMet, Is.True);

            regeneration.Tick();

            Assert.That(regeneration.IsRegenerated, Is.True);
            Assert.That(
                shoot.activeSelf,
                Is.True,
                "Die Triebe muessen erscheinen.");
            Assert.That(
                path.enabled,
                Is.False,
                "Der eine vorgesehene Weg muss sich oeffnen.");
        }

        [Test]
        public void MehrfachesAusloesen_VeraendertNichtsZusaetzlich()
        {
            Create();
            SolvePuzzle();
            MemorySessionState.MarkActivated(siteId);

            int events = 0;
            void Listener() => events++;

            FinsterwaldRegeneration.AnyRegionRegenerated += Listener;

            try
            {
                regeneration.Regenerate();
                regeneration.Regenerate();
                regeneration.Tick();
                regeneration.Tick();

                Assert.That(
                    events,
                    Is.EqualTo(1),
                    "Der Wald darf nur einmal antworten.");
            }
            finally
            {
                FinsterwaldRegeneration.AnyRegionRegenerated -= Listener;
            }
        }

        [Test]
        public void NachEinemSzenenwechsel_BleibtDerWaldVeraendert()
        {
            Create();
            SolvePuzzle();
            MemorySessionState.MarkActivated(siteId);
            regeneration.Regenerate();

            Assert.That(
                RegionRegenerationState.IsRegenerated(StateId), Is.True);

            // Alles wegwerfen und neu aufbauen.
            foreach (GameObject instance in spawned)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            spawned.Clear();

            int events = 0;
            void Listener() => events++;

            FinsterwaldRegeneration.AnyRegionRegenerated += Listener;

            try
            {
                Create();

                Assert.That(
                    regeneration.IsRegenerated,
                    Is.True,
                    "Der wiederhergestellte Abschnitt muss regeneriert sein.");
                Assert.That(
                    shoot.activeSelf,
                    Is.True,
                    "Und sichtbar bleiben.");
                Assert.That(
                    events,
                    Is.Zero,
                    "Eine Wiederherstellung ist kein neues Ereignis — sonst " +
                    "klaenge der Ton bei jedem Betreten erneut.");
            }
            finally
            {
                FinsterwaldRegeneration.AnyRegionRegenerated -= Listener;
            }
        }

        [Test]
        public void DerVertragFuerSpaeter_WirdBenutztWennErDaIst()
        {
            var store = new RecordingStore();
            RegionRegenerationState.Attach(store);

            try
            {
                Create();
                SolvePuzzle();
                MemorySessionState.MarkActivated(siteId);
                regeneration.Regenerate();

                Assert.That(
                    store.Written,
                    Does.Contain(StateId),
                    "Ein eingehaengter Speicher muss den Stand bekommen.");
            }
            finally
            {
                RegionRegenerationState.Attach(null);
            }
        }

        // ------------------------------------------------------------------
        // Szene
        // ------------------------------------------------------------------

        /// <summary>
        /// Portale, Spawns und Memory Sites gehören zur Handarbeit der Szene.
        /// Die Regeneration darf sie nicht anfassen.
        /// </summary>
        [UnityTest]
        public IEnumerator DieRegeneration_LaesstDieHandarbeitInRuhe()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(
                "Finsterwald", LoadSceneMode.Single);

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;

            FinsterwaldRegeneration scene =
                Object.FindAnyObjectByType<FinsterwaldRegeneration>();

            Assert.That(
                scene, Is.Not.Null, "Die Regeneration fehlt in der Szene.");

            int portals = Object.FindObjectsByType<RegionPortal>(
                FindObjectsInactive.Include).Length;
            int spawns = Object.FindObjectsByType<RegionSpawnPoint>(
                FindObjectsInactive.Include).Length;
            int sites = Object.FindObjectsByType<MemorySite>(
                FindObjectsInactive.Include).Length;

            scene.Regenerate();

            yield return null;

            Assert.That(
                Object.FindObjectsByType<RegionPortal>(
                    FindObjectsInactive.Include).Length,
                Is.EqualTo(portals), "Portale veraendert.");
            Assert.That(
                Object.FindObjectsByType<RegionSpawnPoint>(
                    FindObjectsInactive.Include).Length,
                Is.EqualTo(spawns), "Spawns veraendert.");
            Assert.That(
                Object.FindObjectsByType<MemorySite>(
                    FindObjectsInactive.Include).Length,
                Is.EqualTo(sites), "Memory Sites veraendert.");

            RegionRegenerationState.Forget(scene.RegionStateId);
        }

        // ------------------------------------------------------------------

        private sealed class RecordingStore : IRegionStateStore
        {
            public readonly List<string> Written = new List<string>();

            public bool IsRegenerated(string regionStateId) =>
                Written.Contains(regionStateId);

            public void SetRegenerated(string regionStateId, bool value)
            {
                if (value)
                {
                    Written.Add(regionStateId);
                }
            }
        }

        private void Create()
        {
            GameObject root = new GameObject("RegenTest");
            root.SetActive(false);
            spawned.Add(root);

            shoot = new GameObject("Trieb");
            shoot.transform.SetParent(root.transform, false);
            shoot.SetActive(false);

            GameObject gate = new GameObject("Weg");
            gate.transform.SetParent(root.transform, false);
            path = gate.AddComponent<BoxCollider>();

            puzzle = CreatePuzzle(root);

            regeneration = root.AddComponent<FinsterwaldRegeneration>();
            SetPrivateField(regeneration, "regionStateId", StateId);
            SetPrivateField(regeneration, "memorySiteId", siteId);

            regeneration.Configure(
                puzzle,
                new[] { shoot },
                new Renderer[0],
                new Renderer[0],
                null,
                path);

            root.SetActive(true);
            regeneration.enabled = false;
        }

        private BridgePuzzle CreatePuzzle(GameObject root)
        {
            GameObject host = new GameObject("Raetsel");
            host.transform.SetParent(root.transform, false);

            BridgePuzzle created = host.AddComponent<BridgePuzzle>();
            SetPrivateField(created, "puzzleId", "regen_test_bridge");

            var anchors = new Object[3];
            var ids = new[]
            {
                BridgeAnchorId.SouthDeep, BridgeAnchorId.Side,
                BridgeAnchorId.North
            };

            for (int i = 0; i < ids.Length; i++)
            {
                GameObject stone = new GameObject("Anker" + i);
                stone.transform.SetParent(host.transform, false);
                stone.AddComponent<SphereCollider>().isTrigger = true;

                BridgeAnchor anchor = stone.AddComponent<BridgeAnchor>();
                SetPrivateField(anchor, "anchorId", (int)ids[i]);
                anchors[i] = anchor;
            }

            SetPrivateArray(created, "anchors", anchors);

            GameObject log = new GameObject("Stamm");
            log.transform.SetParent(host.transform, false);
            GameObject pose = new GameObject("Pose");
            pose.transform.SetParent(host.transform, false);

            SetPrivateField(created, "fallenLog", log.transform);
            SetPrivateField(created, "deployedPose", pose.transform);

            return created;
        }

        private void SolvePuzzle()
        {
            puzzle.SetPlayerInZone(true);
            puzzle.NotifyEchoObserved();

            foreach (BridgeAnchor anchor in
                     puzzle.GetComponentsInChildren<BridgeAnchor>(true))
            {
                anchor.SetSetting(
                    BridgePuzzleRules.SettingForNotches(
                        BridgePuzzleRules.RequiredNotches(anchor.AnchorId)),
                    true);
            }

            puzzle.TryRelease();

            for (int i = 0; i < 40; i++)
            {
                puzzle.Tick(0.1f);
            }

            for (int i = 0; i < puzzle.RequiredPlanks; i++)
            {
                puzzle.TryPlacePlank();
            }

            Assert.That(
                puzzle.IsSolved, Is.True, "Vorbedingung: Raetsel geloest.");
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

            System.Array typed = System.Array.CreateInstance(
                field.FieldType.GetElementType()!, values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                typed.SetValue(values[i], i);
            }

            field.SetValue(target, typed);
        }
    }
}
