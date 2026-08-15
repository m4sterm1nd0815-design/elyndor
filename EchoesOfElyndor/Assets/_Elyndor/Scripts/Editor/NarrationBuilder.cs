using System;
using System.IO;
using Elyndor.Memory;
using Elyndor.Narration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Legt den Textbestand des Slice an und setzt die Trägerobjekte (P1.9).
    ///
    /// Was hier steht, ist ausschließlich der bestätigte Kanon. Kein Name
    /// fällt, keine Herkunft wird erklärt, keine Frage beantwortet, die der
    /// Slice offen lassen soll. Wer eine Zeile ändern will, ändert sie hier —
    /// nicht in einem Inspectorfeld irgendeiner Szene.
    /// </summary>
    public static class NarrationBuilder
    {
        private const string ScenePath =
            "Assets/_Elyndor/Scenes/Finsterwald.unity";

        private const string CatalogFolder = "Assets/_Elyndor/Narration";

        private const string CatalogPath =
            CatalogFolder + "/FinsterwaldNarration.asset";

        private const string RootName = "Erzaehlung";

        /// <summary>
        /// Der bestätigte Textbestand.
        ///
        /// Die Erinnerung bleibt bruchstückhaft und ohne erkennbare Sprecher.
        /// Was Aren daraus sicher mitnimmt, ist ein einziger Satz: <em>Ich war
        /// hier.</em> Nicht wer bei ihm war, nicht warum, nicht wann, und nicht,
        /// wer seine Erinnerungen genommen hat.
        /// </summary>
        private static NarrationEntry[] BuildEntries() => new[]
        {
            new NarrationEntry
            {
                Key = NarrationKeys.MemoryBridge,
                Duration = 9f,
                Text =
                    "Die Watch surrt. Fuer einen Atemzug traegt der Bach wieder " +
                    "eine Bruecke — Bohlen, ein gespanntes Seil, Schritte. Drei " +
                    "Gestalten pruefen etwas, das nicht mehr zu sehen ist. Eine " +
                    "von ihnen dreht sich um. Dann reisst das Bild."
            },
            new NarrationEntry
            {
                Key = NarrationKeys.MemoryBridgeAftermath,
                Duration = 7f,
                Text =
                    "Ich war hier. Nicht als Fremder — ich kannte diesen " +
                    "Uebergang. Mehr gibt die Erinnerung nicht her."
            },
            new NarrationEntry
            {
                Key = NarrationKeys.UnknownVoiceBridge,
                Duration = 6f,
                Text =
                    "Unbekannte Stimme: „Du suchst immer nach dem Weg, " +
                    "Aren.“"
            },
            new NarrationEntry
            {
                Key = NarrationKeys.InscriptionMark,
                Duration = 7f,
                Text =
                    "In den Stein geritzt: drei Linien, die sich nicht kreuzen. " +
                    "Dasselbe Zeichen wie an der anderen Stelle. Aren weiss " +
                    "nicht, wofuer es steht — nur, dass er es schon einmal " +
                    "gesehen hat."
            },
            new NarrationEntry
            {
                Key = NarrationKeys.LoreRestingPlace,
                Duration = 6f,
                Text =
                    "Kalte Asche, ordentlich zusammengeschoben. Wer hier sass, " +
                    "hatte es nicht eilig."
            },
            new NarrationEntry
            {
                Key = NarrationKeys.LoreStreamStones,
                Duration = 6f,
                Text =
                    "Die Steine im Bach liegen zu regelmaessig fuer Zufall. " +
                    "Jemand hat sie gelegt. Der Bach hat sie seither verschoben."
            },
            new NarrationEntry
            {
                Key = NarrationKeys.LoreBridgeRemnant,
                Duration = 7f,
                Text =
                    "Der Balken ist sauber abgetrennt, nicht gebrochen. Diese " +
                    "Bruecke ist nicht eingestuerzt. Sie wurde geoeffnet."
            }
        };

        /// <summary>Wo die optionalen Texte hängen. Alles freiwillig.</summary>
        private static readonly (string Name, string Key, Vector3 Position)[]
            Inscriptions =
            {
                ("Gravur_Rastplatz", NarrationKeys.InscriptionMark,
                    new Vector3(5.9f, 0f, -25.7f)),
                ("Gravur_Bruecke", NarrationKeys.InscriptionMark,
                    new Vector3(1.7f, 0f, -6.8f)),
                ("Lore_Rastplatz", NarrationKeys.LoreRestingPlace,
                    new Vector3(4.1f, 0f, -24.3f)),
                ("Lore_Bachsteine", NarrationKeys.LoreStreamStones,
                    new Vector3(32.6f, 0f, -1.6f)),
                ("Lore_Brueckenrest", NarrationKeys.LoreBridgeRemnant,
                    new Vector3(3.7f, 0f, 3.4f))
            };

        [MenuItem("Elyndor/Finsterwald/Erzaehlung einrichten")]
        public static void Build()
        {
            NarrationCatalog catalog = EnsureCatalog();

            Scene scene = EditorSceneManager.OpenScene(
                ScenePath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find(RootName);

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            GameObject root = new GameObject(RootName);

            foreach ((string name, string key, Vector3 position) in Inscriptions)
            {
                GameObject marker =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = name;
                marker.transform.SetParent(root.transform, true);
                marker.transform.position =
                    Grounded(position) + Vector3.up * 0.18f;
                marker.transform.localScale =
                    new Vector3(0.42f, 0.32f, 0.12f);
                marker.transform.rotation =
                    Quaternion.Euler(-24f, 0f, 0f);

                BoxCollider trigger = marker.GetComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(5f, 6f, 16f);

                CataloguedExaminable examinable =
                    marker.AddComponent<CataloguedExaminable>();

                examinable.Configure(catalog, key);
                Wire(examinable, ("interactionPrompt", "Ansehen"));
                EditorUtility.SetDirty(examinable);
            }

            // Die unbekannte Stimme kommt erst nach der Erinnerung — und nur
            // dieses eine Mal.
            GameObject voice = new GameObject("Nach der Erinnerung");
            voice.transform.SetParent(root.transform, true);

            MemoryEchoNarration narration =
                voice.AddComponent<MemoryEchoNarration>();

            narration.Configure(catalog, "finsterwald_bruecke_01", new[]
            {
                NarrationKeys.MemoryBridgeAftermath,
                NarrationKeys.UnknownVoiceBridge
            });

            EditorUtility.SetDirty(narration);

            SyncMemorySiteText(catalog);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                "NARRATION_OK: " + catalog.Entries.Count + " Schluessel, " +
                Inscriptions.Length + " optionale Traeger, eine Stimme nach " +
                "der Erinnerung. Kein Name faellt.");
        }

        /// <summary>
        /// Zieht den Text der Memory Site aus dem Katalog nach. Sonst gaebe es
        /// zwei Fassungen desselben Textes — eine im Katalog und eine in der
        /// Szene — und die Uebersetzung fände nur eine davon.
        /// </summary>
        private static void SyncMemorySiteText(NarrationCatalog catalog)
        {
            NarrationEntry entry = catalog.Find(NarrationKeys.MemoryBridge);

            if (entry == null)
            {
                return;
            }

            foreach (MemorySite site in
                     UnityEngine.Object.FindObjectsByType<MemorySite>(
                         FindObjectsInactive.Include))
            {
                if (site.SiteId != "finsterwald_bruecke_01")
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(site);
                serialized.FindProperty("memoryText").stringValue = entry.Text;
                serialized.FindProperty("memoryTextDuration").floatValue =
                    entry.Duration;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(site);
            }
        }

        private static NarrationCatalog EnsureCatalog()
        {
            EnsureFolder(CatalogFolder);

            NarrationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<NarrationCatalog>(CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<NarrationCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetEntries(BuildEntries());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            return catalog;
        }

        // ------------------------------------------------------------------

        private static Vector3 Grounded(Vector3 position)
        {
            Vector3 from = new Vector3(position.x, position.y + 60f, position.z);

            return Physics.Raycast(
                from, Vector3.down, out RaycastHit hit, 200f, ~0,
                QueryTriggerInteraction.Ignore)
                ? hit.point
                : position;
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
                    Debug.LogWarning($"Erzaehlung: Feld '{field}' fehlt.");
                    continue;
                }

                switch (value)
                {
                    case string text:
                        property.stringValue = text;
                        break;

                    case UnityEngine.Object reference:
                        property.objectReferenceValue = reference;
                        break;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
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
