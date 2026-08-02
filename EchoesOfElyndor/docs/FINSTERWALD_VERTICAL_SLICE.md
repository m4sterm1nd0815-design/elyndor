# Finsterwald Vertical Slice

Stand: 2. August 2026
Status: `UMSETZUNGSBEREIT VORBEREITET`, Produktion `BLOCKIERT BIS GATE-0-FREIGABE`

## Ziel und Leitplanken

Der Slice ist ein zusammenhaengender erster Spielabschnitt von 30 bis 45
Minuten. Er beweist Exploration, lesbaren Kampf, ortsgebundenes
Memory-Watch-Gameplay, stille Lore und eine sichtbare Heilung des Waldes. Die
vorhandene Szene, Gameplay-Roots, Portale, Spawns, IDs und `[Handarbeit]`
bleiben bis zur Gate-0-Freigabe unangetastet.

Verbindliche Grenzen:

- keine Zeitreise und keine objektive Videoaufzeichnung;
- Link beobachtet und lenkt Aufmerksamkeit, loest aber nichts;
- keine Fetch-Quest, Checkliste oder Minimap-Pflicht;
- nur die erste Watch-Funktion: einen begrenzten vergangenen Ortszustand
  sichtbar machen;
- Soren und Elian werden nur angedeutet; offene Kanonfragen bleiben offen;
- die Sonnenfelder sind im Slice kein Produktionsziel.

## Startzustand

- Aren erwacht am bestehenden suedlichen Startpunkt im Finsterwald ohne
  Erinnerungen und ohne verstandene Aufgabe.
- Die Memory Watch ist an ihn gebunden, aber noch nicht bewusst einsetzbar.
- Link ist in Sicht- oder Rufweite Arens einziger Begleiter. Kommunikationsform
  und Geschlecht bleiben `OFFEN`; der Slice nutzt deshalb nur Flug, Blick,
  Sitzposition und kurze Rufe.
- Der beschaedigte Rucksack liegt nahe der Aufwachstelle. Sein altes Schwert
  wird zum bleibenden Weltfund, nicht zur Wegwerfwaffe.
- HUD, Inventar und Quickslots beginnen minimal; echte Startressourcen sind
  `VORSCHLAG` und werden erst im Balancing-Paket festgelegt.
- Waldzustand `finsterwald_unruhig`: dichter Nebel, matter Bach, geschlossene
  Wurzelpassage und schlafende Regenerationscluster.

## Endzustand

- Aren hat Bewegung, Ausdauer, Interaktion, leichte/schwere Angriffe, Block,
  Quickslot-Nutzung und den ersten Watch-Einsatz praktisch gelernt.
- Die alte Bruecke ist durch gegenwaertige Handlungen passierbar; kein Objekt
  wurde aus der Vergangenheit geholt.
- `MemorySite_AlteBruecke` ist aktiviert und als Checkpoint gespeichert.
- Zwei normale Gegnerrollen und ein staerkerer Abschluss-Encounter wurden
  gelesen und bewaeltigt.
- Waldzustand `finsterwald_erste_regeneration`: klarerer Bach, geoeffnete
  Blueten/Saplings, weniger lokaler Nebel, waermeres Licht und eine geoeffnete
  Route durch das Root Gate.
- Aren weiss, dass die Watch auf ihn reagiert und dass er den Ort vermutlich
  kannte. Er weiss noch nicht, wer seine Erinnerungen loeschte oder wo Elian ist.
- Der Spieler erreicht den Aussichtspunkt hinter dem Prozessionstor. Ein
  kurzer Abschlussblick verspricht die Rueckkehr in den Finsterwald; kein
  automatischer Uebergang in eine neue Region erfolgt.

## Zeitlicher Spielerablauf

