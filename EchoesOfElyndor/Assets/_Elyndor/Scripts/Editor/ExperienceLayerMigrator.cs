using System;
using System.Collections.Generic;
using Elyndor.Core;
using Elyndor.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Trennt die Erlebnisschicht einer Regionsszene in eine UI-Ebene und eine
    /// Runtime-Ebene.
    ///
    /// Hintergrund: In Finsterwald lag die komplette Erlebnisschicht auf einem
    /// einzigen Canvas-Objekt namens <c>PrototypeHUD</c>. Bei der Integration
    /// des neuen HUD-Fundaments wurde dieses Objekt deaktiviert. Damit starben
    /// nicht nur die UI-Panels, sondern auch nicht-visuelle Systeme wie
    /// Intro, Tutorial und die komplette Soundausgabe — MonoBehaviours auf
    /// einem deaktivierten GameObject erhalten kein Awake/Update.
    ///
    /// Nach der Migration gilt:
    /// - <c>ElyndorExperienceUI</c> (Canvas) traegt ausschliesslich sichtbare
    ///   UI-Systeme und ihre Panels.
    /// - <c>ElyndorExperience</c> (aktiver Runtime-Root ohne Canvas) traegt die
    ///   nicht-visuellen Steuer- und Audiosysteme.
    ///
    /// Ein deaktiviertes UI-Panel kann dadurch nie wieder den Ton oder das
    /// Tutorial mit abschalten.
    ///
    /// Das Werkzeug ist idempotent: Ein zweiter Lauf findet nichts mehr zu tun.
    /// </summary>
    public static class ExperienceLayerMigrator
    {
        public const string LegacyCanvasName = "PrototypeHUD";
        public const string ExperienceUiName = "ElyndorExperienceUI";
        public const string ExperienceRootName = "ElyndorExperience";

        private const string FinsterwaldScenePath =
            "Assets/_Elyndor/Scenes/Finsterwald.unity";

        /// <summary>
        /// Nicht-visuelle Systeme. Reihenfolge ist bewusst gewaehlt:
        /// <see cref="AudioSource"/> muss vor <see cref="SfxLibrary"/> auf dem
        /// Ziel liegen, sonst legt RequireComponent eine zweite Quelle an.
        /// </summary>
        private static readonly Type[] RuntimeSystemTypes =
        {
            typeof(AudioSource),
            typeof(SfxLibrary),
            typeof(IntroSequence),
            typeof(TutorialSequence),
            typeof(CombatTutorial)
        };

        /// <summary>
        /// Systeme, die das neue HUD-Fundament bereits mitbringt. Solange der
        /// alte Canvas deaktiviert war, fiel die Doppelung nicht auf. Sobald er
        /// wieder laeuft, gaebe es zwei Instanzen, die dasselbe Panel steuern —
        /// doppelte Event-Abos inklusive. Die Instanz auf dem alten Canvas
        /// weicht, weil die neue zusaetzlich einen PresentationRoot kennt und
        /// damit die vollstaendiger konfigurierte ist.
        /// </summary>
        private static readonly Type[] SupersededLegacyTypes =
        {
            typeof(InteractionPromptUI)
        };

        [MenuItem("Elyndor/Repair/Erlebnisschicht in Finsterwald migrieren")]
        public static void MigrateFinsterwaldFromMenu()
        {
            MigrationResult result = MigrateScene(FinsterwaldScenePath);
            Debug.Log(result.ToReport());
        }

        /// <summary>
        /// Einstiegspunkt fuer den Batchmode.
        /// </summary>
        public static void MigrateFinsterwaldBatch()
        {
            MigrationResult result = MigrateScene(FinsterwaldScenePath);
            Debug.Log(result.ToReport());

            if (!result.Success)
            {
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        public static MigrationResult MigrateScene(string scenePath)
        {
            MigrationResult result = new MigrationResult(scenePath);

            Scene scene = EditorSceneManager.GetActiveScene();

            if (scene.path != scenePath)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Single);
            }

            if (!scene.IsValid())
            {
                result.Fail($"Szene '{scenePath}' konnte nicht geoeffnet werden.");
                return result;
            }

            GameObject uiRoot = FindRoot(scene, ExperienceUiName)
                ?? FindRoot(scene, LegacyCanvasName);

            if (uiRoot == null)
            {
                result.Fail(
                    $"Weder '{ExperienceUiName}' noch '{LegacyCanvasName}' " +
                    "in der Szene gefunden.");
                return result;
            }

            Undo.RegisterFullObjectHierarchyUndo(uiRoot, "Migrate Experience Layer");

            if (uiRoot.name == LegacyCanvasName)
            {
                uiRoot.name = ExperienceUiName;
                result.Note($"Canvas umbenannt: {LegacyCanvasName} -> {ExperienceUiName}.");
            }

            if (!uiRoot.activeSelf)
            {
                uiRoot.SetActive(true);
                result.Note("UI-Canvas war deaktiviert und wurde aktiviert.");
            }

            GameObject runtimeRoot = FindRoot(scene, ExperienceRootName);

            if (runtimeRoot == null)
            {
                runtimeRoot = new GameObject(ExperienceRootName);
                SceneManager.MoveGameObjectToScene(runtimeRoot, scene);
                Undo.RegisterCreatedObjectUndo(
                    runtimeRoot,
                    "Create Experience Runtime Root");
                result.Note($"Runtime-Root '{ExperienceRootName}' angelegt.");
            }

            if (!runtimeRoot.activeSelf)
            {
                runtimeRoot.SetActive(true);
                result.Note("Runtime-Root war deaktiviert und wurde aktiviert.");
            }

            MoveRuntimeSystems(uiRoot, runtimeRoot, result);
            RewireTutorialIntro(runtimeRoot, result);
            RemoveSupersededLegacyComponents(scene, uiRoot, result);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            return result;
        }

        private static void MoveRuntimeSystems(
            GameObject source,
            GameObject target,
            MigrationResult result)
        {
            List<Component> movedSources = new List<Component>();

            foreach (Type type in RuntimeSystemTypes)
            {
                Component origin = source.GetComponent(type);

                if (origin == null)
                {
                    continue;
                }

                if (target.GetComponent(type) == null)
                {
                    if (!ComponentUtility.CopyComponent(origin))
                    {
                        result.Fail($"Komponente {type.Name} liess sich nicht kopieren.");
                        return;
                    }

                    if (!ComponentUtility.PasteComponentAsNew(target))
                    {
                        result.Fail($"Komponente {type.Name} liess sich nicht einfuegen.");
                        return;
                    }

                    result.Migrated(type.Name);
                }
                else
                {
                    result.Note(
                        $"{type.Name} lag bereits auf '{target.name}'. " +
                        "Quelle wird nur entfernt.");
                }

                movedSources.Add(origin);
            }

            // Rueckwaerts entfernen: SfxLibrary muss vor der AudioSource
            // verschwinden, sonst blockiert RequireComponent das Loeschen.
            for (int index = movedSources.Count - 1; index >= 0; index--)
            {
                Undo.DestroyObjectImmediate(movedSources[index]);
            }
        }

        /// <summary>
        /// <see cref="TutorialSequence"/> haelt eine direkte Referenz auf
        /// <see cref="IntroSequence"/>. Nach dem Verschieben zeigt die Kopie
        /// noch auf die alte Instanz auf dem Canvas, die es gleich nicht mehr
        /// gibt. Diese Referenz wird hier auf die migrierte Instanz gesetzt.
        /// </summary>
        private static void RewireTutorialIntro(
            GameObject runtimeRoot,
            MigrationResult result)
        {
            TutorialSequence tutorial = runtimeRoot.GetComponent<TutorialSequence>();
            IntroSequence intro = runtimeRoot.GetComponent<IntroSequence>();

            if (tutorial == null || intro == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(tutorial);
            SerializedProperty introProperty = serialized.FindProperty("intro");

            if (introProperty == null)
            {
                result.Note(
                    "TutorialSequence besitzt kein Feld 'intro' — " +
                    "keine Umverdrahtung noetig.");
                return;
            }

            if (introProperty.objectReferenceValue as IntroSequence == intro)
            {
                return;
            }

            introProperty.objectReferenceValue = intro;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            result.Note("TutorialSequence.intro auf die migrierte IntroSequence gesetzt.");
        }

        /// <summary>
        /// Entfernt Altinstanzen, die anderswo in der Szene bereits durch eine
        /// neuere Instanz abgeloest sind. Entfernt wird ausschliesslich die
        /// Instanz auf dem alten Canvas und nur dann, wenn ausserhalb davon
        /// mindestens eine weitere existiert.
        /// </summary>
        private static void RemoveSupersededLegacyComponents(
            Scene scene,
            GameObject uiRoot,
            MigrationResult result)
        {
            foreach (Type type in SupersededLegacyTypes)
            {
                Component legacy = uiRoot.GetComponent(type);

                if (legacy == null)
                {
                    continue;
                }

                Component replacement = FindComponentOutside(scene, type, uiRoot);

                if (replacement == null)
                {
                    result.Note(
                        $"{type.Name} liegt nur auf '{uiRoot.name}'. " +
                        "Keine Ablösung vorhanden, Instanz bleibt bestehen.");
                    continue;
                }

                Undo.DestroyObjectImmediate(legacy);
                result.Note(
                    $"Doppelte {type.Name} auf '{uiRoot.name}' entfernt. " +
                    $"Aktiv bleibt die Instanz auf '{replacement.gameObject.name}'.");
            }
        }

        private static Component FindComponentOutside(
            Scene scene,
            Type type,
            GameObject excluded)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component component in
                    root.GetComponentsInChildren(type, true))
                {
                    if (component != null && component.gameObject != excluded)
                    {
                        return component;
                    }
                }
            }

            return null;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        public sealed class MigrationResult
        {
            private readonly List<string> notes = new List<string>();
            private readonly List<string> migratedComponents = new List<string>();
            private readonly string scenePath;

            public MigrationResult(string scenePath)
            {
                this.scenePath = scenePath;
            }

            public bool Success { get; private set; } = true;
            public string Error { get; private set; }
            public IReadOnlyList<string> MigratedComponents => migratedComponents;

            public void Note(string message) => notes.Add(message);

            public void Migrated(string componentName)
            {
                migratedComponents.Add(componentName);
                notes.Add($"{componentName} nach Runtime-Root verschoben.");
            }

            public void Fail(string message)
            {
                Success = false;
                Error = message;
            }

            public string ToReport()
            {
                string header = Success
                    ? $"ExperienceLayerMigrator: OK ({scenePath})"
                    : $"ExperienceLayerMigrator: FEHLGESCHLAGEN ({scenePath}) — {Error}";

                if (notes.Count == 0)
                {
                    return $"{header}\n  Nichts zu tun.";
                }

                return $"{header}\n  - {string.Join("\n  - ", notes)}";
            }
        }
    }
}
