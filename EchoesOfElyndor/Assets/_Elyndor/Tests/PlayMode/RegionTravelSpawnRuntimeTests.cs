using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Elyndor.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Regressionstest fuer die Rueckreise in den Finsterwald.
    ///
    /// Frueher hat eine zweite Komponente Arens Startpose beim Szenenstart
    /// bedingungslos gesetzt und damit <see cref="RegionSpawnPoint"/>
    /// ueberschrieben. Ob der Spieler am Portal oder an der normalen
    /// Waldstartpose landete, entschied allein die undefinierte
    /// Start()-Reihenfolge. Dieser Test haelt fest, dass die Reise am
    /// zugehoerigen Spawn endet.
    ///
    /// Szenenname, Spawn-ID und Sollposition stammen aus der Szene selbst:
    /// aus dem vorhandenen Rueckreise-Portal und dem passenden
    /// <see cref="RegionSpawnPoint"/>.
    /// </summary>
    public sealed class RegionTravelSpawnRuntimeTests
    {
        /// <summary>Die Region, deren Ankunft geprueft wird.</summary>
        private const string RegionUnderTest = "Finsterwald";

        /// <summary>Zulaessiger Abstand zum Spawn in der Ebene.</summary>
        private const float ArrivalTolerance = 1.5f;

        /// <summary>Mindestabstand, der Spawn und Startpose unterscheidbar macht.</summary>
        private const float DistinctPoseDistance = 5f;

        private readonly List<string> consoleErrors = new List<string>();

        private Vector3 regularStartPose;
        private string neighbourScene;

        [UnitySetUp]
        public IEnumerator LoadRegionWithoutPendingSpawn()
        {
            consoleErrors.Clear();
            Application.logMessageReceived += CollectConsoleError;

            RegionTravel.ClearPendingSpawn();

            yield return LoadScene(RegionUnderTest);
            yield return WaitUntilSettled();

            // Ohne offenen Spawn gilt die normale Startpose der Szene.
            regularStartPose = RequirePlayer().position;

            RegionPortal outbound =
                Object.FindAnyObjectByType<RegionPortal>();

            Assert.That(
                outbound,
                Is.Not.Null,
                $"{RegionUnderTest} hat kein Regionsportal.");

            neighbourScene = outbound.TargetSceneName;

            Assert.That(
                neighbourScene,
                Is.Not.Null.And.Not.Empty,
                "Das Regionsportal nennt keine Zielszene.");
        }

        [UnityTearDown]
        public IEnumerator ClearPendingSpawnAfterwards()
        {
            Application.logMessageReceived -= CollectConsoleError;
            RegionTravel.ClearPendingSpawn();
            yield break;
        }

        /// <summary>
        /// Sammelt ausschliesslich rote Console-Meldungen. Bewusst nicht ueber
        /// LogAssert.NoUnexpectedReceived: das wuerde auch an Warnungen
        /// scheitern, die andere Tests der Suite hinterlassen, und damit von
        /// der Ausfuehrungsreihenfolge abhaengen.
        /// </summary>
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

        [UnityTest]
        public IEnumerator TravelBackFromTheNeighbourRegionEndsAtItsSpawnPoint()
        {
            yield return LoadScene(neighbourScene);

            RegionPortal returnPortal = FindPortalTo(RegionUnderTest);

            Assert.That(
                returnPortal,
                Is.Not.Null,
                $"{neighbourScene} hat kein Portal zurueck nach " +
                $"{RegionUnderTest}.");

            string expectedSpawnId = returnPortal.TargetSpawnId;

            Assert.That(
                expectedSpawnId,
                Is.Not.Null.And.Not.Empty,
                "Das Rueckreise-Portal nennt keine Spawn-ID.");

            // Der reale Reiseweg: genau das, was RegionPortal.Interact aufruft.
            int previousPlayerId = RequirePlayer().gameObject.GetInstanceID();
            RegionTravel.TravelTo(
                returnPortal.TargetSceneName, returnPortal.TargetSpawnId);

            yield return WaitForPlayerOfNewScene(previousPlayerId);
            yield return WaitUntilSettled();

            Transform spawnPoint = FindSpawnPoint(expectedSpawnId);

            Assert.That(
                spawnPoint,
                Is.Not.Null,
                $"{RegionUnderTest} hat keinen RegionSpawnPoint " +
                $"mit der ID {expectedSpawnId}.");

            Vector3 player = RequirePlayer().position;

            Assert.That(
                HorizontalDistance(player, spawnPoint.position),
                Is.LessThan(ArrivalTolerance),
                $"Die Reise nach {RegionUnderTest} endet nicht am Spawn " +
                $"{expectedSpawnId}.");

            Assert.That(
                HorizontalDistance(player, regularStartPose),
                Is.GreaterThan(DistinctPoseDistance),
                "Die Reise endet an der normalen Startpose statt am Portal.");

            Assert.That(
                consoleErrors,
                Is.Empty,
                "Die Reise hat rote Console-Meldungen erzeugt.");
        }

        // ------------------------------------------------------------------

        private static IEnumerator LoadScene(string sceneName)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(
                sceneName, LoadSceneMode.Single);

            while (!load.isDone)
                yield return null;

            // Ein Frame, damit alle Start()-Aufrufe der Szene gelaufen sind.
            yield return null;
        }

        /// <summary>
        /// Wartet, bis die neue Szene ihren eigenen Spieler bereitstellt.
        /// Bewusst ueber die Instanz-ID statt ueber eine feste Framezahl:
        /// RegionTravel laedt synchron erst zum Ende des Frames.
        /// </summary>
        private static IEnumerator WaitForPlayerOfNewScene(int previousPlayerId)
        {
            for (int frame = 0; frame < 600; frame++)
            {
                yield return null;

                GameObject player = GameObject.Find("Player");

                if (player != null &&
                    player.GetInstanceID() != previousPlayerId)
                {
                    // Ein weiterer Frame, damit RegionSpawnPoint.Start lief.
                    yield return null;
                    yield break;
                }
            }

            Assert.Fail("Die Zielszene hat keinen eigenen Spieler geladen.");
        }

        /// <summary>
        /// Wartet, bis Spieler und Kamera zur Ruhe gekommen sind. Damit haengt
        /// die Messung weder an einer Framezahl noch an der Reihenfolge, in der
        /// Start-Aufrufe oder Physikschritte abgearbeitet werden.
        /// </summary>
        private static IEnumerator WaitUntilSettled()
        {
            Transform player = RequirePlayer();
            Vector3 lastPlayer = player.position;
            Vector3 lastCamera = CameraPosition();

            for (int frame = 0; frame < 240; frame++)
            {
                yield return null;

                Vector3 currentCamera = CameraPosition();

                bool playerAtRest =
                    Vector3.Distance(lastPlayer, player.position) < 0.002f;
                bool cameraAtRest =
                    Vector3.Distance(lastCamera, currentCamera) < 0.002f;

                lastPlayer = player.position;
                lastCamera = currentCamera;

                if (frame > 2 && playerAtRest && cameraAtRest)
                    yield break;
            }
        }

        private static Vector3 CameraPosition()
        {
            return Camera.main == null
                ? Vector3.zero
                : Camera.main.transform.position;
        }

        private static Transform RequirePlayer()
        {
            GameObject player = GameObject.Find("Player");

            Assert.That(player, Is.Not.Null, "Player fehlt in der Szene.");

            return player.transform;
        }

        private static RegionPortal FindPortalTo(string sceneName)
        {
            foreach (RegionPortal portal in Object.FindObjectsByType<RegionPortal>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (portal.TargetSceneName == sceneName)
                    return portal;
            }

            return null;
        }

        /// <summary>
        /// Sucht den Spawn zur angeforderten ID. Die ID ist ein privates
        /// SerializeField, deshalb ueber Reflection — schlaegt ausdruecklich
        /// fehl, falls das Feld einmal umbenannt wird.
        /// </summary>
        private static Transform FindSpawnPoint(string spawnId)
        {
            FieldInfo field = typeof(RegionSpawnPoint).GetField(
                "spawnId", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                "RegionSpawnPoint hat kein Feld spawnId mehr.");

            foreach (RegionSpawnPoint spawn in
                     Object.FindObjectsByType<RegionSpawnPoint>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if ((string)field.GetValue(spawn) == spawnId)
                    return spawn.transform;
            }

            return null;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(
                new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }
    }
}
