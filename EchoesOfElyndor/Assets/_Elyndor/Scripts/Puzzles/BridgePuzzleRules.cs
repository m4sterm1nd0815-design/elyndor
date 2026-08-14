namespace Elyndor.Puzzles
{
    /// <summary>Die drei drehbaren Ankersteine der geteilten Brücke.</summary>
    public enum BridgeAnchorId
    {
        /// <summary>Der tiefe Anker am Südufer.</summary>
        SouthDeep = 0,

        /// <summary>Der Seitenanker am Seitenstein.</summary>
        Side = 1,

        /// <summary>Der Anker am Nordufer.</summary>
        North = 2
    }

    /// <summary>
    /// Regeln des Brückenrätsels — bewusst ohne Unity-Abhängigkeit, damit
    /// Zustandstabelle und Lösung vollständig geprüft werden können, ohne eine
    /// Szene zu laden.
    ///
    /// Die Lösung selbst ist ein einzelner Vergleich. Der Aufwand steckt nicht
    /// darin, sondern in der Frage, ob ein Spieler sie <em>herleitet</em> statt
    /// alle 27 Kombinationen durchzuprobieren. Das entscheidet der
    /// Usability-Test, nicht dieser Code.
    /// </summary>
    public static class BridgePuzzleRules
    {
        /// <summary>Jeder Anker hat drei stabile Stellungen mit ein, zwei oder drei Kerben.</summary>
        public const int SettingsPerAnchor = 3;

        /// <summary>Anzahl der Anker.</summary>
        public const int AnchorCount = 3;

        /// <summary>Alle Kombinationen — die Zahl, gegen die der Test antritt.</summary>
        public const int TotalCombinations = 27;

        /// <summary>
        /// Die akzeptierte Konfiguration in Kerben: Süd-Tiefanker 1,
        /// Seitenanker 2, Nordanker 3.
        ///
        /// Sie folgt dem Lastverlauf des Echos von tief nach hoch, nicht der
        /// Zahlenreihe der Kerben. Wer stumpf 1-2-3 in Leserichtung einstellt,
        /// liegt falsch — genau das ist der nachvollziehbare Irrtum, den der
        /// Plan vorsieht.
        ///
        /// `VORSCHLAG` für den ersten Usability-Prototyp. Nicht heilig.
        /// </summary>
        public static int RequiredNotches(BridgeAnchorId anchor)
        {
            return anchor switch
            {
                BridgeAnchorId.SouthDeep => 1,
                BridgeAnchorId.Side => 2,
                BridgeAnchorId.North => 3,
                _ => 0
            };
        }

        /// <summary>Kerbenzahl einer Stellung; Stellung 0 hat eine Kerbe.</summary>
        public static int NotchesForSetting(int setting)
        {
            if (setting < 0 || setting >= SettingsPerAnchor)
            {
                return 0;
            }

            return setting + 1;
        }

        /// <summary>Die Stellung, die zu einer Kerbenzahl gehört.</summary>
        public static int SettingForNotches(int notches)
        {
            return notches - 1;
        }

        /// <summary>Trägt diese Konfiguration?</summary>
        public static bool IsCorrect(
            int southDeepNotches, int sideNotches, int northNotches)
        {
            return southDeepNotches == RequiredNotches(BridgeAnchorId.SouthDeep) &&
                   sideNotches == RequiredNotches(BridgeAnchorId.Side) &&
                   northNotches == RequiredNotches(BridgeAnchorId.North);
        }

        /// <summary>
        /// Wie viele Anker stehen richtig? Ausschliesslich für die
        /// Spannungsrückmeldung am Seilbock gedacht — sie meldet, <em>dass</em>
        /// etwas nicht trägt, nie <em>welcher</em> Anker falsch steht. Ein
        /// Hinweis, der den falschen Anker benennt, wäre kein Hinweis mehr,
        /// sondern die Lösung in Raten.
        /// </summary>
        public static int CorrectAnchorCount(
            int southDeepNotches, int sideNotches, int northNotches)
        {
            int correct = 0;

            if (southDeepNotches == RequiredNotches(BridgeAnchorId.SouthDeep))
            {
                correct++;
            }

            if (sideNotches == RequiredNotches(BridgeAnchorId.Side))
            {
                correct++;
            }

            if (northNotches == RequiredNotches(BridgeAnchorId.North))
            {
                correct++;
            }

            return correct;
        }

        /// <summary>Ist der Zustand endgültig?</summary>
        public static bool IsFinal(BridgePuzzleState state)
        {
            return state == BridgePuzzleState.Solved;
        }

        /// <summary>
        /// Schreibt dieser Zustand in den Sitzungszustand? Die beiden
        /// Übergangszustände tun das bewusst nicht: ein Laden mitten in der
        /// Animation soll auf den letzten stabilen Stand zurückfallen und
        /// niemals auf Zwischengeometrie.
        /// </summary>
        public static bool IsStable(BridgePuzzleState state)
        {
            return state != BridgePuzzleState.Recovering &&
                   state != BridgePuzzleState.BridgeDeploying;
        }

        /// <summary>Der stabile Zustand, auf den ein Übergangszustand zurückfällt.</summary>
        public static BridgePuzzleState StableFallback(BridgePuzzleState state)
        {
            return state switch
            {
                BridgePuzzleState.Recovering => BridgePuzzleState.Configuring,
                BridgePuzzleState.BridgeDeploying => BridgePuzzleState.ReadyToRelease,
                _ => state
            };
        }

        /// <summary>
        /// Darf die Watch in diesem Zustand ihre Bereitschaft melden? Vor dem
        /// Betreten der Zone nicht, nach der Lösung weiterhin — das Echo bleibt
        /// betrachtbar, auch wenn die Brücke steht.
        /// </summary>
        public static bool AllowsWatch(BridgePuzzleState state)
        {
            return state != BridgePuzzleState.Dormant &&
                   state != BridgePuzzleState.BridgeDeploying;
        }

        /// <summary>Dürfen die Anker in diesem Zustand gedreht werden?</summary>
        public static bool AllowsAnchorTurning(BridgePuzzleState state)
        {
            return state == BridgePuzzleState.EchoObserved ||
                   state == BridgePuzzleState.Configuring ||
                   state == BridgePuzzleState.ReadyToRelease;
        }

        /// <summary>Darf der Stamm in diesem Zustand freigegeben werden?</summary>
        public static bool AllowsRelease(BridgePuzzleState state)
        {
            return state == BridgePuzzleState.Configuring ||
                   state == BridgePuzzleState.ReadyToRelease;
        }

        /// <summary>Dürfen die Bohlen in diesem Zustand gesetzt werden?</summary>
        public static bool AllowsPlanks(BridgePuzzleState state)
        {
            return state == BridgePuzzleState.Securing;
        }

        /// <summary>
        /// Darf von <paramref name="from"/> nach <paramref name="to"/>
        /// gewechselt werden?
        /// </summary>
        public static bool CanTransition(
            BridgePuzzleState from, BridgePuzzleState to)
        {
            if (from == to)
            {
                return false;
            }

            if (IsFinal(from))
            {
                return false;
            }

            return from switch
            {
                BridgePuzzleState.Dormant =>
                    to == BridgePuzzleState.WatchAvailable,

                BridgePuzzleState.WatchAvailable =>
                    to == BridgePuzzleState.Dormant ||
                    to == BridgePuzzleState.EchoObserved,

                // Das Verlassen der Zone fällt bewusst nicht hinter das
                // gesehene Echo zurück: was Aren einmal gesehen hat, hat er
                // gesehen.
                BridgePuzzleState.EchoObserved =>
                    to == BridgePuzzleState.Configuring,

                BridgePuzzleState.Configuring =>
                    to == BridgePuzzleState.Recovering ||
                    to == BridgePuzzleState.ReadyToRelease,

                BridgePuzzleState.Recovering =>
                    to == BridgePuzzleState.Configuring,

                BridgePuzzleState.ReadyToRelease =>
                    to == BridgePuzzleState.Configuring ||
                    to == BridgePuzzleState.BridgeDeploying,

                BridgePuzzleState.BridgeDeploying =>
                    to == BridgePuzzleState.Securing ||
                    to == BridgePuzzleState.ReadyToRelease,

                BridgePuzzleState.Securing =>
                    to == BridgePuzzleState.Solved,

                _ => false
            };
        }
    }
}
