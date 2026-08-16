# Task Queue

Stand: 16. August 2026
Basis der Bestandsaufnahme: `origin/developer` bei `b5536ee`

Die Tabelle war bis zum 15.08.2026 bei PR #37 stehengeblieben, während die
Pakete P1.2 bis P1.10 bereits gemergt waren. Die Zeilen bis #47 sind
nachgetragen; maßgeblich für den Paketstand bleibt
`FINSTERWALD_VERTICAL_SLICE_STATUS.md`.

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
| Finsterwald Runtime Recovery | INTEGRIERT | PR #32 | Merge-Commit 6a1548d; interaktive Unity-Abnahme bestanden |
| Region Experience Hardening | INTEGRIERT | PR #33 | Merge-Commit 0e40120 |
| Input Unification | INTEGRIERT | PR #34 | Merge-Commit cf615ee |
| Zero Warning / QA Hygiene | INTEGRIERT | PR #35 | Merge-Commit 372d380 |
| Project Bible / Canon Consolidation | INTEGRIERT | PR #36 | Merge-Commit 1beb257 |
| Finsterwald Vertical Slice Plan | INTEGRIERT | PR #37 | Merge-Commit b7281b0 |
| P1.2 Ausdauerbindung | INTEGRIERT | PR #38 | Merge-Commit 14dba67; Rolle, Angriff und Block kosten Ausdauer |
| P1.5 Wurzelstreifer | INTEGRIERT | PR #39 | Merge-Commit 07d1e07; ein Exemplar auf der Lichtung, Blockout-Modell |
| P1.7 Brueckenraetsel | INTEGRIERT | PR #40 | Merge-Commit 098214d; Furt setzt schadenslos zurueck |
| P1.11A Audio-Lesbarkeit | INTEGRIERT | PR #41 | Merge-Commit e343153; kein Ton verraet, welcher Anker falsch steht |
| Gegner-Lebensanzeige | INTEGRIERT | PR #42 | Merge-Commit ef715fd; loest PR #30 ab, dieser wurde als ueberholt geschlossen |
| Brueckenlesbarkeit | INTEGRIERT | PR #43 | Merge-Commit 81357cb; Kerben zaehlbar |
| P1.8 Link-Begleiter | INTEGRIERT | PR #44 | Merge-Commit 6c83cca; kein Bezug auf Raetsel oder Anker im Code |
| P1.9 Lore und Narration | INTEGRIERT | PR #45 | Merge-Commit 831d643; SOREN und ELIAN kommen nicht vor |
| P1.10 Regionsregeneration | INTEGRIERT | PR #46 | Merge-Commit 8a4fa87; Wegoeffnung bewusst nicht verdrahtet |
| Menschliche Abnahmeanleitung | INTEGRIERT | PR #47 | Merge-Commit b5536ee; zwoelf Stationen, ohne Raetsellosung |
| QA-Haertung und Compiler-Gate | INTEGRIERT | PR #48 | Merge-Commit 40b3a53; Frames-statt-Sekunden behoben, Szenenprofil erweitert, Compiler-Gate mit Neubau-Nachweis |
| P1.14 Integrationstest | INTEGRIERT | PR #49 | Merge-Commit d5af354; Kernbogen als ein Ablauf |
| Human Vertical Slice Acceptance | ABGESCHLOSSEN | — | 15.08.2026, **PASS**. Dabei gefunden: Rolle reagierte nicht auf Strg. Station 8 fuer diesen Pruefer nicht mehr beurteilbar |
| Wurzelstreifer Art-Freigabe | ABGESCHLOSSEN | — | 16.08.2026, Game Director: „passt erstmal so". Kuenstlerische Freigabe der In-Game-Darstellung fuer den Vertical-Slice-Stand auf Merge-Commit `08beca9`; Detailgrad, Materialfarben und Tuerkiston brauchen dafuer keine Aenderung. Erinnerungsschliere, Audio, Texturstandard, LOD und der ungenutzte `Trab` bleiben offen |
| Rollen-Belegung und Gate-Haertung | REVIEW | PR #50 | Roll-Action auf Strg; Compiler-Gate fasst offene Editoren nicht mehr an |
| P1.13A Save/Persistence Foundation | REVIEW | `feature/save-persistence-foundation`, PR #51 | Fortschritt ueberlebt Programmstart; kein Selbst-Merge, Game-Director-Review-Punkt |
| Blender-Asset-Pipeline (Nachweis) | REVIEW | PR #51 | Kette Blender → Unity an `ELY_Test_Rock_A` gemessen; `ModelImportValidator` kennt genau dieses Asset |
| P1.12A Maschinenlesbare Asset-Herkunft | REVIEW | PR #51 | `<Assetpfad>.provenance.json` als Gate im `ModelImportValidator`; Scope nur `ELY_Test_Rock_A`, Altbestand bewusst ausgenommen |

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

## Phase 1 — Paketverlauf (historisch)

Diese Abschnitte halten fest, wie die Phase-1-Pakete abgenommen wurden. Sie
beschreiben **kein** laufendes Arbeitspaket. Was gerade offen ist, steht in
`FINSTERWALD_VERTICAL_SLICE_STATUS.md`.

