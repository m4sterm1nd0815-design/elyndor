using System.Collections.Generic;
using System.IO;
using Elyndor.Core;
using Elyndor.Puzzles;
using Elyndor.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Richtet die Regeneration des Brückenabschnitts ein (P1.10).
    ///
    /// Ausgewählt wird nach Nähe zur Brücke, nicht nach Menge: ein paar
    /// Pflanzen, das Wasser der Furt-Seite, das Licht. Der Wald soll nicht
    /// grün werden, sondern an einer Stelle anders sein.
    /// </summary>
    public static class RegenerationBuilder
    {
        private const string ScenePath =
            "Assets/_Elyndor/Scenes/Finsterwald.unity";

        private const string RootName = "Regeneration";

        private const string PlaceholderPath =
            "Assets/_Elyndor/Audio/Placeholder/PLACEHOLDER_Wald_Antwort.wav";

        /// <summary>Mitte der Brückenlücke; von dort aus wird gemessen.</summary>
        private static readonly Vector3 Centre = new Vector3(2f, 0f, -1.2f);

        /// <summary>Wie weit die Veränderung reicht. Bewusst klein.</summary>
        private const float Radius = 13f;

        [MenuItem("Elyndor/Finsterwald/Regeneration einrichten")]
        public static void Build()
        {
            GenerateAnswerPlaceholder();

            Scene scene = EditorSceneManager.OpenScene(
                ScenePath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find(RootName);

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            GameObject root = new GameObject(RootName);

            var shoots = BuildShoots(root.transform);
            var plants = CollectNearbyVegetation();
            var water = CollectNearbyWater();

            Light sun = null;

            foreach (Light light in
                     UnityEngine.Object.FindObjectsByType<Light>(
                         FindObjectsInactive.Include))
            {
                if (light.type == LightType.Directional)
                {
                    sun = light;
                    break;
                }
            }

            FinsterwaldRegeneration regeneration =
                root.AddComponent<FinsterwaldRegeneration>();

            // blockedPath bleibt bewusst leer — siehe Hinweis unten.
            regeneration.Configure(
                UnityEngine.Object.FindAnyObjectByType<BridgePuzzle>(),
                shoots,
                plants,
                water,
                sun,
                null);

            EditorUtility.SetDirty(regeneration);
            AssignAudio();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                "REGENERATION_OK: " + shoots.Length + " Triebe, " +
                plants.Length + " Pflanzen, " + water.Length +
                " Wasserflaechen, Licht=" + (sun != null) +
                ". Wegoeffnung absichtlich nicht verdrahtet — welcher Weg " +
                "sich oeffnen soll, ist eine Level-Entscheidung.");
        }

        /// <summary>
        /// Neue, zunächst abgeschaltete Triebe. Eigene Objekte statt fremder:
        /// vorhandene Handarbeit wird nicht umgebaut, nur ergänzt.
        /// </summary>
        private static GameObject[] BuildShoots(Transform parent)
        {
            var shoots = new List<GameObject>();

            Vector3[] spots =
            {
                new Vector3(0.4f, 0f, -4.6f),
                new Vector3(3.4f, 0f, -3.2f),
                new Vector3(1.2f, 0f, 1.8f),
                new Vector3(4.2f, 0f, 2.4f),
                new Vector3(-1.1f, 0f, -2.4f)
            };

            Material green = EnsureMaterial(
                "Assets/_Elyndor/Art/Puzzles/Bruecke/Wald_Trieb.mat",
                new Color(0.38f, 0.61f, 0.26f));

            foreach (Vector3 spot in spots)
            {
                GameObject shoot =
                    GameObject.CreatePrimitive(PrimitiveType.Capsule);
                shoot.name = "Trieb";
                shoot.transform.SetParent(parent, true);
                shoot.transform.position = Grounded(spot) + Vector3.up * 0.16f;
                shoot.transform.localScale = new Vector3(0.07f, 0.18f, 0.07f);
                shoot.GetComponent<Renderer>().sharedMaterial = green;

                UnityEngine.Object.DestroyImmediate(
                    shoot.GetComponent<Collider>());

                shoot.SetActive(false);
                shoots.Add(shoot);
            }

            return shoots.ToArray();
        }

        private static Renderer[] CollectNearbyVegetation()
        {
            var found = new List<Renderer>();
            GameObject environment = GameObject.Find("Environment");

            if (environment == null)
            {
                return found.ToArray();
            }

            foreach (Renderer renderer in
                     environment.GetComponentsInChildren<Renderer>(false))
            {
                string name = renderer.gameObject.name;

                bool isPlant =
                    name.StartsWith("Flower") || name.StartsWith("Clover") ||
                    name.StartsWith("Grass") || name.StartsWith("Fern");

                if (!isPlant)
                {
                    continue;
                }

                if (Vector3.Distance(renderer.transform.position, Centre) <=
                    Radius)
                {
                    found.Add(renderer);
                }

                // Wenige, nicht alle: der Wald soll nicht ploetzlich gruen sein.
                if (found.Count >= 12)
                {
                    break;
                }
            }

            return found.ToArray();
        }

        private static Renderer[] CollectNearbyWater()
        {
            var found = new List<Renderer>();
            GameObject environment = GameObject.Find("Environment");

            if (environment == null)
            {
                return found.ToArray();
            }

            foreach (Renderer renderer in
                     environment.GetComponentsInChildren<Renderer>(false))
            {
                if (renderer.gameObject.name != "Wasser")
                {
                    continue;
                }

                if (Vector3.Distance(renderer.transform.position, Centre) <=
                    Radius)
                {
                    found.Add(renderer);
                }
            }

            return found.ToArray();
        }

        private static void AssignAudio()
        {
            SfxLibrary library =
                UnityEngine.Object.FindAnyObjectByType<SfxLibrary>();

            AudioClip clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(PlaceholderPath);

            if (library == null || clip == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(library);
            SerializedProperty property =
                serialized.FindProperty("regionRegeneratedClip");

            if (property != null)
            {
                property.objectReferenceValue = clip;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(library);
            }
        }

        /// <summary>
        /// Ein leiser, warmer Aufklang. Erzeugt, weil der Bestand nur
        /// Aufschläge und Schritte kennt — nichts, was „etwas kommt zurück"
        /// klingt. Deterministisch aus festem Keim, im Namen als Platzhalter
        /// gekennzeichnet.
        /// </summary>
        public static void GenerateAnswerPlaceholder()
        {
            EnsureFolder("Assets/_Elyndor/Audio/Placeholder");

            const int rate = 44100;
            const float duration = 1.6f;
            int count = (int)(rate * duration);
            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;

                // Zwei ruhige Teiltoene, eine Quinte auseinander.
                float tone =
                    Mathf.Sin(2f * Mathf.PI * 174f * t) * 0.6f +
                    Mathf.Sin(2f * Mathf.PI * 261f * t) * 0.4f;

                // Langsam auf, langsam ab: nichts daran soll erschrecken.
                float envelope =
                    Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);

                samples[i] = tone * envelope * envelope * 0.55f;
            }

            WriteWav(PlaceholderPath, samples, rate);
            AssetDatabase.ImportAsset(
                PlaceholderPath, ImportAssetOptions.ForceUpdate);
        }

        // ------------------------------------------------------------------

        private static void WriteWav(
            string assetPath, float[] samples, int rate)
        {
            string full = Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!, assetPath);

            using FileStream stream = new FileStream(full, FileMode.Create);
            using BinaryWriter writer = new BinaryWriter(stream);

            int dataBytes = samples.Length * 2;

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(rate);
            writer.Write(rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);

            foreach (float sample in samples)
            {
                writer.Write(
                    (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
            }
        }

        private static Material EnsureMaterial(string path, Color colour)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(Path.GetDirectoryName(path)!.Replace('\\', '/'));

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard");

            Material material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(path)
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", colour);
            }

            AssetDatabase.CreateAsset(material, path);

            return material;
        }

        private static Vector3 Grounded(Vector3 position)
        {
            Vector3 from = new Vector3(position.x, position.y + 60f, position.z);

            return Physics.Raycast(
                from, Vector3.down, out RaycastHit hit, 200f, ~0,
                QueryTriggerInteraction.Ignore)
                ? hit.point
                : position;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
