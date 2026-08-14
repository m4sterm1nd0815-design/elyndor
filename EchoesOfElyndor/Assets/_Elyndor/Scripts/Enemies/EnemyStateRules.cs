namespace Elyndor.Enemies
{
    /// <summary>
    /// Erlaubte Zustandswechsel der Gegnergrundlage — bewusst als reine
    /// Regeltabelle ohne Unity-Abhaengigkeit, damit sie sowohl von der
    /// Zustandsmaschine als auch vom Editor-Validator und von Tests
    /// vollstaendig geprueft werden kann.
    ///
    /// Grundsaetze:
    /// <list type="bullet">
    /// <item>Ein Zustand wird nie auf sich selbst gesetzt.</item>
    /// <item><see cref="EnemyFoundationState.Dead"/> ist endgueltig.</item>
    /// <item>Der Tod ist aus jedem lebenden Zustand heraus moeglich.</item>
    /// <item>Aus <see cref="EnemyFoundationState.Hurt"/> heraus wird nie
    /// direkt angegriffen; der Gegner muss erst wieder wahrnehmen.</item>
    /// <item>Aus <see cref="EnemyFoundationState.Idle"/> heraus wird das Ziel
    /// erst bemerkt (<see cref="EnemyFoundationState.Alert"/>), bevor verfolgt
    /// oder angegriffen wird.</item>
    /// <item><see cref="EnemyFoundationState.Retreat"/> ist aus jedem lebenden
    /// Zustand erreichbar, fuehrt aber nie direkt in den Angriff zurueck.</item>
    /// </list>
    /// </summary>
    public static class EnemyStateRules
    {
        /// <summary>Endzustaende koennen nicht mehr verlassen werden.</summary>
        public static bool IsFinal(EnemyFoundationState state)
        {
            return state == EnemyFoundationState.Dead;
        }

        /// <summary>Darf von <paramref name="from"/> nach <paramref name="to"/> gewechselt werden?</summary>
        public static bool CanTransition(
            EnemyFoundationState from, EnemyFoundationState to)
        {
            // Keine unnoetigen Neusetzungen desselben Zustands.
            if (from == to)
            {
                return false;
            }

            if (IsFinal(from))
            {
                return false;
            }

            // Der Tod hat aus jedem lebenden Zustand heraus Vorrang.
            if (to == EnemyFoundationState.Dead)
            {
                return true;
            }

            // Eine Trefferreaktion unterbricht jeden lebenden Zustand.
            if (to == EnemyFoundationState.Hurt)
            {
                return true;
            }

            // Der Rueckzug bricht jeden lebenden Zustand ab: er wird durch
            // einen Lebenspunktstand ausgeloest, nicht durch die Wahrnehmung,
            // und darf deshalb nicht davon abhaengen, was der Gegner gerade
            // tut. Aus dem Rueckzug selbst heraus wird nicht erneut
            // zurueckgezogen — das verhindert die Selbstwechselsperre oben.
            if (to == EnemyFoundationState.Retreat)
            {
                return true;
            }

            switch (from)
            {
                case EnemyFoundationState.Idle:
                    return to == EnemyFoundationState.Alert;

                case EnemyFoundationState.Alert:
                    return to == EnemyFoundationState.Idle ||
                           to == EnemyFoundationState.Chase ||
                           to == EnemyFoundationState.Attack;

                case EnemyFoundationState.Chase:
                    return to == EnemyFoundationState.Idle ||
                           to == EnemyFoundationState.Alert ||
                           to == EnemyFoundationState.Attack;

                case EnemyFoundationState.Attack:
                    return to == EnemyFoundationState.Idle ||
                           to == EnemyFoundationState.Alert ||
                           to == EnemyFoundationState.Chase;

                case EnemyFoundationState.Hurt:
                    return to == EnemyFoundationState.Idle ||
                           to == EnemyFoundationState.Alert ||
                           to == EnemyFoundationState.Chase;

                // Aus dem Rueckzug heraus wird nie direkt angegriffen: der
                // Gegner muss erst wieder wahrnehmen und aufschliessen.
                case EnemyFoundationState.Retreat:
                    return to == EnemyFoundationState.Idle ||
                           to == EnemyFoundationState.Alert ||
                           to == EnemyFoundationState.Chase;

                default:
                    return false;
            }
        }
    }
}
