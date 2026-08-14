---
Project: Echoes of Elyndor
Version: 1.0
Status: Active
Owner: Lars Becker
Last Updated: 14.08.2026
---

# GAME DESIGN DOCUMENT

Stand: 14. August 2026
Produktionsziel: Finsterwald Vertical Slice

Dieses Dokument ist das **detaillierte Game Design** in der
Dokumentenhierarchie (siehe `docs/README.md`). Es lag zuvor doppelt vor:
inhaltlich unter `docs/GAME_DESIGN_DOCUMENT.md`, als leeres Gerüst hier. Der
Inhalt wurde hierher migriert; der alte Pfad verweist nur noch hierauf.

Der Erzählkanon steht in `docs/02_Story/StoryBible.md`, die kreative Verfassung
in `docs/PROJECT_BIBLE.md`, die technische Wahrheit in `docs/ARCHITECTURE.md`.

## Spielversprechen

`Echoes of Elyndor` ist ein storygetriebenes Top-Down-Action-Adventure mit
offener Erkundung. Der Spieler soll denken: „Da hinten koennte noch etwas
sein.“ Die Welt ist der Hauptcharakter; Neugier, Beobachtung, Bewegung und
Entdeckung kommen vor Erklaertext.

## Designpfeiler

1. Exploration First — Blickfuehrung, Landmarken und Abkuerzungen statt
   Icon-Checklisten.
2. Memories are Gameplay — Erinnerungen veraendern Verstehen und Handeln.
3. Every Puzzle tells a Story — Mechanik entsteht aus einem glaubwuerdigen Ort.
4. Reward Curiosity — Abweichungen liefern Bedeutung, nicht Fuellmaterial.
5. Respect the Player — keine kuenstliche Spielzeit, Fetch-Quests oder
   unverstaendliche Loesungen.

## Zielerlebnis des Vertical Slice

Ein 30- bis 45-minuetiger Ablauf im Finsterwald:

1. Erwachen und erste Orientierung mit Link.
2. Bewegung, Kamera und stille Weltbeobachtung lernen.
3. Eine Spur abseits der Hauptroute entdecken.
4. Erste Waffe finden und ihre Bedeutung verstehen.
5. Einen einfachen Gegnertyp lesen und bekaempfen.
6. Die Memory Watch an einem klar signalisierten Ort einsetzen.
7. Ein ortsgebundenes Erinnerungsraetsel loesen.
8. Eine Memory Site aktivieren und Lore ueber Handlung erfahren.
9. Eine sichtbare Regionsveraenderung ausloesen.
10. Mit neuer Route, Abkuerzung oder Erkenntnis zum Abschluss gelangen.

## Kernsysteme

| System | Ziel | Belegter Stand auf `developer` |
|---|---|---|
| Bewegung | gerichtete Bewegung, Sprint, Rolle, Gravitation | Prototyp vorhanden; Input zentralisiert |
| Kamera | lesbare Top-Down-Fuehrung, Terrain-Schutz | Follow-/Orbit-Prototyp vorhanden |
| Interaktion | ein klares Ziel, Controller und Tastatur | eventbasiert vorhanden |
| Memory Watch | Spuren und vergangene Zustaende sichtbar machen | Memory Site, Echo, Sitzung und VFX-Prototyp vorhanden |
| Kampf | leichte/schwere Angriffe, Blocken, lesbare Treffer | Prototyp und Trainingsdummy vorhanden |
| Ausdauer | Bewegung und Kampf sinnvoll begrenzen | angebunden: Sprint, Rolle, Angriff und Block verbrauchen Ausdauer; Werte vorlaeufig |
| Inventar | gefundene Gegenstaende und Ausruestung | statischer Prototyp vorhanden |
| Quickslots | acht Slots, Auswahl und Nutzung | HUD-Anbindung integriert; Eingabe in PR #7 |
| HUD | Leben, Ausdauer, Erinnerung, Fokus | Foundation, Polish und Binding integriert |
| Quests | Veraenderung durch Geschichte statt Fetch-Aufgaben | einfache Quest-Prototypen vorhanden |
| Regionenreise | verbundene Szenen und Ankunftspunkte | Portale/Spawns fuer drei Szenen vorhanden |
| Save/Load | persistente Welt-, Inventar- und Storyzustaende | noch nicht vorhanden |

## Bewegung und Kamera

