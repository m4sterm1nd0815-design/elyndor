# Gameplay Roadmap

Stand: 11. Oktober 2026

## Aktiver Umbau — Sacred trifft Zelda

Der vorhandene Finsterwald-Vertical-Slice bleibt die technische und erzählerische
Grundlage. Sein Ablauf wird in einer getrennten Open-Zone-Probe auf freiere
Erkundung und kontextabhängige Maussteuerung umgestellt.

1. **Zone und Eingabe — umgesetzt.** Südliche Wahlwege, Rückschleife,
   Brückenzubringer und drei Nordrouten; Linksklick für Bewegung, Kampf und
   Interaktion; WASD in der Probe deaktiviert.
2. **Begegnungsrhythmus — in Arbeit.** Ein Wurzelstreifer als Einstieg, danach
   Memory Watch und Brückenrätsel; nach der Waldregeneration wird ein
   Wurzelstreifer-Paar auf dem nördlichen Höhenweg aktiv.
3. **Vollständiger Durchlauf — als Nächstes.** Beide Südwege, Rätsel,
   Nordbegegnung und alle drei nördlichen Ziele ohne Editor-Eingriff spielen.
4. **Spielgefühl.** Mausbewegung, Animationen, Kampflesbarkeit und bei Bedarf
   längere Wegfindung anhand des Durchlaufs verbessern.
5. **Gate 1.** Erst nach visueller, spielerischer und technischer Abnahme wird
   entschieden, welche Änderungen in die Produktionsszene übernommen werden.

## Ziel

Die technische Grundlage wird zuerst konfliktfrei in `developer` gebuendelt.
Danach entsteht ein zusammenhaengender Finsterwald Vertical Slice. Einzelne
Prototypen gelten erst als fertig, wenn sie im Ablauf integriert und manuell
gespielt wurden.

## Historische Phase 0 — technische Grundlage

1. HUD Foundation und UI Polish — integriert.
2. HUD Gameplay Binding — integriert ueber PR #6.
3. Quickslot Input — Draft-PR #7; Integration und manueller Controllertest offen.
4. World Visual Overhaul/Root Gate — Draft-PR #9 auf aktuellem `developer`;
   Review, Aktualisierung nach PR #7 und Integration offen.
5. External Asset Pipeline — Draft-PR #8; Worktree sauber, Rebase auf aktuellen
   `developer`, Review und Lizenzpruefung offen.
6. Veraltete PRs und Branchbasen bereinigen.

## Phase 1 — Finsterwald Vertical Slice

### Spielerfuehrung und Spielgefuehl

- Start, Route, Abzweigung, Rueckweg und Abschluss als Flow definieren.
- Movement-Werte, Rolle, Beschleunigung und Kamera im echten Terrain abstimmen.
- Ausdauer aus `PlayerVitals` an Sprint, Rolle, Block und Angriffe anbinden.

### Kampfgrundlage

- Angriffs-/Block-Input in zentrale Inputarchitektur integrieren.
- Schaden, Trefferreaktion, kurze Unverwundbarkeit und Feedback klaeren.
- Ersten Gegnertyp mit lesbaren Zustaenden und Weltbezug implementieren.
- Erste Waffe als Weltfund und persistente Ausruestung integrieren.

### Memory Watch und Erzaehlung

- Dezentes zugaengliches Watch-Bereitschaftssignal.
- Erstes ortsgebundenes Raetsel: vergangener Zustand zeigt Spur, nicht Loesung.
- Erste Memory Site in den Ablauf integrieren.
- Link als anfaenglichen Begleiter integrieren.
- Aren, Soren und Elian nur im freigegebenen Umfang andeuten.

### Weltzustand, UI und Abschluss

- Eine sichtbare Regeneration mit stabilem Zustand.
- HUD/Quickslots auf echte Ressourcen und Fundgegenstaende umstellen.
- Regionales Audio, VFX, Licht und Materialien polieren.
- Vollstaendigen 30–45-Minuten-Ablauf testen.

## Nach Gate 1 — nicht freigegeben

- Sonnenfelder-Produktion.
- Nebelmoor-Produktion.
- Tal der verlorenen Wege.
- Ruinen von Arvenfall.
- Weitere Watch-Faehigkeiten, Gegnerfamilien, Dungeons und Kartografie.

## Technische Schulden mit Gameplaywirkung

- World-Visual-Branch enthaelt das integrierte HUD-Binding, muss aber nach
  spaeteren Integrationen wie PR #7 erneut aktualisiert und geprueft werden.
- `PlayerVitals` und Gameplayverbrauch sind noch nicht durchgaengig verbunden.
- Inventar und Memory-Zustand sind sitzungsbasiert, kein Save-System.
- Quickslot-Daten sind Prototyp-Laufzeitdaten, keine finale Itemdefinition.
- Herzcontainer-Zielbild und aktuelle Lebensleiste sind noch nicht vereinheitlicht.
- Builder enthalten duplizierte Terrain-/HUD-Helfer.
