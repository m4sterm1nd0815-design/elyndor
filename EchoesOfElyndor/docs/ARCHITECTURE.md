---
Project: Echoes of Elyndor
Version: 0.0.2
Status: Active
Owner: Lars Becker
Last Updated: 22.07.2026
---



# ARCHITECTURE

Beschreibt die **tatsächlich vorhandene** Architektur. Geplante Systeme
stehen ausschließlich im Abschnitt „Später geplant".

## Assemblies

| Assembly | Ort | Inhalt |
|----------|-----|--------|
| `Elyndor.Runtime` | `Assets/_Elyndor/Scripts/` | Gesamter Gameplay-Code (Referenzen: `Unity.InputSystem`, `UnityEngine.UI`) |
| `Elyndor.Editor` | `Assets/_Elyndor/Scripts/Editor/` | Editor-Tooling, nur Editor-Plattform |

Namespaces folgen der Ordnerstruktur: `Elyndor.<Domäne>`.

---

## Bereits vorhanden (vor Sprint „Whispering Forest Foundation")

### `Elyndor.Player`
- **`PlayerMovement`** — CharacterController-basierte Bewegung: WASD/Stick,
  Sprint, Dodge Roll (Coroutine), Gravitation (für unebenes Terrain),
  Animator-Ansteuerung über `Speed`-Parameter.
  Bezieht sämtliche Eingaben über `PlayerInputReader`
  (`[RequireComponent(typeof(PlayerInputReader))]`) und greift **nicht**
  direkt auf `Keyboard.current` / `Gamepad.current` zu.
- **`PlayerInputReader`** — zentrale Eingabeschicht. Löst das Actions-Asset
  auf (Inspector-Feld, sonst `PlayerInput`, sonst projektweites Asset),
  klont es für die Laufzeit und stellt Move, Look, Sprint, Jump, Roll,
  Interact und die Quickslots als Eigenschaften bereit. Fehlt eine Aktion
  oder ein erwartetes Binding, wird zur Laufzeit eine vollständige
  Standardbelegung erzeugt. `GameplayMapEnabled` meldet, ob die Map
  tatsächlich aktiv ist — Absicht und realer Zustand sind bewusst getrennt.
- **`PlayerState`** — Enum `Normal` / `Rolling`.

### `Elyndor.Cameras`
- **`CameraFollow`** — LateUpdate-Follow mit Offset und `Lerp`
  (framerate-abhängig, Umstellung auf `SmoothDamp` geplant).

### Szenen und Assets
- `Bootstrap.unity` — Startszene: Main Camera, Ground, Directional Light,
  Graphics/Global Volume, Player (Aren-Modell mit Animator).
- `PlayerAnimator.controller` — Locomotion Blend Tree (Idle/Walk/Run).
- `InputSystem_Actions.inputactions` — wird von `PlayerInputReader` verwendet.
  **Wichtig:** Dieses Asset ist zugleich das *projektweite* Actions-Asset
  (`ProjectSettings/EditorBuildSettings.asset`, Schlüssel
  `com.unity.input.settings.actions`). Unity aktiviert es beim Start
  selbsttätig. Wer es klont, muss den Klon vor dem Binden deaktivieren —
  sonst erbt der Klon die Enabled-Flags ohne gültigen `InputActionState`
  und jedes `Enable()` scheitert mit „Map must be contained in state".

---

## In diesem Sprint erstellt

### `Elyndor.Interaction`
- **`IInteractable`** — Vertrag für alle Interaktionsobjekte:
  `InteractionPrompt`, `Position`, `CanInteract`, `Interact`.
- **`InteractableBase`** — abstrakte MonoBehaviour-Basis; hält den
  Prompt-Text, warnt bei fehlendem Trigger-Collider.
- **`InteractionDetector`** — sitzt am Player. Die Reichweite definieren
  die Trigger-Collider der Objekte (der CharacterController löst sie aus);
  der Detector verwaltet die Menge in Reichweite, wählt genau ein aktives
  Ziel (nächstes interagierbares) und meldet Wechsel über das Event
  `TargetChanged`. Interaktionstaste: E / Gamepad West.
- **`ExaminableObject`** — untersuchbares Objekt; sendet seinen Text über
  den Narration-Kanal.

### `Elyndor.Memory`
- **`MemorySite`** — Erinnerungsort (erbt von `InteractableBase`).
  Zustandsmaschine Idle → Activating → Activated; Aktivierungssequenz als
  Coroutine; statische Events `AnyActivationStarted` / `AnyActivationCompleted`.
  Nur Ablauf + Zustand — keine Darstellung, kein UI.
