using System;
using System.IO;
using Elyndor.Memory;
using Elyndor.Persistence;
using Elyndor.Puzzles;
using Elyndor.World;
using UnityEditor;
using UnityEngine;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Der Nachweis, dass der Fortschritt einen echten Programmstart
    /// ueberlebt — und nicht nur ein <c>Save()</c> gefolgt von einem
    /// <c>Load()</c> im selben laufenden Prozess.
    ///
    /// Der Unterschied ist nicht akademisch. Ein Test im selben Prozess
    /// kann gruen sein, waehrend in Wahrheit ein statisches Feld die Daten
    /// haelt und die Datei gar nicht gelesen wird. Deshalb laufen diese
    /// beiden Methoden in <b>zwei getrennten Unity-Prozessen</b>: der erste
    /// schreibt und endet, der zweite startet frisch und findet vor. Zwischen
    /// ihnen existiert nichts als die Datei auf der Platte.
    ///
    /// Aufruf, zweimal nacheinander:
    ///
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath &lt;p&gt; \
    ///   -executeMethod Elyndor.EditorTools.SaveRestartProof.WriteProgress
    ///
    /// Unity.exe -batchmode -quit -projectPath &lt;p&gt; \
    ///   -executeMethod Elyndor.EditorTools.SaveRestartProof.VerifyProgress
    /// </code>
    ///
    /// Der zweite Aufruf endet mit Code 0, wenn der Fortschritt vollstaendig
    /// vorlag, und mit 1, wenn nicht.
    /// </summary>
    public static class SaveRestartProof
    {
        private const string SiteId = "restart_proof_memory";
        private const string PuzzleId = "restart_proof_bridge";
        private const string RegionId = "restart_proof_region";

        /// <summary>
        /// Eine Ankerstellung, die keine Ausgangsstellung ist. Waeren hier
        /// lauter Nullen, wuerde ein verlorener Eintrag wie ein
        /// wiederhergestellter aussehen.
        /// </summary>
        private static readonly int[] AnchorSettings = { 0, 1, 2 };

        /// <summary>
        /// Ein eigener Ordner neben dem echten Spielstand. Der Nachweis darf
        /// den Spielstand eines Menschen nicht anfassen.
        /// </summary>
        private static string ProofDirectory =>
            Path.Combine(Path.GetTempPath(), "elyndor_restart_proof");

        private static SaveService CreateService()
        {
            SaveService service =
                new SaveService(new FileSaveStore(ProofDirectory));

            SaveService.Install(service);
            return service;
        }

        /// <summary>Erster Prozess: Fortschritt herstellen und wegschreiben.</summary>
        public static void WriteProgress()
        {
            try
            {
                Directory.CreateDirectory(ProofDirectory);

                SaveService service = CreateService();
                service.ResetProgress();

                MemorySessionState.MarkActivated(SiteId);
                PuzzleSessionState.SetBridgeState(
                    PuzzleId, BridgePuzzleState.Solved);
                PuzzleSessionState.SetAnchorSettings(PuzzleId, AnchorSettings);
                RegionRegenerationState.MarkRegenerated(RegionId);

                bool written = service.Save();

                Debug.Log(
                    $"RESTART_PROOF_WRITE: geschrieben={written} " +
                    $"ordner={ProofDirectory}");

                EditorApplication.Exit(written ? 0 : 1);
            }
            catch (Exception exception)
            {
                Debug.LogError($"RESTART_PROOF_WRITE fehlgeschlagen: {exception}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Zweiter Prozess: frisch gestartet, nichts im Speicher, nur die
        /// Datei. Findet er den Fortschritt vor, ist die Persistenz echt.
        /// </summary>
        public static void VerifyProgress()
        {
            try
            {
                // Ausdruecklich leer beginnen: haette ein statischer Rest die
                // Daten gehalten, wuerde der Nachweis nichts beweisen.
                MemorySessionState.ForgetAll();
                PuzzleSessionState.ForgetAll();
                RegionRegenerationState.ForgetAll();

                SaveService service = CreateService();
                LoadOutcome outcome = service.LoadAndRestore();

                bool memory = MemorySessionState.IsActivated(SiteId);
                bool puzzle = PuzzleSessionState.GetBridgeState(PuzzleId) ==
                              BridgePuzzleState.Solved;
                bool region = RegionRegenerationState.IsRegenerated(RegionId);

                // Die Ankerstellungen gehoeren zum Raetselstand: ohne sie
                // meldet die wiederhergestellte Bruecke eine Spannung, die es
                // nicht gibt.
                bool anchors = SameSettings(
                    PuzzleSessionState.GetAnchorSettings(PuzzleId),
                    AnchorSettings);

                bool complete = outcome == LoadOutcome.Loaded &&
                                memory && puzzle && region && anchors;

                Debug.Log(
                    $"RESTART_PROOF_VERIFY: outcome={outcome} " +
                    $"erinnerung={memory} raetsel={puzzle} anker={anchors} " +
                    $"region={region} vollstaendig={complete}");

                EditorApplication.Exit(complete ? 0 : 1);
            }
            catch (Exception exception)
            {
                Debug.LogError($"RESTART_PROOF_VERIFY fehlgeschlagen: {exception}");
                EditorApplication.Exit(1);
            }
        }

        private static bool SameSettings(int[] restored, int[] expected)
        {
            if (restored == null || restored.Length != expected.Length)
            {
                return false;
            }

            for (int i = 0; i < expected.Length; i++)
            {
                if (restored[i] != expected[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Raeumt den Nachweis-Ordner wieder weg.</summary>
        public static void CleanUp()
        {
            try
            {
                if (Directory.Exists(ProofDirectory))
                {
                    Directory.Delete(ProofDirectory, true);
                }

                Debug.Log("RESTART_PROOF_CLEANUP: erledigt.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"RESTART_PROOF_CLEANUP fehlgeschlagen: {exception}");
                EditorApplication.Exit(1);
            }
        }
    }
}
