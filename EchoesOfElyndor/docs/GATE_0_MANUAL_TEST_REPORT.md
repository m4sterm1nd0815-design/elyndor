# Gate-0 Manual Test Report

Stand: 2. August 2026
Gesamtstatus: `MANUELL OFFEN`

Gate 0 ist mit diesem Zwischenstand nicht geschlossen. Dieser Bericht
dokumentiert ausschließlich bereits manuell geprüfte Teilbereiche. Die
korrigierte Quickslot-Direktwahl muss erneut im Unity Play Mode bestätigt
werden.

## Finsterwald Root Gate

Status: **BESTANDEN**

Geprüft:

- Durchgang durch den Schleier möglich.
- Seitliche Kollisionen funktionieren.
- Keine Laufwegblockade.
- Kamera funktioniert ohne auffälliges Clipping.
- Größe und Platzierung passen.
- Material wird korrekt dargestellt.
- Farbwirkung passt grundsätzlich zum Finsterwald.

Hinweis: Das Material wirkt sehr dunkel, ist aktuell jedoch kein Blocker.

## Aktueller manueller Input- und HUD-Zwischenstand

- Interaktion mit `E`: **BESTANDEN**
- Interaktionshinweis: **BESTANDEN**
- Quickslot benutzen mit `R`: **BESTANDEN**
- Quickslot-Direktwahl mit `1–8`: **NICHT BESTANDEN**
- Controller: **NICHT GETESTET**

## Technische Korrektur der Quickslot-Direktwahl

Die vorhandenen Input-Actions und `performed`-Callbacks bleiben erhalten.
Zusätzlich wertet der zentrale `PlayerInputReader` die obere Zahlenreihe und
den Nummernblock direkt über `Keyboard.current` aus. Action-Callback und
Keyboard-Fallback werden pro Frame zusammengeführt, sodass ein Tastendruck
höchstens eine Auswahl erzeugt.

Ein automatisierter Play-Mode-Test bestätigt für Slot 1 bis 8:

- Tastendruck wird erkannt;
- `QuickslotInputController` erreicht `SelectSlot`;
- `QuickslotRuntimeInventory.SelectedIndex` wird gesetzt;
- `SelectionChanged` wird exakt einmal ausgelöst;
- `QuickslotBarPresenter` aktualisiert den sichtbaren Slot.

Der Test läuft getrennt für die obere Zahlenreihe und den Nummernblock.
Der manuelle Wiederholungstest bleibt erforderlich.

## Verbleibender Gate-0-Status

Die Quickslot-Direktwahl und der Controller bleiben manuell offen. Dieser
Zwischenstand ist keine vollständige Gate-Freigabe; `TASK_QUEUE.md` bleibt am
Gate.
