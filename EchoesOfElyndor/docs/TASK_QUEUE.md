# Task Queue

Stand: 2. August 2026
Basis der Bestandsaufnahme: `origin/developer` bei `cd7bc35`

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
| Quickslot Input | INTEGRIERT | PR #7 | Merge-Commit c9cf76f |
| World Visual Overhaul | INTEGRIERT | PR #9 | Merge-Commit 2f74fbb |
| Meshy Root Gate | INTEGRIERT | PR #9 und #13 | Visual und Material manuell bestanden |
| External Asset Pipeline | INTEGRIERT | PR #8 | Merge-Commit 68b0ee2 |
| PR #3 UI Polish | INTEGRIERT | geschlossen, nicht gemergt | durch PR #4/`developer` ueberholt |
| PR #5 Developer | REVIEW | developer nach main | nach Gate 0 neu bewerten; kein automatischer Merge |
| Produktionsbibel | INTEGRIERT | PR #10 | Merge-Commit 65e84e2 |
| Root-Gate-Materialkorrektur | INTEGRIERT | PR #13 | Merge-Commit a8b96ca; manuell bestanden |

## Priorisierte Ausfuehrungsreihenfolge — erste zehn Aufgaben

### 1. DOC-001 — Produktions- und Wissensstruktur

- Status: **INTEGRIERT** über PR #10.
- Branch: `feature/project-production-bible`
- Scope: acht verbindliche Dateien: `AGENTS.md` und sieben Fachdokumente.
- Abschluss: Links, Widersprueche, `git diff --check`, Commit und PR gegen
  `developer`.

### 2. P0-QS-001 — Quickslot-PR #7 technisch pruefen

- Status: **INTEGRIERT** über PR #7.
- Automatische Tastatur- und Runtime-Prüfungen bestanden.
- Manuell bestanden: Quickslot-Direktwahl 1–8 und Benutzung mit R.
- Controller-Prüfung bleibt als nicht blockierender Phase-1-Test offen.

### 3. P0-QS-002 — Quickslot Input integrieren

- Status: **INTEGRIERT** über PR #7, Merge-Commit c9cf76f.
- Spätere Gate-0-Korrekturen wurden über PR #14 bis #16 integriert.

### 4. P0-WORLD-001 — World-Branch auf aktuellen developer bringen

- Status: **INTEGRIERT** über PR #9.
- HUD-, Quickslot- und Gameplay-Roots wurden bei der Integration erhalten.

### 5. P0-WORLD-002 — World Overhaul und Root Gate validieren

- Status: **INTEGRIERT**; automatische und manuelle Abnahme dokumentiert.
- Pruefen: drei Szenen, Builder-Idempotenz, Root-Gate-Import, Materialien,
  Scale/Pivot, Collider/LOD, Portale, Spawns, `[Handarbeit]`, Missing Scripts.
- Manuell: dokumentierter Fuenf-Minuten-Rundgang pro Region.

### 6. P0-WORLD-003 — World Overhaul integrieren

- Status: **INTEGRIERT** über PR #9, Merge-Commit 2f74fbb.
- Ziel: konfliktfreier PR gegen `developer`; keine Regression von HUD,
  Quickslots oder Gameplay-Roots.

### 7. P0-ASSET-001 — External Asset Pipeline abschliessen

- Status: **INTEGRIERT** über PR #8, Merge-Commit 68b0ee2.
- Lizenzregeln, Quellenallowlist und sichere Downloadgrenzen sind dokumentiert.

### 8. P0-REMOTE-001 — veralteten PR #3 bereinigen

- Status: `INTEGRIERT`.
- PR #3 ist geschlossen; sein Inhalt ist ueber PR #4 in `developer` enthalten.

### 9. P0-REMOTE-002 — PR #5 bis Gate 0 zurueckstellen

- Status: **REVIEW**; nach Gate 0 neu zu bewerten.
- Kein Merge nach `main`; nach Gate 0 neuen Gesamtstand und Checks bewerten.

### 10. P0-QA-001 — integrierter Unity-Gesamttest

