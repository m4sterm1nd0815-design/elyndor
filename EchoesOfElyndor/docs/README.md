# Echoes of Elyndor – Projektdokumentation

Verbindliche Wissensbasis für Design, Technik und Produktion.

## Verbindliche Hierarchie

Bei Widersprüchen gilt die Reihenfolge von oben nach unten.

| # | Dokument | Rolle |
|---|---|---|
| 1 | `PROJECT_BIBLE.md` | kreative Verfassung — Vision, Prinzipien, Ausschlüsse |
| 2 | `00_Project/GAME_DESIGN_DOCUMENT.md` | detailliertes Game Design |
| 3 | `01_World/` | Welt und Regionen |
| 4 | `02_Story/StoryBible.md` | detaillierter Story-Kanon |
| 5 | `03_Characters/` | Charakter-Kanon |
| 6 | `04_Gameplay/` | Gameplay-Mechaniken |
| 7 | `ARCHITECTURE.md` | einzige technische Wahrheit |

`VISION_GUARD.md` bleibt der Prüfstein gegen Scope-Drift und steht neben der
kreativen Verfassung.

## Kanonregeln

- **Nichts erfinden.** Ungeklärtes wird als `OFFEN` oder `NOCH ZU ENTSCHEIDEN`
  markiert und bleibt es, bis eine ausdrückliche Entscheidung vorliegt.
- **Widersprüche werden dokumentiert, nicht still aufgelöst.** Die bekannten
  Widersprüche stehen gesammelt in `02_Story/StoryBible.md`.
- Ein Dokument weiter oben in der Hierarchie schlägt eines weiter unten. Wo ein
  tieferes Dokument präzisiert statt widerspricht, gilt die Präzisierung.

## Migrierte Dokumente

Diese Dateien wurden konsolidiert und verweisen nur noch auf ihren neuen Ort.
Sie bleiben als Weiterleitung bestehen, damit vorhandene Verweise nicht ins
Leere laufen:

| Alt | Neu |
|---|---|
| `GAME_BIBLE.md` | `02_Story/`, `01_World/`, `03_Characters/`, `04_Gameplay/` |
| `LORE_BIBLE.md` | dieselben Zielorte |
| `GAME_DESIGN_DOCUMENT.md` | `00_Project/GAME_DESIGN_DOCUMENT.md` |
| `Technical/ARCHITECTURE.md` | `ARCHITECTURE.md` |
| `10_Unity/Architecture.md` | `ARCHITECTURE.md` |

## Weitere Fachdokumente

- `Technical/CODING_STANDARD.md`
- `Art/UI_GUIDELINES.md`
- `TASK_QUEUE.md` — Arbeitsstand und Freigaben
- `CHANGELOG.md` — was sich wann geändert hat und warum
- `DECISIONS.md` — getroffene Entscheidungen
- `EXTERNAL_ASSET_POLICY.md`, `ASSET_PIPELINE.md`, `THIRD_PARTY_ASSETS.md`

## Noch leere Platzhalter

`05_Items/`, `06_Dungeons/`, `07_Enemies/`, `08_UI/`, `09_Audio/` sowie
`02_Story/MainQuest.md` und `02_Story/SideQuests.md` enthalten bisher nur
Überschriften. Dort existiert **noch kein Kanon**. Sie werden bewusst nicht
gefüllt, solange keine Quelle vorliegt — erfinden ist ausgeschlossen.
