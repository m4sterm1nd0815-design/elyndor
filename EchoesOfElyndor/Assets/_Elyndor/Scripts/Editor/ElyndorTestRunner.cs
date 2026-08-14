using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Startet die Testsuiten aus dem Editor heraus und schreibt das Ergebnis
    /// in eine Datei.
    ///
    /// Hintergrund: Das Test-Runner-Fenster meldet sein Ergebnis nur an die
    /// Oberflaeche. Ein PlayMode-Lauf loest ausserdem Domain-Reloads aus, die
    /// jede kurzlebig registrierte Rueckmeldung mitnehmen. Deshalb registriert
    /// sich der Callback nach jedem Reload neu und legt das Ergebnis unter
    /// <see cref="ResultDirectory"/> ab — von dort ist es unabhaengig von der
    /// Oberflaeche auswertbar.
    ///
    /// Bewusst klein gehalten: kein QA-Framework, nur ein Einstiegspunkt und
    /// eine Ergebnisdatei.
    /// </summary>
    public static class ElyndorTestRunner
    {
        public const string ResultDirectory = "Temp/ElyndorTests";

        [MenuItem("Elyndor/QA/Run EditMode Tests")]
        public static void RunEditMode() => Run(TestMode.EditMode);

        [MenuItem("Elyndor/QA/Run PlayMode Tests")]
        public static void RunPlayMode() => Run(TestMode.PlayMode);

        public static string ResultPath(TestMode mode) =>
            Path.Combine(ResultDirectory, mode + ".txt");

        /// <summary>
        /// Der laufende Modus. Ueber <see cref="SessionState"/>, weil ein
        /// PlayMode-Lauf Domain-Reloads ausloest und ein statisches Feld dabei
        /// verloren ginge — das Ergebnis landete sonst in der falschen Datei.
        /// </summary>
        private const string RunningModeKey = "Elyndor.TestRunner.RunningMode";

        private static void Run(TestMode mode)
        {
            Directory.CreateDirectory(ResultDirectory);
            File.WriteAllText(ResultPath(mode), $"RUNNING {mode}\n");
            SessionState.SetString(RunningModeKey, mode.ToString());

            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(new Filter { testMode = mode }));

            Debug.Log($"Elyndor QA: {mode}-Lauf gestartet. " +
                      $"Ergebnis landet in '{ResultPath(mode)}'.");
        }

        /// <summary>
        /// Nach jedem Domain-Reload erneut registrieren, sonst geht das
        /// Ergebnis eines PlayMode-Laufs verloren.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RegisterCallbacks()
        {
            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(ScriptableObject.CreateInstance<ResultWriter>());
        }

        /// <summary>
        /// Schreibt Zusammenfassung und Fehlschlaege in die Ergebnisdatei.
        /// ScriptableObject, damit Unity die Instanz ueber Reloads haelt.
        /// </summary>
        private sealed class ResultWriter : ScriptableObject, ICallbacks
        {
            private readonly StringBuilder failures = new StringBuilder();

            public void RunStarted(ITestAdaptor testsToRun)
            {
                failures.Clear();
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                TestMode mode =
                    SessionState.GetString(RunningModeKey, nameof(TestMode.EditMode)) ==
                    nameof(TestMode.PlayMode)
                        ? TestMode.PlayMode
                        : TestMode.EditMode;

                StringBuilder report = new StringBuilder();
                report.AppendLine($"STATUS {result.TestStatus}");
                report.AppendLine(
                    $"passed={result.PassCount} failed={result.FailCount} " +
                    $"skipped={result.SkipCount} " +
                    $"inconclusive={result.InconclusiveCount} " +
                    $"duration={result.Duration:F1}s");

                if (failures.Length > 0)
                {
                    report.AppendLine("--- Fehlschlaege ---");
                    report.Append(failures);
                }

                Directory.CreateDirectory(ResultDirectory);
                File.WriteAllText(ResultPath(mode), report.ToString());
                Debug.Log($"Elyndor QA ({mode}):\n{report}");
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren || result.TestStatus != TestStatus.Failed)
                {
                    return;
                }

                failures.AppendLine($"{result.FullName}: {result.Message}");
            }
        }
    }
}
