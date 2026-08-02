# Task Queue

Stand: 1. August 2026
Basis der Bestandsaufnahme: `origin/developer` bei `c21a8e1`

## Statusregeln

- `FREIGEGEBEN`: darf in Reihenfolge bearbeitet werden.
- `IN ARBEIT`: laufendes, isoliertes Paket; nicht duplizieren.
- `REVIEW`: implementiert, Integration/Abnahme offen.
- `INTEGRIERT`: in `developer` enthalten.
- `BLOCKIERT`: Abhaengigkeit oder Entscheidung fehlt.
- `GEPLANT`: keine automatische Arbeitsfreigabe.
- `GATE`: Stop und ausdrueckliche Freigabe erforderlich.

## Aktueller realer Stand

| Bereich | Status | Branch/PR | Beleg/Notiz |
|---|---|---|---|
| UI Foundation | INTEGRIERT | PR #2, spaeter `developer` | HUD-Grundlage vorhanden |
| UI Polish | INTEGRIERT | PR #4 gegen `developer` | PR #3 gegen `main` ist ueberholt |
| HUD Gameplay Binding | INTEGRIERT | PR #6, Commit `c21a8e1` | am 01.08.2026 in `developer` gemergt |
| Quickslot Input | REVIEW | `feature/quickslot-input`, Draft-PR #7 | lokaler Commit `91ce77c`, Base `developer` |
| World Visual Overhaul | REVIEW | `feature/world-visual-overhaul`, Draft-PR #9 | `5510e06`, enthaelt aktuellen `developer` |
| Meshy Root Gate | REVIEW | Teil von PR #9 | committed/published, nicht mehr ungesichert |
| External Asset Pipeline | REVIEW | `feature/external-asset-pipeline`, Draft-PR #8 | `4b13a42`; Worktree sauber, Basis vor PR #6 |
| PR #3 UI Polish | INTEGRIERT | geschlossen, nicht gemergt | durch PR #4/`developer` ueberholt |
| PR #5 Developer | BLOCKIERT | `developer` → `main` | vor Gate 0 nicht mergebereit |
| Produktionsbibel | REVIEW | `feature/project-production-bible`, PR #10 | Commit wird im PR gefuehrt |
| Root-Gate-Materialkorrektur | REVIEW | `feature/root-gate-material-fix`, PR #13 | URP-Material und Texturzuweisung implementiert; Batch-Compile und World-Validator bestanden; visuelle Play-Mode-Abnahme `MANUELL OFFEN` |

## Priorisierte Ausfuehrungsreihenfolge — erste zehn Aufgaben

### 1. DOC-001 — Produktions- und Wissensstruktur

- Status: `REVIEW`
- Branch: `feature/project-production-bible`
- Scope: acht verbindliche Dateien: `AGENTS.md` und sieben Fachdokumente.
- Abschluss: Links, Widersprueche, `git diff --check`, Commit und PR gegen
  `developer`.

### 2. P0-QS-001 — Quickslot-PR #7 technisch pruefen

- Status: `FREIGEGEBEN`
- Abhaengigkeit: DOC-001 integriert oder konfliktfrei parallel.
- Pruefen: Input Actions, direkte Slotwahl, zyklische Auswahl, Benutzung,
  UI-Fokus, Controller und Tastatur.
- Manuell: `MANUELL OFFEN` bis Unity-Play-Mode-Test dokumentiert ist.

### 3. P0-QS-002 — Quickslot Input integrieren

- Status: `BLOCKIERT` durch Aufgabe 2/Review.
- Ziel: Draft-PR #7 nach bestandener Abnahme in `developer` mergen.
- Commit/PR: `91ce77c`, PR #7; finalen Merge-Commit nachtragen.

### 4. P0-WORLD-001 — World-Branch auf aktuellen `developer` bringen

- Status: `BLOCKIERT` durch Aufgabe 3.
- Branch: `feature/world-visual-overhaul`, aktuell `5510e06`, Draft-PR #9.
- Der Branch enthaelt `c21a8e1` und bewahrt das HUD-Binding. Nach Integration
  von PR #7 muss er erneut auf den dann aktuellen `developer` gebracht werden.

### 5. P0-WORLD-002 — World Overhaul und Root Gate validieren