- **`MemoryEcho`** — rein visuelle Erinnerung (z. B. Geister-Brücke).
  Versteckt sich in `Awake`, blendet per `MaterialPropertyBlock`-Alpha ein.
- **`MemorySessionState`** — statischer Sitzungszustand aktivierter Sites
  (übersteht Szenenwechsel; Reset per `RuntimeInitializeOnLoadMethod`).
  Anschlusspunkt für das spätere Save-System.
- **`MemoryVisionEffect`** — rein visuelle Betonung der Aktivierung:
  animiert das Gewicht eines URP-Volumes (Abdunklung, Entsättigung,
  Vignette); hört auf die statischen `MemorySite`-Events, keine Spiellogik.

### `Elyndor.Core`
- **`NarrationEvents`** — statischer Event-Kanal für erzählende Texte.
  Entkoppelt Weltobjekte von der UI.

### `Elyndor.UI`
- **`InteractionPromptUI`** — zeigt den Prompt des aktiven Ziels; kennt nur
  den `InteractionDetector` (Szenen-Referenz per Inspector).
- **`NarrationUI`** — zeigt Texte aus dem Narration-Kanal zeitbegrenzt an.
- **`MemoryWatchActivationUI`** — Overlay während der Aktivierung; hört auf
  die statischen `MemorySite`-Events.

### `Elyndor.EditorTools`
- **`WhisperingForestSceneBuilder`** — Menü `Elyndor/Setup/Fluesterwald-
  Testszene erstellen`. Baut `Fluesterwald_Prototype.unity` deterministisch
  als Kopie von Bootstrap: 140×140-m-Terrain (Perlin-Höhen, Zonen-Modifier,
  Alphamap-Pfade), acht Bereiche, Catmull-Rom-Pfade, Bach mit zwei
  Querungen, Geister-Brücke (MemoryEcho), Erinnerungsort (MemorySite),
  Examinables, HUD und Natur-Pack-Vegetation (Material-Remapping auf
  eigene URP-Materialien, Maßstabs-Normalisierung, Auto-Kollider).
  Wiederholbar; überschreibt nur die eigene generierte Szene und ihre
  generierten Assets (TerrainData, Layer, Profile, Materialien).

### Ereignisfluss (dieser Sprint)

```
CharacterController betritt Trigger
  → InteractionDetector (Ziel-Auswahl) ──TargetChanged──→ InteractionPromptUI
  → [E] → IInteractable.Interact()
        ExaminableObject ──NarrationEvents──→ NarrationUI
        MemorySite ──AnyActivationStarted──→ MemoryWatchActivationUI
                   ──(Sequenz)──→ MemoryEcho.Reveal()
                   ──MemorySessionState.MarkActivated()
                   ──NarrationEvents──→ NarrationUI
```

---

## Erlebnisschicht in Regionsszenen

Die Szenen trennen sichtbare UI und nicht-visuelle Laufzeitsysteme in zwei
getrennte, unabhängig aktive Wurzeln:

| Wurzel | Inhalt |
|--------|--------|
| `ElyndorUI` (Canvas) | `ElyndorHudFoundation` mit Vitals und Quickslots |
| `ElyndorExperienceUI` (Canvas) | `InteractionPromptUI`, `NarrationUI`, `MemoryWatchActivationUI`, `CompassUI`, `InventoryUI` und deren Panels |
| `ElyndorExperience` (kein Canvas) | `IntroSequence`, `TutorialSequence`, `CombatTutorial`, `SfxLibrary` + `AudioSource` |

Die Trennung ist keine Kosmetik. Vorher lag beides auf einem einzigen
Canvas-Objekt (`PrototypeHUD`). Als dieses Objekt bei der HUD-Integration
deaktiviert wurde, starben mit den Panels auch Intro, Tutorial und die
komplette Soundausgabe — MonoBehaviours auf einem deaktivierten GameObject
erhalten weder `Awake` noch `Update`. Ein abgeschaltetes UI-Panel darf nie
wieder den Ton mitnehmen.

### Was welche Region tatsächlich führt

Die Trennung gilt in allen Regionen, der Umfang aber nicht. Regionen werden
bewusst **nicht** künstlich angeglichen:

| System | Finsterwald | Sonnenfelder | Nebelmoor |
|--------|-------------|--------------|-----------|
| `ElyndorExperienceUI` mit Prompt, Narration, Memory Watch, Kompass, Inventar | ja | ja | ja |
| `ElyndorExperience` mit `SfxLibrary` + `AudioSource` | ja | ja | ja |
| `ElyndorUI` / `ElyndorHudFoundation` (Vitals, Quickslots) | ja | nein | nein |
| `IntroSequence`, `TutorialSequence`, `CombatTutorial` | ja | nein | nein |

