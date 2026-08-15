using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elyndor.Narration
{
    /// <summary>
    /// Die Lokalisierungsschlüssel des Finsterwald-Slice.
    ///
    /// Texte stehen nie im Code und nie in einem Inspectorfeld einer Szene,
    /// sondern immer hinter einem Schlüssel. Sonst müsste eine Übersetzung
    /// die Szene durchsuchen — und ein Text, der nur in einem Prefab steht,
    /// wird zuverlässig übersehen.
    /// </summary>
    public static class NarrationKeys
    {
        // --- Memory Site -------------------------------------------------
        public const string MemoryBridge = "finsterwald.memory.bruecke";

        /// <summary>
        /// Was Aren nach der Erinnerung sicher weiss. Mehr nicht: nicht wer bei
        /// ihm war, nicht warum, nicht wann, nicht wer seine Erinnerungen
        /// genommen hat.
        /// </summary>
        public const string MemoryBridgeAftermath =
            "finsterwald.memory.bruecke.nachhall";

        // --- Unbekannte Stimme -------------------------------------------
        /// <summary>
        /// Sprecher bleibt „Unbekannte Stimme". Kein Name faellt im Slice.
        /// </summary>
        public const string UnknownVoiceBridge = "finsterwald.stimme.bruecke";

        // --- Optionale Lore ----------------------------------------------
        public const string InscriptionMark = "finsterwald.lore.gravur";
        public const string LoreRestingPlace = "finsterwald.lore.rastplatz";
        public const string LoreStreamStones = "finsterwald.lore.bachsteine";
        public const string LoreBridgeRemnant = "finsterwald.lore.brueckenrest";

        /// <summary>Alle Schluessel; fuer Vollstaendigkeitspruefungen.</summary>
        public static readonly string[] All =
        {
            MemoryBridge,
            MemoryBridgeAftermath,
            UnknownVoiceBridge,
            InscriptionMark,
            LoreRestingPlace,
            LoreStreamStones,
            LoreBridgeRemnant
        };

        /// <summary>
        /// Begriffe, die im Vertical Slice <b>nicht</b> vorkommen dürfen.
        ///
        /// Nicht aus Prüderie, sondern weil jeder von ihnen eine Frage
        /// beantworten würde, die der Slice offen lassen soll. Ein Test hält
        /// das fest — Kanon, der nur in einem Dokument steht, hält keinem
        /// Textnachtrag stand.
        /// </summary>
        public static readonly string[] ForbiddenTerms =
        {
            "Soren", "Elian", "mein Sohn", "meinem Sohn", "Vater"
        };
    }

    /// <summary>Ein Eintrag des Katalogs.</summary>
    [Serializable]
    public sealed class NarrationEntry
    {
        [Tooltip("Lokalisierungsschluessel; siehe NarrationKeys.")]
        public string Key;

        [TextArea(2, 8)]
        [Tooltip("Deutscher Text. Uebersetzungen kommen spaeter ueber " +
                 "denselben Schluessel.")]
        public string Text;

        [Min(1f)]
        [Tooltip("Anzeigedauer in Sekunden.")]
        public float Duration = 6f;
    }

    /// <summary>
    /// Der Textbestand des Slice, hinter Schlüsseln.
    ///
    /// Bewusst ein ScriptableObject und kein Lokalisierungspaket: das Projekt
    /// braucht heute eine Sprache und eine saubere Schlüsselstruktur, nicht
    /// eine Abhängigkeit. Wer später ein Paket einführt, findet hier eine
    /// vollständige Schlüsselliste vor und muss keine Szene durchsuchen.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NarrationCatalog",
        menuName = "Elyndor/Narration Catalog")]
    public sealed class NarrationCatalog : ScriptableObject
    {
        [SerializeField] private NarrationEntry[] entries =
            Array.Empty<NarrationEntry>();

        private Dictionary<string, NarrationEntry> lookup;

        public IReadOnlyList<NarrationEntry> Entries => entries;

        /// <summary>Findet einen Eintrag; null, wenn der Schluessel fehlt.</summary>
        public NarrationEntry Find(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            BuildLookup();

            return lookup.TryGetValue(key, out NarrationEntry entry)
                ? entry
                : null;
        }

        public void SetEntries(NarrationEntry[] newEntries)
        {
            entries = newEntries ?? Array.Empty<NarrationEntry>();
            lookup = null;
        }

        private void BuildLookup()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, NarrationEntry>(StringComparer.Ordinal);

            foreach (NarrationEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                lookup[entry.Key] = entry;
            }
        }
    }
}