- Status: `BLOCKIERT` durch Aufgabe 4.
- Pruefen: drei Szenen, Builder-Idempotenz, Root-Gate-Import, Materialien,
  Scale/Pivot, Collider/LOD, Portale, Spawns, `[Handarbeit]`, Missing Scripts.
- Manuell: dokumentierter Fuenf-Minuten-Rundgang pro Region.

### 6. P0-WORLD-003 — World Overhaul integrieren

- Status: `BLOCKIERT` durch Aufgabe 5.
- Ziel: konfliktfreier PR gegen `developer`; keine Regression von HUD,
  Quickslots oder Gameplay-Roots.

### 7. P0-ASSET-001 — External Asset Pipeline abschliessen

- Status: `REVIEW`, Commit `4b13a42`, Draft-PR #8.
- Der Worktree ist sauber. Der Branch basiert noch vor PR #6 und muss vor
  Integration auf den aktuellen `developer` gebracht werden.
- Abnahme: Tests, Herkunftsmanifest, Quellenallowlist, sichere Downloads,
  Lizenzregeln und Abgleich mit `ASSET_PIPELINE.md`.

### 8. P0-REMOTE-001 — veralteten PR #3 bereinigen

- Status: `INTEGRIERT`.
- PR #3 ist geschlossen; sein Inhalt ist ueber PR #4 in `developer` enthalten.

### 9. P0-REMOTE-002 — PR #5 bis Gate 0 zurueckstellen

- Status: `BLOCKIERT` bis Aufgaben 2–8 abgeschlossen sind.
- Kein Merge nach `main`; nach Gate 0 neuen Gesamtstand und Checks bewerten.

### 10. P0-QA-001 — integrierter Unity-Gesamttest

- Status: `BLOCKIERT` bis Quickslot, World und Asset-Pipeline integriert sind.
- Frischer `developer`-Worktree, Batch-Kompilierung, Validatoren,
  `git diff --check`, Missing-Script-Pruefung und manueller Regionen-/Systemtest.

### Gate-0 Input-Korrektur — MANUELL OFFEN

- Branch: `feature/gate0-input-fix`
- Draft-PR: `#14` gegen `developer`
- Scope: `E`-Interaktion und Quickslot-Direktwahl `1–8`.
- Automatisch bestanden: Unity-Kompilierung, HUD-/Input-Validator,
  World-Validator, Binding-Duplikatprüfung und `git diff --check`.
- Manuell offen: erneuter Play-Mode-Test für `E`, `1–8` und Controller.
- Gate 0 bleibt offen; keine Freigabe für Phase 1.

### Gate-0 Runtime-UI-Korrektur — MANUELL OFFEN

- Branch: `feature/gate0-runtime-ui-fix`; Draft-PR: `#15` gegen `developer`
- Scope: sichtbarer Interaktionshinweis und Quickslot-Direktwahl `1–8`.
- Automatisch bestanden: Gate-0-Input-/HUD-Validator einschließlich
  Runtime-Auswahlroute und sichtbarem Fokuszustand.
- Manuell offen: Interaktionshinweis, `1–8` und Controller.
- Bereits bestandene Eingaben `E` und `R` bleiben unverändert.
- Gate 0 bleibt offen; keine Freigabe für Phase 1.
## GATE 0 — nicht ueberschreiten

Status: `GATE`

Erforderliche Belege:

- sauberer und nachvollziehbarer Git-/PR-Stand;
- technische Grundlagen konfliktfrei in `developer`;
- Unity-Batch-Kompilierung und Validatoren erfolgreich;
- manueller Test von Input, HUD, Quickslots, Kampfprototyp, Memory Site,
  Portalen und drei Regionen;
- keine ungeklaerten Asset-Lizenzen oder Secrets.

Nach Erreichen mit Ergebnisbericht stoppen und ausdrueckliche Freigabe fuer
Phase 1 einholen.

## Phase 1 und spaeter

Finsterwald Vertical Slice ist `GEPLANT` und erst nach Gate 0 freigebbar.
Sonnenfelder, Nebelmoor, Tal der verlorenen Wege und Ruinen von Arvenfall sind
nur geplant und duerfen nicht automatisch begonnen werden. Details stehen in
`DEVELOPMENT_PLAN.md`, `WORLD_ROADMAP.md` und `GAMEPLAY_ROADMAP.md`.
