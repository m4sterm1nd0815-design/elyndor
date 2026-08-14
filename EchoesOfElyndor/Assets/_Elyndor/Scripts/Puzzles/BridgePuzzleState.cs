namespace Elyndor.Puzzles
{
    /// <summary>
    /// Zustände des Brückenrätsels „Die geteilte Brücke".
    ///
    /// Die Reihenfolge folgt dem geplanten Ablauf: beobachten, Watch nutzen,
    /// Anker stellen, prüfen, freigeben, sichern. Jeder Zustand ist stabil
    /// genug, um gespeichert zu werden — mit Ausnahme der beiden
    /// Übergangszustände <see cref="Recovering"/> und
    /// <see cref="BridgeDeploying"/>, die auf ihren jeweils letzten stabilen
    /// Zustand zurückfallen.
    /// </summary>
    public enum BridgePuzzleState
    {
        /// <summary>Szene geladen, Rätsel ungelöst, Aren ist nicht in der Zone.</summary>
        Dormant = 0,

        /// <summary>Aren steht in der Resonanzzone; die Watch meldet Bereitschaft.</summary>
        WatchAvailable = 1,

        /// <summary>Das Echo wurde mindestens einmal gesehen.</summary>
        EchoObserved = 2,

        /// <summary>Mindestens ein Anker wurde verstellt.</summary>
        Configuring = 3,

        /// <summary>Der Stamm hat sich verkantet; kurz keine Eingabe.</summary>
        Recovering = 4,

        /// <summary>Die Ankerstellung trägt; der Stamm kann freigegeben werden.</summary>
        ReadyToRelease = 5,

        /// <summary>Der Stamm rollt; die Eingabe am Rätsel ist gesperrt.</summary>
        BridgeDeploying = 6,

        /// <summary>Der Stamm liegt; die Bohlen fehlen noch.</summary>
        Securing = 7,

        /// <summary>Die Brücke trägt. Endzustand.</summary>
        Solved = 8
    }
}
