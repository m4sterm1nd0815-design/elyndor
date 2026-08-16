using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Elyndor.EditorTools
{
    /// <summary>
    /// Liest und prueft die maschinenlesbare Herkunfts- und Lizenzangabe, die
    /// als Sidecar-Datei neben einem Asset liegt:
    /// <c>&lt;Assetpfad&gt;.provenance.json</c>.
    ///
    /// Warum eine Datei neben dem Asset und nicht ein Eintrag in einer
    /// zentralen Liste: Eine Liste und ein Asset koennen auseinanderlaufen,
    /// sobald jemand das Asset verschiebt, umbenennt oder loescht — und
    /// niemand merkt es, weil die Liste weiterhin ordentlich aussieht. Die
    /// Sidecar-Datei wandert mit, und wenn sie fehlt, faellt genau das auf.
    ///
    /// Das Markdown-Herkunftsmanifest in <c>docs/ASSET_PIPELINE.md</c> bleibt
    /// bestehen. Es beschreibt Beschaffungsvorgaenge fuer Menschen; diese
    /// Datei ist das, was ein Werkzeug pruefen kann.
    ///
    /// Diese Klasse liest ausschliesslich. Sie legt keine Sidecar-Datei an,
    /// repariert keine und schreibt nichts zurueck. Ein Pruefwerkzeug, das den
    /// fehlenden Zustand selbst herstellt, prueft anschliessend nur noch sich
    /// selbst.
    /// </summary>
    public static class AssetProvenance
    {
        /// <summary>Kennung des Formats. Steht in jeder Datei und wird geprueft.</summary>
        public const string SchemaId = "elyndor.asset-provenance";

        /// <summary>
        /// Version des Formats. Ein Leser, der eine unbekannte Version sieht,
        /// meldet sie als Fehler, statt sie nach bestem Wissen zu raten.
        /// </summary>
        public const int SchemaVersion = 1;

        /// <summary>Endung, die an den vollen Assetpfad angehaengt wird.</summary>
        public const string SidecarExtension = ".provenance.json";

        /// <summary>
        /// Zulaessige Werte fuer <c>origin</c>. Die drei Faelle entsprechen den
        /// Herkunftsarten, die die Pipelinedokumente ohnehin unterscheiden:
        /// selbst gebaut, generiert, fremd. Ein freier Text an dieser Stelle
        /// waere nicht auswertbar.
        /// </summary>
        public static readonly IReadOnlyList<string> KnownOrigins = new[]
        {
            "authored-in-house",
            "ai-generated",
            "third-party",
        };

        /// <summary>Der Pfad, an dem die Herkunftsdatei zu einem Asset liegt.</summary>
        public static string SidecarPathFor(string assetPath)
        {
            return assetPath + SidecarExtension;
        }

        /// <summary>
        /// Liest die Herkunftsdatei zum Asset und prueft sie. Die Rueckgabe ist
        /// die Liste der Beanstandungen; leer heisst gueltig.
        /// </summary>
        /// <param name="summary">
        /// Einzeiler fuer den Pruefbericht. Auch bei Beanstandungen gesetzt,
        /// damit im Bericht steht, was tatsaechlich in der Datei stand.
        /// </param>
        public static IReadOnlyList<string> ValidateFile(
            string assetPath, out string summary)
        {
            string sidecarPath = SidecarPathFor(assetPath);

            if (!File.Exists(sidecarPath))
            {
                summary = "keine Herkunftsdatei";
                return new[]
                {
                    $"Herkunftsdatei '{sidecarPath}' fehlt. Ohne sie ist " +
                    "nicht belegt, wer das Asset gebaut hat und unter welcher " +
                    "Lizenz es benutzt werden darf.",
                };
            }

            string json;
            try
            {
                json = File.ReadAllText(sidecarPath);
            }
            catch (IOException exception)
            {
                summary = "Herkunftsdatei nicht lesbar";
                return new[]
                {
                    $"Herkunftsdatei '{sidecarPath}' ist nicht lesbar: " +
                    exception.Message,
                };
            }

            return Validate(json, assetPath, out summary);
        }

        /// <summary>
        /// Prueft den Inhalt einer Herkunftsdatei gegen den Assetpfad, zu dem
        /// sie gehoeren soll. Beruehrt die Platte nicht — genau deshalb laesst
        /// sich jeder Fehlerfall pruefen, ohne eine echte Datei zu verbiegen.
        /// </summary>
        public static IReadOnlyList<string> Validate(
            string sidecarJson, string expectedAssetPath, out string summary)
        {
            var problems = new List<string>();
            summary = "unlesbar";

            if (!TryParse(sidecarJson, out Record record, out string parseError))
            {
                problems.Add(parseError);
                return problems;
            }

            summary =
                $"schema={Describe(record.schema)} v{record.schemaVersion} " +
                $"origin={Describe(record.origin)} " +
                $"creator={Describe(record.creator)} " +
                $"license={Describe(record.license)}";

            if (!string.Equals(record.schema, SchemaId, StringComparison.Ordinal))
            {
                problems.Add(
                    $"Feld 'schema' ist {Describe(record.schema)}, erwartet " +
                    $"'{SchemaId}'.");
            }

            if (record.schemaVersion != SchemaVersion)
            {
                problems.Add(
                    $"Feld 'schemaVersion' ist {record.schemaVersion}, dieser " +
                    $"Leser kennt {SchemaVersion}.");
            }

            CheckAssetPath(record, expectedAssetPath, problems);

            RequireText(record.creator, "creator", "wer das Asset gebaut hat", problems);
            RequireText(record.source, "source", "woraus es entstanden ist", problems);
            RequireText(record.license, "license", "unter welcher Lizenz es benutzt werden darf", problems);

            CheckOrigin(record, problems);
            CheckRecordedOn(record, problems);

            return problems;
        }

        private static void CheckAssetPath(
            Record record, string expectedAssetPath, List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(record.assetPath))
            {
                problems.Add(
                    "Feld 'assetPath' fehlt. Eine Herkunftsangabe ohne " +
                    "Assetbezug laesst sich beim naechsten Verschieben nicht " +
                    "mehr zuordnen.");
                return;
            }

            string declared = Normalize(record.assetPath);
            string expected = Normalize(expectedAssetPath);

            if (string.Equals(declared, expected, StringComparison.Ordinal))
            {
                return;
            }

            problems.Add(
                $"Feld 'assetPath' nennt '{declared}', die Datei liegt aber " +
                $"neben '{expected}'. Eine mitkopierte Herkunftsangabe " +
                "beschreibt sonst stillschweigend ein fremdes Asset.");
        }

        private static void CheckOrigin(Record record, List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(record.origin))
            {
                problems.Add(
                    "Feld 'origin' fehlt. Ohne Herkunftsart ist nicht " +
                    "entscheidbar, welche Beschaffungsregeln fuer das Asset " +
                    "gelten.");
                return;
            }

            foreach (string known in KnownOrigins)
            {
                if (string.Equals(record.origin, known, StringComparison.Ordinal))
                {
                    return;
                }
            }

            problems.Add(
                $"Feld 'origin' ist {Describe(record.origin)}, erlaubt sind " +
                $"{string.Join(", ", KnownOrigins)}.");
        }

        private static void CheckRecordedOn(Record record, List<string> problems)
        {
            if (string.IsNullOrWhiteSpace(record.recordedOn))
            {
                problems.Add(
                    "Feld 'recordedOn' fehlt. Eine Lizenzaussage ohne Datum " +
                    "laesst sich spaeter nicht gegen die Quelle nachpruefen.");
                return;
            }

            if (DateTime.TryParseExact(
                    record.recordedOn,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                return;
            }

            problems.Add(
                $"Feld 'recordedOn' ist {Describe(record.recordedOn)}, " +
                "erwartet ein ISO-Datum jjjj-mm-tt.");
        }

        private static void RequireText(
            string value, string field, string what, List<string> problems)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            problems.Add($"Feld '{field}' fehlt — damit ist nicht belegt, {what}.");
        }

        private static bool TryParse(
            string sidecarJson, out Record record, out string error)
        {
            record = null;
            error = null;

            if (string.IsNullOrWhiteSpace(sidecarJson))
            {
                error = "Herkunftsdatei ist leer.";
                return false;
            }

            // Vorpruefung auf ein JSON-Objekt, bevor JsonUtility drankommt.
            // JsonUtility meldet nicht jede kaputte Datei mit einer Ausnahme;
            // abgeschnittener oder gar kein JSON-Inhalt soll aber verlaesslich
            // als Fehler ankommen und nicht als leerer Datensatz.
            string trimmed = sidecarJson.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) ||
                !trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                error =
                    "Herkunftsdatei enthaelt kein JSON-Objekt (erwartet wird " +
                    "ein Inhalt von '{' bis '}').";
                return false;
            }

            try
            {
                record = JsonUtility.FromJson<Record>(trimmed);
            }
            catch (Exception exception)
            {
                error = $"Herkunftsdatei ist kein gueltiges JSON: {exception.Message}";
                return false;
            }

            if (record != null)
            {
                return true;
            }

            error = "Herkunftsdatei ist kein gueltiges JSON: kein Datensatz gelesen.";
            return false;
        }

        private static string Normalize(string path)
        {
            return path == null ? string.Empty : path.Trim().Replace('\\', '/');
        }

        private static string Describe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "leer" : $"'{value}'";
        }

        /// <summary>
        /// Abbild des Dateiinhalts. Kleingeschriebene Felder, weil
        /// <see cref="JsonUtility"/> ueber den Feldnamen abbildet und die
        /// Datei fuer Menschen lesbar bleiben soll.
        ///
        /// Abgebildet werden nur die Felder, die dieser Leser auch prueft.
        /// Weitere Felder in der Datei — <c>sourceUrl</c>, <c>licenseUrl</c>,
        /// <c>modifications</c>, <c>notes</c> — sind zulaessig und bleiben
        /// hier unerwaehnt: Ein Feld hier aufzufuehren, ohne es zu pruefen,
        /// weckt den Eindruck, es waere geprueft.
        ///
        /// Die Startwerte stehen da, weil nur <see cref="JsonUtility"/> in
        /// diese Felder schreibt und der Compiler das nicht sehen kann — ohne
        /// sie meldet er CS0649, und das Compiler-Gate ist rot. Sie aendern
        /// nichts an der Pruefung: ein leeres Pflichtfeld faellt genauso
        /// durch wie ein fehlendes.
        /// </summary>
        [Serializable]
        private sealed class Record
        {
            public string schema = string.Empty;
            public int schemaVersion = 0;
            public string assetPath = string.Empty;
            public string origin = string.Empty;
            public string creator = string.Empty;
            public string source = string.Empty;
            public string license = string.Empty;
            public string recordedOn = string.Empty;
        }
    }
}
