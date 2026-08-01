---
Project: Echoes of Elyndor
Version: 0.0.2
Status: Active
Owner: Lars Becker
Last Updated: 22.07.2026
---



# CHANGELOG

## [Unreleased] — World Visual Overhaul Foundation

- Additiver, idempotenter Visual-Layer für Finsterwald, Sonnenfelder und
  Nebelmoor; bestehende Gameplay-Roots, Portale, Spawns und Memory Sites
  bleiben unangetastet.
- Neue Landmarken-, Ruhe-, Spielerführungs- und Regenerationsgruppen sowie
  regionale Light-Probe-Raster ergänzt.
- Neuer `WorldVisualOverhaulBuilder` mit Build- und Validierungsmenüs.
- Visuelle Richtung, Regionslayout, Assetbestand, Performance-Risiken und
  manueller Testplan unter `docs/` dokumentiert.

## [0.0.9] — 2026-07-25 — Visual Uplift Stufe 1 + Wald-Aufräumen

- **Rendering:** SSAO (PC-Renderer), Schattendistanz 90 m mit 3 Kaskaden,
  dezentes Bloom in allen Regionsprofilen.
- **Neue Shader:** `Elyndor/FoliageWind` (Blattwerk wiegt sich im Wind,
  höhenabhängig, inkl. Schattenwurf) auf allen Cutout-Materialien;
  `Elyndor/StylizedWater` (Wellenbewegung + wandernde Glanzstreifen)
  auf allen Wasserflächen.
- **Atmosphären-Partikel:** Schwebstaub (Finsterwald), Glühwürmchen
  (Memory Site, Lichtung), Nebelschwaden + Irrlichter (Moor),
  Pollenstaub (Sonnenfelder).
- **Kamera:** Blick jetzt bis fast auf Bodenhöhe absenkbar (minPitch 6°),
  mit Terrain-Schutz gegen Eintauchen ins Gelände.
- **Wald-Aufräumen:** „Gestürzte Baumriesen" entfernt (lasen sich als
  Platzierungsfehler); per Modell-Audit (`AuditTreeLineup`, messbare
  Schräglage) stark lehnende Varianten aus den Streu-Pools genommen
  (TST-Variante 10, USN-Kiefer 4).


## [0.0.8] — 2026-07-25 — Sprint „Kapitel 1: Der Finsterwald" (M3.4–M3.6)

- **Rückkehr-Tore** (Lore-Bibel Rückkehrstruktur): Schwarzer See mit
  versunkenen Stufen (SO), verriegelte Waldhütte mit sichtbarem Tagebuch
  und Zahnrad (NW), Baum ohne Schatten mit Watch-Vertiefung (N) —
  sichtbar ab Kapitel 1, nutzbar erst mit späteren Watch-Fähigkeiten.
- **Erwachens-Sequenz** (`IntroSequence`): Schwarzblende mit den
  fragmentierten Erinnerungszeilen des Kapitel-1-Auftakts, überspringbar,
  einmal pro Sitzung, nur im Finsterwald.
- **Quest-Fundament + vier Nebenquests**: `QuestState`/`QuestGiver`/
  `QuestObjective`; Maela (Korb), Borin (Axt), Teren (Packtier),
  Ilya (Wegmarke) als Platzhalter-NPCs mit Lore-Texten.
- Jeweils verifiziert per Unity-Batch-Build: 0 Fehler, 0 Warnungen.


## [0.0.7] — 2026-07-25 — Kanon-Sync auf Lore-Bibel V1

- **Story-Kanon V1 übernommen** (Lore- und Game-Bibel V1): Aren erwacht
  ohne Erinnerungen, Soren als Antagonist/früher Begleiter, Elian als
  Vater und Watch-Schöpfer, 12-Kapitel-Struktur. `GAME_BIBLE.md` ersetzt
  die alte Prämisse (Kartograf, Ring, Großes Vergessen).
- **Flüsterwald → Finsterwald**: Szene (`Finsterwald.unity`), Builder,
  Terrain-/Profil-Assets, Site-ID, Schildertexte und Build Settings
  umbenannt; alte Fluesterwald-Assets entfernt.
- **Sonnenfelder** bleibt als optionales Nebenquest-Gebiet erhalten
  (kein Kapitelgebiet; Entscheidung Lars).
- Encoding-Reparatur: doppelt kodierte Umlaute/Anführungszeichen in
  drei Editor-Skripten korrigiert (Ursache: frühere PowerShell-Edits
  ohne explizites UTF-8; alle Skript-Edits laufen jetzt mit
  `-Encoding utf8`).
- Verifiziert per Unity-Batch-Build: 0 Fehler, 0 Warnungen.


