---
Project: Echoes of Elyndor
Version: 0.0.2
Status: Active
Owner: Lars Becker
Last Updated: 22.07.2026
---



# ROADMAP

## Phasenübersicht

1. **Technisches Fundament** — Struktur, Interaktion, Memory-Watch-Prototyp *(in Arbeit)*
2. **Flüsterwald-Vertical-Slice** — begehbare Testregion (M8)
3. **Interaktion und Memory Watch** — Ausbau der Kernmechanik (M4–M6)
4. **Kartografie** — Arens Karten als Mechanik (M9)
5. **Dialoge und Hannah** — Dialogsystem (M7)
6. **Inventar und Kleidung** — Ausrüstung statt Levelgrenzen
7. **Umweltgefahren und Biome** — Regionen-Gating (Frost, Hitze, Nebel …)
8. **Quests und Story** — Hauptquest, Erinnerungs-Journal
9. **Kampf** — bewusst spät; minimal und der Exploration untergeordnet
10. **Produktion weiterer Regionen** — Sonnenfelder, Nebelmoor, …
11. **Polishing und Veröffentlichung**

Die Sprints unten (M3–M12) sind die konkrete Umsetzung der Phasen 1–5.

---

Jeder Sprint ist klein, in sich abgeschlossen und endet mit einem
lauffähigen Spielstand. Kein Sprint beginnt, bevor der vorherige
committet und im Editor verifiziert wurde.

Leitfrage für jedes Feature:
**Unterstützt es Erkunden, Erinnerungen, Geschichte oder Atmosphäre?**
Wenn nein → wird es nicht gebaut.

---

## Milestone 0 — Pre Production

- Projektstruktur ✅ (bereinigt 22.07.2026, siehe DECISIONS.md ADR-002)
- Vision
- GDD
- Gameplay
- Worldbuilding

### Bereits umgesetzt (Engineering)

| Sprint | Inhalt |
|--------|--------|
| M1.x | Character Controller, WASD, Sprint, Dodge Roll, Kamera |
| M2.x | Aren-Modell integriert, Locomotion Blend Tree, Materialien |
| M2.3 | Struktur-Cleanup, Assembly Definition, Namespaces, Build Settings |

---

## Milestone 1 — Vertical Slice

Ziel: Dorf, Wald (Flüsterwald), Dungeon, Boss, Erinnerungsuhr —
erreicht über die folgenden kleinen Sprints.

### Phase 1 — Fundament

**M3 — Input-Refactor**
`PlayerMovement` pollt aktuell `Keyboard.current` / `Gamepad.current`
direkt. Umstellung auf das `InputSystem_Actions`-Asset.
- Neue Klasse `PlayerInputReader` (kapselt Input; Gameplay-Code kennt nur Events/Properties)
- Voraussetzung für Rebinding, Input-Sperren bei Dialogen, Memory-Watch-Taste
- *Definition of Done:* Bewegung/Sprint/Roll identisch wie vorher, kein direkter Device-Zugriff mehr im Gameplay-Code

**M4 — Interaktionssystem**
Das wichtigste System eines Exploration-Games.
- `IInteractable`-Interface + `InteractionDetector` am Spieler
- Interaktionsprompt-UI („[E] Untersuchen")
- Erste Interactables: Schild lesen, Objekt untersuchen
- *DoD:* Aren kann in der Szene ein Objekt untersuchen und Text sehen

**M5 — Game-State-Grundgerüst**
- `GameStateManager` (Playing, Dialogue, MemoryVision, Paused)
- Input-Routing pro State (baut auf M3 auf)
- *DoD:* Pause-Menü-Skelett; Dialog-/Vision-States blockieren Bewegung sauber

### Phase 2 — Identität

**M6 — Memory Watch v1 (Erinnerungsuhr)**
Kernmechanik, bewusst klein starten:
- Taste hält/toggelt die „Erinnerungssicht"
- URP Volume-Override (Entsättigung + Vignette) als Sichtwechsel
- `MemoryEcho`-Komponente: Objekte, die nur in der Erinnerungssicht
  ein-/ausblenden (verschwundene Brücke, alter Weg, Person der Vergangenheit)
- Designregel: Die Watch löst keine Rätsel — sie macht nur sichtbar
- *DoD:* Ein Testobjekt erscheint nur in der Erinnerungssicht

**M7 — Dialogsystem v1**
- ScriptableObject-basierte Dialogdaten (Sprecher, Zeilen, Keys für spätere Lokalisierung)
- Dialog-UI, Weiterklicken, Input-Sperre über GameState
- Hannah als erste Gesprächspartnerin
- *DoD:* Gespräch mit Hannah von Anfang bis Ende spielbar

**M8 — Flüsterwald Greybox**
- Szene `Fluesterwald.unity`; Bootstrap wird reiner Einstiegspunkt
- 3–4 zusammenhängende Bereiche, mind. 1 Geheimnis abseits des Weges
- 2–3 Memory Echoes + 2 Interactables als Test des Exploration-Loops
- *DoD:* 5 Minuten Erkundung ohne Platzhalter-Bugs

### Phase 3 — Loop schließen

**M9 — Kartografie v1**
Arens Beruf als Mechanik: Die Karte füllt sich durch Erkundung.
- Kartenbildschirm (UI), Aufdecken über besuchte Zonen (Trigger-Volumes)
- *DoD:* Karte des Flüsterwalds deckt sich beim Erkunden auf

**M10 — Save/Load v1**
- JSON-basiert, `ISaveable`-Interface; Position, besuchte Zonen, gelesene Dialoge
- *DoD:* Beenden → Neustarten → Weiterspielen funktioniert

**M11 — Atmosphäre-Pass Flüsterwald**
- Licht-Setup, Ambient-Audio-Zonen, erste Musik, Partikel (Staub/Blätter)
- *DoD:* Der Wald *fühlt* sich nach Flüsterwald an

**M12 — Dorf + Dungeon-Blockout**
- Dorf-Greybox mit 2–3 NPCs (Dialogsystem nutzen)
- Erster Mini-Dungeon: Rätsel um Memory Echoes, kein Kampf-Fokus
- *DoD:* Vertical-Slice-Rundgang Dorf → Wald → Dungeon spielbar

---

## Backlog (bewusst nicht terminiert)

- Erinnerungs-Journal (gesammelte Echoes nachlesen)
- Ausrüstungs-Gating (Winterkleidung, Atemmaske …) — erst mit Region 2
- Memory-Watch-Upgrades
- Kamera-Zonen (feste Kamerawinkel pro Bereich, Tunic-Stil)
- Lokalisierung DE/EN (Dialogsystem ab M7 Key-basiert vorbereiten)
- Boss-Encounter (Design zuerst — Kampf ist nicht der Fokus)

---

## Bekannte technische Schulden

| Thema | Ort | Plan |
|-------|-----|------|
| Direkter Device-Poll statt Input Actions | `PlayerMovement` | M3 |
| ~~Framerate-abhängiges `Lerp`~~ | `CameraFollow` | ✅ Erledigt (Orbit-Kamera mit `SmoothDamp`) |
| ~~Keine Gravitation am CharacterController~~ | `PlayerMovement` | ✅ Erledigt (Sprint „Wachsender Flüsterwald") |
| Mixamo-Kampf-Animationen ungenutzt | `Animation/Mixamo` | Behalten; vor Release ausmisten |
| `ArenLegacy`-Assets | `Art/Characters/ArenLegacy` | Löschen, sobald finaler Aren bestätigt |
