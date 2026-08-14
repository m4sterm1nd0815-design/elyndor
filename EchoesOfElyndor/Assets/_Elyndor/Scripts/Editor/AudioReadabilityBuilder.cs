using System;
using System.IO;
using Elyndor.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Richtet die Gameplay-Lesbarkeit über Ton ein (P1.11A).
    ///
    /// Kein Sounddesign, sondern Hörbarkeit von Regeln: der Spieler soll den
    /// Sprungbiss kommen hören, den Unterschied zwischen Treffer und Block
    /// hören, und am Brückenrätsel hören, ob die Ankerstellung trägt.
    ///
    /// Es entsteht <b>kein</b> Audio-Framework. Alles hängt an der bereits
    /// vorhandenen <see cref="SfxLibrary"/>, die schon vorher zentral auf
    /// Spielereignisse gehört hat.
    ///
    /// Die Clips stammen aus dem vorhandenen Kenney-Bestand (CC0, jeweils
    /// eigene <c>License.txt</c> im Pack). Genau ein Ton wird erzeugt, weil es
    /// dafür im Bestand nichts Passendes gibt — siehe
    /// <see cref="GenerateStoneScrapePlaceholder"/>.
    /// </summary>
    public static class AudioReadabilityBuilder
    {
        private const string ScenePath =
            "Assets/_Elyndor/Scenes/Finsterwald.unity";

        private const string PlaceholderFolder =
            "Assets/_Elyndor/Audio/Placeholder";

        /// <summary>
        /// Der Name sagt es an jeder Stelle, an der er auftaucht: im
        /// Projektfenster, im Inspector der SfxLibrary und im Dateisystem.
        /// </summary>
        private const string StoneScrapePath =
            PlaceholderFolder + "/PLACEHOLDER_Stein_Reiben.wav";

        // Kenney-Bestand, CC0.
        private const string CreakLong =
            "Assets/ThirdParty/Kenney/kenney_rpg-audio/Audio/creak1.ogg";

        private const string CreakShort =
            "Assets/ThirdParty/Kenney/kenney_rpg-audio/Audio/creak3.ogg";

        private const string WoodLight =
            "Assets/ThirdParty/Kenney/kenney_impact-sounds/Audio/impactWood_light_000.ogg";

        private const string WoodHeavy =
            "Assets/ThirdParty/Kenney/kenney_impact-sounds/Audio/impactWood_heavy_000.ogg";

        private const string SoftMedium =
            "Assets/ThirdParty/Kenney/kenney_impact-sounds/Audio/impactSoft_medium_000.ogg";

        private const string SoftHeavy =
            "Assets/ThirdParty/Kenney/kenney_impact-sounds/Audio/impactSoft_heavy_000.ogg";

        private const string PunchMedium =
            "Assets/ThirdParty/Kenney/kenney_impact-sounds/Audio/impactPunch_medium_000.ogg";

        [MenuItem("Elyndor/Setup/Audio-Lesbarkeit einrichten")]
        public static void Build()
        {
            GenerateStoneScrapePlaceholder();
            ApplyToScene();
        }

        // ------------------------------------------------------------------
        // Placeholder
        // ------------------------------------------------------------------

        /// <summary>
        /// Erzeugt ein dumpfes Stein-Reibgeräusch.
        ///
        /// Warum überhaupt erzeugt: der vorhandene Bestand kennt Aufschläge auf
        /// Stein (<c>impactMining</c>), aber kein <em>Reiben</em>. Ein
        /// Aufschlag ist weder dumpf noch reibend; er würde als „etwas ist
        /// zerbrochen" gelesen statt als „das trägt nicht". Da an diesem Ton
        /// eine Spielregel hängt, ist ein klar gekennzeichneter Platzhalter
        /// ehrlicher als ein danebenliegender echter Clip.
        ///
        /// Deterministisch erzeugt: ein fester Zufallskeim, damit ein erneuter
        /// Lauf dieselbe Datei schreibt und keine Scheinänderung entsteht.
        /// </summary>
        public static void GenerateStoneScrapePlaceholder()
        {
            EnsureFolder(PlaceholderFolder);

            const int sampleRate = 44100;
            const float duration = 0.45f;
            int sampleCount = (int)(sampleRate * duration);

            float[] samples = new float[sampleCount];

            // Fester Keim statt UnityEngine.Random: reproduzierbar.
            uint seed = 0x5EED_5A1Eu;
            float lowPassA = 0f;
            float lowPassB = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;

                // Weisses Rauschen als Grundlage des Reibens.
                seed = seed * 1664525u + 1013904223u;
                float noise = (seed >> 8) / 8388608f - 1f;

                // Zweimal einpoliger Tiefpass: macht aus Zischen ein Dumpfes.
                const float cutoff = 0.045f;
                lowPassA += cutoff * (noise - lowPassA);
                lowPassB += cutoff * (lowPassA - lowPassB);

                // Rauheit: langsame Amplitudenmodulation liest sich als
                // Reiben, nicht als Rauschen.
                float roughness =
                    0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * 26f * t);

                // Huellkurve: schneller Ansatz, langes Auslaufen.
                float attack = Mathf.Clamp01(t / 0.03f);
                float release = Mathf.Clamp01((duration - t) / 0.28f);
                float envelope = attack * release;

                samples[i] = lowPassB * roughness * envelope;
            }

            Normalise(samples, 0.7f);
            WriteWav(StoneScrapePath, samples, sampleRate);

            AssetDatabase.ImportAsset(
                StoneScrapePath, ImportAssetOptions.ForceUpdate);

            Debug.Log(
                "AUDIO_PLACEHOLDER_OK: " + StoneScrapePath +
                " erzeugt (dumpfes Stein-Reiben, 0,45 s). " +
                "PLATZHALTER — im Bestand gibt es kein Reibgeraeusch.");
        }

        private static void Normalise(float[] samples, float peak)
        {
            float max = 0f;

            foreach (float sample in samples)
            {
                max = Mathf.Max(max, Mathf.Abs(sample));
            }

            if (max <= 0.0001f)
            {
                return;
            }

            float scale = peak / max;

            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= scale;
            }
        }

        /// <summary>Schreibt 16-Bit-PCM-Mono als WAV.</summary>
        private static void WriteWav(
            string assetPath, float[] samples, int sampleRate)
        {
            string fullPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!, assetPath);

            using FileStream stream = new FileStream(fullPath, FileMode.Create);
            using BinaryWriter writer = new BinaryWriter(stream);

            int dataBytes = samples.Length * 2;

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });

            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);              // Groesse des fmt-Blocks
            writer.Write((short)1);        // PCM
            writer.Write((short)1);        // Mono
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);  // Bytes je Sekunde
            writer.Write((short)2);        // Blockausrichtung
            writer.Write((short)16);       // Bits je Abtastwert

            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);

            foreach (float sample in samples)
            {
                writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
            }
        }

        // ------------------------------------------------------------------
        // Verdrahtung
        // ------------------------------------------------------------------

        /// <summary>Legt die Clips auf die vorhandene SfxLibrary der Szene.</summary>
        public static void ApplyToScene()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath, OpenSceneMode.Single);

            SfxLibrary library =
                UnityEngine.Object.FindAnyObjectByType<SfxLibrary>();

            if (library == null)
            {
                throw new InvalidOperationException(
                    "Audio-Lesbarkeit: Keine SfxLibrary in der Szene.");
            }

            SerializedObject serialized = new SerializedObject(library);

            // Der Telegraph laeuft 0,7 s; creak1 misst 0,661 s und endet damit
            // fast genau dann, wenn der Biss landet.
            Assign(serialized, "enemyTelegraphClip", CreakLong);

            Assign(serialized, "enemyHitLightClip", WoodLight);
            Assign(serialized, "enemyHitHeavyClip", WoodHeavy);
            Assign(serialized, "enemyDefeatedClip", SoftHeavy);

            // Geblockt klingt gedaempft, ungeblockt trifft hart.
            Assign(serialized, "blockedHitClip", SoftMedium);
            Assign(serialized, "unblockedHitClip", PunchMedium);

            // Holz und Seil, wenn es traegt; erzeugtes Stein-Reiben, wenn nicht.
            Assign(serialized, "bridgeTensionHoldsClip", CreakShort);
            Assign(serialized, "bridgeTensionSlackClip", StoneScrapePath);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                "AUDIO_READABILITY_OK: acht Hinweise auf der vorhandenen " +
                "SfxLibrary verdrahtet. Kein neues Audio-System.");
        }

        private static void Assign(
            SerializedObject serialized, string field, string assetPath)
        {
            SerializedProperty property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogWarning(
                    $"Audio-Lesbarkeit: Feld '{field}' fehlt auf SfxLibrary.");
                return;
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            if (clip == null)
            {
                Debug.LogWarning(
                    $"Audio-Lesbarkeit: Clip '{assetPath}' nicht gefunden.");
                return;
            }

            property.objectReferenceValue = clip;
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
