# Asset Pipeline

Stand: 1. August 2026

## Grundsatz

Vor jeder Beschaffung wird der vorhandene Bestand geprueft. Nur benoetigte,
stilistisch passende und eindeutig lizenzierte Dateien gelangen ins produktive
Projekt. Ein kostenloser Download ist keine ausreichende Lizenzfreigabe.

## Quellenprioritaet

1. Bereits im Repository vorhandene und dokumentierte Assets.
2. Eindeutige CC0-Quellen, insbesondere Poly Haven, ambientCG und Kenney.
3. Weitere Quellen nur nach Einzelpruefung der kommerziellen Nutzung,
   Bearbeitung, Weitergabe und Attributionspflicht.
4. Meshy fuer individuelle Hero-Assets und unverwechselbare Regionsobjekte.

Der vorhandene `ASSET_CATALOG.md` ist Kandidatenliste, keine automatische
Importfreigabe.

## Beschaffungsablauf

1. Bedarf, Einsatzort und Ersatz fuer einen bestehenden Platzhalter benennen.
2. Bestand und Kandidatenmanifest durchsuchen.
3. Quelle, Autor, Assetname, URL, Lizenz, Abrufdatum und Lizenznachweis erfassen.
4. Nur benoetigte Dateien in einen isolierten Importbereich laden.
5. Archivinhalt, Dateiformate, Schadsoftware-Risiko und Duplikate pruefen.
6. In Unity importieren und technische Abnahme durchfuehren.
7. Produktives Prefab erstellen; Quellmodell nicht direkt in Szenen verteilen.
8. Herkunftsmanifest und betroffene Regionsdokumentation aktualisieren.
9. Validator, Batch-Kompilierung, Sichtpruefung und Performancepruefung.

## Meshy-Regeln

- Bestehende Assets und CC0-Alternativen zuerst pruefen.
- Pro Asset maximal zwei Generierungsversuche ohne neue Freigabe.
- Vor kostenpflichtiger Generierung Guthaben und erwarteten Verbrauch pruefen.
- Prompt, Generierungs-ID, Datum und Nutzungsrechte dokumentieren, niemals den
  API-Key.
- Importabnahme: Scale, Pivot, Up-Axis, Topologie, Polycount, UVs, Normalen,
  Materialslots, Texturen, Collider, LODs und Silhouette.
- Das Finsterwald Root Gate ist ein bestehendes Meshy-Hero-Asset auf
  `feature/world-visual-overhaul`; es darf nicht erneut generiert werden.

## Unity-Importstandard

- Modelle: FBX bevorzugt, konsistente Meter-Skalierung und aufrechte Achsen.
- Texturen: standardmaessig 1K/2K; groesser nur mit belegtem Hero-Bedarf.
- Materialien: URP-kompatibel, korrekte Alpha-/Normal-Map-Einstellungen,
  moeglichst gemeinsame Materialfamilien.
- Prefabs: benannt, reproduzierbar, ohne fehlende Referenzen und mit sauberer
  Root-Transformation.
- Collider: nur fuer Gameplay; einfache Primitive vor MeshCollider, Deko ohne
  Collider.
- LODs: fuer grosse oder vielfach platzierte Modelle verpflichtend; Schwellen
  anhand Profiling bestimmen.
- Lightmap-/Static-Flags nur passend zur tatsaechlichen Beweglichkeit.

## Vorlaeufige Performancebudgets

Diese Werte sind Leitplanken und muessen mit Zielhardware validiert werden:

| Kategorie | Ziel |
|---|---|
| Standard-Umgebungsmodell | bevorzugt unter 20k Dreiecke |
| Hero-Landmarke | bevorzugt unter 100k Dreiecke, mit LODs |
| Standardtextur | 1K–2K |
| Hero-Textur | maximal 4K nach Begruendung |
| Materialslots pro Standardmodell | 1–2 |
| Collider | minimale, spielrelevante Form |

`NOCH ZU ENTSCHEIDEN`: finale Plattformen, FPS-Ziel, Speicher-, Drawcall- und
Regionsbudgets. Bis dahin entscheidet ein Profiler-Nachweis, nicht die reine
Dateigroesse.

## Ordnerstruktur

```text
Assets/_Elyndor/Art/<Domaene>/<RegionOderSystem>/<AssetName>/
  Source/          optional, nur notwendige freigegebene Quelldateien
  Materials/
  Textures/
  Prefabs/
Assets/ThirdParty/<AnbieterOderPack>/
```

Projekt-eigene Ableitungen liegen unter `_Elyndor`; unveraenderte Fremdpacks
bleiben unter `ThirdParty`. Keine Archive, Formatduplikate oder komplette
Bibliotheken ohne konkreten Bedarf committen.

## Herkunftsmanifest

Jedes externe Asset benoetigt mindestens:

| Feld | Inhalt |
|---|---|
| ID | stabile interne Kennung |
| Name/Version | Originalbezeichnung |
| Autor/Quelle | Urheber und direkte URL |
| Lizenz | exakte Lizenz und Nachweisdatei |
| Abrufdatum | ISO-Datum |
| Dateien | produktive Pfade |
| Aenderungen | Skalierung, Material, Meshbearbeitung |
| Einsatz | Region/System/Prefab |
| Pruefung | Lizenz, Import, Performance, Verantwortlicher |

Das Paket `feature/external-asset-pipeline` liegt als Draft-PR #8 bei Commit
`4b13a42` vor. Es entwickelt Fetcher und Richtlinien parallel und muss vor
Integration auf den aktuellen `developer` gebracht sowie gegen dieses Dokument
abgeglichen werden.

## Abnahmecheckliste

- Lizenz eindeutig und lokal belegt.
- Keine Secrets, Downloader-Caches oder Archive im Commit.
- Keine fehlenden Materialien, Scripts oder Texturen.
- Scale, Pivot, UV, Normalen und Silhouette geprueft.
- Collider und LODs begruendet.
- Prefab statt direkter Szenenstreuung.
- Herkunftsmanifest aktualisiert.
- Unity-Batch-Kompilierung, Validator, `git diff --check` und Sichtpruefung.
