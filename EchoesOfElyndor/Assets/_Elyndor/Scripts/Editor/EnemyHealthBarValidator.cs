using System;
using System.Collections.Generic;
using System.Reflection;
using Elyndor.Enemies;
using Elyndor.Enemies.UI;
using UnityEditor;
using UnityEngine;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Prueft die Gegner-Lebensanzeige rein statisch: die im Inspector
    /// erwarteten Werte muessen vorhanden bleiben, die Anzeige darf keine
    /// zweite Lebensverwaltung neben <see cref="EnemyHealth"/> aufbauen, und
    /// die von Tests und spaeteren Spawnern genutzte Schnittstelle muss
    /// bestehen bleiben.
    ///
    /// Bewusst ohne GameObject und ohne Szene — der Validator darf keine Szene
    /// oeffnen, veraendern oder speichern.
    /// </summary>
    public static class EnemyHealthBarValidator
    {
        [MenuItem("Elyndor/QA/Validate Enemy Health Bar")]
        public static void Validate()
        {
            int errors = 0;

            errors += ValidateConfigurableFields();
            errors += ValidatePublicSurface();
            errors += ValidateNoDuplicatedHealthState();

            if (errors > 0)
            {
                throw new InvalidOperationException(
                    $"Enemy health bar validation failed with {errors} errors.");
            }

            Debug.Log(
                "ENEMY_HEALTH_BAR_VALIDATION_OK: configurable fields present, " +
                "runtime surface intact, no duplicated health state.");
        }

        private static int ValidateConfigurableFields()
        {
            var expected = new Dictionary<Type, string[]>
            {
                [typeof(EnemyHealthBar)] = new[]
                {
                    "health", "visual", "barRoot", "anchor", "viewCamera",
                    "heightOffset", "faceCamera", "hiddenAtFullHealth",
                    "visibleDuration", "hideWhenIdle", "createPlaceholderVisual",
                    "placeholderSize"
                },
                [typeof(EnemyHealthBarVisual)] = new[]
                {
                    "canvas", "backgroundImage", "fillImage"
                }
            };

            int errors = 0;

            foreach (KeyValuePair<Type, string[]> entry in expected)
            {
                foreach (string fieldName in entry.Value)
                {
                    FieldInfo field = entry.Key.GetField(
                        fieldName,
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    if (field == null)
                    {
                        Debug.LogError(
                            $"Enemy health bar: {entry.Key.Name}.{fieldName} " +
                            "fehlt.");
                        errors++;
                        continue;
                    }

                    if (field.GetCustomAttribute<SerializeField>() == null)
                    {
                        Debug.LogError(
                            $"Enemy health bar: {entry.Key.Name}.{fieldName} " +
                            "ist nicht im Inspector konfigurierbar.");
                        errors++;
                    }
                }
            }

            return errors;
        }

        private static int ValidatePublicSurface()
        {
            var expected = new Dictionary<Type, string[]>
            {
                [typeof(EnemyHealthBar)] = new[]
                {
                    "Tick", "Bind", "SetCamera", "SetHeightOffset",
                    "ConfigureVisibility", "get_Fill01", "get_IsBarVisible",
                    "get_HasHealthBinding"
                },
                [typeof(EnemyHealthBarVisual)] = new[]
                {
                    "SetFill", "SetVisible", "Assign", "CreatePlaceholder",
                    "get_Fill", "get_IsVisible"
                }
            };

            int errors = 0;

            foreach (KeyValuePair<Type, string[]> entry in expected)
            {
                foreach (string memberName in entry.Value)
                {
                    MethodInfo method = entry.Key.GetMethod(
                        memberName,
                        BindingFlags.Public | BindingFlags.Instance |
                        BindingFlags.Static);

                    if (method == null)
                    {
                        Debug.LogError(
                            $"Enemy health bar: {entry.Key.Name}.{memberName} " +
                            "fehlt in der oeffentlichen Schnittstelle.");
                        errors++;
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Die Anzeige darf den Lebenszustand nicht nachbauen. Erlaubt ist
        /// allein der abgeleitete Fuellwert; ein eigener Lebens- oder
        /// Maximalwert waere eine zweite Wahrheit neben
        /// <see cref="EnemyHealth"/>.
        /// </summary>
        private static int ValidateNoDuplicatedHealthState()
        {
            string[] forbidden =
            {
                "maxHealth", "currentHealth", "health01", "isDead"
            };

            int errors = 0;

            FieldInfo[] fields = typeof(EnemyHealthBar).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                foreach (string name in forbidden)
                {
                    if (!string.Equals(
                            field.Name, name,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    Debug.LogError(
                        $"Enemy health bar: EnemyHealthBar.{field.Name} " +
                        "dupliziert den Zustand von EnemyHealth.");
                    errors++;
                }
            }

            return errors;
        }
    }
}
