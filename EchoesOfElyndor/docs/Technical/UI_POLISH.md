# UI Polish Sprint

## Änderungen

- fast schwarze halbtransparente Flächen
- deutlich zurückhaltendere Goldrahmen
- stärkere Trennung zwischen Leben, Ausdauer und Erinnerung
- einfache Glyphen für Herz, Ausdauer und Erinnerung
- cyanfarbener Controller-/Tastaturfokus
- reduzierte Fokusanimation bei aktivierter Bewegungsreduktion
- UI-Skalierungsbridge für 75–175 Prozent
- QA-Menü für HUD, Quickslots und EventSystem

## Unity-Menüs

- `Elyndor > UI > Rebuild Polished HUD Foundation`
- `Elyndor > QA > Validate Polished HUD`

## Test

1. Altes HUD-Objekt aus der Szene löschen.
2. Polished HUD neu erzeugen.
3. Prefab unter `ElyndorUI` ziehen.
4. Rect Transform auf vollständiges Stretch mit Offsets 0.
5. Play starten.
6. Per Tab oder Pfeiltasten einen Quickslot auswählen.
7. QA ausführen.
8. Screenshot-Baselines erstellen.

## Noch offen

- echte finale Icon-Sprites
- Gameplay-Anbindung der Quickslots
- Tooltips
- Control-Scheme-spezifische Glyphen
- Herzcontainer statt reiner Lebensleiste
