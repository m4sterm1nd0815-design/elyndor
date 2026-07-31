# Architektur-Entscheidungen (ADR-Log)

Kurzes, chronologisches Log aller Entscheidungen mit Langzeitwirkung.
Neue Einträge oben anfügen.

---

## ADR-004 · 2026-07-22 · Interaktions- und Memory-Architektur

**Entscheidung:**
1. Interaktionsreichweite definieren die **Trigger-Collider der Objekte**,
   nicht der Spieler — der CharacterController löst sie aus. So hat jedes
   Objekt seine eigene sinnvolle Reichweite, und es braucht keinen
   zusätzlichen Rigidbody am Spieler.
2. Der `InteractionDetector` hält genau **ein aktives Ziel** (nächstes
   interagierbares) und meldet Wechsel per C#-Event — kein Polling durch
   die UI, keine `FindObjectOfType`-Aufrufe zur Laufzeit.
3. UI-Entkopplung über **statische Event-Kanäle** (`NarrationEvents`,
   `MemorySite.AnyActivation*`) statt Singletons oder ScriptableObject-
   Event-Assets. Alle statischen Events werden per
   `RuntimeInitializeOnLoadMethod` zurückgesetzt (Enter-Play-Mode-Options-fest).
4. Memory Watch strikt getrennt: Ablauf/Zustand (`MemorySite`),
   Sitzungszustand (`MemorySessionState`), Darstellung (`MemoryEcho`),
   Text (`NarrationEvents` → `NarrationUI`). Effekte/Audio sind Platzhalter
   und später austauschbar, ohne die Logik anzufassen.
5. Testszene wird per **Editor-Builder deterministisch generiert**
   (Kopie von Bootstrap), statt von Hand gepflegt — reproduzierbar,
   reviewbar, Bootstrap bleibt unangetastet.

**Warum:** Lose Kopplung ohne Framework-Overhead; jedes Teilsystem ist
einzeln ersetzbar (finale VFX, Save-System, Input-Refactor), ohne die
anderen zu ändern.

---

## ADR-003 · 2026-07-22 · Assembly Definition + Namespaces

**Entscheidung:** Aller Gameplay-Code liegt in `Elyndor.Runtime.asmdef`
(rootNamespace `Elyndor`). Namespaces folgen der Ordnerstruktur:
`Elyndor.Player`, `Elyndor.Cameras`, künftig `Elyndor.Memory`,
`Elyndor.Dialogue`, `Elyndor.World`, `Elyndor.Core`.

**Warum:** Schnellere Kompilierung (Gameplay-Code kompiliert getrennt
von Plugins), saubere Abhängigkeiten, keine Kollisionen mit
Asset-Store-Code. `Elyndor.Cameras` statt `Elyndor.Camera`, um
Konflikte mit `UnityEngine.Camera` zu vermeiden.

---

## ADR-002 · 2026-07-22 · Projektlayout

**Entscheidung:** Alles Projekt-Eigene liegt unter `Assets/_Elyndor/`
(Underscore = sortiert nach oben, klare Trennung von Fremd-Assets).
Struktur:

```
Assets/_Elyndor/
  Animation/        Controller + Roh-Animationen (Mixamo)
  Art/Characters/   Modelle, Texturen, Materialien pro Charakter
  Input/            Input-Actions-Asset
  Scenes/           Bootstrap + künftige Regionsszenen
  Scripts/          Runtime-Code, Unterordner pro Domäne
```

Unity-Template-Reste (SampleScene, TutorialInfo, Readme) wurden
entfernt; Build Settings zeigen jetzt auf `Bootstrap.unity`.

**Warum:** Fremd-Assets (Asset Store, Plugins) landen später direkt
unter `Assets/` und vermischen sich nie mit eigenem Inhalt.

---

## ADR-001 · 2026-07-22 · Designfilter für Features

**Entscheidung:** Jedes Feature muss mindestens eines unterstützen:
Erkunden, Erinnerungen, Geschichte, Atmosphäre. Die Memory Watch
macht ausschließlich sichtbar — sie löst nie selbst Rätsel.

**Warum:** Schutz vor Feature Creep; die Welt ist der Hauptcharakter.
