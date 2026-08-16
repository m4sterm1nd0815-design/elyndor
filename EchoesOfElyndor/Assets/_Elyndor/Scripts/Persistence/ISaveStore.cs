namespace Elyndor.Persistence
{
    /// <summary>
    /// Wohin ein Spielstand geschrieben und woher er gelesen wird.
    ///
    /// Absichtlich auf Text reduziert: der Speicher weiss nichts ueber
    /// Erinnerungen, Raetsel oder Regionen, und der
    /// <see cref="SaveService"/> weiss nichts ueber Dateien. Genau an dieser
    /// Naht haengen die Tests einen temporaeren Speicher ein, damit sie
    /// niemals einen echten Spielstand anfassen.
    /// </summary>
    public interface ISaveStore
    {
        /// <summary>Liegt ueberhaupt ein Spielstand vor?</summary>
        bool Exists { get; }

        /// <summary>
        /// Liest den Spielstand. Gibt <c>null</c> zurueck, wenn keiner da ist.
        /// Wirft bei Lesefehlern — die Bewertung uebernimmt der Aufrufer.
        /// </summary>
        string Read();

        /// <summary>
        /// Schreibt den Spielstand so, dass ein Abbruch mitten im Schreiben
        /// den letzten gueltigen Stand nicht zerstoert.
        /// </summary>
        void Write(string content);

        /// <summary>Entfernt den Spielstand vollstaendig.</summary>
        void Delete();

        /// <summary>
        /// Liest die letzte bekannte gute Sicherung, oder <c>null</c>.
        /// Wird nur angefasst, wenn der eigentliche Stand unlesbar ist.
        /// </summary>
        string ReadBackup();
    }
}
