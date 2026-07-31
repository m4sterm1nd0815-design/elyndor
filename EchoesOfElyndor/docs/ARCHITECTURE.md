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
  Liest Input direkt über `Keyboard.current` / `Gamepad.current`
  (bekannte technische Schuld, Refactor auf Input-Actions-Asset geplant → Roadmap M3).
- **`PlayerState`** — Enum `Normal` / `Rolling`.

### `Elyndor.Cameras`
- **`CameraFollow`** — LateUpdate-Follow mit Offset und `Lerp`
  (framerate-abhängig, Umstellung auf `SmoothDamp` geplant).

### Szenen und Assets
- `Bootstrap.unity` — Startszene: Main Camera, Ground, Directional Light,
  Graphics/Global Volume, Player (Aren-Modell mit Animator).
- `PlayerAnimator.controller` — Locomotion Blend Tree (Idle/Walk/Run).
- `InputSystem_Actions.inputactions` — vorhanden, wird vom Gameplay-Code
  noch nicht verwendet.

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

## Später geplant (noch NICHT vorhanden)

- `PlayerInputReader` — zentraler Input über das Actions-Asset (M3)
- `GameStateManager` — Playing/Dialogue/MemoryVision/Paused (M5)
- Dialogsystem (`DialogueAsset`, ScriptableObjects) (M7)
- Kartografie-System (M9)
- Save/Load mit `ISaveable` (M10) — ersetzt/persistiert `MemorySessionState`
- Memory-Watch-Post-Processing (URP Volume) statt UI-Overlay
