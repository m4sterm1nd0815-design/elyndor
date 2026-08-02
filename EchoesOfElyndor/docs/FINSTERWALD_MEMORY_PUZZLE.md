# Finsterwald Memory-Watch-Raetsel: Die geteilte Bruecke

Stand: 2. August 2026
Status: `VORSCHLAG`, Umsetzung `BLOCKIERT BIS GATE-0-FREIGABE`

## Spielversprechen

Das erste Raetsel nutzt die bestehende zerstoerte Bruecke und
`MemorySite_AlteBruecke`. Die Watch zeigt einen perspektivischen vergangenen
Zustand. Sie bewegt nichts durch die Zeit, rekonstruiert keine Materie und gibt
keine Bedienfolge vor. Der Spieler liest Echo und Gegenwart und veraendert nur
heutige Objekte.

## Ausgangslage

- Das Brueckendeck fehlt; zwei Brueckenreste stehen an den Ufern.
- Ein schwerer gefallener Stamm liegt oberhalb des Bachs, wird aber von alten
  Seilresten und drei drehbaren Ankersteinen gehalten.
- Am suedlichen Ufer steht ein leerer Seilbock; am Nordufer ist eine intakte
  Aufnahme sichtbar.
- Die Furt ist wegen starker Stroemung vorerst keine sichere Umgehung.
- Die Watch signalisiert Einsatzbereitschaft erst innerhalb des Ortskontexts.

## Beobachtbarer vergangener Zustand

Bei Aktivierung erscheint fuer begrenzte Zeit eine immaterielle,
unvollstaendige Echoebene:

- die fruehere Bruecke als tuerkise Kanten und Fussabdruecke;
- drei Arbeiter-Silhouetten ohne erkennbare Identitaet;
- eine Person prueft nacheinander die Spannung der drei Anker, doch Beginn und
  Ende der Handlung fehlen;
- eine Seillinie laeuft nicht direkt ueber den Bach, sondern zuerst um einen
  Seitenstein und zum heutigen gefallenen Stamm;
- ein kleines Kartenzeichen markiert den trockenen Norduferpunkt.

Die Szene ist eine Spur/Perspektive, keine objektive Aufzeichnung.

## Hinweise

1. Gegenwart: unterschiedliche Kerben an den drei Ankern (eine, zwei, drei).
2. Echo: Belastung laeuft von tief nach hoch, aber die erste Silhouette fehlt.
3. Akustik: korrekt gespannter Anker antwortet mit einem klaren Holzton;
   falscher mit dumpfem Steinreiben.
4. Umwelt: abgeschabte Rinde am Stamm zeigt seine vorgesehene Drehrichtung.
5. Link: landet optional auf dem Seitenstein und blickt zur Seillinie, niemals
   auf die fertige Reihenfolge.

Kein Hinweis ist ausschliesslich farbcodiert.

## Interaktionen

- Watch innerhalb der Resonanzzone aktivieren/deaktivieren.
- Jeden Ankerstein zwischen drei stabilen Stellungen drehen.
- Seilbock pruefen; er meldet Spannung, solange die Ankerstellung plausibel ist.
- Stammfreigabe betaetigen, wenn der Spieler seine Konfiguration testen will.
- Nach erfolgreichem Abrollen den Stamm mit zwei vorhandenen Brueckenbohlen
  sichern und ueberqueren.

## Falsche, aber nachvollziehbare Ansaetze

- Kerben numerisch 1-2-3 statt nach Lastverlauf einstellen: Seil spannt kurz,
  Stamm verkantet, alles setzt reversibel zurueck.
- Echo-Bruecke direkt betreten: sie bleibt immateriell; ein klarer Ton und
  Fussdurchscheinen erklaeren die Grenze ohne Schaden.
- Stamm ohne Seitenstein freigeben: er dreht zurueck in die Ausgangslage.
- Nur dem Link-Sitzpunkt folgen: dort wird die Seillinie sichtbar, aber zwei
  Anker bleiben unbestimmt.
- Furt versuchen: Stroemung und Aren-Animation signalisieren Gefahr; kein
  unsichtbarer Todesrand. `OFFEN`: schadensloses Zuruecksetzen oder
  Ausdauerschwelle.

## Loesung

Der Spieler kombiniert Kerben, Echo-Lastverlauf und Rindenspur:

1. Seitenanker auf Stellung mit zwei Kerben;
2. suedlichen Tiefanker auf eine Kerbe;
3. Nordanker auf drei Kerben;
4. Seilbock pruefen und Stamm freigeben;
5. Stamm rollt kontrolliert in die Brueckenaufnahmen;
6. zwei lose Bohlen sichern den Uebergang.

