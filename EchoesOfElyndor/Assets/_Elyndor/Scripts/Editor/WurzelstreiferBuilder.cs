using System;
using System.Collections.Generic;
using System.IO;
using Elyndor.Combat;
using Elyndor.Enemies;
using Elyndor.Enemies.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Baut den Blockout des Wurzelstreifers und setzt die erste Begegnung in
    /// den Finsterwald.
    ///
    /// Bewusst als Editor-Werkzeug und nicht von Hand zusammengeklickt: der
    /// Aufbau ist damit nachlesbar, wiederholbar und ueberpruefbar. Wer die
    /// Werte spaeter anzweifelt, findet sie hier und nicht in einer
    /// Prefab-Datei.
    ///
    /// Das Modell ist der vorhandene Quaternius-Wolf aus dem Pack "Ultimate
    /// Animated Animals - July 2021". Dessen <c>License.txt</c> weist CC0 1.0
    /// aus; der Asset-Katalog fuehrt ihn unabhaengig davon ebenfalls als CC0.
    /// Er dient ausschliesslich als Blockout fuer Groesse, Bewegung, Hitboxen,
    /// Telegraph, Kamera und Timing — nicht als Art Direction. Die finale
    /// Silhouette beschreibt <c>WURZELSTREIFER_CONCEPT_BRIEF.md</c>.
    /// </summary>
    public static class WurzelstreiferBuilder
    {
        private const string WolfFbxPath =
            "Assets/ThirdParty/Ultimate Animated Animals - July 2021/FBX/Wolf.fbx";

        private const string BlockoutFolder =
            "Assets/_Elyndor/Prefabs/Enemies/Blockout";

        private const string ArtFolder =
            "Assets/_Elyndor/Art/Enemies/Wurzelstreifer";

        private const string PrefabPath =
            BlockoutFolder + "/Wurzelstreifer_Blockout.prefab";

        private const string ControllerPath =
            ArtFolder + "/Wurzelstreifer_Blockout.controller";

        private const string MaterialPath =
            ArtFolder + "/Wurzelstreifer_Blockout.mat";

        private const string FinsterwaldScenePath =
            "Assets/_Elyndor/Scenes/Finsterwald.unity";

        private const string EncounterRootName = "Erste Begegnung";
        private const string EncounterInstanceName = "Wurzelstreifer_Lichtung";

        /// <summary>
        /// Zielmassstab des Blockouts. Das Rohmodell misst rund 5,53 m Laenge;
        /// 0,32 ergibt daraus 1,77 m und liegt damit im Zielband von 1,6-1,8 m
        /// des Konzepts.
        /// </summary>
        private const float ModelScale = 0.32f;

        /// <summary>Mitte der bestehenden "Kleinen Lichtung".</summary>
        private static readonly Vector3 EncounterPosition =
            new Vector3(-26f, 0f, -17f);

        // ------------------------------------------------------------------
        // Schritt 1: Assets
        // ------------------------------------------------------------------

        [MenuItem("Elyndor/Setup/Wurzelstreifer-Blockout bauen")]
        public static void BuildBlockout()
        {
            EnsureFolder(BlockoutFolder);
            EnsureFolder(ArtFolder);

            GameObject model = EnsureRiggedModel();
            AnimatorController controller = BuildAnimatorController();
            Material material = BuildMaterial(model);

            GameObject prefab = BuildPrefab(model, controller, material);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "WURZELSTREIFER_BLOCKOUT_OK: Prefab unter " + PrefabPath +
                ", Controller unter " + ControllerPath +
                ", Material unter " + MaterialPath + ". " +
                "Modell: Quaternius-Wolf (CC0) nur als Blockout. " +
                "Prefab=" + (prefab != null));
        }

        /// <summary>
        /// Der Wolf wird als Generic-Rig importiert, hatte aber keinen Avatar —
        /// ohne ihn sind die zwoelf mitgelieferten Clips nicht abspielbar. Das
        /// ist eine Importeinstellung des vorhandenen Assets, kein Eingriff in
        /// die Datei selbst und keine Neubeschaffung.
        /// </summary>
        private static GameObject EnsureRiggedModel()
        {
            var importer = AssetImporter.GetAtPath(WolfFbxPath) as ModelImporter;

            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Wurzelstreifer: Wolf-Modell nicht gefunden unter " +
                    WolfFbxPath);
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup =
                    ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();

                Debug.Log(
                    "Wurzelstreifer: Avatar fuer den Blockout erzeugt. Die " +
                    "Animationen des Packs sind damit nutzbar.");
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                WolfFbxPath);

            if (model == null)
            {
                throw new InvalidOperationException(
                    "Wurzelstreifer: Wolf-Modell liess sich nicht laden.");
            }

            return model;
        }

        /// <summary>
        /// Der Controller enthaelt nur Zustaende mit Clips und keine
        /// Uebergaenge — die Auswahl trifft
        /// <see cref="EnemyAnimationDriver"/> anhand der Zustandsmaschine.
        /// Ein Bedingungsnetz waere eine zweite, nur im Editorfenster
        /// nachlesbare Zustandsmaschine.
        ///
        /// Zwei Zustaende greifen bewusst auf denselben Clip zurueck: es gibt
        /// im Pack weder einen Seitwaertsschritt noch ein schweres Straucheln.
        /// Der Konzeptentwurf fuehrt beide als Anforderung an das finale Asset.
        /// </summary>
        private static AnimatorController BuildAnimatorController()
        {
            Dictionary<string, AnimationClip> clips = LoadClips();

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);

            AnimatorStateMachine machine =
                controller.layers[0].stateMachine;

            AddState(machine, clips, EnemyAnimationDriver.StateIdle, "Idle", true);
            AddState(machine, clips, EnemyAnimationDriver.StateListen, "Idle_2_HeadLow");
            AddState(machine, clips, EnemyAnimationDriver.StateWalk, "Walk");
            AddState(machine, clips, EnemyAnimationDriver.StateRun, "Gallop");
            AddState(machine, clips, EnemyAnimationDriver.StateTelegraph, "Idle_2_HeadLow");
            AddState(machine, clips, EnemyAnimationDriver.StateStrike, "Attack");
            AddState(machine, clips, EnemyAnimationDriver.StateFlinch, "Idle_HitReact_Left");
            AddState(machine, clips, EnemyAnimationDriver.StateStagger, "Idle_HitReact_Right");
            AddState(machine, clips, EnemyAnimationDriver.StateFlee, "Gallop");
            AddState(machine, clips, EnemyAnimationDriver.StateDefeat, "Death");

            EditorUtility.SetDirty(controller);

            return controller;
        }

        private static void AddState(
            AnimatorStateMachine machine,
            IReadOnlyDictionary<string, AnimationClip> clips,
            string stateName,
            string clipName,
            bool isDefault = false)
        {
            AnimatorState state = machine.AddState(stateName);

            if (clips.TryGetValue(clipName, out AnimationClip clip))
            {
                state.motion = clip;
            }
            else
            {
                Debug.LogWarning(
                    $"Wurzelstreifer: Clip '{clipName}' fehlt; Zustand " +
                    $"'{stateName}' bleibt ohne Bewegung.");
            }

            // Der Sprungbiss und die Niederlage duerfen nicht in Schleife
            // laufen; die Clips selbst sind nicht als Loop markiert, deshalb
            // genuegt hier die Geschwindigkeit.
            state.speed = 1f;

            if (isDefault)
            {
                machine.defaultState = state;
            }
        }

        private static Dictionary<string, AnimationClip> LoadClips()
        {
            var clips = new Dictionary<string, AnimationClip>(
                StringComparer.OrdinalIgnoreCase);

            foreach (UnityEngine.Object asset in
                     AssetDatabase.LoadAllAssetsAtPath(WolfFbxPath))
            {
                if (asset is not AnimationClip clip)
                {
                    continue;
                }

                if (clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    continue;
                }

                // Die Clips heissen "AnimalArmature|Idle"; nur der Teil hinter
                // dem Trennzeichen ist die Bewegung.
                int separator = clip.name.LastIndexOf('|');
                string shortName = separator >= 0
                    ? clip.name.Substring(separator + 1)
                    : clip.name;

                clips[shortName] = clip;
            }

            return clips;
        }

        /// <summary>
        /// Eigenes Material statt des Pack-Materials: die Emission muss
        /// einschaltbar sein, damit die tuerkise Resonanz ueberhaupt sichtbar
        /// werden kann. Das Pack-Material selbst bleibt unveraendert.
        /// </summary>
        private static Material BuildMaterial(GameObject model)
        {
            Material source = null;

            foreach (Renderer renderer in
                     model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterial != null)
                {
                    source = renderer.sharedMaterial;
                    break;
                }
            }

            Shader shader = source != null
                ? source.shader
                : Shader.Find("Universal Render Pipeline/Lit");

            Material material = new Material(shader);

            if (source != null)
            {
                material.CopyPropertiesFromMaterial(source);
            }

            material.name = "Wurzelstreifer_Blockout";
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);
            }

            AssetDatabase.DeleteAsset(MaterialPath);
            AssetDatabase.CreateAsset(material, MaterialPath);

            return material;
        }

        private static GameObject BuildPrefab(
            GameObject model,
            AnimatorController controller,
            Material material)
        {
            GameObject root = new GameObject("Wurzelstreifer_Blockout");

            try
            {
                // --- Modell als reines Blockout ---------------------------
                GameObject visual =
                    (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "Blockout (Quaternius-Wolf, CC0)";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale =
                    Vector3.one * ModelScale;

                foreach (Renderer renderer in
                         visual.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = material;
                }

                Animator animator = visual.GetComponent<Animator>()
                    ?? visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                // --- Koerper --------------------------------------------
                // Ein niedriger, langgezogener Vierbeiner: die Kapsel bleibt
                // flach, damit Aren ueber ihn hinwegsehen kann.
                CharacterController body =
                    root.AddComponent<CharacterController>();
                body.height = 0.9f;
                body.radius = 0.45f;
                body.center = new Vector3(0f, 0.45f, 0f);
                body.stepOffset = 0.35f;
                body.slopeLimit = 50f;

                // --- Gegnergrundlage --------------------------------------
                EnemyStateMachine stateMachine =
                    root.AddComponent<EnemyStateMachine>();
                EnemyHealth health = root.AddComponent<EnemyHealth>();
                EnemyPerception perception =
                    root.AddComponent<EnemyPerception>();
                EnemyMovement movement = root.AddComponent<EnemyMovement>();
                EnemyAttack attack = root.AddComponent<EnemyAttack>();
                EnemyHitReaction hitReaction =
                    root.AddComponent<EnemyHitReaction>();
                EnemyRetreat retreat = root.AddComponent<EnemyRetreat>();
                EnemyLeash leash = root.AddComponent<EnemyLeash>();
                EnemyController controllerComponent =
                    root.AddComponent<EnemyController>();

                // --- Profil und Rueckmeldung ------------------------------
                Wurzelstreifer profile = root.AddComponent<Wurzelstreifer>();
                EnemyAnimationDriver animationDriver =
                    root.AddComponent<EnemyAnimationDriver>();

                // Die Komponenten faenden sich zur Laufzeit auch selbst. Sie
                // hier zu verdrahten macht das Prefab im Inspector lesbar:
                // wer es oeffnet, sieht die Verbindungen, statt sie aus dem
                // Code erschliessen zu muessen.
                Wire(controllerComponent, ("stateMachine", stateMachine),
                    ("health", health), ("perception", perception),
                    ("movement", movement), ("attack", attack),
                    ("hitReaction", hitReaction), ("retreat", retreat),
                    ("leash", leash));

                Wire(hitReaction, ("stateMachine", stateMachine),
                    ("health", health), ("perception", perception));

                Wire(retreat, ("health", health));

                Wire(animationDriver, ("animator", animator),
                    ("controller", controllerComponent));

                BuildHealthBar(root, health);

                ParticleSystem dust = BuildBarkDust(root);
                WurzelstreiferFeedback feedback =
                    root.AddComponent<WurzelstreiferFeedback>();

                SerializedObject serialized = new SerializedObject(feedback);
                serialized.FindProperty("hitParticles").objectReferenceValue = dust;
                serialized.FindProperty("targetRenderer").objectReferenceValue =
                    visual.GetComponentInChildren<Renderer>(true);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // Das Profil legt seine Werte zwar beim Start ohnehin auf die
                // Komponenten. Es hier zusaetzlich anzuwenden macht sie im
                // Prefab sichtbar — sonst zeigte der Inspector Standardwerte,
                // die mit dem laufenden Spiel nichts zu tun haben.
                profile.Apply();

                AssetDatabase.DeleteAsset(PrefabPath);

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>Setzt private SerializeField-Verweise einer Komponente.</summary>
        private static void Wire(
            UnityEngine.Object target,
            params (string Field, UnityEngine.Object Value)[] assignments)
        {
            SerializedObject serialized = new SerializedObject(target);

            foreach ((string field, UnityEngine.Object value) in assignments)
            {
                SerializedProperty property = serialized.FindProperty(field);

                if (property == null)
                {
                    Debug.LogWarning(
                        $"Wurzelstreifer: Feld '{field}' fehlt auf " +
                        $"{target.GetType().Name}.");
                    continue;
                }

                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Die Lebensanzeige des Gegners.
        ///
        /// Auf einem eigenen Kindobjekt, nicht auf dem Gegner selbst: die
        /// Anzeige dreht ihren eigenen Transform jeden Takt zur Kamera und
        /// wuerde sonst den Gegner mitdrehen.
        ///
        /// Die Hoehe ist an dieses Tier angepasst. Der Vorgabewert von 2,1 m
        /// stammt von einer aufrechten Figur; ueber einem 0,9 m hohen
        /// Vierbeiner schwebte die Leiste dort weit ueber ihm in der Luft.
        /// </summary>
        private static void BuildHealthBar(GameObject root, EnemyHealth health)
        {
            GameObject bar = new GameObject("Lebensanzeige");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = Vector3.zero;

            EnemyHealthBar healthBar = bar.AddComponent<EnemyHealthBar>();

            Wire(healthBar, ("health", health), ("anchor", root.transform));

            SerializedObject serialized = new SerializedObject(healthBar);
            serialized.FindProperty("heightOffset").floatValue = 1.25f;
            serialized.FindProperty("placeholderSize").vector2Value =
                new Vector2(0.8f, 0.1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Kleiner Rinden- und Holzstaub beim Treffer. Bewusst winzig: der
        /// Auftrag verlangt einfache Gameplay-VFX, keine Partikelwolke.
        /// </summary>
        private static ParticleSystem BuildBarkDust(GameObject root)
        {
            GameObject dustObject = new GameObject("Rindenstaub");
            dustObject.transform.SetParent(root.transform, false);
            dustObject.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            ParticleSystem system = dustObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = system.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.45f;
            main.startSpeed = 1.6f;
            main.startSize = 0.05f;
            main.gravityModifier = 0.9f;
            main.startColor = new Color(0.36f, 0.29f, 0.22f);
            main.maxParticles = 24;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 12)
            });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            return system;
        }

        // ------------------------------------------------------------------
        // Schritt 2: Begegnung in der Szene
        // ------------------------------------------------------------------

        [MenuItem("Elyndor/Finsterwald/Erste Begegnung platzieren")]
        public static void PlaceFirstEncounter()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "Wurzelstreifer: Bitte zuerst den Blockout bauen.");
            }

            Scene scene = EditorSceneManager.OpenScene(
                FinsterwaldScenePath, OpenSceneMode.Single);

            EnsurePlayerDamageReceiver();

            GameObject encounterRoot = GameObject.Find(EncounterRootName);

            if (encounterRoot == null)
            {
                encounterRoot = new GameObject(EncounterRootName);
                Undo.RegisterCreatedObjectUndo(
                    encounterRoot, "Erste Begegnung");
            }

            // Der erste Kampf ist Unterricht, kein Schwierigkeitscheck: es
            // darf genau ein Exemplar geben. Statt nur den erwarteten Namen
            // zu ersetzen, wird jeder Gegner in der Szene entfernt — ein
            // zweiter, versehentlich liegengebliebener Wurzelstreifer waere
            // sonst erst im Spiel aufgefallen.
            foreach (EnemyController stray in UnityEngine.Object
                         .FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            {
                UnityEngine.Object.DestroyImmediate(stray.gameObject);
            }

            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = EncounterInstanceName;
            instance.transform.SetParent(encounterRoot.transform, true);
            instance.transform.position = GroundedPosition(EncounterPosition);

            // Blickrichtung nach Sueden: Aren kommt aus dieser Richtung, der
            // Gegner soll ihn nicht im Ruecken haben.
            instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                "WURZELSTREIFER_ENCOUNTER_OK: genau ein Exemplar auf der " +
                "Lichtung bei " + instance.transform.position.ToString("F2") +
                ".");
        }

        /// <summary>
        /// Ohne den Empfaenger liefe der Biss des Gegners am Block vorbei
        /// direkt in die Lebenspunkte — die 70-Prozent-Regel waere in der
        /// Szene wirkungslos, obwohl sie im Code steht.
        /// </summary>
        private static void EnsurePlayerDamageReceiver()
        {
            PlayerCombat combat =
                UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();

            if (combat == null)
            {
                Debug.LogWarning(
                    "Wurzelstreifer: Kein PlayerCombat in der Szene; der " +
                    "Schadensempfaenger wurde nicht gesetzt.");
                return;
            }

            if (combat.GetComponent<PlayerDamageReceiver>() == null)
            {
                Undo.AddComponent<PlayerDamageReceiver>(combat.gameObject);

                Debug.Log(
                    "Wurzelstreifer: PlayerDamageReceiver an '" +
                    combat.gameObject.name + "' ergaenzt.");
            }
        }

        /// <summary>Legt die Position auf den Boden, statt sie zu raten.</summary>
        private static Vector3 GroundedPosition(Vector3 position)
        {
            Vector3 from = new Vector3(position.x, position.y + 60f, position.z);

            if (Physics.Raycast(
                    from, Vector3.down, out RaycastHit hit, 200f,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return position;
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
