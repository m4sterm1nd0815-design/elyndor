using System.Collections;
using System.Collections.Generic;
using Elyndor.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Elyndor.Tests
{
    /// <summary>
    /// Prueft im laufenden Spiel, was der Startbereich im Finsterwald leisten
    /// soll: Aren steht auf dem Boden, blickt eine freie Strecke entlang,
    /// hat keine Krone im Kamerabild, die Kamera behaelt ihren Abstand, und
    /// Wegschild wie Rucksack stehen dort, wo sie fuehren beziehungsweise
    /// entdeckt werden sollen.
    ///
    /// Alle Messungen beziehen sich auf Arens tatsaechliche Startpose in der
    /// Szene. Es gibt bewusst keinen Hilfsmarker und keine fest verdrahtete
    /// Wegrichtung, damit die Tests die Sichtfuehrung pruefen und nicht ihre
    /// eigene Implementierung spiegeln.
    /// </summary>
    public sealed class FinsterwaldStartGuidanceRuntimeTests
    {
        private const string SceneName = "Finsterwald";

        /// <summary>Freie Strecke, die Aren vor sich sehen koennen muss.</summary>
        private const float RequiredClearSight = 20f;

        private readonly List<string> consoleErrors = new List<string>();

        private Transform player;
        private Vector3 lookDirection;

        [UnitySetUp]
        public IEnumerator LoadFinsterwald()
        {
            consoleErrors.Clear();
            Application.logMessageReceived += CollectConsoleError;

            // Unabhaengig davon, was zuvor lief: kein offener Portal-Spawn.
            RegionTravel.ClearPendingSpawn();

            AsyncOperation load = SceneManager.LoadSceneAsync(
                SceneName, LoadSceneMode.Single);

            while (!load.isDone)
                yield return null;

            player = GameObject.Find("Player")?.transform;
            Assert.That(player, Is.Not.Null, "Player fehlt in der Szene.");

            // Aren wird vom CharacterController auf den Boden gesetzt und die
            // Kamera schwingt aus ihrer Editor-Pose ein. Es wird auf Ruhe
            // gewartet statt auf eine feste Framezahl, damit die Messungen
            // nicht von der Bildrate des Testlaufs abhaengen.
            yield return WaitUntilSettled();

            lookDirection = player.forward;
            lookDirection.y = 0f;
            lookDirection.Normalize();
        }

        [UnityTearDown]
        public IEnumerator DetachLogHandler()
        {
            Application.logMessageReceived -= CollectConsoleError;
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

        /// <summary>
        /// Wartet, bis Aren und die Kamera zur Ruhe gekommen sind, hoechstens
        /// aber eine Sekunde Spielzeit.
        /// </summary>
        private IEnumerator WaitUntilSettled()
        {
            Vector3 lastPlayer = player.position;
            Vector3 lastCamera = Camera.main == null
                ? Vector3.zero
                : Camera.main.transform.position;

            for (int frame = 0; frame < 240; frame++)
            {
                yield return null;

                Vector3 currentCamera = Camera.main == null
                    ? Vector3.zero
                    : Camera.main.transform.position;

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

        [UnityTest]
        public IEnumerator ArenStandsOnTheGroundAtTheStart()
        {
            CharacterController controller =
                player.GetComponent<CharacterController>();

            Assert.That(
                controller, Is.Not.Null, "Der Spieler hat keinen Controller.");

            // Fusssohle der Kapsel statt Pivot: unabhaengig von Hoehe und
            // Center des CharacterControllers.
            float solePosition =
                player.position.y +
                controller.center.y -
                controller.height * 0.5f;

            Assert.That(
                solePosition - GroundHeight(player.position),
                Is.InRange(-0.3f, 0.4f),
                "Aren startet nicht auf dem Boden.");

            yield break;
        }

        [UnityTest]
        public IEnumerator ArenLooksAlongAClearStretchOfForest()
        {
            Vector3 eye = player.position + Vector3.up * 1.6f;

            for (float along = 4f; along <= RequiredClearSight; along += 2f)
            {
                Vector3 target = player.position + lookDirection * along;
                target.y = GroundHeight(target) + 1.2f;

                Collider blocker = FirstBlocker(eye, target);

                Assert.That(
                    blocker,
                    Is.Null,
                    $"Arens Blick ist bei {along} m durch " +
                    $"{(blocker == null ? string.Empty : blocker.transform.name)} " +
                    "verdeckt.");
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator NoLargeFoliageCrowdsTheStartCamera()
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

                if (Mathf.Max(bounds.size.x, bounds.size.z) <= 3f)
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
                    lookDirection, offset.normalized, Vector3.up)),
                Is.GreaterThan(20f),
                "Der Rucksack liegt weiterhin im zentralen Startfokus.");

            yield break;
        }

        [UnityTest]
        public IEnumerator TheSignpostStandsBesideThePathAhead()
        {
            Transform signpost = FindSignpost();

            Assert.That(
                signpost, Is.Not.Null, "Startbereich/Wegschild fehlt.");

            Vector3 offset = signpost.position - player.position;
            offset.y = 0f;

            Assert.That(
                offset.magnitude,
                Is.LessThan(14f),
                "Das Wegschild steht nicht mehr im Startbild.");

            float angle = Mathf.Abs(Vector3.SignedAngle(
                lookDirection, offset.normalized, Vector3.up));

            Assert.That(
                angle,
                Is.LessThan(40f),
                "Das Wegschild steht nicht mehr am Hauptweg.");

            // Seitlicher Versatz: das Schild fuehrt, ohne den Weg zu verstellen.
            Assert.That(
                offset.magnitude * Mathf.Sin(angle * Mathf.Deg2Rad),
                Is.GreaterThan(1.5f),
                "Das Wegschild steht mitten im Laufweg.");

            yield break;
        }

        [UnityTest]
        public IEnumerator TheStartRaisesNoConsoleErrors()
        {
            // Der Szenenaufbau lief bereits im Setup; hier wird nur noch
            // ausdruecklich festgehalten, dass dabei nichts rot war.
            for (int frame = 0; frame < 5; frame++)
                yield return null;

            Assert.That(
                consoleErrors,
                Is.Empty,
                "Der Spielstart hat rote Console-Meldungen erzeugt.");
        }

        private static Transform FindSignpost()
        {
            foreach (Transform candidate in Object.FindObjectsByType<Transform>(
                         FindObjectsSortMode.None))
            {
                if (candidate.name == "Wegschild" &&
                    candidate.parent != null &&
                    candidate.parent.name == "Startbereich")
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Erster Kollider auf der Sichtlinie, der nicht zu Aren gehoert.
        /// Bewusst ueber RaycastAll, damit das Ergebnis nicht davon abhaengt,
        /// welchen Treffer die Physik zuerst meldet.
        /// </summary>
        private Collider FirstBlocker(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float length = direction.magnitude;
            direction /= length;

            RaycastHit[] hits = Physics.RaycastAll(
                from, direction, length, ~0, QueryTriggerInteraction.Ignore);

            Collider nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(player) ||
                    hit.collider.transform == player)
                {
                    continue;
                }

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearest = hit.collider;
                }
            }

            return nearest;
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
