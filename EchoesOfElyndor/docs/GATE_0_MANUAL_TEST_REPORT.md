# Gate-0 Manual Test Report

Stand: 2. August 2026
Gesamtstatus: `MANUELL OFFEN`

Gate 0 ist mit diesem Zwischenstand nicht geschlossen. Dieser Bericht
dokumentiert ausschließlich bereits manuell geprüfte Teilbereiche.

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
- Quickslot-Direktwahl über die obere Zahlenreihe `1–8`: **BESTANDEN**
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

Der Test läuft getrennt für die obere Zahlenreihe und den Nummernblock. Die
Quickslot-Direktwahl über die obere Zahlenreihe wurde zusätzlich manuell
bestätigt.

## Manueller HUD- und Kampfprototyp-Test

Status: **TEILWEISE BESTANDEN**

Bestanden:

- HUD ist sichtbar.
- Lebensanzeige ist sichtbar.
- Ausdaueranzeige ist sichtbar.
- Erinnerungsanzeige ist sichtbar.
- Angriff auf die Kampfpuppe funktioniert.
- Die Kampfpuppe reagiert sichtbar auf Treffer.
- Die Unity-Konsole bleibt während des Tests sauber.

Offen / nicht bestanden:

- Die Ausdauer verändert sich beim Sprinten oder Springen nicht.
- Die Gameplay-Anbindung des Ausdauerverbrauchs fehlt offenbar noch.

## Memory Site / Brückenmechanik

Status: **BESTANDEN**

Geprüft:

- Interaktion funktioniert.
- Der Bildschirm wird sichtbar abgedunkelt.
- Die Brücke erscheint korrekt.
- Die Mechanik wird im Play Mode ausgelöst.
- Es wurden keine blockierenden Fehler beobachtet.

## Portalinteraktion Finsterwald → Sonnenfelder

Status: **NICHT BESTANDEN**

Beobachtet:

- Am Regionsportal erscheint kein Interaktionshinweis.
- Die Interaktion mit `E` bleibt am Portal ohne Wirkung.
- Die Reise von Finsterwald nach Sonnenfelder konnte nicht durchgeführt werden.

## Verbleibender Gate-0-Status

Die Controller-Bindings wurden noch nicht manuell getestet. Zusätzlich bleiben
folgende vorgeschriebenen manuellen Gate-0-Prüfungen offen:

- Gameplay-Anbindung des Ausdauerverbrauchs;
- Portalreisen;
- Rundgang durch Finsterwald;
- Rundgang durch Sonnenfelder;
- Rundgang durch Nebelmoor;

Dieser Zwischenstand ist keine vollständige Gate-Freigabe; `TASK_QUEUE.md`
bleibt am Gate.
