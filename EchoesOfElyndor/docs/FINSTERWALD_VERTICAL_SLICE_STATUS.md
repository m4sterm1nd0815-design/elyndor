# Finsterwald Vertical Slice – aktueller Stand

Stand: 15. August 2026
Basis: `origin/developer` bei `b5536ee` (nach PR #47)

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
| **P1.2** Vitals und Ausdauer | **umgesetzt** | Sprint, Rolle (20), leichter (8) und schwerer Angriff (18), Block (10/s). Keine Teilzahlung; Block bricht bei Erschöpfung zusammen |
| **P1.3** Kampfbasis | **umgesetzt** | `PlayerCombat` mit leichtem/schwerem Angriff und Block über die Action-Schicht |
| **P1.4** Gegner-KI-Grundlage | **umgesetzt** | `EnemyController`, `EnemyPerception`, `EnemyStateMachine`, `EnemyStateRules`, `EnemyMovement`, `EnemyAttack`, `EnemyHealth`, `EnemyHitReaction`; Validator grün |
| **P1.5** Wurzelstreifer | **umgesetzt** | Ein Exemplar auf der Lichtung. 0,7 s Telegraph, 1,2 s Gegenfenster, 40 LP, 10 Schaden, einmaliger Rückzug unter 30 %, 12 m Ortsbindung, Lebensanzeige. Blockout-Modell; finale Kunst offen |
| **P1.6** Echohüter und Namenloser Hüter | **offen** | — |
| **P1.7** Memory-Watch-Rätsel | **umgesetzt** | Die geteilte Brücke ist von Anfang bis Ende spielbar. Drei Anker, Seilbock, Stammfreigabe, zwei Bohlen, Furt mit schadensloser Rücksetzung. Kerben seit dem Lesbarkeitsdurchgang zählbar |
| **P1.8** Link-Begleiter | **umgesetzt** | `LinkCompanion` und `LinkPerch`, fünf Sitzpunkte. Kein Collider, keine Sprache, keine Lösungsanzeige. Primitiv-Blockout; finale Kunst offen |
| **P1.9** Lore und Narration | **umgesetzt** | `NarrationCatalog` mit freigegebenen Kurztexten, Gravur (zweimal), Rastplatz, Bachsteine, Brückenrest, Erinnerungsfragment und die Stimme ohne Namen. SOREN und ELIAN kommen nicht vor; `NarrationCanonTests` prüft das |
| **P1.10** Regionsregeneration | **umgesetzt** | `FinsterwaldRegeneration` antwortet erst, wenn Rätsel **und** Erinnerung erledigt sind. Idempotent über `RegionRegenerationState`; Wiederherstellung löst kein Ereignis aus. Kein Save-System, aber `IRegionStateStore` als Vertrag für P1.13. **Bewusst offen:** welcher Weg sich öffnet (`blockedPath` ist nicht verdrahtet) — das ist eine Leveldesign-Entscheidung |
| **P1.11** Audio und VFX | **teilweise** | **P1.11A umgesetzt:** Telegraph, Treffer, Block, Niederlage und die beiden Spannungstöne des Rätsels laufen über die vorhandene `SfxLibrary`. **Offen:** Hörprobe durch einen Menschen (`AUDIO_AUDITION.md`), Ambience, türkise Bruchlinien am finalen Modell |
| **P1.12** Level- und Art-Polishing | **teilweise** | World Visual Overhaul und Startbereich-Führung integriert |
| **P1.13** Save und Checkpoints | **offen** | kein Save-System; Save-Bereiche sind in `ARCHITECTURE.md` nur als Vertrag vorgemerkt |
| **P1.14** Integrationstest | **umgesetzt** | `FinsterwaldSliceIntegrationRuntimeTests`: zehn Tests fahren den Kernbogen im echten Finsterwald — Begegnung, Resonanzzone, Erinnerung, Rätsel, Antwort des Waldes. Kampf über simulierte Geräte, jede Interaktion über den `InteractionDetector`. Belegt Reihenfolge, Softlock-Freiheit, Umkehrbarkeit und die Doppelbedingung der Regeneration |

## Nächste sinnvolle Schritte

Der Kernbogen steht jetzt vollständig: Bewegung → Begegnung → Memory Watch →
Rätsel → Antwort der Welt. Was fehlt, ist nicht mehr Mechanik, sondern Urteil.

Seit P1.14 läuft der Bogen auch als Test durch, nicht nur von Hand.

1. **Menschliche Abnahme** — `FINSTERWALD_HUMAN_ACCEPTANCE.md` und
   `AUDIO_AUDITION.md`. Der Slice ist an mehreren Stellen bewusst nicht
   entschieden, weil nur ein Mensch entscheiden kann: ob das Rätsel hergeleitet
   oder geraten wird, ob die Töne tragen, ob die Regeneration leise genug ist.
   **Das ist der nächste Schritt** — alles Weitere baut auf Antworten, die
   noch fehlen.
2. **Wegöffnung der Regeneration** — welcher Weg sich öffnet, ist eine
   Leveldesign-Entscheidung nach der Abnahme. `blockedPath` bleibt bis dahin
   unverdrahtet.
3. Erst danach **P1.6** (weitere Gegnertypen), **P1.12** (Art) und **P1.13**
   (Save).

**Keine erledigte Arbeit erneut bauen:** P1.2, P1.5, P1.7, P1.8, P1.9, P1.10,
P1.11A und P1.14 sind umgesetzt. Ältere Planungsdokumente, die sie als offen
führen, sind in diesem Punkt überholt.

## Offene Punkte aus der Planung, die weiterhin gelten

- `OFFEN`: Zielplattform, FPS-Ziel, Tod/Respawn und finale Heilungswirtschaft.
- `OFFEN`: Startressourcen für HUD, Inventar und Quickslots (Balancing-Paket).
- `ENTSCHIEDEN` (15.08.2026): Link ist eine Eule — ohne festgelegtes
  Geschlecht, ohne erklärte Herkunft, ohne menschliche Sprache. Der Slice nutzt
  nur Flug, Blick, Sitzposition und kurze Rufe; so ist es umgesetzt.
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
