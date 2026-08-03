using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Raeumt den unmittelbaren Startbereich im Finsterwald so auf, dass der
    /// Hauptweg beim Spielstart ohne Text erkennbar ist.
    ///
    /// Das Werkzeug arbeitet ausschliesslich in einem schmalen Korridor um
    /// Arens Startpose: es schiebt stoerende Kronen, Buesche und grosse
    /// Bodenpflanzen seitlich aus dem Kamerabild, richtet das vorhandene
    /// Wegschild am Weg aus, versetzt den Rucksack aus dem zentralen
    /// Startfokus und legt vorhandene Kiesel und Felsen als Wegkante
    /// beziehungsweise Trittsteine aus.
    ///
    /// Es werden keine Objekte angelegt oder geloescht und keine neuen Assets
    /// importiert. Ein zweiter Lauf findet den Korridor bereits frei und
    /// aendert nichts mehr.
    /// </summary>
    public static class FinsterwaldStartGuidanceBuilder
    {
        private const string ScenePath =
            "Assets/_Elyndor/Scenes/Finsterwald.unity";

        /// <summary>
        /// Der Korridor wird um Arens gespeicherte Startpose gebaut. Der
        /// Spieler-Transform in der Szene ist die einzige Quelle dafuer.
        /// </summary>
        private const string PlayerObjectName = "Player";

        /// <summary>
        /// Richtung des Hauptwegs ab dem Startpunkt. Der Weg fuehrt nach Norden
        /// zum Rastbereich bei z = -56 und weicht dabei leicht nach Osten ab.
        /// </summary>
        private const float PathYaw = 7f;

        // Korridormasse in Metern, gemessen entlang des Wegs (s) und quer (t).
        private const float PocketBack = -12f;
        private const float ClearingForward = 7f;
        private const float ClearingHalfWidth = 4.5f;
        private const float LaneForward = 18f;
        private const float LaneHalfWidth = 3.2f;

        // Zielband, in das verschobene Baeume ausweichen.
        private const float TreeBandInner = 9.5f;
        private const float TreeBandOuter = 20f;
        private const float TreeSpacing = 2.6f;

        // Zielband fuer Buesche: sie duerfen den Weg dichter saeumen.
        private const float ShrubBandInner = 3.6f;
        private const float ShrubBandOuter = 5.4f;
        private const float ShrubSpacing = 1.4f;

        // Anstellwinkel des Wegschilds gegenueber der reinen Wegrichtung.
        private const float SignReadingAngle = 30f;

        private static Vector3 pathForward;
        private static Vector3 pathRight;
        private static Vector3 origin;

        [MenuItem("Elyndor/Finsterwald/Startbereich - Sichtfuehrung aufraeumen")]
        public static void Build()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Transform player = Find(PlayerObjectName);

            if (player == null)
            {
                Debug.LogError(
                    "Startbereich: Der Spieler wurde nicht gefunden. " +
                    "Es wurde nichts veraendert.");
                return;
            }

            origin = player.position;
            pathForward = Quaternion.Euler(0f, PathYaw, 0f) * Vector3.forward;
            pathRight = Quaternion.Euler(0f, PathYaw, 0f) * Vector3.right;

            Debug.Log(
                $"Startbereich: Korridor ab {origin} mit Wegrichtung {PathYaw} Grad.");

            int movedTrees = MoveOutOfCorridor(
                CollectChildren("Environment/Vegetation/Baeume")
                    .Concat(CollectChildren("Environment/Vegetation/Waldrand"))
                    .ToList(),
                TreeBandInner,
                TreeBandOuter,
                TreeSpacing,
                "Baumkrone");

            int movedShrubs = MoveOutOfCorridor(
                CollectChildren("Environment/Vegetation/Buesche")
                    .Concat(CollectChildren("Environment/Vegetation/Farne"))
                    .ToList(),
                ShrubBandInner,
                ShrubBandOuter,
                ShrubSpacing,
                "Busch");

            int movedPlants = MoveLargeGroundPlantsAside();

            AlignSignpost();
            OffsetBackpack();
            int stones = LayOutPathStones();

            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log(
                $"Startbereich fertig: {movedTrees} Baeume, {movedShrubs} Buesche, " +
                $"{movedPlants} Bodenpflanzen versetzt, {stones} Steine ausgelegt.");
        }

        // ------------------------------------------------------------------
        // Korridor
        // ------------------------------------------------------------------

        private static Vector2 ToPathSpace(Vector3 worldPosition)
        {
            Vector3 offset = worldPosition - origin;
            offset.y = 0f;

            return new Vector2(
                Vector3.Dot(offset, pathForward),
                Vector3.Dot(offset, pathRight));
        }

        private static Vector3 ToWorld(float along, float lateral, float height)
        {
            Vector3 flat = origin + pathForward * along + pathRight * lateral;
            return new Vector3(flat.x, height, flat.z);
        }

        private static bool IsInCorridor(Vector2 pathSpace, float laneScale)
        {
            float along = pathSpace.x;
            float lateral = Mathf.Abs(pathSpace.y);

            if (along < PocketBack || along > LaneForward)
            {
                return false;
            }

            float halfWidth = along <= ClearingForward
                ? ClearingHalfWidth
                : LaneHalfWidth;

            return lateral <= halfWidth * laneScale;
        }

        /// <summary>
        /// Schiebt alle Objekte, die im Sichtkorridor stehen, seitlich in ein
        /// freies Band. Hoehe ueber dem Terrain bleibt erhalten.
        /// </summary>
        private static int MoveOutOfCorridor(
            IReadOnlyList<Transform> candidates,
            float bandInner,
            float bandOuter,
            float spacing,
            string label)
        {
            List<Vector3> occupied =
                candidates.Select(c => c.position).ToList();

            int moved = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                Transform candidate = candidates[i];
                Vector2 pathSpace = ToPathSpace(candidate.position);

                if (!IsInCorridor(pathSpace, 1f))
                {
                    continue;
                }

                float side = pathSpace.y >= 0f ? 1f : -1f;

                if (Mathf.Abs(pathSpace.y) < 0.5f)
                {
                    side = (i % 2 == 0) ? 1f : -1f;
                }

                if (!TryFindFreeSlot(
                        pathSpace.x, side, bandInner, bandOuter, spacing,
                        occupied, out Vector3 target))
                {
                    Debug.LogWarning(
                        $"Startbereich: kein freier Platz fuer {candidate.name} " +
                        "— Objekt bleibt stehen.");
                    continue;
                }

                float heightOverGround =
                    candidate.position.y - TerrainHeight(candidate.position);

                occupied[i] = new Vector3(target.x, candidate.position.y, target.z);

                Undo.RecordObject(candidate, "Startbereich Sichtfuehrung");
                candidate.position = new Vector3(
                    target.x,
                    TerrainHeight(target) + heightOverGround,
                    target.z);

                Debug.Log(
                    $"VERSETZT|{label}|{candidate.name}|" +
                    $"quer={pathSpace.y:F1} -> {ToPathSpace(candidate.position).y:F1}");

                moved++;
            }

            return moved;
        }

        private static bool TryFindFreeSlot(
            float along,
            float side,
            float bandInner,
            float bandOuter,
            float spacing,
            IReadOnlyList<Vector3> occupied,
            out Vector3 target)
        {
            float[] shifts = { 0f, 1.6f, -1.6f, 3.2f, -3.2f, 4.8f, -4.8f };

            for (float lateral = bandInner; lateral <= bandOuter; lateral += 0.7f)
            {
                foreach (float shift in shifts)
                {
                    Vector3 candidate =
                        ToWorld(along + shift, side * lateral, 0f);

                    if (IsInCorridor(ToPathSpace(candidate), 1.1f))
                    {
                        continue;
                    }

                    if (occupied.All(o =>
                            (new Vector2(o.x, o.z) -
                             new Vector2(candidate.x, candidate.z)).sqrMagnitude >
                            spacing * spacing))
                    {
                        target = candidate;
                        return true;
                    }
                }
            }

            target = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Grosse Blattpflanzen direkt vor Aren an den Wegrand nehmen, damit
        /// der Boden im Startbild lesbar bleibt.
        /// </summary>
        private static int MoveLargeGroundPlantsAside()
        {
            int moved = 0;

            foreach (Transform plant in
                     CollectChildren("Environment/Vegetation/Waldboden"))
            {
                Vector2 pathSpace = ToPathSpace(plant.position);

                bool inFocus =
                    pathSpace.x > -2f &&
                    pathSpace.x < 9f &&
                    Mathf.Abs(pathSpace.y) < 1.6f;

                if (!inFocus)
                {
                    continue;
                }

                Renderer renderer = plant.GetComponentInChildren<Renderer>();

                if (renderer == null ||
                    Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z) < 1.2f)
                {
                    continue;
                }

                float side = pathSpace.y >= 0f ? 1f : -1f;
                float heightOverGround =
                    plant.position.y - TerrainHeight(plant.position);
                Vector3 target = ToWorld(pathSpace.x, side * 2.9f, 0f);

                Undo.RecordObject(plant, "Startbereich Sichtfuehrung");
                plant.position = new Vector3(
                    target.x,
                    TerrainHeight(target) + heightOverGround,
                    target.z);

                Debug.Log($"VERSETZT|Bodenpflanze|{plant.name}");
                moved++;
            }

            return moved;
        }

        // ------------------------------------------------------------------
        // Einzelobjekte
        // ------------------------------------------------------------------

        /// <summary>
        /// Stellt das vorhandene Wegschild an den rechten Wegrand vor Aren und
        /// dreht die Tafel so, dass ihre Laengsachse den Hauptweg anzeigt.
        /// </summary>
        private static void AlignSignpost()
        {
            Transform signpost = FindByPath("Environment/Startbereich/Wegschild");

            if (signpost == null)
            {
                Debug.LogWarning("Startbereich: Wegschild nicht gefunden.");
                return;
            }

            float heightOverGround =
                signpost.position.y - TerrainHeight(signpost.position);
            Vector3 target = ToWorld(9f, 3.1f, 0f);

            Transform board = signpost.Find("Tafel");
            bool longAxisIsX =
                board == null || board.localScale.x >= board.localScale.z;

            // Die lange Tafelachse zeigt in Wegrichtung. Zusaetzlich ist das
            // Schild um SignReadingAngle zum Startpunkt gedreht, damit Aren die
            // Tafel schon aus der Startkamera als Schild erkennt.
            float yaw = (longAxisIsX ? PathYaw - 90f : PathYaw) -
                        SignReadingAngle;

            Undo.RecordObject(signpost, "Startbereich Sichtfuehrung");
            signpost.position = new Vector3(
                target.x,
                TerrainHeight(target) + heightOverGround,
                target.z);
            signpost.rotation = Quaternion.Euler(0f, yaw, 0f);

            Debug.Log(
                $"VERSETZT|Wegschild|{signpost.position}|yaw={yaw:F1}" +
                $"|Tafel-Laengsachse={(longAxisIsX ? "X" : "Z")}");
        }

        /// <summary>
        /// Setzt den Rucksack links neben den Startfokus: sichtbar und in
        /// wenigen Schritten erreichbar, aber nicht im Blickzentrum.
        /// </summary>
        private static void OffsetBackpack()
        {
            Transform backpack = FindByPath("Environment/Beschaedigter Rucksack");

            if (backpack == null)
            {
                Debug.LogWarning("Startbereich: Rucksack nicht gefunden.");
                return;
            }

            float heightOverGround =
                backpack.position.y - TerrainHeight(backpack.position);
            Vector3 target = ToWorld(4f, -2.6f, 0f);

            Undo.RecordObject(backpack, "Startbereich Sichtfuehrung");
            backpack.position = new Vector3(
                target.x,
                TerrainHeight(target) + heightOverGround,
                target.z);

            Debug.Log($"VERSETZT|Rucksack|{backpack.position}");
        }

        /// <summary>
        /// Legt vorhandene Kiesel als Trittspur in den Weg und vorhandene
        /// kleine Felsen als Wegkante daneben. Es entstehen keine neuen Objekte.
        /// </summary>
        private static int LayOutPathStones()
        {
            // Die flachen Kiesel liegen abwechselnd knapp neben der Wegmitte:
            // sie fuehren den Blick, ohne Arens ersten Schritt zu verstellen.
            (float along, float lateral)[] treadSlots =
            {
                (4f, 1.4f), (7.5f, -1.3f), (11f, 1.5f),
                (14.5f, -1.4f), (18f, 1.3f)
            };

            (float along, float lateral)[] edgeSlots =
            {
                (5.5f, 2.9f), (9f, -2.9f), (13f, 3f), (17.5f, -2.8f)
            };

            int placed = 0;

            placed += PlaceStones(
                "Environment/Vegetation/Kiesel", treadSlots, "Trittspur");
            placed += PlaceStones(
                "Environment/Vegetation/Felsen", edgeSlots, "Wegkante");

            return placed;
        }

        private static int PlaceStones(
            string groupPath,
            (float along, float lateral)[] slots,
            string label)
        {
            List<Transform> stones = CollectChildren(groupPath)
                .Where(s =>
                {
                    Renderer renderer = s.GetComponentInChildren<Renderer>();
                    return renderer != null && renderer.bounds.size.y < 1.2f;
                })
                .OrderBy(s => Vector3.Distance(s.position, origin))
                .Take(slots.Length)
                .ToList();

            for (int i = 0; i < stones.Count; i++)
            {
                Transform stone = stones[i];
                float heightOverGround =
                    stone.position.y - TerrainHeight(stone.position);
                Vector3 target = ToWorld(slots[i].along, slots[i].lateral, 0f);

                Undo.RecordObject(stone, "Startbereich Sichtfuehrung");
                stone.position = new Vector3(
                    target.x,
                    TerrainHeight(target) + heightOverGround,
                    target.z);

                Debug.Log($"VERSETZT|{label}|{stone.name}|{stone.position}");
            }

            return stones.Count;
        }

        // ------------------------------------------------------------------
        // Hilfsfunktionen
        // ------------------------------------------------------------------

        private static List<Transform> CollectChildren(string groupPath)
        {
            Transform group = FindByPath(groupPath);

            if (group == null)
            {
                Debug.LogWarning($"Startbereich: {groupPath} nicht gefunden.");
                return new List<Transform>();
            }

            List<Transform> children = new List<Transform>(group.childCount);

            foreach (Transform child in group)
            {
                children.Add(child);
            }

            return children;
        }

        private static float TerrainHeight(Vector3 position)
        {
            Terrain terrain = Terrain.activeTerrain;

            return terrain == null
                ? 0f
                : terrain.SampleHeight(position) + terrain.transform.position.y;
        }

        private static Transform Find(string name)
        {
            return Object
                .FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == name);
        }

        private static Transform FindByPath(string path)
        {
            return Object
                .FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .FirstOrDefault(t => HierarchyPath(t) == path);
        }

        private static string HierarchyPath(Transform transform)
        {
            string path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }
    }
}
