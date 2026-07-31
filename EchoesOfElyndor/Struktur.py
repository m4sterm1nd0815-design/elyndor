from pathlib import Path

# ======================================================
# Echoes of Elyndor - Projektstruktur Generator
# ======================================================

PROJECT_NAME = "Echos-of-Elyndor"

root = Path(PROJECT_NAME)

# -----------------------------
# Ordnerstruktur
# -----------------------------

folders = [
    "docs",
    "docs/00_Project",
    "docs/01_World",
    "docs/02_Story",
    "docs/03_Characters",
    "docs/04_Gameplay",
    "docs/05_Items",
    "docs/06_Dungeons",
    "docs/07_Enemies",
    "docs/08_UI",
    "docs/09_Audio",
    "docs/10_Unity",

    "unity",
    "concept-art",
    "references",
    "music",
    "sounds",
    "logo",
    "trailers",
]

# -----------------------------
# Dateien
# -----------------------------

files = {
    "README.md": """# Echoes of Elyndor

Ein storygetriebenes Top-Down-Action-Adventure.

## Vision

- Zelda-Inspiration
- Handgemalter Grafikstil
- Erinnerungsuhr als Kernmechanik
- Emotionale Geschichte
- Rätsel & Erkundung
""",

    ".gitignore": """# Unity
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
UserSettings/

# VS Code
.vscode/

# Python
__pycache__/
*.pyc

# OS
.DS_Store
Thumbs.db
""",

    "docs/Roadmap.md": "# Roadmap\n\n",

    "docs/00_Project/Vision.md": "# Vision\n",
    "docs/00_Project/CorePillars.md": "# Core Pillars\n",
    "docs/00_Project/DeveloperDiary.md": "# Developer Diary\n",

    "docs/01_World/WorldBible.md": "# World Bible\n",
    "docs/01_World/Lore.md": "# Lore\n",

    "docs/02_Story/StoryBible.md": "# Story Bible\n",
    "docs/02_Story/MainQuest.md": "# Main Quest\n",
    "docs/02_Story/SideQuests.md": "# Side Quests\n",

    "docs/03_Characters/Hero.md": "# Hero\n",
    "docs/03_Characters/NPCs.md": "# NPCs\n",
    "docs/03_Characters/Enemies.md": "# Characters\n",

    "docs/04_Gameplay/Combat.md": "# Combat\n",
    "docs/04_Gameplay/Puzzles.md": "# Puzzle System\n",
    "docs/04_Gameplay/Weapons.md": "# Weapons\n",
    "docs/04_Gameplay/Items.md": "# Items\n",
    "docs/04_Gameplay/MemoryWatch.md": "# Memory Watch\n",

    "docs/05_Items/LegendaryWeapons.md": "# Legendary Weapons\n",
    "docs/05_Items/Collectibles.md": "# Collectibles\n",

    "docs/06_Dungeons/DungeonOverview.md": "# Dungeon Overview\n",

    "docs/07_Enemies/EnemyList.md": "# Enemy List\n",
    "docs/07_Enemies/Bosses.md": "# Bosses\n",

    "docs/08_UI/UIConcept.md": "# UI Concept\n",

    "docs/09_Audio/Music.md": "# Music\n",
    "docs/09_Audio/SFX.md": "# Sound Effects\n",

    "docs/10_Unity/Architecture.md": "# Unity Architecture\n",
}

# -----------------------------
# Ordner erstellen
# -----------------------------

print("Erstelle Ordner...")

for folder in folders:
    (root / folder).mkdir(parents=True, exist_ok=True)

# -----------------------------
# Dateien erstellen
# -----------------------------

print("Erstelle Dateien...")

for filename, content in files.items():
    path = root / filename
    path.parent.mkdir(parents=True, exist_ok=True)

    if not path.exists():
        path.write_text(content, encoding="utf-8")

print("\n===================================")
print(" Echoes of Elyndor Projekt erstellt")
print("===================================")
print(root.resolve())