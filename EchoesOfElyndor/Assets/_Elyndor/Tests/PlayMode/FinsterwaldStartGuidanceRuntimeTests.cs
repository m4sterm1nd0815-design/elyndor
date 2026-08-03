using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Prueft im laufenden Spiel, dass der Finsterwald-Start die
    /// Sichtfuehrung zum Hauptweg tatsaechlich liefert: richtige
    /// Startposition und Blickrichtung, freie Sicht den Weg entlang,
    /// keine Krone unmittelbar vor der Kamera und ein erreichbarer
    /// Rucksack ausserhalb des Blickzentrums.
    /// </summary>
    public sealed class FinsterwaldStartGuidanceRuntimeTests
    {
        private const string SceneName = "Finsterwald";

        private Transform player;
        private Transform startPoint;
        private Vector3 pathDirection;

        [UnitySetUp]
        public IEnumerator LoadFinsterwald()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(
                SceneName, LoadSceneMode.Single);

            while (!load.isDone)
                yield return null;

            // PlayerStartSetup setzt im Start() und richtet die Kamera einen
            // Frame spaeter aus; CameraFollow zieht in LateUpdate nach.
            for (int frame = 0; frame < 8; frame++)
                yield return null;

            player = GameObject.Find("Player")?.transform;
            startPoint = GameObject.Find("PlayerStart")?.transform;

            Assert.That(player, Is.Not.Null, "Player fehlt in der Szene.");
            Assert.That(
                startPoint, Is.Not.Null, "PlayerStart fehlt in der Szene.");

            pathDirection = Quaternion.Euler(0f, 7f, 0f) * Vector3.forward;
        }

        [UnityTest]
        public IEnumerator ArenStartsOnTheStartPointFacingThePath()
        {
            Vector3 flatPlayer =
                new Vector3(player.position.x, 0f, player.position.z);
            Vector3 flatStart =
                new Vector3(startPoint.position.x, 0f, startPoint.position.z);

            Assert.That(
                Vector3.Distance(flatPlayer, flatStart),
                Is.LessThan(0.3f),
                "Aren steht nicht auf dem Startpunkt.");

            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(
                    player.eulerAngles.y, startPoint.eulerAngles.y)),
                Is.LessThan(5f),
                "Arens Blickrichtung weicht vom Startpunkt ab.");

            yield break;
        }

        [UnityTest]
        public IEnumerator TheMainPathIsVisibleFromTheStart()
        {
            Vector3 eye = player.position + Vector3.up * 1.6f;

            for (float along = 4f; along <= 20f; along += 2f)
            {
                Vector3 target = player.position + pathDirection * along;
                target.y = GroundHeight(target) + 1.2f;

                Vector3 toTarget = target - eye;

                bool blocked = Physics.Raycast(
                    eye,
                    toTarget.normalized,
                    out RaycastHit hit,
                    toTarget.magnitude,
                    ~0,
                    QueryTriggerInteraction.Ignore);

                Assert.That(
                    blocked,
                    Is.False,
                    $"Die Sicht auf den Hauptweg ist bei {along} m durch " +
                    $"{(blocked ? hit.transform.name : string.Empty)} verdeckt.");
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator NoLargeFoliageSitsInTheStartCamera()
        {
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(
                         FindObjectsSortMode.None))
            {
                if (renderer is ParticleSystemRenderer)
                    continue;

                // Arens eigenes Modell steht naturgemaess im Bild.
                if (renderer.transform.IsChildOf(player))
                    continue;

                Bounds bounds = renderer.bounds;

                bool isLarge =
                    Mathf.Max(bounds.size.x, bounds.size.z) > 3f;

                if (!isLarge)
                    continue;

                float distance = Vector2.Distance(
                    new Vector2(bounds.center.x, bounds.center.z),
                    new Vector2(player.position.x, player.position.z));

                Assert.That(
                    distance,
                    Is.GreaterThan(6f),
                    $"{renderer.transform.name} steht bei {distance:F1} m " +
                    "direkt im Startbild.");
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator TheStartCameraKeepsItsFullDistance()
        {
            Camera camera = Camera.main;

            Assert.That(camera, Is.Not.Null, "Keine Main Camera vorhanden.");

            float distance = Vector3.Distance(
                camera.transform.position,
                player.position + Vector3.up * 1.2f);

            Assert.That(
                distance,
                Is.GreaterThan(10f),
                "Die Startkamera wird von Bewuchs an Aren herangezogen.");

            yield break;
        }

        [UnityTest]
        public IEnumerator TheBackpackStaysReachableBesideTheStartFocus()
        {
            Transform backpack =
                GameObject.Find("Beschaedigter Rucksack")?.transform;

            Assert.That(backpack, Is.Not.Null, "Rucksack fehlt in der Szene.");

            Vector3 offset = backpack.position - player.position;
            offset.y = 0f;

            Assert.That(
                offset.magnitude,
                Is.InRange(2f, 8f),
                "Der Rucksack ist nicht mehr in wenigen Schritten erreichbar.");

            Assert.That(
                Mathf.Abs(Vector3.SignedAngle(
                    pathDirection, offset.normalized, Vector3.up)),
                Is.GreaterThan(20f),
                "Der Rucksack liegt weiterhin im zentralen Startfokus.");

            yield break;
        }

        [UnityTest]
        public IEnumerator TheSignpostStandsBesideThePathAhead()
        {
            Transform signpost = null;

            foreach (Transform candidate in Object.FindObjectsByType<Transform>(
                         FindObjectsSortMode.None))
            {
                if (candidate.name != "Wegschild" ||
                    candidate.parent == null ||
                    candidate.parent.name != "Startbereich")
                {
                    continue;
                }

                signpost = candidate;
                break;
            }

            Assert.That(
                signpost, Is.Not.Null, "Startbereich/Wegschild fehlt.");

            Vector3 offset = signpost.position - player.position;
            offset.y = 0f;

            Assert.That(
                offset.magnitude,
                Is.LessThan(14f),
                "Das Wegschild steht nicht mehr im Startbild.");

            Assert.That(
                Mathf.Abs(Vector3.SignedAngle(
                    pathDirection, offset.normalized, Vector3.up)),
                Is.LessThan(35f),
                "Das Wegschild steht nicht mehr am Hauptweg.");

            yield break;
        }

        private static float GroundHeight(Vector3 position)
        {
            Terrain terrain = Terrain.activeTerrain;

            return terrain == null
                ? 0f
                : terrain.SampleHeight(position) + terrain.transform.position.y;
        }
    }
}