Finsterwald ist der Vertical Slice und trägt deshalb als einzige Region das
HUD-Fundament sowie Intro und Tutorial. Sonnenfelder und Nebelmoor führen diese
Systeme nicht — das ist Designentscheidung, kein Defekt.

`Bootstrap` ist eine Sandbox ohne Erlebnisschicht: keine UI, kein Ton, keine
Erinnerungsorte.

### Absicherung

Abgesichert wird die Struktur durch `SceneIntegrityAnalyzer` (Editor) mit einem
Profil je Szene. Geprüft werden Pflichtsysteme unter deaktivierten Vorfahren,
doppelte Einzelsysteme, UI-Wurzeln mit Scale 0, fehlende Scripts und nicht
gesetzte Pflichtreferenzen.

Damit die Regionsspezifik nicht zur Lücke wird, kennt ein Profil zwei Stufen:

- `RequireActive` — muss vorhanden **und** wirksam sein.
- `AllowOptionalActive` — darf fehlen; ist es vorhanden, muss es wirksam sein.
  Nebelmoor braucht kein Intro. Läge dort trotzdem eines unter einem
  deaktivierten Vorfahren, wäre das genau die Finsterwald-Fehlerklasse.

| Einstiegspunkt | Zweck |
|----------------|-------|
| `RegionSceneIntegrityProfiles` | die Profile je Szene |
| `RegionSceneIntegrityValidator` | ein QA-Durchlauf über alle Szenen, lädt jede Szene selbst, batchmode-tauglich |
| `FinsterwaldSceneIntegrityValidator` | unverändert die Einzelprüfung der Referenzregion |
| `ExperienceLayerMigrator` | idempotente Trennung je Szene, einzeln oder über alle Regionen |

`RegionScenes` hält die Szenenpfade an genau einer Stelle, damit Migrator,
Prüfung und Tests nicht auseinanderlaufen und keine Region still aus der
Prüfung fällt.

**Nebenwirkung, bewusst abgesichert:** Die Prüfung öffnet Szenen. Sie stellt die
ursprünglich offene Szene danach wieder her und schreibt das in den Report — ohne
das blieb die zuletzt geprüfte Szene aktiv, und ein anschließender Play-Mode-Test
lief unbemerkt auf der falschen Region. Hat die offene Szene ungespeicherte
Änderungen, bricht die Prüfung ab, statt sie zu verwerfen.

Die übrigen Validatoren (`RegionPortalValidator`, `MovementCameraValidator`)
öffnen ebenfalls Szenen und stellen bisher nichts wieder her. Vor einem
Play-Mode-Test bleibt daher die Regel: aktive Szene erneut prüfen.

### Whitespace-Gate

`git diff --check` ist Pflicht vor jedem Paket. Unity serialisiert leere
String-Felder als `key: ` mit Leerzeichen am Zeilenende, was in einem einzigen
Szenendiff über tausend Meldungen erzeugt — das Gate wäre damit praktisch
wertlos. `.gitattributes` nimmt Unity-YAML-Assets deshalb gezielt **nur** aus
der Trailing-Space-Prüfung. In Code und Doku meldet `git diff --check`
unverändert alles, und selbst in `.unity` bleibt etwa `space before tab` aktiv.

---

## Eingabe

`PlayerInputReader` ist die einzige Stelle, die Geräte liest. Gameplay-Code
fragt ausschließlich Eigenschaften des Readers ab, nie `Keyboard.current`,
`Gamepad.current` oder `Mouse.current`.

Vorher lasen `PlayerCombat`, `IntroSequence`, `TutorialSequence` und
`InventoryUI` ihre Tasten selbst. Das hatte drei konkrete Folgen:

- **Doppelbelegung blieb unsichtbar.** `Interact` lag im Actions-Asset auf
  `<Gamepad>/buttonNorth`, und `InventoryUI` las dieselbe Taste direkt. Am
  Controller öffnete ein Druck das Inventar **und** untersuchte gleichzeitig das
  Objekt davor.
- **Code und Asset liefen auseinander.** Das Tutorial prüfte `leftCtrl` für die
  Rolle, während das Asset sie auf `C` legt — wer die Rolle wie vorgesehen
  auslöste, kam im Tutorial nicht weiter. `PlayerCombat` hörte auf den rechten
  Trigger, den das Asset gar nicht kannte.
- **Nichts davon war testbar**, weil kein Test an einer Belegung vorbeikam, die
  nur im Code stand.

Belegung im Actions-Asset (Map `Player`):

