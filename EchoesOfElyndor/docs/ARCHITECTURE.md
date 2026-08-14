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

Abgesichert wird die Struktur durch `SceneIntegrityAnalyzer` (Editor) mit dem
Profil aus `FinsterwaldSceneIntegrityValidator`. Geprüft werden Pflichtsysteme
unter deaktivierten Vorfahren, doppelte Einzelsysteme, UI-Wurzeln mit Scale 0,
fehlende Scripts und nicht gesetzte Pflichtreferenzen.

**Offene technische Schuld:** `PlayerCombat`, `IntroSequence`,
`TutorialSequence` und `InventoryUI` lesen weiterhin direkt
`Keyboard.current` / `Gamepad.current`. Tastatur und Gamepad werden dort
jeweils explizit behandelt, Rebinding und zentrale Eingabeumschaltung sind an
diesen Stellen aber nicht möglich.

---

## Später geplant (noch NICHT vorhanden)

- `GameStateManager` — Playing/Dialogue/MemoryVision/Paused (M5)
- Dialogsystem (`DialogueAsset`, ScriptableObjects) (M7)
- Kartografie-System (M9)
- Save/Load mit `ISaveable` (M10) — ersetzt/persistiert `MemorySessionState`
- Memory-Watch-Post-Processing (URP Volume) statt UI-Overlay
