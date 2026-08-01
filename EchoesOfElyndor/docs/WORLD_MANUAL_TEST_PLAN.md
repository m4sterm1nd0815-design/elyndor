# World Manual Test Plan

## Automatische Vorbereitung

1. `Elyndor > World Visuals > Build Current Regions` ausführen.
2. `Elyndor > World Visuals > Validate Current Regions` ausführen.
3. Console auf Missing Scripts, Materialfehler und doppelte zentrale
   Komponenten prüfen.

## Play-Mode je Region

- Start und Ankunftsspawns funktionieren.
- Hauptweg ist ohne Kollision mit Dekoration begehbar.
- Zwei Nebenwege und Rückweg sind visuell lesbar.
- Portale laden das korrekte Ziel und setzen den korrekten Spawn.
- Memory Sites lassen sich einmal aktivieren und bleiben während der Sitzung aktiv.
- Kamera wird nicht von neuen Bögen, Ruinen oder Sitzsteinen blockiert.
- Ruhelicht, Partikel und Nebel verdecken weder Aren noch Interaktionshinweise.
- D-Pad, HUD, Kampf und Input bleiben unverändert.

## Visuelle Abnahme

- Screenshot vom Startblick, der Hauptlandmarke, dem Memory-Site-Anlauf und
  einem Nebenpfad in 1920×1080 und 3840×2160.
- Game View bei niedriger und hoher Kamera prüfen.
- Frame Debugger auf Materialduplikate und transparente Überzeichnung prüfen.
- Profiler-Rundgang von mindestens fünf Minuten je Region auf Zielhardware.
