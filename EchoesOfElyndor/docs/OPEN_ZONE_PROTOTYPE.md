# Open Zone – Finsterwald-Probe

Stand: 11. Oktober 2026. Status: **Zone verbunden und Maussteuerung ergänzt;
visuelle und manuelle Abnahme offen**.

Die aktuelle Entscheidung „Sacred trifft Zelda“ betrifft zunächst den Spielfluss:
frei zwischen Zielen wählen, kämpfen, die Memory Watch verwenden und über
Rückwege erkunden. Die bisherigen Systeme, Figuren und Erzählregeln gelten weiter.
Das erste Experiment prüft Kamera und räumliche Wahl auf dem vorhandenen
Finsterwald-Terrain. Es trifft keine Entscheidung über Klassen, Loot oder die
endgültige Größe weiterer Regionen.

## Probe öffnen

In diesem Branch liegt die separate Szene unter
`Assets/_Elyndor/Prototypes/OpenZone/Finsterwald_OpenZone.unity`.
Ist sie noch nicht vorhanden, im Unity-Menü
`Elyndor > Open Zone > Create or Open Finsterwald Prototype` ausführen.
Ein weiterer Aufruf öffnet nur die bestehende Probe; er überschreibt keine
nachträgliche Handarbeit. Die originale `Finsterwald.unity` und deren TerrainData
werden nicht gespeichert oder verändert.

Die Kamera blickt zunächst aus etwa 21 Metern bei 60 Grad auf Aren und erlaubt
einen begrenzten Orbit von 50–70 Grad sowie Zoom von 16–27 Metern. Diese Werte
sind ausdrücklich Testwerte. Mit Tastatur/Maus und Controller vergleichen.

Die Probe nutzt die linke Maustaste kontextabhängig: auf freien Terrainboden
klicken, um dorthin zu laufen; die Taste gedrückt halten, damit Aren dem Cursor
laufend folgt; auf einen Gegner klicken oder gedrückt halten, um sich zu nähern
und in Reichweite leicht anzugreifen. Der Spieler muss dafür eine Waffe in der
Haupthand tragen. Interaktionsobjekte lassen sich ebenfalls anklicken: Aren
nähert sich und löst die Interaktion in Reichweite einmal aus. Ein türkisfarbener
Zielring zeigt das aktuelle Laufziel, ein roter Ring das Angriffsziel und ein
goldener Ring ein Interaktionsziel. Aren bremst innerhalb der letzten drei
Meter vor einem Laufziel weich ab und richtet sich an seiner tatsächlichen
Bewegungsrichtung aus. Rechtsklick bewegt weiterhin die Kamera. Die
Maus-Vorschau markiert anwählbare Gegner rot und Interaktionsobjekte gold,
bevor geklickt wird. Hinweise in Reichweite nennen in dieser Probe
„Linksklick“ statt der alten E-Taste. Die
Tastaturbewegung über WASD und Pfeiltasten ist in dieser Probe abgeschaltet;
das Gamepad bleibt als alternative Eingabe erhalten. UI-Klicks lösen keine
Bewegung aus. Das alte WASD-Starttutorial ist in der Probe deaktiviert.

Diese erste Eingabeprobe steuert direkt auf das Ziel zu. Automatische
Wegfindung über längere Strecken ist noch offen. Eine lokale Hindernisprobe
lenkt Aren vor Bäumen, Felsen und Ruinen
seitlich um den unmittelbar erkannten Collider. Sie ersetzt kein NavMesh und
kann in Sackgassen oder komplexen Ruinen weiterhin scheitern.

Der Südteil enthält zwei gleichzeitig erreichbare Alternativen vom Start:
westlich zur Lichtung mit dem vorhandenen Wurzelstreifer, östlich zum Rastplatz.
Eine Waldschleife verbindet beide und ein eigener Zubringer führt zur Brücke.
Hinter der Brücke verzweigt die Zone zum vergessenen Schrein und Tor, über den
Höhenweg sowie zum Steinkreis und Aussichtspunkt. Das Brückenrätsel bleibt der
vorgesehene Zugang in den Norden; keiner der südlichen Wege umgeht den Bach.
Auf dem Höhenweg wartet nach der Antwort des Waldes ein Paar vorhandener
Wurzelstreifer. Vor gelöster Brücke und gesehener Memory Site ist diese zweite
Begegnung inaktiv. So bleibt der einzelne Gegner auf der Lichtung der sichere
Einstieg, während der Norden den Kampf mit zwei versetzt reagierenden Gegnern
prüft.
Die neuen Wegpunkte liegen unsichtbar unter `[Prototype] Open Zone Routes`; sie
dienen der Lageprüfung, nicht als UI-Marker.

Beim Spielen dieser Szene verwendet der Editor einen flüchtigen Spielstand.
Der vorhandene Fortschritt wird weder geladen noch beschrieben. Beim Stoppen
der Probe geht der dabei erreichte Fortschritt verloren. Die Testszene darf
nicht in die Build Settings aufgenommen werden; ein Build-Check verhindert das.

