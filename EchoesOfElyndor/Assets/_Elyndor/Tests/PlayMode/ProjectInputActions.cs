using UnityEngine;
using UnityEngine.InputSystem;

namespace Elyndor.Tests
{
    /// <summary>
    /// Haelt das projektweite Actions-Asset fuer Tests fest.
    ///
    /// Hintergrund: <see cref="InputTestFixture"/> setzt das Input-System in
    /// ihrem Setup zurueck. Danach liefert <see cref="InputSystem.actions"/>
    /// <c>null</c>, und jeder spaeter laufende Test, der das Projekt-Asset
    /// prueft, ueberspringt sich still — die Absicherung waere weg, ohne dass
    /// je ein Test rot wird. Genau das ist passiert, als eine weitere Fixture
    /// dazukam, die alphabetisch vor den Projekt-Asset-Tests laeuft.
    ///
    /// Zurueckschreiben scheidet aus: <c>InputSystem.actions</c> wirft im Play
    /// Mode. Deshalb wird die Referenz einmal beim Start der Wiedergabe
    /// festgehalten, bevor irgendeine Fixture sie loeschen kann.
    /// </summary>
    internal static class ProjectInputActions
    {
        private static InputActionAsset captured;
        private static bool hasCaptured;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureOnPlayModeStart()
        {
            Capture();
        }

        /// <summary>
        /// Das projektweite Asset, unabhaengig davon, ob vorher schon eine
        /// Fixture das Input-System zurueckgesetzt hat.
        /// </summary>
        internal static InputActionAsset Current
        {
            get
            {
                // Rueckfall, falls der Startaufruf nicht gegriffen hat: der
                // erste Zugriff haelt fest, was dann noch da ist.
                if (!hasCaptured)
                {
                    Capture();
                }

                return captured != null ? captured : InputSystem.actions;
            }
        }

        private static void Capture()
        {
            InputActionAsset current = InputSystem.actions;

            if (current == null)
            {
                return;
            }

            captured = current;
            hasCaptured = true;
        }
    }
}
