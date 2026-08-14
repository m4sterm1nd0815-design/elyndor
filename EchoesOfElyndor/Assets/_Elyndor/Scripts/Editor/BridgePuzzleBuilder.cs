using System;
using System.IO;
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

        private const string MaterialFolder =
            "Assets/_Elyndor/Art/Puzzles/Bruecke";

        private const string StoneMaterialPath =
            MaterialFolder + "/Bruecke_Stein.mat";

        private const string NotchMaterialPath =
            MaterialFolder + "/Bruecke_Kerbe.mat";

        private const string WoodMaterialPath =
            MaterialFolder + "/Bruecke_Holz.mat";

        private const string RopeMaterialPath =
            MaterialFolder + "/Bruecke_Seil.mat";

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
            Material stoneMaterial = EnsureMaterial(
                StoneMaterialPath, new Color(0.42f, 0.41f, 0.38f));
            Material notchMaterial = EnsureMaterial(
                NotchMaterialPath, new Color(0.11f, 0.10f, 0.09f));

            // Die drei Anker stehen bewusst weit auseinander: tief am
            // Suedufer, seitlich am Seitenstein, hoch am Nordufer. Der
            // Lastverlauf des Echos laeuft genau diesen Weg.
            //
            // Ihre Formen unterscheiden sich, damit man im Gespraech ueber sie
            // reden kann, ohne auf sie zu zeigen: der Suedanker ist breit und
            // liegt tief, der Seitenanker ist ein schlanker hoher Pfosten, der
            // Nordanker liegt dazwischen. Drei gleiche Zylinder waeren zwar
            // ordentlich, aber ununterscheidbar.
            var placements =
                new (BridgeAnchorId Id, string Name, Vector3 Position,
                     Vector3 Scale)[]
                {
                    (BridgeAnchorId.SouthDeep, "Anker_Sued_Tief",
                        SouthAbutment + sideways * 1.6f,
                        new Vector3(1.15f, 0.28f, 1.15f)),
                    // Weit genug vom liegenden Stamm entfernt: bei 4,2 m
                    // steckte der Stein sichtbar in ihm.
                    (BridgeAnchorId.Side, "Anker_Seitenstein",
                        middle + sideways * 2.8f + Vector3.forward * 2.6f,
                        new Vector3(0.68f, 0.75f, 0.68f)),
                    (BridgeAnchorId.North, "Anker_Nord",
                        NorthAbutment + sideways * 1.4f,
                        new Vector3(0.9f, 0.48f, 0.9f))
                };

            var anchors = new BridgeAnchor[placements.Length];

            for (int i = 0; i < placements.Length; i++)
            {
                (BridgeAnchorId id, string name, Vector3 position,
                    Vector3 scale) = placements[i];

                GameObject stone = GameObject.CreatePrimitive(
                    PrimitiveType.Cylinder);
                stone.name = name;
                stone.transform.SetParent(root.transform, true);
                stone.transform.position =
                    Grounded(position) + Vector3.up * scale.y;
                stone.transform.localScale = scale;

                stone.GetComponent<Renderer>().sharedMaterial = stoneMaterial;

                GameObject[] notchGroups =
                    BuildNotchFaces(stone.transform, notchMaterial);

                // Trigger fuer den InteractionDetector.
                SphereCollider trigger = stone.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 2.2f;

                BridgeAnchor anchor = stone.AddComponent<BridgeAnchor>();
                Wire(anchor, ("anchorId", (int)id),
                    ("notchGroups", notchGroups));
                anchors[i] = anchor;
            }

            return anchors;
        }

        /// <summary>
        /// Legt die drei Kerbenseiten eines Ankers an: eine, zwei und drei
        /// Kerben.
        ///
        /// Vorher trug ein Anker eine einzige Kerbe, und seine Stellung war
        /// allein an der Drehung des Steins abzulesen. Damit war die
        /// <em>Zahl</em> — auf die es ankommt, und von der die Loesung spricht
        /// — überhaupt nicht sichtbar. Ein Spieler haette „irgendwo eine
        /// Kerbe" gesehen und daraus nichts schliessen koennen.
        ///
        /// Die Seite <c>f</c> sitzt bei lokal -120°·f. Der Stein dreht sich um
        /// +120°·Stellung; damit steht genau die Seite vorn, deren Nummer der
        /// Stellung entspricht — und die traegt <c>f + 1</c> Kerben.
        /// </summary>
        private static GameObject[] BuildNotchFaces(
            Transform stone, Material notchMaterial)
        {
            var groups = new GameObject[BridgePuzzleRules.SettingsPerAnchor];

            for (int face = 0; face < groups.Length; face++)
            {
                int notches = BridgePuzzleRules.NotchesForSetting(face);

                GameObject group = new GameObject($"Kerben_{notches}");
                group.transform.SetParent(stone, false);
                group.transform.localPosition = Vector3.zero;
                group.transform.localRotation =
                    Quaternion.Euler(0f, -120f * face, 0f);

                // Die Kerben liegen auf der Oberseite, nicht am Mantel. Ein
                // stehender Spieler blickt auf diese Steine hinab; am Mantel
                // liefen die Kerben um den Zylinder herum und waren aus keinem
                // Winkel zu zaehlen.
                const float spacing = 0.17f;
                float start = -(notches - 1) * spacing * 0.5f;

                for (int n = 0; n < notches; n++)
                {
                    GameObject mark =
                        GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mark.name = "Kerbe";
                    mark.transform.SetParent(group.transform, false);

                    // Unitys Zylinder reicht lokal von y = -1 bis y = +1; die
                    // Oberseite liegt also bei 1, nicht bei 0,5. Bei 0,5 lagen
                    // die Kerben auf halber Hoehe im Stein und waren gar nicht
                    // zu sehen.
                    mark.transform.localPosition =
                        new Vector3(start + n * spacing, 1f, 0.06f);
                    mark.transform.localScale =
                        new Vector3(0.09f, 0.18f, 0.5f);

                    mark.GetComponent<Renderer>().sharedMaterial = notchMaterial;

                    UnityEngine.Object.DestroyImmediate(
                        mark.GetComponent<Collider>());
                }

                // Nur die Gruppe der Ausgangsstellung ist sichtbar. Zur
                // Laufzeit setzt der Anker das selbst; im Editor laeuft sein
                // Awake nicht, und wer die Szene oeffnet, saehe sonst alle
                // sechs Kerben auf einmal.
                group.SetActive(face == 0);

                groups[face] = group;
            }

            return groups;
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
            // Deutlich seitlich versetzt, damit er keinen Anker verdeckt.
            log.transform.position =
                middle + sideways * 7.5f + Vector3.up * 0.9f;
            log.transform.rotation =
                Quaternion.FromToRotation(Vector3.up, sideways);

            log.GetComponent<Renderer>().sharedMaterial = EnsureMaterial(
                WoodMaterialPath, new Color(0.36f, 0.26f, 0.16f));

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
            Material wood = EnsureMaterial(
                WoodMaterialPath, new Color(0.36f, 0.26f, 0.16f));

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
                plank.GetComponent<Renderer>().sharedMaterial = wood;

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
                loose.GetComponent<Renderer>().sharedMaterial = wood;

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

        /// <summary>
        /// Baut ein bedienbares Teil. Die Form sagt, wozu es dient — vorher
        /// standen hier zwei gleiche Wuerfel, und wer sie ansah, konnte
        /// Seilbock und Freigabe nicht unterscheiden.
        /// </summary>
        private static void BuildPart(
            GameObject root,
            BridgePartRole role,
            string name,
            Vector3 position,
            BridgePuzzle puzzle)
        {
            Material wood = EnsureMaterial(
                WoodMaterialPath, new Color(0.36f, 0.26f, 0.16f));
            Material rope = EnsureMaterial(
                RopeMaterialPath, new Color(0.62f, 0.55f, 0.38f));

            GameObject part = new GameObject(name);
            part.transform.SetParent(root.transform, true);
            part.transform.position = Grounded(position);

            if (role == BridgePartRole.TensionCheck)
            {
                // Seilbock: zwei schraege Pfosten mit einem Seil darueber.
                // Eine Form, die man als „hier laeuft ein Seil" liest.
                AddPost(part.transform, new Vector3(-0.35f, 0.45f, 0f),
                    new Vector3(0.12f, 0.9f, 0.12f), -14f, wood);
                AddPost(part.transform, new Vector3(0.35f, 0.45f, 0f),
                    new Vector3(0.12f, 0.9f, 0.12f), 14f, wood);

                GameObject line = GameObject.CreatePrimitive(
                    PrimitiveType.Cylinder);
                line.name = "Seil";
                line.transform.SetParent(part.transform, false);
                line.transform.localPosition = new Vector3(0f, 0.88f, 0f);
                line.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                line.transform.localScale = new Vector3(0.06f, 0.5f, 0.06f);
                line.GetComponent<Renderer>().sharedMaterial = rope;
                UnityEngine.Object.DestroyImmediate(
                    line.GetComponent<Collider>());
            }
            else
            {
                // Stammfreigabe: ein Pfosten mit deutlich abgewinkeltem Hebel.
                AddPost(part.transform, new Vector3(0f, 0.4f, 0f),
                    new Vector3(0.16f, 0.8f, 0.16f), 0f, wood);

                GameObject lever = GameObject.CreatePrimitive(
                    PrimitiveType.Cube);
                lever.name = "Hebel";
                lever.transform.SetParent(part.transform, false);
                lever.transform.localPosition = new Vector3(0.22f, 0.86f, 0f);
                lever.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
                lever.transform.localScale = new Vector3(0.6f, 0.1f, 0.1f);
                lever.GetComponent<Renderer>().sharedMaterial = wood;
                UnityEngine.Object.DestroyImmediate(
                    lever.GetComponent<Collider>());
            }

            BoxCollider trigger = part.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.8f, 0f);
            trigger.size = new Vector3(3.2f, 2.6f, 3.2f);

            BridgePuzzlePart component = part.AddComponent<BridgePuzzlePart>();
            Wire(component, ("role", (int)role));
            Wire(component, ("puzzle", puzzle));
        }

        private static void AddPost(
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            float tiltDegrees,
            Material material)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Pfosten";
            post.transform.SetParent(parent, false);
            post.transform.localPosition = localPosition;
            post.transform.localRotation =
                Quaternion.Euler(0f, 0f, tiltDegrees);
            post.transform.localScale = localScale;
            post.GetComponent<Renderer>().sharedMaterial = material;

            // Der Pfosten selbst bleibt fest: man soll dagegenlaufen koennen.
            post.GetComponent<BoxCollider>().isTrigger = false;
        }

        /// <summary>
        /// Legt ein schlichtes Material an oder nimmt das vorhandene. Bewusst
        /// eigene Materialien und keine Standardfarben: die Funktionsobjekte
        /// sollen sich vom Wald absetzen, ohne dass dafuer Kunst noetig waere.
        /// </summary>
        private static Material EnsureMaterial(string path, Color color)
        {
            Material existing =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(MaterialFolder);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard");

            Material material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(path)
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.12f);
            }

            AssetDatabase.CreateAsset(material, path);

            return material;
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            string leaf = Path.GetFileName(path);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

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
