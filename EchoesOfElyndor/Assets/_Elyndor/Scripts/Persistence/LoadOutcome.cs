namespace Elyndor.Persistence
{
    /// <summary>
    /// Was beim Laden herausgekommen ist.
    ///
    /// Ausdruecklich mehr als „hat geklappt" oder „hat nicht geklappt": ein
    /// beschaedigter Stand und gar kein Stand fuehren beide zu einem leeren
    /// Spiel, sind aber voellig verschiedene Vorfaelle. Der eine ist der
    /// Normalfall beim ersten Start, der andere ein Datenverlust, den
    /// jemand erfahren sollte.
    /// </summary>
    public enum LoadOutcome
    {
        /// <summary>Kein Spielstand vorhanden — sauberer Anfang.</summary>
        NoSave = 0,

        /// <summary>Gelesen und uebernommen.</summary>
        Loaded = 1,

        /// <summary>Der Stand war unlesbar; die Sicherung hat getragen.</summary>
        RecoveredFromBackup = 2,

        /// <summary>Stand und Sicherung unlesbar. Es wurde leer begonnen.</summary>
        Corrupt = 3,

        /// <summary>
        /// Der Stand stammt aus einer neueren Fassung des Spiels und wurde
        /// deshalb <em>nicht</em> geladen. Er bleibt unberuehrt liegen.
        /// </summary>
        FromNewerVersion = 4
    }
}
