# Development Plan

Stand: 1. August 2026
Ziel: kontrollierter Weg zum Finsterwald Vertical Slice

## Arbeitsmodell

- `main` ist stabil, `developer` ist Integrationsbasis.
- Ein Arbeitspaket besitzt einen Feature-Branch und einen PR gegen `developer`.
- `TASK_QUEUE.md` bestimmt Freigabe und Reihenfolge.
- Kleine, pruefbare Pakete; keine Vermischung paralleler Arbeiten.
- Technische Pakete enden mit automatischen Pruefungen und klar markierten
  manuellen Tests.
- Ein Gate ist eine bewusste Freigabeentscheidung, kein automatisch
  ueberspringbarer Task.

## Paketstandard

Jedes Paket beschreibt:

- Ziel und Nicht-Ziele;
- Basisbranch und erlaubte Dateien;
- Abhaengigkeiten;
- Akzeptanzkriterien;
- automatische und manuelle Tests;
- Branch, Commit und PR;
- bekannte Risiken und Rueckbauweg.

## Phase 0 — Repository und offene Arbeit bereinigen

### P0.1 Produktionsstruktur

- Diese acht verbindlichen Dokumente und `AGENTS.md` erstellen.
- Links, Kanon, Status und Ausfuehrungsreihenfolge validieren.
- Abhaengigkeit: keine; Basis `origin/developer`.

### P0.2 Quickslot Input integrieren

- Draft-PR #7 pruefen, Unity kompilieren und Controller/Tastatur testen.
- Nach erfolgreicher Review in `developer` integrieren.
- Abhaengigkeit: HUD-Binding PR #6, bereits integriert.

### P0.3 World Visual Overhaul und Root Gate integrieren

- Draft-PR #9 nach Integration von PR #7 erneut auf aktuellen `developer`
  bringen.
- HUD-/Quickslot-Systeme bei jeder Aktualisierung erhalten.
- Builder, drei Szenen, Root Gate, Reviewbelege und manuelle Rundgaenge pruefen.
- Abhaengigkeit: P0.2, damit der Rebase den finalen UI-Stand kennt.

### P0.4 External Asset Pipeline abschliessen

- Draft-PR #8 und den sauberen isolierten Worktree separat pruefen und auf
  aktuellen `developer` bringen.
- Lizenzmanifest, Tests und Sicherheitsgrenzen mit `ASSET_PIPELINE.md`
  vereinheitlichen.
- Keine Dateien ungeprueft aus dem Worktree uebernehmen.

### P0.5 Remote-Hygiene

- PR #3 ist als durch PR #4/`developer` ueberholt geschlossen.
- PR #5 erst nach Gate-0-Abnahme fuer eine Promotion nach `main` neu bewerten.
- Veraltete Remote-Branches nur nach belegter Integration entfernen.

### P0.6 Integrationsabnahme

- Frischen Integrationsstand von `developer` bauen.
- Finsterwald, Sonnenfelder und Nebelmoor laden und Portalreise pruefen.
- HUD, Quickslots, Input, Kampfprototyp und Memory Site manuell testen.
- Fehlende Scripts, Console-Fehler und Scope-Regressionen pruefen.

## Gate 0 — technische Grundlage

Stop und ausdrueckliche Freigabe erforderlich.

Kriterien:

- Arbeitskopien sauber oder klar dokumentiert und isoliert;
- `developer` enthaelt HUD, Quickslot Input, World Overhaul/Root Gate und
  freigegebene Asset-Pipeline-Grundlage konfliktfrei;
- alle technischen Pakete kompilieren und Validatoren bestehen;
- manueller Unity-Test der integrierten Systeme dokumentiert;
- keine veralteten PRs mit missverstaendlichem Integrationspfad;
- keine ungeprueften Lizenzen oder Secrets.

## Phase 1 — Finsterwald Vertical Slice

Erst nach Gate-0-Freigabe.

| Paket | Inhalt | Abhaengigkeit | Qualitaetskriterium |
|---|---|---|---|
| P1.1 | Route und 30–45-Minuten-Flow | Gate 0 | kompletter Greybox-Durchlauf |
| P1.2 | Movement/Kamera/Spielgefuehl | P1.1 | Controller + M/T, Terrain, Framerate |
| P1.3 | echte Ausdaueranbindung | P1.2 | Sprint/Rolle/Kampf teilen eine Ressource |
| P1.4 | Kampfgrundlage und erste Waffe | P1.3 | lesbare Inputs, Treffer und Block |
| P1.5 | erster Gegnertyp | P1.4 | Verhalten, Telegraphing, Reaktion, Niederlage |
| P1.6 | Watch-Signal und erstes Raetsel | P1.1 | Spur statt Loesung, zugaengliches Signal |
| P1.7 | Memory Site und Lore-Einstieg | P1.6 | Handlung vor Erklaertext |
| P1.8 | Link-Integration | P1.1/P1.7 | Begleitung ohne Hinweis-Spam |
| P1.9 | sichtbare Regeneration | P1.6/P1.7 | persistente, erkennbare Veraenderung |
| P1.10 | Audio-/Visual-/Performance-Polish | alle | Zielhardware-Profil und visuelle Abnahme |
| P1.11 | finaler Slice-Playthrough | alle | 30–45 Minuten ohne Editor-Eingriff |

## Gate 1 — Vertical-Slice-Abnahme

Stop und ausdrueckliche Freigabe erforderlich.

- Vollstaendiger manueller Playthrough mit Controller und Maus/Tastatur.
- Visuelle Abnahme der Kernroute und schwieriger Lichtsituationen.
- Performancepruefung auf festgelegter Zielhardware.
- Keine blockierenden Fehler, fehlenden Scripts oder Lizenzluecken.
- Entscheidung: Sonnenfelder, Nebelmoor oder erneute Finsterwald-Iteration.

## Nach Gate 1 — nur Planung

Sonnenfelder, Nebelmoor, Tal der verlorenen Wege und Ruinen von Arvenfall sind
nicht zur automatischen Abarbeitung freigegeben. Produktionsstart benoetigt eine
ausdrueckliche Gate-1-Entscheidung.
