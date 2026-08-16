using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Faehrt alle Validatoren in einem einzigen Batchmode-Start durch und
    /// schreibt ein Sammelergebnis.
    ///
    /// Hintergrund: Jeder Validator hat einen eigenen Menueeintrag. Einzeln
    /// aufgerufen kostet jeder davon einen vollstaendigen Unity-Start. Fuer die
    /// Regressionspruefung ist das die teuerste denkbare Variante — und sie
    /// verteilt das Ergebnis auf neun Logs statt auf eines.
    ///
    /// Diese Klasse fuegt <b>keine</b> Pruefung hinzu. Sie ruft ausschliesslich
    /// die vorhandenen Einstiegspunkte auf und zaehlt, was sie melden.
    /// </summary>
    public static class NightRegressionRunner
    {
        public const string ResultPath = "Temp/ElyndorTests/Validators.txt";

        /// <summary>
        /// Ein Validator mit dem Namen, unter dem er im Bericht auftaucht.
        /// </summary>
        private readonly struct Check
        {
            public readonly string Name;
            public readonly Action Run;

            public Check(string name, Action run)
            {
                Name = name;
                Run = run;
            }
        }

        /// <summary>
        /// Reihenfolge ist bewusst festgelegt: Validatoren, die selbst Szenen
        /// oeffnen, hinterlassen eine andere aktive Szene. Deshalb wird vor
        /// jedem Lauf Finsterwald neu geoeffnet — sonst prueft ein Validator,
        /// der auf der aktiven Szene arbeitet, stillschweigend die falsche.
        /// </summary>
        private static IReadOnlyList<Check> Checks => new[]
        {
            new Check("MovementCamera",
                MovementCameraValidator.ValidateCurrentRegions),
            new Check("EnemyFoundation",
                EnemyFoundationValidator.Validate),
            new Check("EnemyHealthBar",
                EnemyHealthBarValidator.Validate),
            new Check("HudPolish",
                HudPolishValidator.Validate),
            new Check("HudGameplayBinding",
                HudGameplayBindingInstaller.Validate),
            new Check("Gate0Input",
                Gate0InputBindingValidator.ValidateFinsterwald),
            new Check("RegionPortals",
                RegionPortalValidator.ValidateAllRegions),
            new Check("SceneIntegrityFinsterwald",
                SceneIntegrity.FinsterwaldSceneIntegrityValidator.ValidateFromMenu),
            new Check("SceneIntegrityAlleSzenen",
                SceneIntegrity.RegionSceneIntegrityValidator.ValidateFromMenu),
            new Check("ModelImports",
                ModelImportValidator.Validate),
        };

        [MenuItem("Elyndor/QA/Alle Validatoren durchlaufen")]
        public static void RunAllFromMenu()
        {
            string report = RunAll(out int failed);
            if (failed == 0)
            {
                Debug.Log(report);
                return;
            }

            Debug.LogError(report);
        }

        /// <summary>Einstiegspunkt fuer den Batchmode.</summary>
        public static void RunAllBatch()
        {
            string report = RunAll(out int failed);

            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            File.WriteAllText(ResultPath, report);

            Debug.Log(report);
            EditorApplication.Exit(failed == 0 ? 0 : 1);
        }

        private static string RunAll(out int failed)
        {
            StringBuilder report = new StringBuilder();
            failed = 0;

            foreach (Check check in Checks)
            {
                int errors = 0;
                string firstError = null;

                void Collect(string condition, string stack, LogType type)
                {
                    if (type != LogType.Error &&
                        type != LogType.Exception &&
                        type != LogType.Assert)
                    {
                        return;
                    }

                    errors++;
                    firstError ??= condition;
                }

                Application.logMessageReceived += Collect;

                string thrown = null;
                try
                {
                    // Jeder Validator startet auf derselben Szene. Ohne das
                    // pruefen die aktiv-szenen-basierten Validatoren je nach
                    // Reihenfolge eine fremde Region.
                    EditorSceneManager.OpenScene(
                        RegionScenes.Finsterwald, OpenSceneMode.Single);

                    check.Run();
                }
                catch (Exception exception)
                {
                    thrown = exception.GetType().Name + ": " + exception.Message;
                }
                finally
                {
                    Application.logMessageReceived -= Collect;
                }

                bool clean = errors == 0 && thrown == null;
                if (!clean)
                {
                    failed++;
                }

                report.AppendLine(
                    $"{(clean ? "OK  " : "FAIL")} {check.Name}" +
                    (clean ? string.Empty : $" — errors={errors}") +
                    (thrown != null ? $" — Ausnahme: {thrown}" : string.Empty) +
                    (firstError != null ? $"\n     erste Meldung: {Head(firstError)}" : string.Empty));
            }

            report.Insert(0,
                $"Elyndor Validatoren: {Checks.Count - failed}/{Checks.Count} sauber\n");

            return report.ToString();
        }

        /// <summary>Erste Zeile einer Meldung, damit der Bericht lesbar bleibt.</summary>
        private static string Head(string message)
        {
            int newline = message.IndexOf('\n');
            string head = newline < 0 ? message : message.Substring(0, newline);
            return head.Length <= 200 ? head : head.Substring(0, 200) + " …";
        }
    }
}
