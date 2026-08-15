using System;
using System.Collections.Generic;
using System.IO;
using Elyndor.Companion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Setzt Link, die Eule, in den Finsterwald (P1.8).
    ///
    /// Das Modell ist ein <b>Platzhalter aus Primitiven</b>. Im Projekt gibt es
    /// keinen Vogel — das Tierpack enthaelt Wolf, Hirsch, Fuchs und Vieh, aber
    /// nichts Gefiedertes. Statt etwas zu beschaffen steht hier eine erkennbare
    /// Eulensilhouette aus Kugeln und Quadern, die ihren Zweck im Namen traegt.
    /// Die finale Link-Kunst kommt spaeter.
    ///
    /// Die Sitzpunkte liegen an drei Orten des Slice: Startbereich, Lichtung
    /// und Bruecke. Keiner von ihnen kennt das Raetsel.
    /// </summary>
    public static class LinkCompanionBuilder
    {
        private const string ScenePath =
            "Assets/_Elyndor/Scenes/Finsterwald.unity";

        private const string RootName = "Link";
        private const string PerchRootName = "Link-Sitzpunkte";

        private const string MaterialFolder =
            "Assets/_Elyndor/Art/Companion";

        private const string FeatherMaterialPath =
            MaterialFolder + "/Link_Blockout_Gefieder.mat";

        private const string EyeMaterialPath =
            MaterialFolder + "/Link_Blockout_Auge.mat";

        /// <summary>
        /// Sitzpunkte: Ort, Blickziel, Ruhe.
        ///
        /// Der Sitzpunkt an der Bruecke schaut auf die Mitte der Luecke, also
        /// auf den <em>raeumlichen</em> Zusammenhang zwischen beiden Ufern —
        /// nicht auf einen Anker. Was dort zu sehen ist, hilft beim Verstehen
        /// der Lage und verraet keine Stellung.
        /// </summary>
        private static readonly (string Name, Vector3 Position,
            Vector3 LookAt, bool Quiet)[] Perches =
        {
            ("Sitz_Startbereich", new Vector3(1.6f, 0f, -74.5f),
                new Vector3(-2.9f, 1f, -82.5f), true),
            ("Sitz_Rastplatz", new Vector3(6.2f, 0f, -50.0f),
                new Vector3(4.5f, 1f, -52.5f), false),
            ("Sitz_Lichtung", new Vector3(-22.5f, 0f, -13.5f),
                new Vector3(-26f, 1f, -17f), false),
            ("Sitz_Bruecke_Sued", new Vector3(-1.4f, 0f, -6.0f),
                new Vector3(2f, 0.5f, -1.2f), false),
            ("Sitz_Bruecke_Seitenstein", new Vector3(6.8f, 0f, 1.4f),
                new Vector3(2f, 0.5f, -1.2f), false)
        };

        /// <summary>Hoehe der Sitzpunkte ueber dem Boden — Aeste, keine Steine.</summary>
        private const float PerchHeight = 3.1f;

        [MenuItem("Elyndor/Finsterwald/Link platzieren")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath, OpenSceneMode.Single);

            RemoveExisting();

            GameObject perchRoot = new GameObject(PerchRootName);
            var perches = new List<LinkPerch>();

            foreach ((string name, Vector3 position, Vector3 lookAt,
                         bool quiet) in Perches)
            {
                GameObject perchObject = new GameObject(name);
                perchObject.transform.SetParent(perchRoot.transform, true);
                perchObject.transform.position =
                    Grounded(position) + Vector3.up * PerchHeight;

                GameObject look = new GameObject("Blickziel");
                look.transform.SetParent(perchObject.transform, true);
                look.transform.position = lookAt;

                LinkPerch perch = perchObject.AddComponent<LinkPerch>();
                Wire(perch, ("lookTarget", look.transform));

                SerializedObject serialized = new SerializedObject(perch);
                serialized.FindProperty("quiet").boolValue = quiet;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                perches.Add(perch);
            }

            GameObject link = BuildBlockout();
            LinkCompanion companion = link.GetComponent<LinkCompanion>();
            Wire(companion, ("perches", perches.ToArray()));

            link.transform.position = perches[0].transform.position;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                "LINK_OK: genau eine Eule, " + perches.Count +
                " Sitzpunkte. Modell ist ein Primitiv-Platzhalter; kein " +
                "Vogel im Bestand, nichts beschafft.");
        }

        /// <summary>
        /// Entfernt jede vorhandene Link-Instanz und alle Sitzpunkte. Zwei
        /// Eulen waeren im Spiel sofort sichtbar und im Test schwer zu
        /// erklaeren.
        /// </summary>
        private static void RemoveExisting()
        {
            foreach (LinkCompanion existing in
                     UnityEngine.Object.FindObjectsByType<LinkCompanion>(FindObjectsInactive.Include))
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject perchRoot = GameObject.Find(PerchRootName);

            if (perchRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(perchRoot);
            }

            foreach (LinkPerch stray in
                     UnityEngine.Object.FindObjectsByType<LinkPerch>(FindObjectsInactive.Include))
            {
                UnityEngine.Object.DestroyImmediate(stray.gameObject);
            }
        }

        /// <summary>
        /// Eulensilhouette aus Primitiven: gedrungener Koerper, runder Kopf
        /// ohne Hals, zwei grosse Augen, Federohren. Rund 0,3 m hoch.
        ///
        /// <b>Ohne Collider.</b> Link kann Aren nicht im Weg stehen — das ist
        /// keine Feineinstellung, sondern der Grund, warum ein Begleiter nie
        /// zur Falle wird.
        /// </summary>
        private static GameObject BuildBlockout()
        {
            Material feather = EnsureMaterial(
                FeatherMaterialPath, new Color(0.38f, 0.33f, 0.28f));
            Material eye = EnsureMaterial(
                EyeMaterialPath, new Color(0.86f, 0.72f, 0.22f));

            GameObject root = new GameObject(RootName);

            AddPart(root.transform, PrimitiveType.Capsule, "Koerper",
                new Vector3(0f, 0.12f, 0f), new Vector3(0.16f, 0.12f, 0.14f),
                Quaternion.identity, feather);

            AddPart(root.transform, PrimitiveType.Sphere, "Kopf",
                new Vector3(0f, 0.25f, 0f), new Vector3(0.17f, 0.15f, 0.15f),
                Quaternion.identity, feather);

            AddPart(root.transform, PrimitiveType.Sphere, "Auge_L",
                new Vector3(-0.045f, 0.26f, 0.07f), Vector3.one * 0.05f,
                Quaternion.identity, eye);

            AddPart(root.transform, PrimitiveType.Sphere, "Auge_R",
                new Vector3(0.045f, 0.26f, 0.07f), Vector3.one * 0.05f,
                Quaternion.identity, eye);

            AddPart(root.transform, PrimitiveType.Cube, "Schnabel",
                new Vector3(0f, 0.235f, 0.085f),
                new Vector3(0.025f, 0.04f, 0.03f),
                Quaternion.Euler(45f, 0f, 0f), eye);

            AddPart(root.transform, PrimitiveType.Cube, "Federohr_L",
                new Vector3(-0.06f, 0.33f, -0.01f),
                new Vector3(0.035f, 0.07f, 0.03f),
                Quaternion.Euler(0f, 0f, 12f), feather);

            AddPart(root.transform, PrimitiveType.Cube, "Federohr_R",
                new Vector3(0.06f, 0.33f, -0.01f),
                new Vector3(0.035f, 0.07f, 0.03f),
                Quaternion.Euler(0f, 0f, -12f), feather);

            AddPart(root.transform, PrimitiveType.Cube, "Fluegel_L",
                new Vector3(-0.085f, 0.13f, -0.01f),
                new Vector3(0.03f, 0.15f, 0.11f),
                Quaternion.identity, feather);

            AddPart(root.transform, PrimitiveType.Cube, "Fluegel_R",
                new Vector3(0.085f, 0.13f, -0.01f),
                new Vector3(0.03f, 0.15f, 0.11f),
                Quaternion.identity, feather);

            root.AddComponent<LinkCompanion>();

            return root;
        }

        private static void AddPart(
            Transform parent,
            PrimitiveType type,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;

            // Kein Collider: Link soll nie zwischen Aren und seinem Weg stehen.
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        // ------------------------------------------------------------------

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

        private static Material EnsureMaterial(string path, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);

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

            AssetDatabase.CreateAsset(material, path);

            return material;
        }

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
                    Debug.LogWarning($"Link: Feld '{field}' fehlt.");
                    continue;
                }

                switch (value)
                {
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
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
