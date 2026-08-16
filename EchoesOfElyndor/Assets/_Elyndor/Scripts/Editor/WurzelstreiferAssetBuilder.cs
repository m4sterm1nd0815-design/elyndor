using System;
using System.Collections.Generic;
using System.IO;
using Elyndor.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Baut das fertige Asset des Wurzelstreifers auf: Importeinstellungen,
    /// URP-Materialien, Animator-Controller und Prefab.
    ///
    /// Quelle ist <c>Art_Source/Wurzelstreifer/build_wurzelstreifer.py</c>. Das
    /// Skript baut das Modell in Blender aus den Zahlen des freigegebenen
    /// Konzeptentwurfs und exportiert die FBX; dieses Werkzeug macht daraus
    /// das, was jemand in eine Szene zieht.
    ///
    /// Warum die Einstellungen hier stehen und nicht im Inspector: was jemand
    /// im Inspector klickt, steht nirgends geschrieben und ist beim naechsten
    /// Asset wieder weg. So steht es im Pipelinedokument, und so ist es auch
    /// beim Pipeline-Testfels gehalten.
    ///
    /// <b>Stand der Freigabe.</b> Das Asset ist technisch integriert. Die
    /// finale kuenstlerische Abnahme steht aus und ist ein menschliches Gate;
    /// kein Wert in dieser Datei behauptet etwas darueber.
    /// </summary>
    public static class WurzelstreiferAssetBuilder
    {
        private const string Folder =
            "Assets/_Elyndor/Art/Enemy/Finsterwald/ELY_Enemy_Wurzelstreifer";

        internal const string ModelPath =
            Folder + "/ELY_Enemy_Wurzelstreifer.fbx";

        private const string BarkMaterialPath =
            Folder + "/M_ELY_Enemy_Wurzelstreifer.mat";

        private const string CrackMaterialPath =
            Folder + "/M_ELY_Enemy_Wurzelstreifer_Risse.mat";

        private const string ControllerPath =
            Folder + "/ELY_Enemy_Wurzelstreifer.controller";

        /// <summary>Das Lieferprodukt. Modelle gehen nie direkt in Szenen.</summary>
        internal const string PrefabPath =
            "Assets/_Elyndor/Prefabs/Enemies/Wurzelstreifer.prefab";

        private const string UrpLitShader = "Universal Render Pipeline/Lit";

        /// <summary>
        /// Das Modell ist in Metern gebaut und kommt in Metern an. Jede
        /// Abweichung von 1 waere eine Korrektur am falschen Ende — der
        /// Standard verlangt ausdruecklich, eine falsche Groesse neu zu
        /// exportieren statt sie im Prefab zu skalieren.
        /// </summary>
        private const float ModelScale = 1f;

        /// <summary>
        /// Welcher Clip hinter welchem Animator-Zustand liegt.
        ///
        /// Die Zustandsnamen kommen aus <see cref="EnemyAnimationDriver"/>,
        /// die Clipnamen aus dem Blender-Aufbau. Anders als beim Blockout
        /// greift kein Zustand mehr auf einen geliehenen Clip zurueck: der
        /// Konzeptentwurf verlangt fuer den seitlichen Schritt und das schwere
        /// Straucheln eigene Zyklen, und beide gibt es jetzt.
        ///
        /// "Schritt" bekommt den Seitwaertszyklus, weil
        /// <see cref="EnemyAnimationDriver.SelectState"/> diesen Zustand
        /// ausschliesslich beim Umkreisen waehlt. Der mitgelieferte Clip
        /// "Trab" bleibt deshalb ungenutzt — die Zustandsmaschine kennt heute
        /// keinen Vorwaertsschritt.
        /// </summary>
        private static readonly (string State, string Clip)[] StateClips =
        {
            (EnemyAnimationDriver.StateIdle, "Idle"),
            (EnemyAnimationDriver.StateListen, "Lauschen"),
            (EnemyAnimationDriver.StateWalk, "Schritt"),
            (EnemyAnimationDriver.StateRun, "Lauf"),
            (EnemyAnimationDriver.StateTelegraph, "Telegraph"),
            (EnemyAnimationDriver.StateStrike, "Sprungbiss"),
            (EnemyAnimationDriver.StateFlinch, "Flinch"),
            (EnemyAnimationDriver.StateStagger, "Stagger"),
            (EnemyAnimationDriver.StateFlee, "Flucht"),
            (EnemyAnimationDriver.StateDefeat, "Niederlage"),
        };

        /// <summary>
        /// Welche Clips in Schleife laufen. Ein Zyklus ohne Schleifenflag
        /// haelt am letzten Bild an, und der Gegner steht dann mitten im
        /// Schritt still.
        /// </summary>
        private static readonly HashSet<string> LoopingClips =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Idle", "Lauschen", "Schritt", "Trab", "Lauf", "Flucht",
            };

        [MenuItem("Elyndor/Art/Wurzelstreifer-Asset aufbauen")]
        public static void Build()
        {
            if (!File.Exists(ModelPath))
            {
                throw new FileNotFoundException(
                    $"Wurzelstreifer: {ModelPath} fehlt. Zuerst mit " +
                    "Art_Source/Wurzelstreifer/build_wurzelstreifer.py aus " +
                    "Blender exportieren.", ModelPath);
            }

            ConfigureImporter();

            Material bark = BuildBarkMaterial();
            Material cracks = BuildCrackMaterial();
            AnimatorController controller = BuildAnimatorController();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);

            if (model == null)
            {
                throw new InvalidOperationException(
                    $"Wurzelstreifer: {ModelPath} liess sich nicht laden.");
            }

            EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));

            GameObject prefab = WurzelstreiferBuilder.BuildPrefab(
                model,
                "Wurzelstreifer",
                ModelScale,
                controller,
                new[] { bark, cracks },
                PrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "WURZELSTREIFER_ASSET_OK: Prefab unter " + PrefabPath +
                ", Modell " + ModelPath +
                ", Controller " + ControllerPath +
                ". Technisch integriert; finale Art-Abnahme steht aus. " +
                "Prefab=" + (prefab != null));
        }

        /// <summary>Einstiegspunkt fuer den Batchmode: aufbauen und pruefen.</summary>
        public static void BuildAndValidateBatch()
        {
            try
            {
                Build();
                ModelImportValidator.Validate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"WURZELSTREIFER_ASSET fehlgeschlagen: {exception}");
                EditorApplication.Exit(1);
            }
        }

        // ------------------------------------------------------------------
        // Import
        // ------------------------------------------------------------------

        private static void ConfigureImporter()
        {
            if (AssetImporter.GetAtPath(ModelPath) is not ModelImporter importer)
            {
                throw new InvalidOperationException(
                    $"Kein ModelImporter an {ModelPath}.");
            }

            // 1 Blender-Meter = 1 Unity-Meter.
            importer.globalScale = 1f;
            importer.useFileScale = true;

            // Normalen kommen aus der Datei: Blender hat mit
            // mesh_smooth_type='FACE' Glaettungsgruppen geschrieben. Der Rumpf
            // ist glatt schattiert, Rindenplatten und Risse sind facettiert —
            // liesse man Unity nachrechnen, verschwaende die Kante zwischen
            // beidem.
            importer.importNormals = ModelImporterNormals.Import;

            // Bleibt aus. Gemessen am 16.08.2026 mit diesem Asset: die Option
            // verschiebt die Achsdrehung nur zwischen Mesh und Wurzel, statt
            // sie aufzuloesen. Geloest wird sie beim Export mit
            // bake_space_transform=True. Siehe BLENDER_ASSET_PIPELINE.md.
            importer.bakeAxisConversion = false;

            // Ohne Normal Map braucht das Material keine Tangenten.
            importer.importTangents = ModelImporterTangents.None;

            // Generic, nicht Humanoid: ein Vierbeiner hat in Unitys
            // Humanoid-Muskelschema keine Entsprechung. So steht es auch im
            // Konzeptentwurf.
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;

            // Der Wurzelknochen bleibt als Transform erhalten. Das Zusammen-
            // falten der Hierarchie spart Rechenzeit, macht aber jede spaetere
            // Frage nach einem Knochen — etwa "wo sitzt der Kopf" — im
            // Inspector unbeantwortbar.
            importer.optimizeGameObjects = false;

            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;

            // Ein bewegtes Objekt wird nicht in eine Lightmap gebacken.
            importer.generateSecondaryUV = false;

            // Kein Material aus der Datei. Was Blender mitschickt, hat keinen
            // Shader, den URP kennt — es waere ein magentafarbenes Objekt mit
            // einem plausiblen Namen.
            importer.materialImportMode = ModelImporterMaterialImportMode.None;

            foreach (AssetImporter.SourceAssetIdentifier remap
                     in new List<AssetImporter.SourceAssetIdentifier>(
                         importer.GetExternalObjectMap().Keys))
            {
                importer.RemoveRemap(remap);
            }

            importer.clipAnimations = BuildClipSettings(importer);
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Uebernimmt die vom Import gefundenen Takes und setzt je Clip das
        /// Schleifenflag.
        ///
        /// Die Laengen werden ausdruecklich <b>nicht</b> angefasst: sie stehen
        /// so in der Datei, weil der Blender-Aufbau mit 50 Bildern je Sekunde
        /// arbeitet und die gemessenen Dauern des Konzeptentwurfs — 0,7 s
        /// Telegraph, 0,18 s Flinch, 0,8 s Straucheln — damit glatt aufgehen.
        /// Sie hier noch einmal zu setzen hiesse, dieselbe Zahl an zwei Orten
        /// zu pflegen.
        /// </summary>
        private static ModelImporterClipAnimation[] BuildClipSettings(
            ModelImporter importer)
        {
            ModelImporterClipAnimation[] clips =
                importer.defaultClipAnimations;

            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.loopTime = LoopingClips.Contains(ShortName(clip.name));

                // Ohne Root Motion bewegt der Animator den Transform nicht;
                // das macht EnemyMovement. Die Clips verschieben den
                // Wurzelknochen ohnehin nicht waagerecht.
                clip.lockRootRotation = false;
                clip.keepOriginalPositionY = true;
            }

            return clips;
        }

        // ------------------------------------------------------------------
        // Materialien
        // ------------------------------------------------------------------

        private static Material BuildBarkMaterial()
        {
            Material material = LoadOrCreate(BarkMaterialPath);

            // Werte aus dem Principled BSDF der .blend: dunkles, feuchtes Holz
            // mit graubrauner Rinde.
            material.SetColor("_BaseColor", new Color(0.086f, 0.071f, 0.055f, 1f));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 1f - 0.86f);

            // Ausdruecklich ohne Emission. Der Rumpf darf nie leuchten: die
            // Art Direction schliesst eine dauerhafte tuerkise Einfaerbung des
            // ganzen Koerpers aus.
            //
            // Das ist kein Schoenheitsfehler, sondern der Grund, warum die
            // Risse einen eigenen Slot haben. WurzelstreiferFeedback schreibt
            // _EmissionColor ueber einen MaterialPropertyBlock auf den
            // Renderer, und der gilt fuer alle Slots. Nur weil hier das
            // Emissionskeyword aus ist, ignoriert URP die Farbe an diesem
            // Material — und leuchten die Linien allein.
            material.DisableKeyword("_EMISSION");
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material BuildCrackMaterial()
        {
            Material material = LoadOrCreate(CrackMaterialPath);

            material.SetColor("_BaseColor", new Color(0.031f, 0.094f, 0.090f, 1f));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 1f - 0.62f);

            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;

            // Schwarz im Ruhezustand. Die Helligkeit kommt zur Laufzeit aus
            // WurzelstreiferFeedback; ein Startwert ueber 0 waere genau das
            // Dauerleuchten, das die Art Direction ausschliesst.
            material.SetColor("_EmissionColor", Color.black);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreate(string path)
        {
            Shader shader = Shader.Find(UrpLitShader);

            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"Shader '{UrpLitShader}' nicht gefunden. Ohne URP-Shader " +
                    "waere das Material im Spiel magenta.");
            }

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            return material;
        }

        // ------------------------------------------------------------------
        // Animator
        // ------------------------------------------------------------------

        /// <summary>
        /// Nur Zustaende mit Clips, keine Uebergaenge — die Auswahl trifft
        /// <see cref="EnemyAnimationDriver"/> anhand der Zustandsmaschine. Ein
        /// Bedingungsnetz waere eine zweite, nur im Editorfenster nachlesbare
        /// Zustandsmaschine.
        /// </summary>
        private static AnimatorController BuildAnimatorController()
        {
            Dictionary<string, AnimationClip> clips = LoadClips();

            AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            bool first = true;

            foreach ((string stateName, string clipName) in StateClips)
            {
                AnimatorState state = machine.AddState(stateName);

                if (clips.TryGetValue(clipName, out AnimationClip clip))
                {
                    state.motion = clip;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Wurzelstreifer: Clip '{clipName}' fehlt im Modell. " +
                        $"Zustand '{stateName}' haette keine Bewegung.");
                }

                state.speed = 1f;

                if (first)
                {
                    machine.defaultState = state;
                    first = false;
                }
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Dictionary<string, AnimationClip> LoadClips()
        {
            var clips = new Dictionary<string, AnimationClip>(
                StringComparer.OrdinalIgnoreCase);

            foreach (UnityEngine.Object asset in
                     AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                if (asset is not AnimationClip clip)
                {
                    continue;
                }

                if (clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    continue;
                }

                clips[ShortName(clip.name)] = clip;
            }

            return clips;
        }

        /// <summary>
        /// Die Clips heissen "ELY_Enemy_Wurzelstreifer_Rig|Idle"; nur der Teil
        /// hinter dem Trennzeichen ist die Bewegung.
        /// </summary>
        private static string ShortName(string name)
        {
            int separator = name.LastIndexOf('|');
            return separator >= 0 ? name.Substring(separator + 1) : name;
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
    }
}