- Controller First, Maus/Tastatur vollstaendig unterstuetzt.
- Bewegung muss unmittelbar, vorhersehbar und unabhaengig von der Framerate
  wirken.
- Sprint und Rolle verbrauchen echte Ausdauer; keine parallele Scheinressource.
- Kamera priorisiert Spieler, bevorstehende Route und Gefahrenlesbarkeit.
- Baumkronen-/Terrainverdeckung wird durch Silhouette und Kameraschutz geloest.
- `OFFEN`: finale Geschwindigkeiten, Roll-Unverwundbarkeit und Lock-on.

## Kampf

- Keine HP-Schwaemme; Gegner werden ueber Verhalten und klare Fenster gelernt.
- Erste Grundlage: leichte und schwere Angriffe, Blocken, Schaden und Reaktion.
- Waffen werden in der Welt gefunden, behalten und aufgewertet; keine
  Wegwerfwaffen-Schleife.
- Der erste Gegnertyp braucht Telegraphing, Trefferreaktion, Niederlage und
  nachvollziehbaren Weltbezug.
- `NOCH ZU ENTSCHEIDEN`: Tod/Respawn, Heilungswirtschaft, Lock-on und Parieren.

## Memory-Watch-Gameplay

- Kein Zeitreisen.
- Ein Einsatz beginnt mit Weltbeobachtung und einem dezenten Leuchten plus
  nicht-farblichem Signal.
- Die Watch zeigt eine Spur oder einen vergangenen Zustand; der Spieler zieht
  selbst den Schluss und fuehrt die Handlung aus.
- Der erste Vertical-Slice-Einsatz bleibt auf eine klar begrenzte Faehigkeit
  beschraenkt.
- Belohnung ist eine Kombination aus Weg, Weltveraenderung und Erkenntnis.

## Erkundung, Welt und Quests

- Hauptroute ist ohne Minimap-Zwang lesbar; Nebenpfade werden durch Form, Licht,
  Klang und Kontrast angeboten.
- Jede bedeutende Abzweigung beantwortet oder stellt eine Frage.
- Quests veraendern sichtbar einen Ort, NPC-Zustand oder Zugang.
- Sammelobjekte sind nur erlaubt, wenn jedes Exemplar erzahlerischen oder
  spielerischen Wert besitzt.
- Regionen regenerieren sich sichtbar; Art und Umfang pro Region sind im
  Arbeitspaket festzulegen.

## UI und Barrierefreiheit

- Minimalistisches, kontextsensitives HUD.
- Gold = wichtig, Tuerkis = Erinnerung, Rot = Gefahr, Gruen = Natur,
  Violett = Magie; kein Zustand nur ueber Farbe.
- Sichtbarer Controller-/Tastaturfokus, keine Navigationssackgassen.
- UI-Skalierung 75–175 %, grosse Schrift, hoher Kontrast und reduzierte Bewegung.
- Memory-Watch-Bereitschaft dezent, aber eindeutig und zugaenglich anzeigen.

## Audio und visuelle Sprache

- Ruhe ist ein Gestaltungsmittel; Musik startet bewusst.
- Wind, Wasser, Tiere und regionale Details tragen Orientierung.
- Handgemachte, warme, mystische Fantasy zwischen Comic und Realismus.
- Jede Region braucht eigene Palette, Silhouetten, Materialfamilie und Klangwelt.

## Progression und Oekonomie

- Keine Klasse; Fortschritt ueber Waffen, Relikte, Runen, Erinnerungen und
  Faehigkeiten.
- Herzen statt abstrakter Lebensleiste sind das Zielbild; der aktuelle HUD-
  Prototyp nutzt noch normalisierte Vitalwerte.
- Leichtes Crafting ist grundsaetzlich vorgesehen, aber fuer den Vertical Slice
  `NICHT FREIGEGEBEN`.
- Keine Mikrotransaktionen, Lootboxen, Battle Passes oder Pay-to-Win-Systeme.

## Vertical-Slice-Abnahme

- 30–45 Minuten ohne Editor-Eingriff spielbar.
- Vollstaendiger Controller- und Maus-/Tastaturdurchlauf.
- Keine Compilerfehler, fehlenden Scripts oder blockierenden Navigationsfehler.
- Kernroute, Nebenfund, Kampf, Watch-Raetsel, Memory Site und Weltveraenderung
  funktionieren in einem zusammenhaengenden Ablauf.
- Visuelle, akustische und Performance-Abnahme gemaess Gate 1.
