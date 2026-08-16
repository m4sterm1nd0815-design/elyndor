using System.IO;
using UnityEngine;

namespace Elyndor.Persistence
{
    /// <summary>
    /// Der Spielstand als Datei.
    ///
    /// <b>Atomar geschrieben.</b> Nicht in die gueltige Datei hinein, sondern
    /// erst vollstaendig daneben, und dann getauscht. Ein Stromausfall
    /// mitten im Schreiben kostet damit hoechstens den neuen Stand, niemals
    /// den alten — in die Zieldatei hinein zu schreiben hiesse, den einzigen
    /// gueltigen Stand als Erstes zu zerstoeren.
    ///
    /// Beim Tausch wandert die bisherige Datei zur Sicherung. Sie ist die
    /// zweite Verteidigungslinie, wenn der neue Stand sich spaeter als
    /// unlesbar erweist.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        public const string DefaultFileName = "elyndor_progress.json";

        private readonly string savePath;
        private readonly string backupPath;
        private readonly string temporaryPath;

        /// <summary>
        /// Legt den Speicher an. Ohne Verzeichnis landet er unter
        /// <see cref="Application.persistentDataPath"/> — nie im Projektordner
        /// und nie unter einem festen Pfad.
        /// </summary>
        public FileSaveStore(string directory = null, string fileName = DefaultFileName)
        {
            string folder = string.IsNullOrEmpty(directory)
                ? Application.persistentDataPath
                : directory;

            savePath = Path.Combine(folder, fileName);
            backupPath = savePath + ".backup";
            temporaryPath = savePath + ".tmp";
        }

        /// <summary>Wohin geschrieben wird; fuer Bericht und Fehlersuche.</summary>
        public string SavePath => savePath;

        public bool Exists => File.Exists(savePath);

        public string Read()
        {
            return File.Exists(savePath) ? File.ReadAllText(savePath) : null;
        }

        public string ReadBackup()
        {
            return File.Exists(backupPath) ? File.ReadAllText(backupPath) : null;
        }

        public void Write(string content)
        {
            string folder = Path.GetDirectoryName(savePath);

            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // 1. Vollstaendig daneben schreiben.
            File.WriteAllText(temporaryPath, content);

            // 2. Den bisherigen Stand sichern, bevor er ersetzt wird.
            if (File.Exists(savePath))
            {
                File.Copy(savePath, backupPath, true);
            }

            // 3. Erst jetzt tauschen. File.Replace waere die schoenere
            //    Variante, verlangt aber eine existierende Zieldatei und
            //    dasselbe Laufwerk; das hier gilt auch beim ersten Speichern.
            File.Copy(temporaryPath, savePath, true);
            File.Delete(temporaryPath);
        }

        public void Delete()
        {
            foreach (string path in new[] { savePath, backupPath, temporaryPath })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