### HISTORISCH — „P1.2 Movement, Kamera und Spielgefuehl"

> **Achtung, alte Nummerierung.** Dieser Abschnitt stammt aus der Zeit vor dem
> Slice-Plan und meint mit `P1.2` die Bewegung und Kamera. In der heute
> gueltigen Zaehlung ist das **P1.1**, und `P1.2` ist die Ausdauerbindung
> (PR #38). Der Abschnitt bleibt als Beleg der damaligen Abnahme stehen;
> maszgeblich fuer den Paketstand ist `FINSTERWALD_VERTICAL_SLICE_STATUS.md`.

- Status: `INTEGRIERT` über PR #19, Merge-Commit `3408ce5` (02.08.2026)
- Branch: `feature/movement-camera`
- Implementierungscommit: `4b12c57`
- PR: `#19` gegen `developer`
- Scope: Beschleunigung, Abbremsung, Sprint-Zuverlaessigkeit,
  framerate-stabile Drehung, Kamera-Fokus und Kamera-Occlusion.
- Bewahrt: vorhandene Animationen, zentrale Input-Struktur, Rolle und Sprung.
- Ausgeschlossen: Ausdauerverbrauch, Gegner, Lore, Memory-Watch-Raetsel,
  Portalumbau und neue Regionen.
- Automatisch bestanden: Unity-Batch-Kompilierung, Movement-/Kamera-Validator,
  World-Validator, zwei vorhandene Play-Mode-Tests und `git diff --check`.
- Manuell offen: Maus/Tastatur und Controller in engen Finsterwald-Passagen,
  Sprintwechsel, Kameraorbit, Zoom und Occlusion an Baum-/Felsgruppen.
### P1.3 — Finsterwald-Startbereich und Spielerfuehrung

- Status: `INTEGRIERT`
- Branch: `feature/finsterwald-start-guidance`
- PR #24, Merge-Commit `8471ebd`; manuelle Abnahme am 3. August 2026
  ausdruecklich bestaetigt.
- Scope: ausschliesslich der unmittelbar sichtbare Startbereich im
  Finsterwald (Korridor von rund 12 m hinter bis 18 m vor dem Startpunkt).
- Umgesetzt: 19 Baumkronen, ein Busch und eine grosse Bodenpflanze seitlich
  aus dem Startbild versetzt; vorhandenes `Startbereich/Wegschild` an den
  rechten Wegrand gestellt und zum Hauptweg gedreht; `Beschaedigter Rucksack`
  aus dem Blickzentrum nach links versetzt; vorhandene Kiesel und Felsen als
  Trittspur und Wegkante ausgelegt; Arens Startpose fest im Spieler-Transform
  hinterlegt.
- Bewahrt: `PlayerMovement`, `CameraFollow`, Portalsystem, Memory-System,
  Gegner, Input und HUD sind unveraendert.
- Ausgeschlossen: Questpfeile, Marker, neue UI-Hinweise, neue externe Assets,
  andere Regionen und eine Neugestaltung des uebrigen Finsterwalds.
- Werkzeug: `Elyndor/Finsterwald/Startbereich - Sichtfuehrung aufraeumen`
  (`FinsterwaldStartGuidanceBuilder`), idempotent; Korridorursprung ist Arens
  Startpose in der Szene.
- Review-Bereinigung (siehe unten): `PlayerStartSetup`, der Hilfsmarker
  `PlayerStart` und die `Lichtschneise` wurden wieder entfernt. Der Szenen-Diff
  gegen `developer` legt damit kein einziges Objekt mehr an und aendert nur
  33 Transformationen, alle innerhalb von 40 m um den Startpunkt.
- Automatisch bestanden: Unity-Batch-Kompilierung ohne Fehler, World-Validator,
  Movement-/Kamera-Validator, Regionsportal-Validator, Gate-0-Input-Validator,
  zehn Play-Mode-Tests inklusive `FinsterwaldStartGuidanceRuntimeTests` und
  `RegionTravelSpawnRuntimeTests` (zwei Laeufe stabil),
  Pruefung auf fehlende Scripts (0),
  Idempotenzprobe des Builders und `git diff --check` ohne Befund.
- Manuell offen: Rundgang ab Start bis zum Rastbereich, Kameraorbit und Zoom
  im geoeffneten Korridor, Aufnahme des Rucksacks, Untersuchen des Wegschilds
  und Reise durch das Root Gate zu den Sonnenfeldern.
- Nachlauf: der beim manuellen Rundgang von Hand gebaute `TestEnemy` ist
  bewusst nicht Teil dieses Pakets. Eine Gegnerplatzierung im Finsterwald
  bleibt einem eigenen Arbeitspaket vorbehalten.

#### Review von PR #24 — Befunde

- `PlayerStartSetup` erfuellte keine notwendige Laufzeitfunktion: die Klasse
  hat nur dupliziert, was der Spieler-Transform der Szene selbst ausdrueckt.
  `AlignCamera()` war wirkungslos, weil `CameraFollow.LateUpdate` Position und
  Blickrichtung der Kamera in jedem Frame neu setzt; `flatForward` wurde
  berechnet und nie benutzt; `lookTarget` war in der Szene nicht belegt.
  Zusaetzlich hat die Klasse `RegionSpawnPoint` bedingungslos ueberschrieben —
  bei der Rueckkehr aus den Sonnenfeldern entschied allein die undefinierte
  `Start()`-Reihenfolge darueber, ob Aren am Portal oder am Waldstart landet
  (es ist keine Ausfuehrungsreihenfolge konfiguriert). Komponente, Script und
  Meta wurden entfernt, die Startpose steht jetzt direkt im Spieler-Transform
  und liegt exakt auf dem Boden.
- Dieser Befund ist mit `RegionTravelSpawnRuntimeTests` abgesichert: der Test
  nimmt das vorhandene Rueckreise-Portal, reist ueber `RegionTravel.TravelTo`
  und prueft, dass Aren am `RegionSpawnPoint` mit der Portal-Spawn-ID landet
  und nicht an der normalen Waldstartpose. Gegenprobe mit absichtlich falscher
  Spawn-ID: der Test schlaegt mit 80,98 m Abweichung fehl.
- Die `Lichtschneise` war technisch unbedenklich (keine Schatten, keine
  Occlusion, nur Layer `Default`, realtime, buildsicher), aber unverhaeltnis-
  maessig: 451 Renderer lagen in ihrer Reichweite von 34 m und bekamen ein
  zusaetzliches Per-Pixel-Licht, waehrend das einzige bestehende Zusatzlicht
  der Szene (`Warm Guidance Light`) mit Reichweite 7 arbeitet. Sichtbar wurde
  sie erst bei 60 cd; bei der beabsichtigten Staerke war keine Wirkung messbar.
  Nur das Lichtobjekt wurde entfernt, Vegetationskorridor, Schild, Rucksack
  und Wegfuehrung blieben unangetastet.


### P1.4 — Gegner-Grundlage (EnemyFoundation)

- Status: `INTEGRIERT` über PR #25, Merge-Commit 57097ce.
- Branch: `feature/enemy-foundation`
- Scope: erster technischer Gegner-Prototyp als Basis fuer den spaeteren
  Wurzelstreifer. Bewusst generisch benannt; noch kein konkreter Gegner.
- Neue Runtime-Skripte unter `Assets/_Elyndor/Scripts/Enemies`
  (`Elyndor.Enemies`): `EnemyFoundationState`, `EnemyStateRules`,
  `EnemyStateMachine`, `EnemyHealth`, `EnemyPerception`, `EnemyMovement`,
  `EnemyAttack`, `EnemyHitReaction`, `EnemyController`.
- `EnemyHealth` implementiert das vorhandene `IDamageable`, damit der
  bestehende `PlayerCombat` ohne Anpassung trifft. Der Schaden am Spieler
  laeuft ueber das vorhandene `PlayerVitals.TakeDamage`.
- Editor: `Elyndor/QA/Validate Enemy Foundation`
  (`EnemyFoundationValidator`) prueft die Zustandstabelle vollstaendig und
  rein statisch — ohne GameObject, ohne Szene.
- Bewusst ausgeschlossen: NavMesh, Animationen, VFX, Loot, Spawning,
  Bosslogik, Loretexte, Gegner-Prefab und jede Platzierung im Finsterwald.
- Keine Szenenaenderung: der Branch enthaelt keinen `*.unity`-Diff. Tests
  bauen den Gegner vollstaendig zur Laufzeit auf.
- Automatisch bestanden: Unity-Batch-Kompilierung ohne Fehler,
  21 neue Play-Mode-Tests (`EnemyFoundationRuntimeTests`), alle bestehenden
  Play-Mode-Tests, Enemy-Foundation-Validator, World-Validator, Movement-/
  Kamera-Validator, Regionsportal-Validator, Gate-0-Input-Validator,
  Missing-Script-Pruefung und `git diff --check`.
- Manuell bestanden: Zusammenbau eines Gegners im Editor, Sichtpruefung von
  Wahrnehmungsradius und Sichtkegel im Terrain, Abstimmung von Werten fuer
  Reichweite, Tempo und Abklingzeit sowie ein Treffertest mit dem
  vorhandenen `PlayerCombat`.
- Abnahme abgeschlossen: PR #25 ist in `developer` gemergt (Merge-Commit
  57097ce), die manuelle Pruefung der Gegner-Grundlage ist bestanden.

## Phase 1 und spaeter

Finsterwald Vertical Slice ist nach Abschluss von Gate 0 **FREIGEGEBEN**,
wurde jedoch noch nicht begonnen.
Sonnenfelder, Nebelmoor, Tal der verlorenen Wege und Ruinen von Arvenfall sind
nur geplant und duerfen nicht automatisch begonnen werden. Details stehen in
`DEVELOPMENT_PLAN.md`, `WORLD_ROADMAP.md` und `GAMEPLAY_ROADMAP.md`.