- Status: **ABGESCHLOSSEN**; Belege im Gate-0-Bericht.
- Frischer `developer`-Worktree, Batch-Kompilierung, Validatoren,
  `git diff --check`, Missing-Script-Pruefung und manueller Regionen-/Systemtest.

### Gate-0 Input- und Portal-Korrekturen — INTEGRIERT

- PR #14: Tastaturinteraktion und Quickslot-Bindings, Merge-Commit 00332cb.
- PR #15: Runtime-HUD- und Interaktionshinweis, Merge-Commit 7699626.
- PR #16: zuverlässige Quickslot-Direktwahl, Merge-Commit 9c8c613.
- PR #17: interaktive Regionsportale, Merge-Commit 8e032af.
- Automatisch bestanden: Unity-Batch-Kompilierung, Gate-0-Input-/HUD-Validator,
  World- und Portal-Validator, Play-Mode-Quickslot-Test und git diff --check.
- Manuell bestanden: E, Interaktionshinweis, Quickslots 1–8, Nutzung mit R,
  HUD, Kampfprototyp, Memory Site, Root Gate, Portalreisen und Rundgänge durch
  Finsterwald, Sonnenfelder und Nebelmoor.
- Controller-Bindings bleiben als nicht blockierender Phase-1-Test offen.

## GATE 0 — ABGESCHLOSSEN

Status: **ABGESCHLOSSEN**

Freigabe: ausdrückliche Nutzerentscheidung vom 2. August 2026.

Dokumentationspaket:

- Branch: `feature/gate0-final-report`
- Abschlusscommit: `7f3678a`
- Draft-PR: #18 gegen `developer`

Belege:

- Technischer Integrationsstand liegt konfliktfrei auf developer.
- Vorgeschriebene automatische Prüfungen wurden erfolgreich abgeschlossen.
- Input, HUD, Quickslots, Kampfprototyp, Memory Site, Root Gate, Portale und
  alle drei Regionen wurden manuell geprüft.
- Der vollständige Zwischen- und Abschlussstand ist in
  `GATE_0_MANUAL_TEST_REPORT.md` dokumentiert.

Nicht blockierende Phase-1-Prioritäten:

1. Ausdauerverbrauch an Sprint, Sprung beziehungsweise weitere freigegebene
   Gameplay-Aktionen anbinden.
2. Controller-Bindings manuell vollständig prüfen.
3. Portalpunkte sichtbar machen, Trigger freistellen und Spielerführung
   verbessern.
4. Nebelmoor-Nebeldichte für bessere Lesbarkeit abstimmen.

## Phase 1 — aktives Arbeitspaket

### P1.2 — Movement, Kamera und Spielgefuehl

- Status: `REVIEW`
- Branch: `feature/movement-camera`
- Scope: Beschleunigung, Abbremsung, Sprint-Zuverlaessigkeit,
  framerate-stabile Drehung, Kamera-Fokus und Kamera-Occlusion.
- Bewahrt: vorhandene Animationen, zentrale Input-Struktur, Rolle und Sprung.
- Ausgeschlossen: Ausdauerverbrauch, Gegner, Lore, Memory-Watch-Raetsel,
  Portalumbau und neue Regionen.
- Automatisch bestanden: Unity-Batch-Kompilierung, Movement-/Kamera-Validator,
  World-Validator, zwei vorhandene Play-Mode-Tests und `git diff --check`.
- Manuell offen: Maus/Tastatur und Controller in engen Finsterwald-Passagen,
  Sprintwechsel, Kameraorbit, Zoom und Occlusion an Baum-/Felsgruppen.
## Phase 1 und spaeter

Finsterwald Vertical Slice ist nach Abschluss von Gate 0 **FREIGEGEBEN**,
wurde jedoch noch nicht begonnen.
Sonnenfelder, Nebelmoor, Tal der verlorenen Wege und Ruinen von Arvenfall sind
nur geplant und duerfen nicht automatisch begonnen werden. Details stehen in
`DEVELOPMENT_PLAN.md`, `WORLD_ROADMAP.md` und `GAMEPLAY_ROADMAP.md`.