Format angelehnt an [Keep a Changelog](https://keepachangelog.com/de/).
Neueste Einträge oben.

---

## [0.0.6] — 2026-07-25 — Sprint „Weltwerkstatt I: Größerer Wald + Handarbeit-Schicht"

### Flüsterwald auf 200 × 200 m (~2× Fläche)
- Drei neue Story-Orte im Außenring: **Verfallener Wachturm** (NO-Kuppe,
  abgetragene statt eingestürzte Ostwand), **Stiller Teich** (Westsenke
  mit Schilf), **Verlassenes Jägerlager** (SW, nie entzündetes Feuer) —
  jeweils mit Anbindungspfad und untersuchbarem Detail.
- Bach bis an die neuen Kartenränder verlängert; Vegetations-Budgets
  ungefähr verdoppelt (Bäume, Gras, Büsche, Waldrand).

### Handarbeit-Schicht (Weltwerkstatt Stufe 3, Grundgerüst)
- Beide Builder erneuern beim Regenerieren nur noch die generierten
  Wurzeln (`Environment`, `*_Terrain`, `MemoryVisionVolume`,
  `PrototypeHUD`). Die neue **`[Handarbeit]`-Wurzel wird nie angetastet**:
  Dort platzierte Objekte (Deko, Charaktere, Feintuning) überleben jede
  Regenerierung.
- Weltwerkstatt Stufe 1+2 (gemeinsame Builder-Basis + Regionen als
  ScriptableObject-Daten) als Folge-Sprint aufgesetzt.

---

## [0.0.5] — 2026-07-24 — Sprint „Elyndor öffnet sich"

Branch: `feature/whispering-forest-foundation`

### Zwei neue Regionen (RegionSceneBuilder)
- **Sonnenfelder** (`Sonnenfelder.unity`): warmes Licht, goldene Wiesen,
  drei Kornfelder mit gepflanzten Reihen, verlassener Hof (untersuchbar),
  Memory Site „Alte Mühle" — die Geister-Windmühle erhebt sich über dem
  Steinfundament. Wege nach Westen (Flüsterwald) und Norden (Nebelmoor).
- **Nebelmoor** (`Nebelmoor.unity`): dichter Nebel (0.045), gedämpftes
  Licht, fünf dunkle Tümpel in Senken, nur tote/verdrehte Bäume,
  versunkener Karren (untersuchbar, verweist auf die Mühlen-Geschichte),
  Memory Site „Alter Steg" — Geisterplanken über dem schwarzen Wasser.
- Jede Region: eigenes Terrain, eigene Post-Processing-Profile, eigene
  Skybox/Fog/Ambient-Stimmung, eigener HUD-Aufbau.

### Regions-Verbindungen (`Elyndor.World`)
- `RegionPortal` (Trigger → Szenenwechsel), `RegionTravel` (statischer
  Reise-Zustand), `RegionSpawnPoint` (Spawn nach Ankunft).
- Flüsterwald ↔ Sonnenfelder ↔ Nebelmoor als zusammenhängender Kontinent-
  Ausschnitt; Memory-Site-Zustände überleben Regionswechsel
  (MemorySessionState war bereits szenenwechselfest).
- Wegweiser-Schilder kündigen die Nachbarregion erzählerisch an.

### Spieler unter Baumkronen sichtbar
- Neuer Shader `Elyndor/OccludedSilhouette` (ZTest Greater): zeichnet
  ausschließlich auf verdeckten Pixeln.
- `PlayerSilhouette` hängt das Silhouetten-Material an alle Renderer des
  Spielers — verdeckt ihn eine Baumkrone oder ein Hügel, erscheint eine
  sanfte hellblaue Silhouette (Tunic-Stil). GPU-seitig, ohne Raycasts.

### Bekannte Schulden
- RegionSceneBuilder dupliziert HUD-/Terrain-Helfer des Forest-Builders —
  gemeinsame Builder-Basis steht aus.

---

## [0.0.4] — 2026-07-22 — Sprint „Der wachsende Flüsterwald"

Branch: `feature/whispering-forest-foundation` — der Wald wächst auf
140 × 140 m (~5× Fläche), organisch statt flächig.

### Welt
- **Unity-Terrain statt flacher Plane**: sanfte Hügel (Perlin-Noise),
  Senken, ein Aussichtshügel (+7 m), abgeflachte Lichtung, erhöhtes
  Ruinen-Plateau; Terrain-Layer (Wald/Pfad/Fels/Wiese) mit prozeduralen
  Noise-Texturen, Pfade und Zonen per Alphamap gezeichnet.
- **Acht fließend verbundene Bereiche**: Startbereich, Waldpfad,
  kleine Lichtung, alter Bach, Steinkreis, versteckter Pfad,
  kleine Ruine, Aussichtspunkt.
- **Kurvige Splines-Pfade** (Catmull-Rom) statt gerader Wege; ein
  schmaler, schwächer gezeichneter versteckter Pfad zur Ruine.
- **Der alte Bach** quert die Karte, ist eingegraben und nur an zwei
  entdeckbaren Stellen passierbar: umgestürzter Stamm (Westen) und
  flache Furt mit Trittsteinen (Osten). Die zerstörte Brücke am
  Hauptweg bleibt der Memory-Watch-Moment — und ist vom Nordufer aus
  später erneut sichtbar (Rundweg).
- **Landmarken zur Blickführung**: großer alter Baum (Lichtung),
  markanter Fels (Weggabelung), toter Baum als Silhouette
  (Aussichtspunkt), Steinkreis auf Anhöhe, Bachlauf als Leitlinie,
  alte Wegmarkierungen an Entscheidungspunkten.
- **Drei neue untersuchbare Details** (bestehendes System, keine neue
  Mechanik): Steinkreis, verwitterte Inschrift in der Ruine,
  Aussichtspunkt-Schild.

### Natur Pack integriert
- Analyse: Low-Poly-Naturset (FBX + Texturen inkl. Normal Maps),
  keine Prefabs/Materialien enthalten; Modelle in 4 Formaten dupliziert.
- Eigene URP-Materialien für alle Pack-Texturen (Bark/Leaves/Rocks/
  Grass/Flowers, Alpha-Cutout für Blattwerk, Normal Maps aktiviert);
  Material-Remapping beim Platzieren über die FBX-Materialnamen.
- **Maßstabs-Normalisierung**: Jedes Modell wird vermessen und auf
  Zielhöhen skaliert (Laubbäume ~8 m, Kiefern ~11 m, Büsche ~1,1 m …).
- Kollider automatisch: Kapsel-Stammkollider für Bäume; Kleinvegetation
  bewusst ohne Kollider.
- ~250 Bäume in Baumgruppen + Einzelbäumen (Artenmix nach Standort:
  Kiefern auf Höhen, verdrehte Bäume am Bach, tote Bäume als Akzente),
  dichter Waldrand (~110 Bäume) statt sichtbarer Grenzmauern,
  Büsche, Gras, Farne, Kiesel, Pilze, Blumen — deterministisch verteilt.

### Technik
- `PlayerMovement`: **Gravitation ergänzt** (geplante Schuld aus der
  Roadmap) — nötig für Höhenunterschiede; Bewegung/Sprint/Roll sonst
  unverändert.
- Asset-Reorganisation: Packs von `Assets/_Elyndor/Objects/` nach
  `Assets/ThirdParty/` (ADR-002); Format-Duplikate (OBJ/glTF/.blend,
  Nicht-Unity-FBX) entfernt; leerer Medieval-Village-Ordner entfernt
  (Import war fehlgeschlagen).
- Alte Primitive-Prefabs und nicht mehr genutzte Prototyp-Materialien
  entfernt (durch Natur-Pack-Modelle bzw. Terrain-Layer ersetzt).
- Unsichtbare Weltgrenzen-Collider + dichter Baumrand statt
  Platzhalter-Mauern.

---

## [0.0.3] — 2026-07-22 — Sprint M2.5 „The First Breath of Elyndor"

Branch: `feature/whispering-forest-foundation` — reiner Atmosphäre-Pass,
keine neuen Gameplay-Systeme.

### Atmosphäre (Unity-Einstellungen, per Scene Builder)
- **Fog:** ExponentialSquared, Dichte 0.018, graugrüner Nebelton —
  ruhige Tiefenwirkung, der Wald „verliert sich" in der Ferne.
- **Licht:** Tiefstehende, warme Morgensonne (38°/-32°), weiche Schatten
  (Stärke 0.8) für lange Schattenwürfe quer zum Weg.
- **Ambient:** Trilight (kühler Himmel, gedämpfte Mitten, dunkler Boden).
- **Skybox:** Neues prozedurales Skybox-Material (`Proto_Skybox`),
  entsättigter Himmelston; als `RenderSettings.sun` verknüpft.
- **Post-Processing:** `Fluesterwald_Base_Profile` (dezente Vignette 0.18,
  Sättigung -8, Kontrast +6) auf dem Global Volume.
- **Farbpalette** aller Prototyp-Materialien entsättigt und harmonisiert
  (Vergessen / Ruhe / Neugier statt Spielzeug-Farben).

### Memory-Watch-Moment
- Neu: `MemoryVisionEffect` (Runtime) blendet während der Aktivierung ein
  zweites Volume (`Fluesterwald_MemoryVision_Profile`: Abdunklung,
  Entsättigung -45, kühle Farbtemperatur, Vignette 0.38) weich ein und
  nach dem Erscheinen der Erinnerung wieder aus.
- Aktivierungs-Overlay fadet jetzt sanft über eine CanvasGroup
  (`MemoryWatchActivationUI` überarbeitet), Abdunklung statt Farbfläche.
- Geister-Brücke: sanfteres Einblenden (2.5 s) und dezentes Eigenleuchten
  (Emission) — „Hier war einmal etwas."

### Ergänzte Objekte
- Vegetation als Prefabs (`Prefabs/Prototype`): zwei Baumvarianten
  (Moos/Verblasst), Busch, Stein, Grasbüschel, zwei Blumenarten —
  deterministisch verteilt (28 Bäume inkl. Ufer-Gegenseite, 16 Büsche,
  12 Steine, 55 Grasbüschel).
- Erzählerische Details ohne Mechanik: umgestürzter Baum mit Wurzelballen,
  alte Wegmarkierung mit verblasster Farbmarke, halb versunkene Steinreste
  an Flussufer und Erinnerungsort, stiller Blumenring um den Steinkreis.

### Performance
- Alle statischen Umgebungsobjekte mit `BatchingStatic` markiert
  (Geister-Brücke bewusst ausgenommen wegen Alpha-Fade).
- Vegetation als Prefab-Instanzen, saubere Hierarchie
  (Environment → Vegetation/Erzaehl-Details/Bereichsgrenzen).
- Keine neuen Update()-Methoden — alles event- und coroutine-basiert.

### Bewusst ausgelassen
- Audio: Im Projekt existieren keine Audio-Assets; statt ungeprüfter
  externer Sounds bleibt Audio ein dokumentierter Platzhalter
  (modulare Einbindung folgt, sobald geeignete Assets vorliegen).

---

## [0.0.2] — 2026-07-22 — Sprint „Whispering Forest Foundation"

Branch: `feature/whispering-forest-foundation`

### Hinzugefügt
- **Interaktionssystem** (`Elyndor.Interaction`): `IInteractable`,
  `InteractableBase`, `InteractionDetector` (ein aktives Ziel,
  `TargetChanged`-Event, Taste E / Gamepad West), `ExaminableObject`.
- **Memory-Watch-Prototyp** (`Elyndor.Memory`): `MemorySite`
  (Aktivierungssequenz, Zustand, statische Events), `MemoryEcho`
  (visuelles Einblenden per Alpha-Fade), `MemorySessionState`
  (Sitzungszustand, szenenwechselfest).
- **Narration-Kanal** (`Elyndor.Core.NarrationEvents`) zur Entkopplung
  von Weltobjekten und UI.
- **Prototyp-HUD** (`Elyndor.UI`): `InteractionPromptUI`, `NarrationUI`,
  `MemoryWatchActivationUI` (Legacy-UGUI-Text, bewusst minimal).
- **Editor-Tooling** (`Elyndor.Editor`-Assembly):
  `WhisperingForestSceneBuilder` erzeugt `Fluesterwald_Prototype.unity`
  deterministisch aus Bootstrap (Weg, Bäume, Fluss, Brückenreste,
  Geister-Brücke, Wegschild, Erinnerungsort, HUD) inkl.
  Prototyp-Materialien unter `Art/Materials/Prototype`.
- **Docs**: `GAME_BIBLE.md`, `ARCHITECTURE.md`, `CHANGELOG.md`;
  Roadmap um Phasenübersicht ergänzt.

### Geändert
- `Elyndor.Runtime.asmdef`: Referenz auf `UnityEngine.UI` ergänzt.
- `EditorBuildSettings`: Testszene zusätzlich zur Bootstrap-Szene
  aufgenommen (durch den Scene Builder).

### Unverändert (bewusst)
- `PlayerMovement`, `PlayerState`, `CameraFollow`, `PlayerAnimator`,
  `Bootstrap.unity` — bestehende Player-/Kamera-/Animationssysteme
  wurden nicht angefasst.

---

## [0.0.1] — 2026-07-22 — M2.3 Struktur-Cleanup

### Hinzugefügt
- `Elyndor.Runtime.asmdef`, Namespaces (`Elyndor.Player`, `Elyndor.Cameras`)
- `Docs/Roadmap.md` (Sprints M3–M12), `Docs/DECISIONS.md` (ADR-Log)

### Geändert
- Build Settings von SampleScene auf `Bootstrap.unity` korrigiert
- `InputSystem_Actions` → `_Elyndor/Input/`, Mixamo → `_Elyndor/Animation/`
- Scripts in Domänen-Ordner (`Player/`, `Camera/`)

### Entfernt
- Unity-Template-Reste (SampleScene, TutorialInfo, Readme.asset),
  `desktop.ini`-Dateien, Stub-Duplikate in `Docs/`
