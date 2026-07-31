# Kostenloser Asset-Katalog fuer Echoes of Elyndor

Stand: 31.07.2026

## Lizenzregel

Fuer die automatische Vorauswahl werden nur Assets mit klar ausgewiesener
CC0-Lizenz verwendet. Andere kostenlose Assets kommen erst nach einer
separaten Lizenzpruefung ins Projekt.

Wegen der bereits grossen Repository-Groesse werden Packs nicht komplett
importiert. Pro Region werden nur benoetigte Modelle, Texturen und Audiodateien
uebernommen. Texturen standardmaessig 1K oder 2K, ausser ein Hero-Asset braucht
nachweislich mehr.

## Prioritaet A – naechste Ausbaustufe

| Asset | Einsatz | Umfang | Lizenz | Quelle |
|---|---|---:|---|---|
| Quaternius Ultimate Stylized Nature Pack | Finsterwald, Sonnenfelder, Nebelmoor; Baeume, Felsen, Pflanzen | 63 Modelle | CC0 | https://quaternius.com/packs/ultimatestylizednature.html |
| Quaternius Medieval Village Pack | Erstes Dorf, Hofstellen, Wegpunkte | 44 Modelle | CC0 | https://quaternius.com/packs/medievalvillage.html |
| Kenney Fantasy UI Borders | Dialog-, Journal-, Karten- und Inventarrahmen | 140 Dateien | CC0 | https://kenney.nl/assets/fantasy-ui-borders |
| Kenney Game Icons | Eingabehinweise und Controller-Prompts | 105 Dateien | CC0 | https://kenney.nl/assets/game-icons |
| Kenney Cartography Pack | Arens Kartografie-System und Kartenlegende | 85 Dateien | CC0 | https://kenney.nl/assets/cartography-pack |
| Kenney Particle Pack | Memory-Echos, Staub, Funken, magische Spuren | 80 Dateien | CC0 | https://kenney.nl/assets/particle-pack |

## Prioritaet B – Figuren und Weltbelebung

| Asset | Einsatz | Umfang | Lizenz | Quelle |
|---|---|---:|---|---|
| Quaternius RPG Character Pack | Platzhalter und Nebenfiguren | 6 animierte Figuren | CC0 | https://quaternius.com/packs/rpgcharacters.html |
| Quaternius Ultimate Animated Animal Pack | Wald- und Dorfleben | 12 Tiere, je 12+ Animationen | CC0 | https://quaternius.com/packs/ultimateanimatedanimals.html |
| Quaternius Ultimate Monsters | Gegner-Prototypen und spaetere Regionen | 50 animierte Monster | CC0 | https://quaternius.com/packs/ultimatemonsters.html |
| Quaternius Ultimate Modular Ruins Pack | Tempel, Erinnerungsruinen, Dungeon-Aussenbereiche | 90 Modelle | CC0 | https://quaternius.com/packs/ultimatemodularruins.html |

## Materialien und Beleuchtung

Poly Haven stellt HDRIs, PBR-Texturen und Modelle unter CC0 bereit:
https://polyhaven.com/license

Vorgabe fuer Elyndor:

- nur gezielt einzelne Oberflaechen laden;
- bevorzugt 1K/2K statt 8K;
- keine kompletten Bibliotheken einchecken;
- Originalquelle und Assetname in einer lokalen LICENSE-Datei dokumentieren.

## Audio-Kandidaten

Die folgenden OpenGameArt-Eintraege weisen CC0 aus und eignen sich als
Platzhalter beziehungsweise Ausgangsmaterial:

- Sunset Plains – ruhige Erkundung/Sonnenfelder:
  https://opengameart.org/content/sunset-plains
- Global Resonance – leise, mystische Hintergrundflaeche:
  https://opengameart.org/content/global-resonance
- Dark Cavern Ambient – Dungeon-/Hoehlenatmosphaere:
  https://opengameart.org/content/dark-cavern-ambient
- Dark Place (loop) – dunkle Erinnerungsorte:
  https://opengameart.org/content/dark-place-loop

Vor einem finalen Release wird jede verwendete Audiodatei nochmals einzeln
gegen ihre Quellseite und beigefuegte Lizenzdatei geprueft.

## Import-Reihenfolge

1. Input-Refactor abschliessen und in Unity testen.
2. UI-Basis aus Fantasy UI Borders + Game Icons importieren.
3. Dorf-Blockout mit Medieval Village Pack erstellen.
4. Kartografie-Prototyp mit Cartography Pack bauen.
5. Natur nur regionsweise erweitern.
6. Tiere und Monster erst einbauen, wenn ihr Gameplay feststeht.
7. Audio als eigenes, kleines Paket mit Lizenzdatei importieren.

## Nicht automatisch importieren

- Packs mit unklarer oder gemischter Lizenz.
- Kostenlose Assets, die nur fuer nicht-kommerzielle Nutzung freigegeben sind.
- Vollstaendige 4K/8K-Bibliotheken.
- Mehrere visuell widerspruechliche Low-Poly-Stile gleichzeitig.
- Assets ohne erkennbare Originalquelle.