| Zeit | Abschnitt | Verbindlicher Ablauf | Tutorial und Ziel |
|---:|---|---|---|
| 0-4 Min. | Erwachen | Aren richtet sich auf, Link landet nahe dem Rucksack. Freie Kamera- und Bewegungsprobe im geschuetzten Startbereich. | Nur Bewegen, Kamera, Interaktion. Keine Textwand. |
| 4-8 Min. | Rucksack und Schwert | Rucksack untersuchen, Schwert dauerhaft ausruesten, leere Seiten als Hinweis auf Aren als Kartograf wahrnehmen. | Inventar einmalig, leichter Angriff an sicherem Ziel; Quickslot-Hinweis erst bei echter Ressource. |
| 8-12 Min. | Root Gate und Rastplatz | Hauptweg durch die bestehende Tutorial Threshold/Root-Gate-Silhouette. Der Rastplatz zeigt, dass jemand nicht zurueckkehrte. | Sprint und Ausdauer; Kamera/Silhouette durch Baumkronen. |
| 12-16 Min. | Lichtung | Link nutzt erstmals einen bestehenden Perch und beobachtet den Weg. Ein einzelner Wurzelstreifer lehrt Wahrnehmung, Telegraphing, Treffer und Ausweichen. | Rolle, Block und Trefferreaktion gestaffelt statt gleichzeitig erklaert. |
| 16-21 Min. | Abzweigung | Hauptweg fuehrt zur zerstoerten Bruecke. Die optionale westliche Spur fuehrt ueber Licht, Blumen und Link-Ruf zum vergessenen Schrein. | Exploration wird belohnt, nicht verlangt. |
| 21-29 Min. | Watch-Raetsel | Am Brueckenansatz leuchtet die Watch dezent mit Symbol/Puls. Der vergangene Zustand zeigt intakte Laufspuren, Seilfuehrung und die Stellung dreier Anker. Spieler untersucht Gegenwart, richtet Anker und loest den vorhandenen Stammsteg. | Watch zeigt Belege, keine Bedienfolge. Falsche Versuche bleiben reversibel. |
| 29-33 Min. | Memory Site | Nach dem Uebergang wird die alte Memory Site aktiviert. Ein Echo zeigt mehrere anonyme Haende beim Reparieren und eine Kartenskizze mit Arens Markierung. | Memory-Ressource, Site/Checkpoint und Lore durch Handlung. |
| 33-38 Min. | Regeneration | Aktivierung und reparierter Wasserlauf loesen die erste sichtbare Regeneration aus. Der neue Kontrast oeffnet eine lesbare Route zum Forgotten Processional Gate. | Weltzustand sichtbar und spaeter persistent. |
| 38-43 Min. | Abschluss-Encounter | Zwei Gegnerrollen bereiten den Namenlosen Hueter vor. Der Hueter prueft Abstand, Block/Ausweichen und schwere Angriffsfenster; kein HP-Schwamm. | Kombination der erlernten Systeme, keine neue Kernregel. |
| 43-45 Min. | Regionsabschluss | Der Hueter beruhigt sich oder zerfaellt in harmlose Erinnerungspartikel. Root Gate/Aussicht werden frei; Link landet voraus. Abschlussbild und kurzer Lore-Satz. | Ruhe nach Spannung, kein Cliffhanger-Infodump. |

Zielzeit: 36-41 Minuten auf dem Hauptpfad, 41-45 Minuten mit der
Nebenentdeckung. Unter 30 Minuten gilt als zu direkt, ueber 45 Minuten ohne
freiwillige Erkundung als zu lang.

## Hauptpfad

Startbereich -> Rucksack -> Tutorial Threshold/Root Gate -> verlassener
Rastplatz -> kleine Lichtung -> zerstoerte Bruecke -> Watch-Raetsel -> alte
Memory Site -> regenerierter Nordpfad -> Forgotten Processional Gate ->
Aussichtspunkt.

Die vorhandene Furt bleibt als Rueckweg-/Entdeckungsreserve erhalten, darf aber
das erste Raetsel nicht trivial umgehen. `VORSCHLAG`: vor der Raetselloesung
ist sie durch zu starke Stroemung lesbar, aber nicht unsichtbar gesperrt; nach
der Regeneration wird sie zur Abkuerzung.

## Optionale Nebenentdeckung

Der westliche Pfad zur Lichtung, zum Uralten Waechter und zum vergessenen
Schrein dauert 4-6 Minuten. Link setzt sich nur dann auf den zweiten Perch,
wenn der Spieler bereits abweicht. Belohnung:

