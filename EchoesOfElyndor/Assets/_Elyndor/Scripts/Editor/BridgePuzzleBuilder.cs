using System;
using Elyndor.Puzzles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Baut das Memory-Watch-Rätsel „Die geteilte Brücke" in den Finsterwald.
    ///
    /// Als Editor-Werkzeug und nicht von Hand zusammengeklickt: der Aufbau ist
    /// damit nachlesbar und wiederholbar, und die Positionen stehen an einer
    /// Stelle statt verstreut in einer Szenendatei.
    ///
    /// Alle Maße leiten sich aus der <em>bestehenden</em> Handarbeit ab: die
    /// beiden Brückenreste, der Bachlauf und die vorhandene Memory Site
    /// bestimmen, wo Anker, Stamm und Zone liegen. Nichts davon wird verschoben.
    /// </summary>
    public static class BridgePuzzleBuilder
    {
        private const string ScenePath =
            "Assets/_Elyndor/Scenes/Finsterwald.unity";

        private const string PuzzleRootName = "Brueckenraetsel";

        /// <summary>Südlicher Brückenrest; aus der Szene übernommen.</summary>
        private static readonly Vector3 SouthAbutment =
            new Vector3(0.97f, 0.14f, -6.20f);

        /// <summary>Nördlicher Brückenrest; aus der Szene übernommen.</summary>
        private static readonly Vector3 NorthAbutment =
            new Vector3(3.03f, -0.42f, 3.80f);

        /// <summary>Mitte des Furt-Steinpfads; aus der Szene übernommen.</summary>
        private static readonly Vector3 FordCentre =
            new Vector3(34f, -0.85f, 0f);

        [MenuItem("Elyndor/Finsterwald/Brueckenraetsel bauen")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath, OpenSceneMode.Single);

            GameObject environment = GameObject.Find("Environment");

            if (environment == null)
            {
                throw new InvalidOperationException(
                    "Brueckenraetsel: 'Environment' fehlt in der Szene.");
            }

            // Alten Aufbau vollstaendig entfernen, damit ein erneuter Lauf
            // nichts verdoppelt.
            Transform existing = environment.transform.Find(PuzzleRootName);

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject root = new GameObject(PuzzleRootName);
            root.transform.SetParent(environment.transform, true);

            BridgePuzzle puzzle = root.AddComponent<BridgePuzzle>();

            Vector3 span = NorthAbutment - SouthAbutment;
            Vector3 middle = SouthAbutment + span * 0.5f;
            float length = span.magnitude;
            Vector3 direction = span / length;
            Vector3 sideways =
                Vector3.Cross(Vector3.up, direction).normalized;

            BridgeAnchor[] anchors = BuildAnchors(root, middle, sideways);
            Transform deployedPose = BuildDeployedPose(root, middle, direction, length);
            GameObject log = BuildLog(root, middle, sideways, length);
            Collider walkway = BuildWalkway(log.transform, length);
            GameObject[] planks = BuildPlanks(root, middle, direction, sideways);

            BuildResonanceZone(root, middle);
            BuildPart(root, BridgePartRole.TensionCheck, "Seilbock",
                SouthAbutment + sideways * -1.8f + Vector3.up * 0.4f, puzzle);
            BuildPart(root, BridgePartRole.Release, "Stammfreigabe",
                middle + sideways * -3.2f + Vector3.up * 0.4f, puzzle);

            Wire(puzzle,
                ("anchors", anchors),
                ("plankVisuals", planks));

            Wire(puzzle,
                ("fallenLog", log.transform),
                ("deployedPose", deployedPose),
                ("walkway", walkway));

            foreach (BridgeAnchor anchor in anchors)
            {
                Wire(anchor, ("puzzle", puzzle));
            }

            BuildFord(environment);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                "BRIDGE_PUZZLE_OK: drei Anker, Stamm, zwei Bohlen, " +
                "Resonanzzone und Furt gesetzt. Loesung: Sued 1, Seite 2, " +
                "Nord 3 Kerben.");
        }

        // ------------------------------------------------------------------

        private static BridgeAnchor[] BuildAnchors(
            GameObject root, Vector3 middle, Vector3 sideways)
        {
            // Die drei Anker stehen bewusst weit auseinander: tief am
            // Suedufer, seitlich am Seitenstein, hoch am Nordufer. Der
            // Lastverlauf des Echos laeuft genau diesen Weg.
            var placements = new (BridgeAnchorId Id, string Name, Vector3 Position)[]
            {
                (BridgeAnchorId.SouthDeep, "Anker_Sued_Tief",
                    SouthAbutment + sideways * 1.6f),
                (BridgeAnchorId.Side, "Anker_Seitenstein",
                    middle + sideways * 4.2f),
                (BridgeAnchorId.North, "Anker_Nord",
                    NorthAbutment + sideways * 1.4f)
            };

            var anchors = new BridgeAnchor[placements.Length];

            for (int i = 0; i < placements.Length; i++)
            {
                (BridgeAnchorId id, string name, Vector3 position) = placements[i];

                GameObject stone = GameObject.CreatePrimitive(
                    PrimitiveType.Cylinder);
                stone.name = name;
                stone.transform.SetParent(root.transform, true);
                stone.transform.position = Grounded(position) +
                                           Vector3.up * 0.35f;
                stone.transform.localScale = new Vector3(0.9f, 0.35f, 0.9f);

                // Der Kerbenmarker macht die Stellung ohne Farbe lesbar.
                GameObject notch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                notch.name = "Kerbe";
                notch.transform.SetParent(stone.transform, false);
                notch.transform.localPosition = new Vector3(0f, 0f, 0.55f);
                notch.transform.localScale = new Vector3(0.18f, 0.7f, 0.12f);
                UnityEngine.Object.DestroyImmediate(
                    notch.GetComponent<Collider>());

                // Trigger fuer den InteractionDetector.
                SphereCollider trigger = stone.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 1.6f;

                BridgeAnchor anchor = stone.AddComponent<BridgeAnchor>();
                Wire(anchor, ("anchorId", (int)id));
                anchors[i] = anchor;
            }

            return anchors;
        }

        private static Transform BuildDeployedPose(
            GameObject root, Vector3 middle, Vector3 direction, float length)
        {
            GameObject pose = new GameObject("Stamm_Zielpose");
            pose.transform.SetParent(root.transform, true);

            // Die Deckhoehe liegt zwischen beiden Brueckenresten, plus den
            // halben Stammdurchmesser.
            pose.transform.position = new Vector3(
                middle.x, middle.y + 0.35f, middle.z);
            pose.transform.rotation =
                Quaternion.FromToRotation(Vector3.up, direction);

            return pose.transform;
        }

        private static GameObject BuildLog(
            GameObject root, Vector3 middle, Vector3 sideways, float length)
        {
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "Gefallener Stamm";
            log.transform.SetParent(root.transform, true);

            // Unity-Zylinder sind zwei Einheiten hoch.
            log.transform.localScale =
                new Vector3(0.7f, length * 0.5f, 0.7f);

            // Ausgangslage: quer ueber dem Bach, in alten Seilresten haengend.
            log.transform.position =
                middle + sideways * 5.5f + Vector3.up * 0.9f;
            log.transform.rotation =
                Quaternion.FromToRotation(Vector3.up, sideways);

            UnityEngine.Object.DestroyImmediate(log.GetComponent<Collider>());

            CapsuleCollider body = log.AddComponent<CapsuleCollider>();
            body.direction = 1;
            body.height = 2f;
            body.radius = 0.5f;

            return log;
        }

        /// <summary>
        /// Die Lauffläche liegt oben auf dem Stamm und ist abgeschaltet, bis
        /// die Bohlen liegen. Ohne sie wäre der runde Stamm begehbar, sobald er
        /// unten liegt — und die Bohlen wären Zierde.
        /// </summary>
        private static Collider BuildWalkway(Transform log, float length)
        {
            GameObject walkway = new GameObject("Lauffläche");
            walkway.transform.SetParent(log, false);
            walkway.transform.localPosition = new Vector3(0f, 0f, 0f);

            BoxCollider surface = walkway.AddComponent<BoxCollider>();

            // Der Stamm ist lokal zwei Einheiten hoch und wird ueber die Skala
            // gestreckt; die Lauffläche folgt in denselben lokalen Einheiten.
            surface.size = new Vector3(1.6f, 2f, 0.5f);
            surface.center = new Vector3(0f, 0f, 0.7f);
            surface.enabled = false;

            return surface;
        }

        private static GameObject[] BuildPlanks(
            GameObject root, Vector3 middle, Vector3 direction, Vector3 sideways)
        {
            var planks = new GameObject[2];

            for (int i = 0; i < planks.Length; i++)
            {
                float offset = i == 0 ? -2.2f : 2.2f;

                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"Bohle_{i + 1}";
                plank.transform.SetParent(root.transform, true);
                plank.transform.position =
                    middle + direction * offset + Vector3.up * 0.45f;
                plank.transform.rotation =
                    Quaternion.LookRotation(direction, Vector3.up);
                plank.transform.localScale = new Vector3(1.4f, 0.12f, 3.6f);

                UnityEngine.Object.DestroyImmediate(
                    plank.GetComponent<Collider>());

                plank.SetActive(false);
                planks[i] = plank;
            }

            // Die Bohlen, die noch am Ufer liegen und gelegt werden koennen.
            for (int i = 0; i < planks.Length; i++)
            {
                Vector3 position = SouthAbutment +
                                   sideways * (i == 0 ? -2.6f : -3.6f) +
                                   Vector3.up * 0.15f;

                GameObject loose = GameObject.CreatePrimitive(PrimitiveType.Cube);
                loose.name = $"Lose Bohle {i + 1}";
                loose.transform.SetParent(root.transform, true);
                loose.transform.position = Grounded(position) + Vector3.up * 0.1f;
                loose.transform.localScale = new Vector3(0.4f, 0.12f, 2.4f);

                BoxCollider trigger = loose.GetComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(4f, 6f, 2f);

                BridgePuzzlePart part = loose.AddComponent<BridgePuzzlePart>();
                Wire(part, ("role", (int)BridgePartRole.Plank));
            }

            return planks;
        }

        private static void BuildResonanceZone(GameObject root, Vector3 middle)
        {
            GameObject zone = new GameObject("Resonanzzone");
            zone.transform.SetParent(root.transform, true);
            zone.transform.position = middle;

            SphereCollider volume = zone.AddComponent<SphereCollider>();
            volume.isTrigger = true;
            volume.radius = 14f;

            zone.AddComponent<WatchResonanceZone>();
        }

        private static void BuildPart(
            GameObject root,
            BridgePartRole role,
            string name,
            Vector3 position,
            BridgePuzzle puzzle)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(root.transform, true);
            part.transform.position = Grounded(position) + Vector3.up * 0.5f;
            part.transform.localScale = new Vector3(0.5f, 1f, 0.5f);

            BoxCollider trigger = part.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(5f, 3f, 5f);

            BridgePuzzlePart component = part.AddComponent<BridgePuzzlePart>();
            Wire(component, ("role", (int)role));
            Wire(component, ("puzzle", puzzle));
        }

        /// <summary>
        /// Die Furt bekommt ein Auslösevolumen mit schadensloser Rücksetzung.
        /// Sie bleibt begehbar bis zur Mitte — kein unsichtbarer Rand.
        /// </summary>
        private static void BuildFord(GameObject environment)
        {
            Transform ford = environment.transform.Find("Furt");

            if (ford == null)
            {
                Debug.LogWarning(
                    "Brueckenraetsel: 'Furt' fehlt; die Ruecksetzung wurde " +
                    "nicht gesetzt.");
                return;
            }

            Transform existing = ford.Find("Stroemung");

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject current = new GameObject("Stroemung");
            current.transform.SetParent(ford, true);
            current.transform.position = FordCentre;

            BoxCollider volume = current.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = new Vector3(6f, 4f, 3.2f);

            GameObject safe = new GameObject("Sicheres Ufer");
            safe.transform.SetParent(current.transform, true);
            safe.transform.position =
                Grounded(FordCentre + new Vector3(0.5f, 0f, -4.5f));

            FordReset reset = current.AddComponent<FordReset>();
            Wire(reset, ("safeReturn", safe.transform));
        }

        // ------------------------------------------------------------------

        /// <summary>Legt eine Position auf den Boden, statt sie zu raten.</summary>
        private static Vector3 Grounded(Vector3 position)
        {
            Vector3 from = new Vector3(position.x, position.y + 60f, position.z);

            if (Physics.Raycast(
                    from, Vector3.down, out RaycastHit hit, 200f, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return position;
        }

        private static void Wire(
            UnityEngine.Object target,
            params (string Field, object Value)[] assignments)
        {
            SerializedObject serialized = new SerializedObject(target);

            foreach ((string field, object value) in assignments)
            {
                SerializedProperty property = serialized.FindProperty(field);

                if (property == null)
                {
                    Debug.LogWarning(
                        $"Brueckenraetsel: Feld '{field}' fehlt auf " +
                        $"{target.GetType().Name}.");
                    continue;
                }

                switch (value)
                {
                    case int number:
                        property.intValue = number;
                        break;

                    case UnityEngine.Object reference:
                        property.objectReferenceValue = reference;
                        break;

                    case Array array:
                        property.arraySize = array.Length;

                        for (int i = 0; i < array.Length; i++)
                        {
                            property.GetArrayElementAtIndex(i)
                                    .objectReferenceValue =
                                array.GetValue(i) as UnityEngine.Object;
                        }

                        break;

                    default:
                        Debug.LogWarning(
                            $"Brueckenraetsel: Typ von '{field}' wird nicht " +
                            "unterstuetzt.");
                        break;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