## Prüfrunde

1. Vom Start beide Südwege getrennt erkunden: erst Westen, dann Osten;
   anschließend umgekehrt. Auf der Waldschleife ohne Umweg zum gemeinsamen
   Rastbereich zurückkehren und danach dem Zubringer zur Brücke folgen.
2. Auf beiden Wegen Kamera, Baumkronen, Bodenhöhe und erkennbare Landmarken
   beurteilen. Insbesondere prüfen, ob der höhere Blick Gegnerangriffe und
   Interaktionsziele noch gut lesbar lässt.
3. Wurzelstreifer, Rucksack, HUD, Portale und vorhandene Memory-Watch-Brücke
   testen. Die Brücke darf nicht von einem neuen Weg umgangen werden. Nach dem
   Öffnen alle drei Nordrouten bis Schrein/Tor, Höhenweg und
   Steinkreis/Aussichtspunkt ablaufen.
   Zusätzlich auf freie Wegstellen und auf den Wurzelstreifer klicken:
   Bewegung und Angriffe nach Annäherung prüfen. Mit Rechtsklick die Kamera
   drehen und sicherstellen, dass WASD die Figur nicht bewegt.
4. Play stoppen und die Originalszene öffnen. Dort müssen Kamera, Terrain,
   Objekte und echter Spielstand unverändert geblieben sein.
5. Für die spätere Freigabe zusätzlich Playthrough mit Maus/Tastatur und
   Controller sowie Performanceprofil mit benannter Hardware dokumentieren.

**Manuell offen:** Wegbreite und Kampfgefühl, visuelle Qualität, Controller,
Rätselherleitbarkeit, Save-Abnahme über zwei echte Neustarts im Originalspiel.
Die Probe ersetzt die bestehende Gate-1-Abnahme nicht.

## Technischer Stand

- Unity 6000.4.5f1 hat die Probe im Batch-Modus erzeugt und die
  Szenenintegrität bestätigt. 150 Vegetationsobjekte wurden aus den
  Wegkorridoren verschoben; das Terrain bleibt 200 × 200 Meter groß.
- Edit-Mode-Tests: Open-Zone-Probe 7/7, bestehende Speicher-Tests 17/17.
  `git diff --check` ist grün. Die Originalszene und ihre TerrainData
  zeigen keine Änderungen im Branch.
- Nach Ergänzung der Maussteuerung: Live-Editor-Kompilierung, Szenenvalidator,
  Open-Zone-Edit-Mode-Tests 2/2 und Play-Start ohne Konsolenfehler. Die
  tatsächlichen Klickwege bleiben manuell zu prüfen.
- Nach dem Zonenausbau: sieben Wegabschnitte automatisch auf Gelände und feste
  Hindernisse abgetastet. Ergebnis: 0 blockierte Prüfpunkte und keine Steigung
  über 40 Grad. Die Ostroute wurde dabei um die kleine Ruine herumgeführt.
  Szenenvalidator, Open-Zone-Edit-Mode-Tests 7/7 und Play-Start bleiben grün;
  die Unity-Konsole meldet 0 Fehler und 0 Warnungen.
- Die lokale Hindernisumgehung ist mit einem direkten Collider vor Aren
  abgesichert: Die resultierende Bewegung bleibt vorwärts gerichtet und erhält
  einen seitlichen Anteil, statt weiter in das Hindernis zu drücken.
- Die Kontextinteraktion ist gegen doppeltes Auslösen abgesichert: In Reichweite
  wird genau eine Aktion ausgeführt, bis die linke Maustaste losgelassen wurde.
- Maus-Vorschau und Interaktionshinweis verwenden dieselben Farben und Begriffe
  wie die Klicksteuerung; ein Test schützt den Hinweis „[Linksklick]“.
- Die Szene enthält 39 aktive Interaktionsziele. Der Editor-Audit bestätigt,
  dass jedes davon mindestens einen aktivierten Collider für Mausklicks besitzt.
- Der Nordkampf enthält zwei weitere Instanzen des vorhandenen Wurzelstreifers.
  Ein Gate-Test bestätigt, dass sie vor der Regeneration inaktiv bleiben und
  anschließend gemeinsam freigeschaltet werden.
- Ein frischer Import des unveränderten `com.unity.shadergraph` 17.4.0
  meldete unter diesem Unity-Editor zwei `GUID`-Kompilierfehler. Für die
  lokale Validierung wurde ausschließlich die ignorierte `Library/PackageCache`
  korrigiert; diese Korrektur ist nicht Teil des Branches. Ein sauberer
  Checkout muss vor der Produktionsübernahme mit einer kompatiblen
  Unity-/ShaderGraph-Kombination erneut kompiliert werden.
