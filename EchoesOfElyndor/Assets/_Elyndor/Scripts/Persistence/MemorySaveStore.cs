namespace Elyndor.Persistence
{
    /// <summary>
    /// Ein Spielstand, der nur im Arbeitsspeicher lebt.
    ///
    /// Fuer Tests: sie duerfen unter keinen Umstaenden einen echten
    /// Spielstand anfassen. Ausserdem laesst sich damit ein beschaedigter
    /// Stand herstellen, ohne eine Datei zu verbiegen.
    /// </summary>
    public sealed class MemorySaveStore : ISaveStore
    {
        private string content;
        private string backup;

        public MemorySaveStore(string initialContent = null)
        {
            content = initialContent;
        }

        public bool Exists => content != null;

        public string Read() => content;

        public string ReadBackup() => backup;

        public void Write(string newContent)
        {
            if (content != null)
            {
                backup = content;
            }

            content = newContent;
        }

        public void Delete()
        {
            content = null;
            backup = null;
        }

        /// <summary>Setzt einen Inhalt, ohne die Sicherung zu beruehren.</summary>
        public void Corrupt(string brokenContent)
        {
            content = brokenContent;
        }
    }
}
