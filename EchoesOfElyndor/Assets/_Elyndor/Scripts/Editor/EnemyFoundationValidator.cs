using System;
using System.Collections.Generic;
using System.Reflection;
using Elyndor.Enemies;
using UnityEditor;
using UnityEngine;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Prueft die Gegnergrundlage rein statisch: die Zustandstabelle wird
    /// vollstaendig durchgespielt, und jede Fachkomponente muss ihre
    /// konfigurierbaren Werte weiterhin als SerializeField anbieten.
    ///
    /// Bewusst ohne GameObject und ohne Szene — der Validator darf keine
    /// Szene oeffnen, veraendern oder speichern.
    /// </summary>
    public static class EnemyFoundationValidator
    {
        [MenuItem("Elyndor/QA/Validate Enemy Foundation")]
        public static void Validate()
        {
            int errors = 0;

            errors += ValidateStateRules();
            errors += ValidateConfigurableFields();

            if (errors > 0)
            {
                throw new InvalidOperationException(
                    $"Enemy foundation validation failed with {errors} errors.");
            }

            Debug.Log(
                "ENEMY_FOUNDATION_VALIDATION_OK: state table complete, " +
                "Dead is final, no self transitions, configurable fields present.");
        }

        private static int ValidateStateRules()
        {
            int errors = 0;

            EnemyFoundationState[] states =
                (EnemyFoundationState[])Enum.GetValues(
                    typeof(EnemyFoundationState));

            foreach (EnemyFoundationState from in states)
            {
                bool hasAnyExit = false;

                foreach (EnemyFoundationState to in states)
                {
                    bool allowed = EnemyStateRules.CanTransition(from, to);

                    if (from == to && allowed)
                    {
                        Debug.LogError(
                            $"Enemy foundation: {from} darf nicht auf sich " +
                            "selbst wechseln.");
                        errors++;
                    }

                    if (from == EnemyFoundationState.Dead && allowed)
                    {
                        Debug.LogError(
                            $"Enemy foundation: Dead ist nicht endgueltig — " +
                            $"Wechsel nach {to} ist erlaubt.");
                        errors++;
                    }

                    if (allowed)
                    {
                        hasAnyExit = true;
                    }
                }

                bool shouldHaveExit = from != EnemyFoundationState.Dead;

                if (shouldHaveExit && !hasAnyExit)
                {
                    Debug.LogError(
                        $"Enemy foundation: {from} ist eine Sackgasse.");
                    errors++;
                }

                // Der Tod muss aus jedem lebenden Zustand erreichbar sein.
                if (shouldHaveExit &&
                    !EnemyStateRules.CanTransition(
                        from, EnemyFoundationState.Dead))
                {
                    Debug.LogError(
                        $"Enemy foundation: aus {from} ist kein Tod moeglich.");
                    errors++;
                }
            }

            // Aus einer Trefferreaktion darf nie direkt angegriffen werden.
            if (EnemyStateRules.CanTransition(
                    EnemyFoundationState.Hurt, EnemyFoundationState.Attack))
            {
                Debug.LogError(
                    "Enemy foundation: aus Hurt darf nicht direkt " +
                    "angegriffen werden.");
                errors++;
            }

            return errors;
        }

        private static int ValidateConfigurableFields()
        {
            var expected = new Dictionary<Type, string[]>
            {
                [typeof(EnemyHealth)] = new[] { "maxHealth", "currentHealth" },
                [typeof(EnemyPerception)] = new[]
                {
                    "detectionRadius", "viewAngle", "loseTargetDelay",
                    "requireLineOfSight"
                },
                [typeof(EnemyMovement)] = new[]
                {
                    "moveSpeed", "stopDistance", "turnSpeedDegrees"
                },
                [typeof(EnemyAttack)] = new[]
                {
                    "attackRange", "attackCooldown", "damage"
                },
                [typeof(EnemyHitReaction)] = new[] { "hurtDuration" }
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
                            $"Enemy foundation: {entry.Key.Name}.{fieldName} " +
                            "fehlt.");
                        errors++;
                        continue;
                    }

                    if (field.GetCustomAttribute<SerializeField>() == null)
                    {
                        Debug.LogError(
                            $"Enemy foundation: {entry.Key.Name}.{fieldName} " +
                            "ist nicht im Inspector konfigurierbar.");
                        errors++;
                    }
                }
            }

            return errors;
        }
    }
}
