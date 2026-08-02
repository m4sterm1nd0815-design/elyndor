# Task Queue

Stand: 2. August 2026
Basis der Bestandsaufnahme: `origin/developer` bei `3a35bfc`

## Statusregeln

- `FREIGEGEBEN`: darf in Reihenfolge bearbeitet werden.
- `IN ARBEIT`: laufendes, isoliertes Paket; nicht duplizieren.
- `REVIEW`: implementiert oder geplant, Integration/Abnahme offen.
- `INTEGRIERT`: in `developer` enthalten.
- `BLOCKIERT`: AbhÃ¤ngigkeit oder Entscheidung fehlt.
- `BLOCKIERT BIS GATE-0-FREIGABE`: ausdrÃ¼cklich keine Implementierung beginnen.
- `GEPLANT`: keine automatische Arbeitsfreigabe.
- `GATE â€” MANUELL OFFEN`: technische Belege vorhanden, manuelle Abnahme und ausdrÃ¼ckliche Freigabe fehlen.

## Aktueller realer Stand

| Bereich | Status | Branch/PR | Beleg/Notiz |
|---|---|---|---|
| Produktions- und Wissensstruktur | INTEGRIERT | PR #10 | Merge-Commit `2756795` |
| Quickslot Input | INTEGRIERT | PR #7 | Merge-Commit `c9cbc4d` |
| World Visual Overhaul und Root Gate | INTEGRIERT | PR #9 | Merge-Commit `2f3abea` |
| External Asset Pipeline | INTEGRIERT | PR #8 | Merge-Commit `68c79bf`; Such-/Dry-Run-Pipeline, keine Downloads |
| Gate-0 HUD-/Szenenintegration | INTEGRIERT | PR #11 | Merge-Commit `3a35bfc` |
| Gate-0 automatische PrÃ¼fung | INTEGRIERT | `developer` | Batch-Kompilierung und vorhandene Validatoren technisch bestanden |
| Gate-0 manuelle Unity-Abnahme | GATE â€” MANUELL OFFEN | kein Implementierungsbranch | Play-Mode-PrÃ¼fungen stehen aus |
| PR #5 Developer â†’ Main | BLOCKIERT | PR #5 | nicht mergen; Status vor spÃ¤terer Freigabe erneut prÃ¼fen |
| Finsterwald Vertical-Slice-Plan | REVIEW | `feature/finsterwald-vertical-slice-plan`, PR #12 | reiner Dokumentations-PR gegen `developer`; Commit ist der aktuelle PR-Head |

## Gate 0 â€” nicht Ã¼berschreiten

Status: **GATE â€” MANUELL OFFEN**

Technisch vorbereitet und bereits belegt:

- Grundlagen konfliktfrei in `developer` integriert;
- Unity-Batch-Kompilierung erfolgreich;
- vorhandene automatische Validatoren erfolgreich;
- Missing-Script- und zentrale KomponentenprÃ¼fungen ohne bekannten Blocker;
- Asset-Pipeline blieb im Such-/Dry-Run-Modus.

Manuell in Unity noch abzunehmen:

- Input mit Tastatur/Maus und Gamepad;
- HUD, Vitals, Inventory und Quickslots im Play Mode;
- Kampfprototyp und AusrÃ¼sten der vorhandenen Waffe;
- erste Memory Site und Watch-Aktivierungsfluss;
- Portale, Spawns und ÃœbergÃ¤nge der drei Regionen;
- Rundgang durch Finsterwald, Sonnenfelder und Nebelmoor;
- visuelle PrÃ¼fung des Root Gate sowie fehlender Scripts und doppelter zentraler Komponenten im Inspector.

Gate 0 darf erst nach dokumentierter manueller Abnahme und ausdrÃ¼cklicher Freigabe geschlossen werden. Bis dahin sind Ã„nderungen an Spielszenen und bestehenden Gameplay-Systemen fÃ¼r Phase 1 gesperrt.

