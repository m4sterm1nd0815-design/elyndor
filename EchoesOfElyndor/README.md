# Echoes of Elyndor

> *Not everything that is forgotten is lost.
> But everything that is lost begins with being forgotten.*

---

## Vision

Echoes of Elyndor ist ein storygetriebenes
Top-Down-Action-Adventure.

Der Spieler erlebt Erinnerungen vergangener Ereignisse,
um eine sterbende Welt zu verstehen.

Dieses Repository enthält die komplette Entwicklung
des Spiels.

---

## Technik

- Unity 6000.4.5f1 (Unity 6) · URP · New Input System
- Startszene: `Assets/_Elyndor/Scenes/Bootstrap.unity`

## Projektstruktur

```
Assets/_Elyndor/          Alles Projekt-Eigene (Fremd-Assets bleiben außerhalb)
  Animation/              Animator Controller + Roh-Animationen (Mixamo)
  Art/Characters/         Modelle, Texturen, Materialien pro Charakter
  Input/                  Input-Actions-Asset
  Scenes/                 Bootstrap + Regionsszenen
  Scripts/                Runtime-Code (Elyndor.Runtime.asmdef)
    Player/               Bewegung, Player State
    Camera/               Kamera-Logik
Docs/                     GDD, Worldbuilding, Roadmap, Architektur-Entscheidungen
```

## Dokumentation

- [Roadmap](Docs/Roadmap.md) — Sprints, Definition of Done, technische Schulden
- [Architektur-Entscheidungen](Docs/DECISIONS.md) — ADR-Log
- [Game Design Document](Docs/00_Project/GAME_DESIGN_DOCUMENT.md)

## Arbeitsweise

Kleine, lauffähige Sprints. Jede größere Änderung wird in
`Docs/DECISIONS.md` bzw. der Roadmap dokumentiert.

Designfilter für jedes Feature:
**Unterstützt es Erkunden, Erinnerungen, Geschichte oder Atmosphäre?**
