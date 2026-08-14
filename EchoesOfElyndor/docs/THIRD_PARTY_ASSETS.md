# Drittanbieterassets

Diese Datei ist der menschenlesbare Nachweis externer Assets. Die maschinenlesbare Quelle ist `Tools/AssetFetcher/asset_manifest.json`. Beide Nachweise werden erst ergänzt, wenn ein Asset tatsächlich geprüft und in das Projekt aufgenommen wurde.

## Aktueller Bestand

Über die neue AssetFetcher-Pipeline wurden noch keine externen Assets importiert oder heruntergeladen.

| Name | Quelle | Source-ID | Urheber | Lizenz | Download-Datum | Originaldateien / SHA-256 | Unity-Zielpfad | Änderungen |
|---|---|---|---|---|---|---|---|---|
| _Keine_ | — | — | — | — | — | — | — | — |

## Bestandsassets aus der Zeit vor der Pipeline

Diese Assets lagen bereits vor Einführung der AssetFetcher-Pipeline im
Projekt. Sie sind hier aufgeführt, wenn ihre Nutzung oder ihre
Importeinstellungen sich geändert haben.

| Pack | Lizenznachweis | Genutzt für | Änderung |
|---|---|---|---|
| Quaternius, *Ultimate Animated Animals - July 2021* | `License.txt` im Pack weist CC0 1.0 Universal aus; `ASSET_CATALOG.md` führt das Pack unabhängig davon als CC0 mit Quell-URL | `Wolf.fbx` als **Blockout** des Wurzelstreifers (Größe, Bewegung, Hitboxen, Telegraph, Kamera, Timing) | Am 15.08.2026 wurde in `Wolf.fbx.meta` ein Avatar erzeugt (`avatarSetup = CreateFromThisModel`). Ohne ihn sind die zwölf mitgelieferten Animationsclips nicht abspielbar. Die FBX-Datei selbst ist unverändert. |

Der Blockout definiert **nicht** die Art Direction. Der finale Entwurf steht
in `07_Enemies/WURZELSTREIFER_CONCEPT_BRIEF.md`.

## Pflichtangaben

Jeder neue Eintrag enthält:

- Name und eindeutige Source-ID,
- Quelle und direkte Assetseite,
- Urheber bzw. die vom Anbieter ausgewiesenen Beitragenden,
- eindeutig geprüfte Lizenz (in Phase 1 ausschließlich CC0),
- Download-Datum in UTC,
- sämtliche Originaldateien mit Dateigröße und SHA-256,
- Unity-Zielpfad unter `Assets/_Elyndor/ThirdParty/`,
- jede vorgenommene Änderung oder ausdrücklich „keine“,
- Ergebnis der technischen Importprüfung einschließlich Materialien, Prefab, Collider und LODs.

API-Metadaten von Poly Haven werden als „Poly Haven public API“ gekennzeichnet. Bei Kenney wird ein manueller Download samt manueller Lizenzprüfung dokumentiert, solange kein stabiler offizieller automatischer Downloadweg freigegeben ist.
