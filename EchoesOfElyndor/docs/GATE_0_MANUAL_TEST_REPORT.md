# Gate-0 Manual Test Report

Stand: 2. August 2026
Gesamtstatus: **ABGESCHLOSSEN**

Gate 0 wurde nach Abschluss aller vorgeschriebenen manuellen Prüfungen
freigegeben. Verbleibende Qualitäts- und Integrationspunkte sind als
nicht blockierende Phase-1-Aufgaben dokumentiert.

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

## Portalreise Finsterwald → Sonnenfelder

Status: **BESTANDEN**

Geprüft:

- Der unsichtbare Portaltrigger wurde gefunden.
- Der Hinweis `Weiterreisen` erscheint.
- Die Interaktion mit `E` startet die Regionsreise.
- Sonnenfelder wird erfolgreich geladen.

## Portalreise Sonnenfelder → Nebelmoor

Status: **BESTANDEN**

Geprüft:

- Der Portaltrigger kann gefunden werden.
- Der Hinweis `Weiterreisen` erscheint.
- Die Interaktion mit `E` startet die Regionsreise.
- Nebelmoor wird erfolgreich geladen.

## Gesamtbewertung der Portalreisen

Status: **TECHNISCH FUNKTIONSFÄHIG**

Die folgenden Qualitätsmängel blockieren Gate 0 nicht, werden jedoch als
priorisierte Phase-1-Aufgabe festgehalten:

- Spielerführung und Sichtbarkeit der Portalpunkte sind unzureichend.
- Ohne Kenntnis der Triggerpositionen sind die Übergänge schwer auffindbar.
- Das tatsächliche Reiseportal im Finsterwald ist unsichtbar.
- Das sichtbare Root Gate ist nur Dekoration und liegt weit vom Reiseportal
  entfernt.
- Der Finsterwald-Triggerbereich ist durch eine Birke teilweise blockiert.

## Regionsrundgänge

### Finsterwald

Status: **BESTANDEN**

Der vorgeschriebene manuelle Rundgang wurde abgeschlossen.

### Sonnenfelder

Status: **BESTANDEN**

Der vorgeschriebene manuelle Rundgang wurde abgeschlossen.

### Nebelmoor

Status: **BESTANDEN**

Geprüft:

- Bewegung funktioniert.
- Die Kamera folgt korrekt.
- Das HUD bleibt sichtbar.
- Es bestehen keine offensichtlichen Laufwegblockaden.
- Die Unity-Konsole bleibt sauber.

Offener Qualitätsmangel:

- Die Nebeldichte ist aktuell sehr hoch und schränkt die Sicht deutlich ein.
- Die Nebelabstimmung wird als priorisierte Phase-1-Polishing-Aufgabe geführt.
- Der Qualitätsmangel blockiert Gate 0 nicht.

## Gate-0-Abschluss

Status: **ABGESCHLOSSEN**

Alle vorgeschriebenen manuellen Gate-0-Prüfungen sind abgeschlossen.

Zusätzliche offene, nicht blockierende Punkte für Phase 1:

- Controller-Bindings wurden noch nicht manuell getestet.
- Der Ausdauerverbrauch ist noch nicht an das Gameplay angebunden.
- Die Portalpunkte sind technisch funktionsfähig, aber schlecht sichtbar und
  ohne Kenntnis ihrer Triggerpositionen schwer auffindbar.
- Der Nebel im Nebelmoor ist aktuell zu stark.

Phase 1 ist damit freigegeben, wurde mit diesem Dokumentationspaket jedoch
noch nicht begonnen.
