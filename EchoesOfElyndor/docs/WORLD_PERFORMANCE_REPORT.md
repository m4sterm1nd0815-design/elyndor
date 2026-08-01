# World Performance Report

## Gemeinsame Maßnahmen

- Der Visual-Layer ist idempotent und ersetzt nur seine eigene Root-Gruppe.
- Statische Geometrie wird für Batching, Occlusion und Occludee markiert.
- Zusätzliche Echtzeitlichter sind auf kleine, schattenlose Ruhelichter begrenzt.
- Partikel nutzen bestehende begrenzte Builder-Konfigurationen.
- Light Probes liegen in einem groben regionalen Raster statt pro Objekt.
- Gameplay-Roots, Terrain-Daten und bestehende Vegetationsbudgets bleiben
  unverändert.

## Finsterwald

Höchstes Risiko durch etwa 1.700 bestehende GameObjects und dichte Vegetation.
Der neue Layer ergänzt nur kleine Landmarkengruppen. Vor Art-Lock sind echte
LODGroups für Baum- und Ruinenmodelle sowie Occlusion-Bakes erforderlich.

## Sonnenfelder

Offene Sichtachsen erhöhen die gleichzeitig sichtbare Objektzahl. Orchard und
Bewässerung bleiben bewusst klein. Später Terrain-Details und GPU-Instancing
statt vieler einzelner Halme verwenden.

## Nebelmoor

Nebel reduziert Fernsicht, transparente Wasser- und Partikelflächen können
aber Overdraw erzeugen. Steg und Landmarken sind opak; Irrlichter bleiben
zahlenmäßig begrenzt.

## Messpflicht vor Release

CPU/GPU Frame Debugger, Rendering Debugger, Profiler und Memory Profiler auf
Zielhardware verwenden. Die aktuelle Prüfung ist strukturell, keine belastbare
Frametime-Zertifizierung.