## Phase 1 â€” Finsterwald Vertical Slice

Planungsgrundlagen:

- `FINSTERWALD_VERTICAL_SLICE.md`
- `FINSTERWALD_ENCOUNTER_PLAN.md`
- `FINSTERWALD_MEMORY_PUZZLE.md`
- `FINSTERWALD_LORE_FLOW.md`
- `FINSTERWALD_ASSET_REQUIREMENTS.md`
- `FINSTERWALD_TECHNICAL_WORK_PACKAGES.md`

Keines der folgenden Pakete ist begonnen oder automatisch freigegeben.

| ID | Arbeitspaket | Branch nach Freigabe | Status | AbhÃ¤ngigkeit |
|---|---|---|---|---|
| P1.1 | Movement und Kamera | `feature/finsterwald-movement-camera` | BLOCKIERT BIS GATE-0-FREIGABE | Gate 0 |
| P1.2 | Vitals-/Ausdaueranbindung | `feature/finsterwald-vitals-stamina` | BLOCKIERT BIS GATE-0-FREIGABE | P1.1 |
| P1.3 | Kampfbasis | `feature/finsterwald-combat-foundation` | BLOCKIERT BIS GATE-0-FREIGABE | P1.1, P1.2 |
| P1.4 | Gegner-KI-Grundlage | `feature/finsterwald-enemy-ai` | BLOCKIERT BIS GATE-0-FREIGABE | P1.3 |
| P1.5 | Erster Gegnertyp â€“ Wurzelstreifer | `feature/finsterwald-root-strider` | BLOCKIERT BIS GATE-0-FREIGABE | P1.4 |
| P1.6 | Encounter-Erweiterung und Namenloser HÃ¼ter | `feature/finsterwald-encounter-roster` | BLOCKIERT BIS GATE-0-FREIGABE | P1.5 |
| P1.7 | Memory-Watch-RÃ¤tsel | `feature/finsterwald-memory-bridge-puzzle` | BLOCKIERT BIS GATE-0-FREIGABE | P1.1 |
| P1.8 | Link-Begleiter | `feature/finsterwald-link-companion` | BLOCKIERT BIS GATE-0-FREIGABE | P1.1, Lore-Freigabe |
| P1.9 | Lore-/Narrationsintegration | `feature/finsterwald-lore-flow` | BLOCKIERT BIS GATE-0-FREIGABE | P1.7, P1.8, Lore-Freigabe |
| P1.10 | Regionsregeneration | `feature/finsterwald-region-regeneration` | BLOCKIERT BIS GATE-0-FREIGABE | P1.7, P1.9 |
| P1.11 | Audio und VFX | `feature/finsterwald-audio-vfx` | BLOCKIERT BIS GATE-0-FREIGABE | P1.5â€“P1.10, Assetfreigabe |
| P1.12 | Level- und Art-Polishing | `feature/finsterwald-level-art-polish` | BLOCKIERT BIS GATE-0-FREIGABE | P1.6â€“P1.11 |
| P1.13 | Save-/Checkpoint-Grundlage | `feature/finsterwald-checkpoints` | BLOCKIERT BIS GATE-0-FREIGABE | P1.7, P1.10, P1.12 |
| P1.14 | Integrationstest und Slice-Abnahme | `feature/finsterwald-vertical-slice-integration` | BLOCKIERT BIS GATE-0-FREIGABE | P1.1â€“P1.13 |

## Phase 2 und spÃ¤ter

Sonnenfelder, Nebelmoor, Tal der verlorenen Wege und Ruinen von Arvenfall bleiben ausschlieÃŸlich geplant. Details stehen in `DEVELOPMENT_PLAN.md`, `WORLD_ROADMAP.md` und `GAMEPLAY_ROADMAP.md`. Keine dieser Arbeiten wird vor den jeweils vorgesehenen Gates automatisch begonnen.
