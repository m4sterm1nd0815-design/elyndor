# Gate-0 Manual Test Report

Stand: 2. August 2026
Gesamtstatus: `MANUELL OFFEN`

Gate 0 ist mit diesem Zwischenstand nicht geschlossen. Dieser Bericht
dokumentiert ausschließlich bereits manuell geprüfte Teilbereiche. Behobene
Input-Bindings müssen erneut im Unity Play Mode bestätigt werden.

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

## Input-Zwischenstand vor der Korrektur

- Quickslot benutzen mit `R`: **BESTANDEN**
- Quickslot-Direktwahl mit `1–8`: **NICHT BESTANDEN**
- Interaktion mit `E`: **NICHT BESTANDEN**
- Controller: **NICHT GETESTET**

Die automatisierte Korrektur ersetzt ungültige Zahlenreihen-Pfade durch
`digit1` bis `digit8`, entfernt die unbeabsichtigte Hold-Anforderung von
`Interact` und bindet den Finsterwald-Player explizit an das kanonische
Input-Actions-Asset. Der manuelle Wiederholungstest bleibt offen.

## Verbleibender Gate-0-Status

Weitere manuelle Gate-0-Prüfungen bleiben offen. Dieser Zwischenstand ist
keine vollständige Gate-Freigabe; `TASK_QUEUE.md` bleibt am Gate.
