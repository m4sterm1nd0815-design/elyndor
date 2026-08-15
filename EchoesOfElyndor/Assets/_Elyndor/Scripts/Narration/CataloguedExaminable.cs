using Elyndor.Core;
using Elyndor.Interaction;
using UnityEngine;

namespace Elyndor.Narration
{
    /// <summary>
    /// Ein untersuchbares Objekt, dessen Text aus dem Katalog kommt.
    ///
    /// Gegenstück zu <c>ExaminableObject</c>, das seinen Text im Inspector
    /// trägt. Für optionale Lore ist der Katalog der richtige Ort: dieselbe
    /// Gravur taucht an mehreren Stellen auf und soll überall dasselbe sagen —
    /// mit drei Inspectorfeldern wären es früher oder später drei Fassungen.
    ///
    /// Rein optional. Das Spiel muss vollständig funktionieren, wenn ein
    /// Spieler nichts davon anfasst.
    /// </summary>
    public sealed class CataloguedExaminable : InteractableBase
    {
        [Header("Katalog")]
        [SerializeField] private NarrationCatalog catalog;
        [SerializeField] private string narrationKey;

        [Tooltip("Nur einmal lesbar? Wiederholbar ist die Regel — eine " +
                 "Inschrift verschwindet nicht, wenn man sie zweimal ansieht.")]
        [SerializeField] private bool onceOnly;

        private bool used;

        public string NarrationKey => narrationKey;
        public NarrationCatalog Catalog => catalog;

        /// <summary>
        /// Setzt Katalog und Schluessel. Fuer Aufbauwerkzeuge — damit die
        /// Verdrahtung nicht ueber SerializedProperty laufen muss.
        /// </summary>
        public void Configure(NarrationCatalog newCatalog, string key)
        {
            catalog = newCatalog;
            narrationKey = key;
        }

        public override bool CanInteract(GameObject interactor) =>
            !onceOnly || !used;

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            used = true;

            NarrationEntry entry = catalog == null
                ? null
                : catalog.Find(narrationKey);

            if (entry == null)
            {
                // Bewusst eine Warnung und kein Fehler: ein fehlender
                // optionaler Text darf das Spiel nicht rot faerben.
                Debug.LogWarning(
                    $"{name}: Kein Katalogeintrag fuer '{narrationKey}'.",
                    this);
                return;
            }

            NarrationEvents.RaiseMessage(entry.Text, entry.Duration);
        }
    }

}
