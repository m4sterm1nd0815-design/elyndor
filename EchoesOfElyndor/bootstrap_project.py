from pathlib import Path
from datetime import date

# ==========================================================
# Echoes of Elyndor
# Bootstrap Project
# Version 0.0.1
# ==========================================================

ROOT = Path(".")

TODAY = date.today().strftime("%d.%m.%Y")

HEADER = f"""---
Project: Echoes of Elyndor
Version: 0.0.1
Status: Draft
Owner: Lars Becker
Last Updated: {TODAY}
---

"""

FILES = {

"README.md": f"""# Echoes of Elyndor

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

""",

"docs/00_Project/PROJECT_CHARTER.md": HEADER + """

# PROJECT CHARTER

## Vision

Ein emotionales Action Adventure erschaffen,
das Spieler noch Jahre später in Erinnerung behalten.

---

## Mission

Wir entwickeln kein möglichst großes Spiel.

Wir entwickeln ein möglichst gutes Spiel.

---

## Designregel Nr. 1

Jede Entscheidung muss das Spielerlebnis verbessern.
Nicht unser Ego.

""",

"docs/00_Project/GAME_DESIGN_DOCUMENT.md": HEADER + """

# GAME DESIGN DOCUMENT

Dieses Dokument bildet die Grundlage
für das gesamte Projekt.

Alle Mechaniken,
Systeme,
Rätsel,
Items,
Waffen
und Dungeons
werden hier definiert.

""",

"docs/00_Project/CORE_PILLARS.md": HEADER + """

# CORE PILLARS

## 1 Exploration First

Der Spieler soll ständig Neues entdecken.

---

## 2 Memories are Gameplay

Erinnerungen sind Spielmechanik.

---

## 3 Every Puzzle tells a Story

Kein Rätsel existiert grundlos.

---

## 4 Reward Curiosity

Neugier wird immer belohnt.

---

## 5 Respect the Player

Keine künstliche Spielzeit.
Keine unfairen Rätsel.

""",

"docs/00_Project/DEVELOPER_DIARY.md": HEADER + f"""

# Developer Diary

## Entry 001

Datum:
{TODAY}

Heute begann offiziell
die Entwicklung von Echoes of Elyndor.

Beschlossen:

- Erinnerungsuhr
- Herzsystem
- Waffen finden & verbessern
- Erinnerungsrätsel
- HUD Resonanzsystem
- Fokus auf Exploration

Status:

🟢 Pre Production gestartet.

""",

"docs/04_Gameplay/MEMORY_WATCH.md": HEADER + """

# MEMORY WATCH

Die Erinnerungsuhr ist die zentrale Spielmechanik.

Sie erlaubt KEINE Zeitreise.

Sie macht Erinnerungen sichtbar.

---

## Funktionen

- Rätsel
- Erkundung
- Story
- Bossmechaniken
- Geheimnisse

---

## HUD

Normal:

⚪ Grau

Erinnerung erkannt:

🟡 Pulsieren

Je näher der Spieler kommt:

✨ stärkeres Leuchten

---

## Designregel

Die Uhr zeigt Möglichkeiten.

Nie Lösungen.

""",

"docs/Roadmap.md": HEADER + """

# ROADMAP

## Milestone 0

Pre Production

- Projektstruktur
- Vision
- GDD
- Gameplay
- Worldbuilding

---

## Milestone 1

Vertical Slice

- Dorf
- Wald
- Dungeon
- Boss
- Erinnerungsuhr

"""

}

print()
print("="*50)
print(" Echoes of Elyndor Bootstrap")
print("="*50)
print()

created = 0
skipped = 0

for filename, content in FILES.items():

    path = ROOT / filename

    path.parent.mkdir(parents=True, exist_ok=True)

    if path.exists():
        skipped += 1
        print(f"SKIP  {filename}")
        continue

    path.write_text(content, encoding="utf-8")
    created += 1
    print(f"CREATE {filename}")

print()
print("="*50)
print(f"Created : {created}")
print(f"Skipped : {skipped}")
print("="*50)
print()
print("Bootstrap complete.")