| Aktion | Tastatur | Gamepad |
|--------|----------|---------|
| Move | WASD / Pfeile | linker Stick |
| Look | Maus | rechter Stick |
| Sprint | Linke Umschalt | linker Stick gedrückt |
| Jump | Leertaste | A |
| Crouch (Rolle) | C | B |
| Interact | E | Y |
| Attack | Maustaste links, Enter | X, rechter Trigger |
| Block | Q | linker Trigger |
| Inventory | I | Select |
| Quickslots | 1–8, R | Schultertasten, rechter Stick gedrückt |

`<Gamepad>/start` bleibt bewusst unbelegt und ist für Pause reserviert.

Abgesichert durch `InputBindingCollisionTests`: kein Gamepad-Control darf zwei
Aktionen bedienen, `Interact` und `Inventory` dürfen kein Control teilen, die
bisherigen Belegungen müssen erhalten bleiben und `start` frei. Diese Tests
laufen bewusst **ohne** `InputTestFixture` — deren Setup löscht
`InputSystem.actions` und würde die Prüfung still überspringen.

`SetGameplayInputEnabled` schaltet die gesamte Gameplay-Map ab; das ist der
Anschlusspunkt für Dialoge, Pause und Memory-Vision.

**Verbleibende Schuld:** Intro, Tutorial und Inventar-UI liegen nicht auf der
Spielfigur und suchen den Reader zur Laufzeit über
`PlayerInputReader.FindInLoadedScenes()`, wenn das optionale serialisierte Feld
leer ist. Das vermeidet, jede bestehende Szene neu verdrahten zu müssen, ist
aber eine Suche statt einer Referenz.

---

## Verbindliche technische Grundsätze

Aus `docs/Technical/ARCHITECTURE.md` hierher übernommen; jenes Dokument war eine
zweite, konkurrierende technische Wahrheit und verweist jetzt nur noch hierauf.

- Unity 6 und URP bleiben Basis.
- Lose gekoppelte Systeme, Events und Interfaces statt harter Referenzen.
- `ScriptableObject`s für statische Definitionen, Runtime-Modelle für
  veränderliche Zustände.
- UI zeigt Modelle an; Gameplaylogik gehört nicht in Views.
- **Keine neue Singleton-Struktur ohne dokumentierte Begründung.**
- **Stabile IDs sind Savegame-Verträge** und dürfen nicht beiläufig geändert
  werden.

### Save-Bereiche

`inventory`, `equipment`, `quickslots`, `narrative`, `settings`, `world`

Als Vertrag vorgemerkt. **Umgesetzt ist davon seit P1.13A nur `world`** — und
auch das nur so weit, wie der Finsterwald-Slice es braucht.

### Persistenz (P1.13A)

```
MemorySessionState ─┐
PuzzleSessionState ─┼─► SaveService ─► ISaveStore ─► Datei
RegionRegenerationState ┘      └─► SaveData (versioniert, saveVersion = 1)
```

- Die drei Sitzungszustände melden über ein `Changed`-Ereignis nur, **dass**
  sich etwas geändert hat. Sie kennen den `SaveService` nicht.
- Der `SaveService` kennt keine Dateien; das ist Sache des `ISaveStore`. An
  dieser Naht hängen Tests einen temporären Speicher ein.
- Für Regionen wird der vorhandene Vertrag `IRegionStateStore` bedient.
- Gestartet über `SaveBootstrap` per `RuntimeInitializeOnLoadMethod` —
  **keine Szene wurde dafür geändert**, kein neues Objekt, keine neue
  Referenz. Ein bereits eingehängter Dienst wird nicht ersetzt.
- **Nicht lebensnotwendig:** fällt das Speichern aus, läuft das Spiel weiter
  und verliert nur Fortschritt.

Einzelheiten in `Technical/SAVE_SYSTEM.md`.

### Qualitätsanforderung je System

Jedes neue System braucht Fehlerbehandlung, Null-Prüfungen, einen Validator oder
Test und Integrationsdokumentation.

## Später geplant (noch NICHT vorhanden)

- `GameStateManager` — Playing/Dialogue/MemoryVision/Paused (M5)
- Dialogsystem (`DialogueAsset`, ScriptableObjects) (M7)
- Kartografie-System (M9)
- Save-Slots, Hauptmenü, Cloud-Save und Plattformanbindung (P1.13B und später)
- Gespeicherte Startposition (`spawnId` ist im Format vorgesehen, aber leer) —
  wartet auf eine Leveldesign-Entscheidung
- Memory-Watch-Post-Processing (URP Volume) statt UI-Overlay
