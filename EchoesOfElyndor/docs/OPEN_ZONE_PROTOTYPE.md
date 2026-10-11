# Open Zone – Finsterwald-Probe

Stand: 11. Oktober 2026. Status: **Maussteuerung ergänzt; visuelle und manuelle Abnahme offen**.

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

Die Probe nutzt einen kontextabhängigen Linksklick: auf freien Terrainboden
klicken, um dorthin zu laufen; auf einen Gegner klicken, um sich zu nähern und
in Reichweite einen leichten Angriff auszuführen. Der Spieler muss dafür eine
Waffe in der Haupthand tragen. Rechtsklick bewegt weiterhin die Kamera,
WASD und Gamepad bleiben als zusätzliche Eingaben erhalten. Eine WASD-Eingabe
bricht das aktuelle Klickziel ab. UI-Klicks lösen keine Bewegung aus.

Diese erste Eingabeprobe steuert direkt auf das Ziel zu. Automatische
Wegfindung um Hindernisse, Kontextinteraktion per Klick und wiederholte
Angriffe bei gedrückter Maustaste sind noch offen. An Bäumen, Felsen und
Ruinen kann die Figur deshalb hängen bleiben; das ist ein Prüfbefund für
den nächsten Ausbau, kein fertiges Sacred-Steuerungssystem.

Der Südteil enthält zwei gleichzeitig erreichbare Alternativen vom Start:
westlich zur Lichtung mit dem vorhandenen Wurzelstreifer, östlich zum Rastplatz.
Eine Waldschleife verbindet beide. Das Brückenrätsel bleibt nördlich davon;
keiner der neuen Wege quert den Bach. Die neuen Wegpunkte liegen unsichtbar unter
`[Prototype] Open Zone Routes`; sie dienen der Lageprüfung, nicht als UI-Marker.

Beim Spielen dieser Szene verwendet der Editor einen flüchtigen Spielstand.
Der vorhandene Fortschritt wird weder geladen noch beschrieben. Beim Stoppen
der Probe geht der dabei erreichte Fortschritt verloren. Die Testszene darf
nicht in die Build Settings aufgenommen werden; ein Build-Check verhindert das.

## Prüfrunde

1. Vom Start beide Wege getrennt erkunden: erst Westen, dann Osten; anschließend
   umgekehrt. Auf der Waldschleife ohne Umweg zum gemeinsamen Rastbereich
   zurückkehren.
2. Auf beiden Wegen Kamera, Baumkronen, Bodenhöhe und erkennbare Landmarken
   beurteilen. Insbesondere prüfen, ob der höhere Blick Gegnerangriffe und
   Interaktionsziele noch gut lesbar lässt.
3. Wurzelstreifer, Rucksack, HUD, Portale und vorhandene Memory-Watch-Brücke
   testen. Die Brücke darf nicht von einem neuen Weg umgangen werden.
   Zusätzlich auf freie Wegstellen und auf den Wurzelstreifer klicken:
   Bewegung und genau ein Angriff nach Annäherung prüfen. Danach mit WASD
   das Klickziel abbrechen und mit Rechtsklick die Kamera drehen.
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
- Edit-Mode-Tests: Open-Zone-Probe 2/2, bestehende Speicher-Tests 17/17.
  `git diff --check` ist grün. Die Originalszene und ihre TerrainData
  zeigen keine Änderungen im Branch.
- Nach Ergänzung der Maussteuerung: Live-Editor-Kompilierung, Szenenvalidator,
  Open-Zone-Edit-Mode-Tests 2/2 und Play-Start ohne Konsolenfehler. Die
  tatsächlichen Klickwege bleiben manuell zu prüfen.
- Ein frischer Import des unveränderten `com.unity.shadergraph` 17.4.0
  meldete unter diesem Unity-Editor zwei `GUID`-Kompilierfehler. Für die
  lokale Validierung wurde ausschließlich die ignorierte `Library/PackageCache`
  korrigiert; diese Korrektur ist nicht Teil des Branches. Ein sauberer
  Checkout muss vor der Produktionsübernahme mit einer kompatiblen
  Unity-/ShaderGraph-Kombination erneut kompiliert werden.