Die konkrete Reihenfolge ist `VORSCHLAG` und muss im Usability-Test gegen
zufaelliges Durchprobieren validiert werden. Entscheidend ist: Die Watch zeigt
nur Beziehungen; der Spieler fuehrt die Schlussfolgerung aus.

## UI- und Accessibility-Signal

- dezenter tuerkiser Puls am Watch-Rand, maximal alle 2,5 s;
- gleichzeitig ein kleines, formbasiertes Echo-Symbol und leiser Zweiklang;
- Signal wird staerker, wenn Aren Echo und relevantes Gegenwartsobjekt im Blick
  hat, nicht wenn er eine fertige Loesung auswaehlt;
- reduzierte Bewegung ersetzt Puls durch weichen Helligkeitswechsel;
- hoher Kontrast und Lautstaerke getrennt einstellbar;
- kein permanenter Zielmarker und kein Text `Benutze die Watch hier`.

## Technische Zustandsmaschine

| Zustand | Eintritt | Erlaubte Aktionen | Austritt |
|---|---|---|---|
| `Dormant` | Szene geladen, Raetsel ungeloest | Beobachten | Resonanzzone betreten -> `WatchAvailable` |
| `WatchAvailable` | Spieler in Zone | Watch aktivieren, Welt untersuchen | Watch -> `EchoObserved`; Zone verlassen -> `Dormant` |
| `EchoObserved` | Echo mindestens einmal gesehen | Anker drehen, Echo erneut ansehen | erster Anker -> `Configuring` |
| `Configuring` | mindestens ein Anker veraendert | drehen, pruefen, reversibel testen | falscher Test -> `Recovering`; korrekt -> `ReadyToRelease` |
| `Recovering` | Stamm verkantet | keine Eingabe fuer 1,2 s | Ausgangsposition -> `Configuring` |
| `ReadyToRelease` | korrekte Konfiguration | Stamm freigeben | Animation -> `BridgeDeploying` |
| `BridgeDeploying` | Freigabe | Eingabe lokal gesperrt, Kamera bleibt kurz | Stamm eingerastet -> `Securing` |
| `Securing` | Stamm liegt | zwei Bohlen interagieren | beide gesetzt -> `Solved` |
| `Solved` | Bruecke sicher | ueberqueren, Memory Site nutzen | persistent |

Zustands-ID: `finsterwald_bridge_memory_puzzle_v1`. Jeder Zustand schreibt nur
bei stabilen Uebergaengen. Laden waehrend einer Animation setzt auf den letzten
stabilen Zustand zurueck.

## Erforderliche Trigger, Prefabs und Daten

- `MemoryPuzzleController` mit datengesteuerter Zustandsdefinition;
- `WatchResonanceZone` und bestehender InteractionDetector-Anschluss;
- drei `MemoryAnchor`-Prefabs mit stabiler ID und drei Stellungen;
- `RopeTensionRig`, `FallenLogBridge`, zwei `BridgePlank`-Interaktionen;
- Echo-Prefab auf Basis von `MemoryEcho`, getrennt von Kollisionsgeometrie;
- `WatchAvailabilityPresenter`, Audio-/VFX-Profil und Accessibility-Varianten;
- `WorldStateFlag`/Save-Daten fuer Raetselzustand und fertige Bruecke;
- Cinemachine ist nicht belegt; Kamerabetonung muss mit bestehender
  `CameraFollow`-Architektur oder einem kleinen, lose gekoppelten Impuls erfolgen.

## Testfaelle

1. Watch ausserhalb der Zone: kein Bereitschaftssignal.
2. Signal mit Farbe deaktiviert: Form und Ton bleiben eindeutig.
3. Echo mehrfach aktivieren: keine doppelten Objekte oder Zustandsfortschritte.
4. Jede falsche Ankerkonfiguration: reversibel, kein Softlock.
5. 27 Kombinationen: genau eine akzeptierte Konfiguration.
6. Stammfreigabe waehrend Echo/Fade: deterministischer Zustand.
7. Szene verlassen/laden in jedem stabilen Zustand: korrekte Wiederherstellung.
8. Save waehrend `BridgeDeploying`: Rueckkehr zu `ReadyToRelease` oder atomar
   `Securing`, niemals Zwischengeometrie.
9. Geloeste Bruecke: Collider, NavMesh und Kamera funktionieren; Echo bleibt
   optional betrachtbar.
10. Controller und Maus/Tastatur: alle Interaktionen ohne Fokusfalle.
11. Link deaktiviert/ausser Sicht: Raetsel bleibt vollstaendig loesbar.
12. Drei neue Testspieler: Median 6-9 Minuten; mindestens zwei begruenden die
   Loesung, ohne zufaellig alle Kombinationen zu testen.
