# Richtlinie für externe Assets

## Zweck und Priorität

Diese Richtlinie gilt für externe Grafik-, Modell-, Textur- und HDRI-Assets in Echoes of Elyndor. Die Auswahl erfolgt immer in dieser Reihenfolge:

1. geeignete vorhandene Projektassets,
2. eindeutig als CC0 veröffentlichte Assets aus einer freigegebenen Quelle,
3. Meshy nur für begründete, individuelle Hero-Assets und nach separater Freigabe.

Phase 1 erlaubt ausschließlich Poly Haven, ambientCG und Kenney. Eine technisch erreichbare URL ist keine Freigabe. Neue Quellen müssen zuerst in dieser Richtlinie und in `Tools/AssetFetcher/approved_sources.json` geprüft und aufgenommen werden.

## Zulässige Nutzung der Quellen

### Poly Haven

Die öffentliche API `https://api.polyhaven.com` darf für Metadatensuche und später freigegebene Downloads verwendet werden. Jeder Request muss den eindeutigen User-Agent aus `approved_sources.json` senden. In Oberfläche, Logs, Suchausgabe und Assetnachweis muss erkennbar sein, dass die Daten von Poly Haven stammen. Unterstützte Assettypen sind HDRIs, Texturen und Modelle; filterbar sind Typ, Kategorie, Stichwort und Auflösung. Die Assets werden nur akzeptiert, wenn ihre Lizenz weiterhin eindeutig CC0 ist.

### ambientCG

Die offizielle API darf für Materialien/Oberflächen und geeignete 3D-Assets verwendet werden. Nur CC0 wird akzeptiert. Für PBR-Materialien sind mindestens vorhandene Varianten der folgenden Maps zu erfassen: Albedo/Color, Normal, Roughness, Displacement/Height, Ambient Occlusion, Metallic und Opacity. Fehlt eine erwartete Map, muss dies im Importbericht stehen; fehlende Maps werden nicht erfunden.

### Kenney

Nur Pakete mit eindeutiger CC0-Kennzeichnung dürfen verwendet werden. Solange kein stabiler offizieller API- oder Direktdownloadweg konfiguriert ist, wird die Website weder gescrapt noch technisch umgangen. Das Tool liefert deshalb lediglich den offiziellen Kataloglink und dokumentiert den manuellen Such- und Downloadbedarf. Vor Aufnahme sind Lizenz, Paketname, Quelle und Originaldatei manuell zu prüfen.

## Sicherheitsregeln

- Keine Login-Daten, Cookies, Tokens oder geheimen Header verwenden oder protokollieren.
- Keine nicht freigegebenen Websites scrapen oder Schutzmechanismen umgehen.
- Nur HTTPS und die pro Quelle erlaubten Hosts verwenden.
- Unbekannte, fehlende oder von CC0 abweichende Lizenzen automatisch ablehnen.
- Keine ausführbaren oder aktiven Dateitypen herunterladen oder importieren. Die Blockliste umfasst unter anderem EXE, MSI, DLL, Skripte, JAR, DMG und PKG.
- Content-Type, Endung und Größe vor Verarbeitung prüfen; Metadaten und Assets haben feste Größenlimits.
- Für jeden später freigegebenen Download SHA-256, Datum und Originaldateien speichern. Anbieter-Hashes dürfen zusätzlich dokumentiert werden, ersetzen SHA-256 aber nicht.
- ZIP-Dateien ausschließlich in einem neu angelegten temporären Ordner entpacken. Absolute Pfade, Laufwerkspfade, `..`, symbolische Links, nicht erlaubte Dateitypen, übergroße Inhalte und ZIP-Slip werden abgelehnt.
- Schreibziele müssen innerhalb des Unity-Projekts liegen. Bestehende Dateien und Assetordner dürfen niemals überschrieben werden; Konflikte brechen den Vorgang ab.
- Logs enthalten nur Vorgangsdaten, keine Query-Secrets oder Anmeldedaten.

Downloads sind in Phase 1 in der Konfiguration ausdrücklich deaktiviert. `search`, `preview`, `dry-run` und lokale Archivvalidierung führen keinen Assetdownload durch.

## Unity-Import

Das Zielmuster lautet `Assets/_Elyndor/ThirdParty/<Quelle>/<Assetname>/`. Bevorzugt werden FBX, OBJ, glTF/GLB sowie PNG, TGA, TIFF, EXR und HDR in sinnvoller Auflösung. Originaldateien bleiben nachvollziehbar; Konvertierungen und andere Änderungen werden im Manifest vermerkt.

Ein Dry-Run erstellt einen Importbericht. Materialien werden nie automatisch überschrieben. Prefabs werden erst nach sauberem Modell-, Textur- und Materialimport erzeugt. Collider und LOD-Gruppen werden nur nach Prüfung von Form, Nutzung, Polygonzahl und vorhandenen Anbieter-LODs ergänzt. Spielszenen werden durch das AssetFetcher-Werkzeug nicht verändert.

## Freigabeablauf

1. Projektbestand auf Wiederverwendung prüfen.
2. Metadaten mit `search` abrufen und Ergebnis als JSON sichern.
3. Lizenz, Autor, Source-ID, Typ, Kategorie, Auflösung und PBR-Vollständigkeit prüfen.
4. Mit `preview` bzw. `dry-run` Zielpfad und Importbericht kontrollieren.
5. Vor einem späteren Download explizite Review/Freigabe einholen.
6. Datei und Archiv prüfen, SHA-256 bilden, in temporärem Ordner entpacken.
7. Ausschließlich in einen neuen, leeren Zielordner kopieren und Unity-Import technisch prüfen.
8. Manifest und `THIRD_PARTY_ASSETS.md` aktualisieren.

## Kommandozeilenbeispiele

```powershell
python Tools/AssetFetcher/asset_fetcher.py --help
python Tools/AssetFetcher/asset_fetcher.py search --source poly-haven --type texture --category rock --keyword moss --resolution 2k --limit 1
python Tools/AssetFetcher/asset_fetcher.py preview search-result.json --index 0
python Tools/AssetFetcher/asset_fetcher.py dry-run search-result.json --index 0 --resolution 2k
python Tools/AssetFetcher/asset_fetcher.py validate-archive C:\temp\candidate.zip
```

`--write-manifest` ist bewusst optional: Ein Suchtreffer allein ist noch kein aufgenommenes Drittanbieterasset.
