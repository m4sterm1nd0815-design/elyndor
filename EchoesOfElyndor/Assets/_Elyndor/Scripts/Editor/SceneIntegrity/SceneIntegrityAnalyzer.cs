using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools.SceneIntegrity
{
    /// <summary>
    /// Prueft eine Szene gegen ein <see cref="SceneIntegrityProfile"/>.
    ///
    /// Der Analysator sucht strukturelle Fehler, die keinen Compilerfehler und
    /// keine Konsolenmeldung erzeugen und deshalb still durchrutschen: eine
    /// Komponente unter einem deaktivierten Vorfahren, ein UI-Root mit Scale 0,
    /// ein verlorenes Script, ein doppelt vorhandenes Einzelsystem oder eine
    /// nicht gesetzte Pflichtreferenz.
    ///
    /// Genau diese Klasse Fehler hatte in Finsterwald Interaktion, Narration,
    /// Erinnerungsuhr-Overlay, Tutorial und den kompletten Ton abgeschaltet,
    /// ohne dass Compiler, Konsole oder die vorhandenen Tests etwas gemeldet
    /// haetten.
    /// </summary>
    public static class SceneIntegrityAnalyzer
    {
        private const float ScaleEpsilon = 0.0001f;

        public static List<SceneIntegrityIssue> Analyze(
            Scene scene,
            SceneIntegrityProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            List<SceneIntegrityIssue> issues = new List<SceneIntegrityIssue>();

            if (!scene.IsValid() || !scene.isLoaded)
            {
                issues.Add(new SceneIntegrityIssue(
                    SceneIntegrityIssueCode.RequiredComponentMissing,
                    string.Empty,
                    $"Szene '{scene.name}' ist nicht geladen."));
                return issues;
            }

            List<GameObject> allObjects = CollectGameObjects(scene);

            CheckMissingScripts(allObjects, issues);
            CheckRequiredActive(allObjects, profile, issues);
            CheckOptionalActive(allObjects, profile, issues);
            CheckSingleInstances(allObjects, profile, issues);
            CheckRootScales(scene, profile, issues);
            CheckRequiredReferences(allObjects, profile, issues);

            return issues;
        }

        private static List<GameObject> CollectGameObjects(Scene scene)
        {
            List<GameObject> objects = new List<GameObject>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                // true = auch deaktivierte Objekte. Ohne das wuerde der
                // Analysator ausgerechnet den Fehlerfall uebersehen.
                foreach (Transform transform in
                    root.GetComponentsInChildren<Transform>(true))
                {
                    objects.Add(transform.gameObject);
                }
            }

            return objects;
        }

        private static void CheckMissingScripts(
            List<GameObject> objects,
            List<SceneIntegrityIssue> issues)
        {
            foreach (GameObject gameObject in objects)
            {
                Component[] components = gameObject.GetComponents<Component>();

                for (int index = 0; index < components.Length; index++)
                {
                    if (components[index] != null)
                    {
                        continue;
                    }

                    issues.Add(new SceneIntegrityIssue(
                        SceneIntegrityIssueCode.MissingScriptReference,
                        GetPath(gameObject),
                        $"Komponentenplatz {index} verweist auf ein fehlendes Script."));
                }
            }
        }

        private static void CheckRequiredActive(
            List<GameObject> objects,
            SceneIntegrityProfile profile,
            List<SceneIntegrityIssue> issues)
        {
            foreach (Type type in profile.RequiredActiveTypes)
            {
                List<Component> found = FindComponents(objects, type);

                if (found.Count == 0)
                {
                    issues.Add(new SceneIntegrityIssue(
                        SceneIntegrityIssueCode.RequiredComponentMissing,
                        string.Empty,
                        $"Pflichtsystem '{type.Name}' fehlt in der Szene."));
                    continue;
                }

                bool anyActive = false;

                foreach (Component component in found)
                {
                    if (component.gameObject.activeInHierarchy)
                    {
                        anyActive = true;
                        break;
                    }
                }

                if (anyActive)
                {
                    continue;
                }

                Component first = found[0];
                string blocker = FindInactiveAncestorName(first.gameObject);

                issues.Add(new SceneIntegrityIssue(
                    SceneIntegrityIssueCode.RequiredComponentInactive,
                    GetPath(first.gameObject),
                    $"Pflichtsystem '{type.Name}' ist vorhanden, laeuft aber nicht: " +
                    $"'{blocker}' ist deaktiviert. Awake und Update werden nie aufgerufen."));
            }
        }

        /// <summary>
        /// Prueft Systeme, die eine Region haben darf, aber nicht haben muss.
        /// Ein fehlendes System ist hier kein Befund — ein vorhandenes, das
        /// unter einem deaktivierten Vorfahren haengt, sehr wohl. Genau so
        /// bleibt der Validator regionsspezifisch, ohne Luecken zu lassen.
        /// </summary>
        private static void CheckOptionalActive(
            List<GameObject> objects,
            SceneIntegrityProfile profile,
            List<SceneIntegrityIssue> issues)
        {
            foreach (Type type in profile.OptionalActiveTypes)
            {
                List<Component> found = FindComponents(objects, type);

                if (found.Count == 0)
                {
                    continue;
                }

                bool anyActive = false;

                foreach (Component component in found)
                {
                    if (component.gameObject.activeInHierarchy)
                    {
                        anyActive = true;
                        break;
                    }
                }

                if (anyActive)
                {
                    continue;
                }

                Component first = found[0];
                string blocker = FindInactiveAncestorName(first.gameObject);

                issues.Add(new SceneIntegrityIssue(
                    SceneIntegrityIssueCode.RequiredComponentInactive,
                    GetPath(first.gameObject),
                    $"Optionales System '{type.Name}' ist in dieser Szene vorhanden, " +
                    $"laeuft aber nicht: '{blocker}' ist deaktiviert. " +
                    "Entweder wirksam machen oder aus der Szene entfernen."));
            }
        }

        private static void CheckSingleInstances(
            List<GameObject> objects,
            SceneIntegrityProfile profile,
            List<SceneIntegrityIssue> issues)
        {
            foreach (Type type in profile.SingleInstanceTypes)
            {
                List<Component> found = FindComponents(objects, type);

                if (found.Count <= 1)
                {
                    continue;
                }

                List<string> paths = new List<string>();

                foreach (Component component in found)
                {
                    paths.Add(GetPath(component.gameObject));
                }

                issues.Add(new SceneIntegrityIssue(
                    SceneIntegrityIssueCode.DuplicateSingleInstanceComponent,
                    paths[0],
                    $"'{type.Name}' existiert {found.Count}-mal, erlaubt ist eine " +
                    $"Instanz. Fundstellen: {string.Join(", ", paths)}."));
            }
        }

        private static void CheckRootScales(
            Scene scene,
            SceneIntegrityProfile profile,
            List<SceneIntegrityIssue> issues)
        {
            if (profile.ScaleCheckedRootNames.Count == 0)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();

            foreach (string rootName in profile.ScaleCheckedRootNames)
            {
                foreach (GameObject root in roots)
                {
                    if (root.name != rootName)
                    {
                        continue;
                    }

                    Vector3 scale = root.transform.localScale;

                    if (Mathf.Abs(scale.x) > ScaleEpsilon &&
                        Mathf.Abs(scale.y) > ScaleEpsilon &&
                        Mathf.Abs(scale.z) > ScaleEpsilon)
                    {
                        continue;
                    }

                    issues.Add(new SceneIntegrityIssue(
                        SceneIntegrityIssueCode.ZeroScaleRoot,
                        GetPath(root),
                        $"Wurzelobjekt hat Scale {scale} und ist damit unsichtbar, " +
                        "ohne deaktiviert zu wirken."));
                }
            }
        }

        private static void CheckRequiredReferences(
            List<GameObject> objects,
            SceneIntegrityProfile profile,
            List<SceneIntegrityIssue> issues)
        {
            foreach (KeyValuePair<Type, string[]> entry in profile.RequiredReferences)
            {
                foreach (Component component in FindComponents(objects, entry.Key))
                {
                    foreach (string fieldName in entry.Value)
                    {
                        FieldInfo field = entry.Key.GetField(
                            fieldName,
                            BindingFlags.Instance |
                            BindingFlags.NonPublic |
                            BindingFlags.Public);

                        if (field == null)
                        {
                            issues.Add(new SceneIntegrityIssue(
                                SceneIntegrityIssueCode.MissingRequiredReference,
                                GetPath(component.gameObject),
                                $"'{entry.Key.Name}' besitzt kein Feld '{fieldName}'. " +
                                "Das Profil passt nicht mehr zum Code."));
                            continue;
                        }

                        if (!IsNull(field.GetValue(component)))
                        {
                            continue;
                        }

                        issues.Add(new SceneIntegrityIssue(
                            SceneIntegrityIssueCode.MissingRequiredReference,
                            GetPath(component.gameObject),
                            $"Pflichtreferenz '{entry.Key.Name}.{fieldName}' ist nicht gesetzt."));
                    }
                }
            }
        }

        private static List<Component> FindComponents(
            List<GameObject> objects,
            Type type)
        {
            List<Component> found = new List<Component>();

            foreach (GameObject gameObject in objects)
            {
                foreach (Component component in gameObject.GetComponents(type))
                {
                    if (component != null)
                    {
                        found.Add(component);
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Unity-Objekte melden sich nach Destroy als "fake null". Ein blosser
        /// Referenzvergleich wuerde eine zerstoerte Referenz faelschlich als
        /// gesetzt durchgehen lassen.
        /// </summary>
        private static bool IsNull(object value)
        {
            if (value is UnityEngine.Object unityObject)
            {
                return unityObject == null;
            }

            return value == null;
        }

        private static string FindInactiveAncestorName(GameObject gameObject)
        {
            Transform current = gameObject.transform;
            string lastInactive = gameObject.name;

            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    lastInactive = current.gameObject.name;
                }

                current = current.parent;
            }

            return lastInactive;
        }

        private static string GetPath(GameObject gameObject)
        {
            string path = gameObject.name;
            Transform current = gameObject.transform.parent;

            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }

            return path;
        }
    }
}