- Umwelt-Lore ueber eine weiterhin gepflegte, namenlose Erinnerung;
- ein nichtverbrauchbares Kartenfragment oder eine kleine dauerhafte
  Memory-Kapazitaet ist `VORSCHLAG`;
- eine spaetere Abkuerzung zur Memory Site wird sichtbar, aber nicht sofort
  freigeschaltet.

Keine Belohnung ist fuer die Hauptloesung erforderlich.

## Tutorial-Taktung

1. Eine Eingabe wird erst gezeigt, wenn ihr Zweck sichtbar ist.
2. Maximal ein neuer Pflichtgedanke pro 60-90 Sekunden.
3. Bewegung vor Kampf, Kampf vor Watch, Watch vor Weltzustand.
4. Text verschwindet nach erfolgreicher Anwendung und kehrt bei Inaktivitaet
   einmal kontextuell zurueck.
5. Controller und Maus/Tastatur erhalten gleichwertige Prompts; kein Zustand
   wird nur ueber Farbe vermittelt.
6. Link-Rufe sind optionale Aufmerksamkeitssignale, niemals Anweisungen.

## Erster Einsatz von Link

`VORSCHLAG`: Nach dem Rucksack fliegt Link zum vorhandenen Perch der Lichtung,
blickt abwechselnd auf Aren und eine sichere Luecke im Unterholz und ruft
einmal. Im ersten Kampf wechselt Link auf einen hohen Ast und warnt nur durch
Koerperhaltung vor dem ersten sichtbaren Angriff. Link markiert weder Gegner
noch Loesungsobjekte und wiederholt das Signal nicht dauerhaft.

## Erste Lore-Enthuellungen

- Aren erkennt Kartenzeichen und fuehrt unbewusst eine korrekte Linie auf den
  leeren Seiten: seine Kartografenpraemisse wird gezeigt, nicht erklaert.
- Die Watch passt ausschliesslich auf seine Resonanz und traegt ein
  Herstellerzeichen. Dass es von Elian stammt, bleibt fuer Aren unklar.
- Eine ruhige, unbenannte Erinnerungsspur deutet an, dass Aren frueher nicht
  allein reiste. Die Zuordnung zu Soren bleibt `OFFEN`.
- Link kennt sichere Sitzpunkte und reagiert vertraut auf die Watch. Herkunft
  und Verbindung bleiben `OFFEN`.
- Der Wald erinnert sich nicht als Archiv, sondern in unvollstaendigen,
  perspektivischen Spuren.

## Sichtbare Regeneration

Die Veraenderung konzentriert sich auf Bruecke, Bach und Nordpfad, damit sie
vergleichbar bleibt:

- truebes Wasser klaert sich lokal und reflektiert waermeres Licht;
- bestehende Sapling-Cluster richten sich auf, einzelne Knospen oeffnen sich;
- Memory-Gluehwuermchen verlassen den Site-Ring und ziehen zum Root Gate;
- lokaler Nebel nimmt ab, Vogel- und Wasserklang kehren zurueck;
- eine Wurzelbarriere entspannt sich und gibt die Route frei;
- keine vollstaendige Heilung: tote Baeume, Ruinen und Schwarzer See bleiben.

Der Zustand braucht eine stabile ID und muss Save/Load sowie Szenenreload
ueberstehen.

## Realismuspruefung

- Die vorhandenen Ankerpunkte bilden den Ablauf raeumlich bereits ab.
- 36-41 Minuten Pflichtinhalt lassen 4-6 Minuten freiwillige Exploration zu.
- Nur ein vollstaendiges Raetsel, zwei normale Gegnerrollen und ein starker
  Encounter begrenzen den Scope.
- Vitals, HUD, Quickslots, Memory Site, Interaktion und Weltportal liefern
  Anknuepfpunkte, brauchen aber vor Nutzung die in den Arbeitspaketen genannten
  Integrationen.
- `OFFEN`: Zielplattform, FPS-Ziel, Tod/Respawn und finale Heilungswirtschaft.
