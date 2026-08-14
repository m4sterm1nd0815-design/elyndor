# Finsterwald Vertical Slice – aktueller Stand

Stand: 14. August 2026
Basis: `origin/developer` bei `1beb257`

Dieses Dokument gleicht die Planung vom 2. August 2026 gegen den heutigen Code
ab. **Es geht den älteren Planungsdokumenten vor**, wo sie sich widersprechen.

Der Entwurf selbst hat sich gehalten: Ablauf, Encounter-Rollen, Rätselaufbau,
Lore-Dramaturgie und Assetbedarf sind weiterhin gültig. Überholt waren nur die
technischen Annahmen und die Gate-0-Blockade.

## Was sich seit dem 2. August geändert hat

Gate 0 ist abgeschlossen. Zusätzlich wurde seither integriert, was in der
Planung noch als offen galt:

- **Eingabe vereinheitlicht.** `PlayerInputReader` ist die einzige Stelle im
  Projekt, die Geräte liest. Attack, Block, Interact, Inventory und Roll liegen
  als eigene Actions vor; `Interact` und `Inventory` teilen sich am Controller
  keine Taste mehr.
- **Erlebnisschicht getrennt.** `ElyndorExperienceUI` (sichtbar) und
  `ElyndorExperience` (nicht-visuell) sind in allen drei Regionen getrennt. Ein
  abgeschaltetes UI-Panel kann den Ton nicht mehr mitnehmen.
- **Gegner-Grundlage integriert.** Zustandsmaschine, Wahrnehmung, Bewegung,
  Angriff, Leben und Trefferreaktion existieren als eigene Komponenten.
- **Szenenintegrität projektweit.** Finsterwald, Sonnenfelder, Nebelmoor und
  Bootstrap werden gegen je ein eigenes Profil geprüft.
- **Compiler-Warnungen auf 0.**
- **Kanon konsolidiert.** Der Erzählkanon liegt in `02_Story/StoryBible.md`;
  `LORE_BIBLE.md` und `GAME_BIBLE.md` sind Weiterleitungen.

## Stand je Arbeitspaket

| Paket | Stand | Beleg |
|---|---|---|
| **P1.1** Movement und Kamera | **umgesetzt** | `PlayerMovement`, `CameraFollow`; Eingabezentralisierung mit der Input-Vereinheitlichung abgeschlossen |
| **P1.2** Vitals und Ausdauer | **teilweise** | `PlayerVitals` vorhanden, Sprintverbrauch nachweislich aktiv. **Offen:** Rolle, Block und Angriff verbrauchen noch keine Ausdauer |
| **P1.3** Kampfbasis | **umgesetzt** | `PlayerCombat` mit leichtem/schwerem Angriff und Block über die Action-Schicht |
| **P1.4** Gegner-KI-Grundlage | **umgesetzt** | `EnemyController`, `EnemyPerception`, `EnemyStateMachine`, `EnemyStateRules`, `EnemyMovement`, `EnemyAttack`, `EnemyHealth`, `EnemyHitReaction`; Validator grün |
| **P1.5** Wurzelstreifer | **offen** | nur `TrainingDummy` vorhanden |
| **P1.6** Echohüter und Namenloser Hüter | **offen** | — |
| **P1.7** Memory-Watch-Rätsel | **teilweise** | `MemorySite`, `MemoryEcho`, `MemoryVisionEffect`, `MemoryWatchActivationUI` vorhanden und im Play Mode verifiziert. **Offen:** das Ankerrätsel an der Brücke |
| **P1.8** Link-Begleiter | **offen** | keine Komponente vorhanden |
| **P1.9** Lore und Narration | **teilweise** | `NarrationUI` und `ExaminableObject` funktionieren. **Offen:** die Slice-Texte und Memory-Fragmente |
| **P1.10** Regionsregeneration | **offen** | keine Region-State-Komponente vorhanden |
| **P1.11** Audio und VFX | **teilweise** | `SfxLibrary` mit zugewiesenen Clips, Schritte nachweislich hörbar; `MemoryVisionEffect` vorhanden. **Offen:** slice-spezifisches Feedback |
| **P1.12** Level- und Art-Polishing | **teilweise** | World Visual Overhaul und Startbereich-Führung integriert |
| **P1.13** Save und Checkpoints | **offen** | kein Save-System; Save-Bereiche sind in `ARCHITECTURE.md` nur als Vertrag vorgemerkt |
| **P1.14** Integrationstest | **offen** | — |

## Nächste sinnvolle Schritte

Die ursprüngliche Abhängigkeitsfolge `P1.1 → P1.2 → P1.3 → P1.4 → P1.5` ist an
den Stellen P1.1, P1.3 und P1.4 bereits erfüllt. Damit sind zwei Pakete
unmittelbar arbeitsfähig:

1. **P1.2 abschließen** — Rolle, Block und Angriff an die Ausdauer anbinden.
   Klein, klar abgegrenzt, und es schließt die letzte Lücke in der
   Spielerbasis. `PlayerVitals.TrySpendStamina` existiert bereits.
2. **P1.5 Wurzelstreifer** — der erste echte Gegnertyp auf der vorhandenen
   Gegner-Grundlage. Ab hier entsteht spielbarer Inhalt statt Infrastruktur.

Danach greift die Reihenfolge des Planungsdokuments unverändert weiter.

## Offene Punkte aus der Planung, die weiterhin gelten

- `OFFEN`: Zielplattform, FPS-Ziel, Tod/Respawn und finale Heilungswirtschaft.
- `OFFEN`: Startressourcen für HUD, Inventar und Quickslots (Balancing-Paket).
- `OFFEN`: Links Kommunikationsform, Geschlecht und Herkunft — der Slice nutzt
  deshalb nur Flug, Blick, Sitzposition und kurze Rufe.
- `VORSCHLAG`-Markierungen in Encounter-, Rätsel- und Lore-Plan bleiben
  Vorschläge, bis ein Arbeitspaket sie ausdrücklich freigibt.

## Nicht übernommen

Der Planungsstand vom 2. August enthielt zusätzlich eine vollständige
Neufassung von `TASK_QUEUE.md`. Sie wurde **bewusst nicht übernommen**: Sie
stammt von vor Gate 0 und würde jeden seither als `INTEGRIERT` verbuchten
Eintrag zurückdrehen. Die aktuelle `TASK_QUEUE.md` bleibt maßgeblich.

## Verwandte Dokumente

- `FINSTERWALD_VERTICAL_SLICE.md` — Ablauf, Start- und Endzustand, Taktung
- `FINSTERWALD_TECHNICAL_WORK_PACKAGES.md` — Paketzuschnitt und Abnahmekriterien
- `FINSTERWALD_ENCOUNTER_PLAN.md` — Gegnerrollen und Werte
- `FINSTERWALD_MEMORY_PUZZLE.md` — Aufbau des Brückenrätsels
- `FINSTERWALD_LORE_FLOW.md` — Informationsdramaturgie
- `FINSTERWALD_ASSET_REQUIREMENTS.md` — Assetbedarf und Beschaffungsregeln
