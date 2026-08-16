---
Project: Echoes of Elyndor
Version: 0.0.2
Status: Active
Owner: Lars Becker
Last Updated: 22.07.2026
---



# CHANGELOG

## [Unreleased] — Der Wurzelstreifer hat aufgehört, ein Wolf zu sein

Auf der Kleinen Lichtung stand seit P1.5 ein Quaternius-Wolf. Er war nie die
Art Direction, sondern ein Maßband: an ihm sind Größe, Timing, Hitboxen und
Telegraph gemessen worden. Diese Messungen stehen im freigegebenen
Konzeptentwurf, und aus ihnen ist jetzt das Modell gebaut — im Haus in Blender,
über die MCP-Brücke, ohne einen einzigen der drei freigegebenen Meshy-Versuche.

- **Das Modell ist ein Skript, nicht eine Datei.**
  `Art_Source/Wurzelstreifer/build_wurzelstreifer.py` baut Rumpf, Kopf, Beine,
  Wurzelstränge, Rindenplatten und Bruchlinien aus den Zahlen des
  Konzeptentwurfs; die `.blend` ist sein Ergebnis. Damit steht jede Maßangabe
  als benannte Konstante da und nicht als Vertexposition, von der niemand mehr
  sagen kann, ob sie Absicht war. Fester Seed, zwei Läufe ergeben dasselbe Mesh.
- **Er lief zuerst rückwärts.** `axis_forward='-Z'` dreht Blenders +Y auf Unitys
  −Z, und −Z ist in Unity hinten. Das Modell importierte dabei vollkommen
  sauber: Wurzel gerade, Größe richtig, Pivot am Boden, Avatar gültig, alle elf
  Clips mit korrekter Dauer. Keine dieser Prüfungen schlug an. Figuren zeigen
  in der Quelldatei jetzt nach −Y, und der Validator misst die Blickrichtung
  seither an zwei benannten Knochen.
- **Die Rindenplatten rissen vom Körper ab, sobald er sich bewegte.** Sie
  liegen als eigene Schalen auf der Haut, und automatische Gewichte behandeln
  sie eigenständig — ein paar Millimeter weiter außen, also anders gewichtet.
  Beim ersten Krümmen des Rückens klaffte ein Loch. Die 423 Vertices dieser
  Schalen übernehmen jetzt die Gewichte des Hautvertex unter sich.
- **Der Rindenstaub hatte seit dem Blockout kein Material.** Ein per Skript
  angelegtes Partikelsystem kommt ohne auf die Welt, und ein leerer
  Materialslot rendert unter URP magenta — beim ersten Treffer, mitten im
  Kampf. Aufgefallen ist es, weil der Modellvalidator jetzt auch Prefabs mit
  Partikeln prüft.
- **Türkis leuchtet nur noch in den Rissen.** Die Bruchlinien haben einen
  eigenen Materialslot mit eingeschalteter Emission; am Rindenmaterial ist das
  Keyword aus, und URP ignoriert die Farbe aus dem Property Block dort
  vollständig. Damit tut `WurzelstreiferFeedback` endlich das, was die Art
  Direction verlangt, statt den ganzen Körper einzufärben.
- **50 Bilder je Sekunde, damit drei Zahlen stimmen.** Der Konzeptentwurf legt
  0,7 s Telegraph, 0,18 s Flinch und 0,8 s Straucheln fest. Bei 50 fps sind das
  glatt 35, 9 und 40 Bilder; bei 30 fps wäre der Flinch 5,4 Bilder lang und die
  Vorgabe schon beim Anlegen der Datei verfehlt. Gemessen kommen alle elf Clips
  auf die Hundertstelsekunde genau in Unity an.
- **Der offene Achsenpunkt der Pipeline ist zu.** Zweimal exportiert, zweimal
  gemessen: `bake_space_transform=True` lässt das Mesh auf (0,0,0) stehen,
  `False` verdreht es. Beide Varianten liefern gültigen Avatar und korrekte
  Clips — die Warnung, die Option ziehe bei Rigs die Räume auseinander, hat
  sich hier nicht bestätigt.
- **Der Validator kann jetzt Figuren.** Er suchte bisher nach `MeshFilter` und
  hätte bei jeder gehäuteten Figur „kein Mesh im Modell" gemeldet. Er prüft nun
  Knochenzahl, Wurzelknochen, Avatar, Clipnamen, Clipdauern und
  Blickrichtung — und misst die Maße an der Ruhepose statt an
  `Renderer.bounds`, die Unity für animierte Modelle bewusst zu groß auslegt.

**Blockout und fertiges Asset laufen durch dieselbe Aufbaumethode.** Die
gemessenen Kampfwerte — Kapselhöhe, Reichweite, Halteabstand, Höhe der
Lebensanzeige — stehen weiterhin an genau einer Stelle. Am Gameplay ist nichts
geändert.

**Die finale künstlerische Abnahme ist erteilt.** Am 16.08.2026 hat der Game
Director die In-Game-Darstellung mit „passt erstmal so" für den gegenwärtigen
Vertical-Slice-Stand freigegeben; Detailgrad, Materialfarben und Türkiston
brauchen dafür keine Änderung. Die Freigabe gilt dem Stand auf Merge-Commit
`08beca9` und ist eine menschliche Entscheidung — kein grünes Gate in diesem
Eintrag sagt etwas darüber, ob der Gegner gut aussieht. Was weiterhin offen
ist — Erinnerungsschliere, Audio, Texturstandard und der ungenutzte `Trab` —,
steht unverändert in `Technical/WURZELSTREIFER_ASSET.md`; die offene
LOD-Entscheidung in `FINSTERWALD_ASSET_REQUIREMENTS.md`.

## [Unreleased] — Der Stamm blieb im Bach, und schuld war das Speichern

Ein Mensch hat gemeldet: Trotz korrekt ausgeführter Schritte gibt das
Brückenrätsel den Stamm nicht frei, und der Seilbock meldet weiterhin, er
könne tragen. Am Rätsel lag es nicht. Es lag daran, dass ein **fortgesetztes**
Spiel bisher von keinem Test gefahren wurde — alle beginnen bei null.

- **Der Spielstand kam zu spät.** `SaveBootstrap` lief auf `AfterSceneLoad`,
  die Welt liest ihren Stand aber beim Aufbau: `BridgePuzzle` und
  `FinsterwaldRegeneration` im `Awake`. Der Fortschritt traf also ein, nachdem
  zwei der drei Leser ihn schon gelesen hatten. Nach jedem echten
  Programmstart stand die Brücke wieder auf Anfang, obwohl die Datei stimmte —
  und der nächste stabile Übergang schrieb den kleineren Stand obendrein
  zurück. Jetzt `BeforeSceneLoad`, und der Zeitpunkt ist als Test festgenagelt.
- **Ein gesehenes Echo blieb nicht gesehen.** Die Memory Site stellt sich als
  bereits benutzt wieder her und lässt sich richtigerweise kein zweites Mal
  aktivieren. Genau ihr Ereignis öffnet aber das Rätsel. Ein fortgesetztes
  Spiel stand damit für immer vor gesperrten Ankern und einer nicht
  bedienbaren Stammfreigabe — das ist der gemeldete Fehler. Führt der
  Spielstand das Echo, gilt der Schritt jetzt als vollzogen.
- **Die Ankersteine standen wieder auf Anfang.** Gespeichert wurde nur der
  Zustandsname, und `ReadyToRelease` *bedeutet* „die Anker stehen richtig".
  Die Stellungen gehören jetzt zum Spielstand — abgelegt nach Ankerkennung,
  nicht nach Reihenfolge im Feld. Ein alter Stand ohne sie verstellt keinen
  Stein und darf keine Spannung mehr behaupten: er fällt auf `Configuring`.
- **Die fertige Brücke meldete sich bei jedem Start neu.** Die Lösungsmeldung
  hängt jetzt am Übergang statt am Zustand. Wiederherstellen ist kein
  Nacherleben.

Neu: `FinsterwaldBridgeResumeRuntimeTests` fährt das fortgesetzte Spiel im
echten Finsterwald über den `InteractionDetector` — ein Objekt, das nicht von
selbst zum Ziel wird, ist für einen Spieler nicht vorhanden, und genau das war
hier der Fall.

**Menschlich offen:** Station 8 der Abnahme muss neu geprüft werden
(Bedienbarkeit *und* Herleitbarkeit), und die Fünf-Minuten-Prüfung über zwei
echte Programmstarts gilt nicht als bestanden, bis sie jemand erneut
durchgeführt hat. Ein grüner Test belegt beides ausdrücklich nicht.

## [Unreleased] — Die Strecke von Blender nach Unity ist befahren

Zum ersten Mal ist ein Modell aus unserem eigenen Blender in Unity angekommen.
Testobjekt war ein stilisierter Findling, 98 Dreiecke, einen Meter hoch —
bewusst wertlos als Kunst, denn geprüft wurde die Strecke, nicht das Objekt.

Er hat vier Anläufe gebraucht. Alle vier Befunde stehen jetzt in
`Technical/BLENDER_ASSET_PIPELINE.md`, dem neuen Standarddokument für 3D-Assets.

- **100× zu klein.** Blenders Vorgabe für `apply_scale_options` schreibt den
  Einheitenfaktor als 1 in die Datei; FBX zählt das in Zentimetern. Der Fels kam
  mit 0,007 m statt 1,00 m an. Das ist der teuerste der vier Fehler: Er sieht in
  Blender richtig aus, im Unity-Inspector richtig aus, und fällt erst neben
  einer Figur auf — wo man ihn typischerweise mit einer Prefab-Skalierung
  „repariert" und ab da keine Größen-, Collider- oder Physikangabe im Projekt
  mehr stimmt. `FBX_SCALE_ALL` behebt es.
- **Auf dem Rücken liegend, und der Bericht war grün.** Builder und Validator
  haben die Wurzelrotation auf Identität gezwungen und damit die Achsdrehung
  überschrieben, bevor irgendetwas sie prüfen konnte. Sie setzen jetzt nur noch
  die Position, und der Validator prüft die Wurzel mit — sie ist genau der Ort,
  an dem eine nicht umgerechnete Achse landet.
- **Achsdrehung auf der Importwurzel.** `(270.02, 0, 0)` — nicht 270. Unitys
  eigenes `bakeAxisConversion` macht daraus `(89.98, 0, 0)` und verschiebt das
  Problem, statt es zu lösen. `bake_space_transform=True` beim Export legt die
  Umrechnung in die Meshdaten; die Wurzel bleibt bei null.
- **Material über einen abgekündigten Pfad.** `MaterialLocation.External` gibt
  es in Unity 6 nicht mehr; der Import lief trotzdem durch und legte still einen
  zweiten Materialordner an. Aus der Datei kommt jetzt gar kein Material mehr.
  Das URP-Material entsteht in Unity und hängt am Prefab — dort, wo ein falscher
  Shader sonst bis zur ersten Szene unsichtbar geblieben wäre.

Dauerhaft neu:

- **`ModelImportValidator`** prüft eingetragene Modelle gegen den Standard und
  läuft als zehnter Validator im Nachtlauf mit. Er prüft **nicht** den ganzen
  Bestand: die Modelle aus der Meshy-Zeit sind vor diesem Standard entstanden,
  und ein Gate, das am ersten Tag rot ist, wird ignoriert statt befolgt.
- **`Art_Source/`** außerhalb von `Assets/` für `.blend`-Quelldateien. Unity
  würde eine `.blend` sonst bei jedem Refresh durch Blender importieren.
- **`.gitattributes`:** `*.meta` bekommt dieselbe Trailing-Space-Ausnahme wie
  Szenen und Prefabs; `.blend` und `.fbx` sind als `binary` markiert.

## [Unreleased] — Der Fortschritt überlebt das Beenden (P1.13A)

Erinnerungen, Rätselstand und die Antwort des Waldes halten jetzt über einen
Programmstart hinaus.

- **Keine Szene wurde dafür angefasst.** Der Kernbogen ist menschlich
  abgenommen; Persistenz nachzurüsten darf ihn nicht verändern. Das
  Speichersystem startet über `RuntimeInitializeOnLoadMethod` — kein neues
  Objekt in einer Szene, keine neue Referenz, die jemand lösen könnte.
- **Niemand kennt das Speichersystem.** Die drei Sitzungszustände melden nur,
  *dass* sich etwas geändert hat. Der `SaveService` weiß umgekehrt nichts über
  Dateien; das ist Sache des `ISaveStore`. An dieser Naht hängen die Tests
  ihren temporären Speicher ein.
- **Der Vertrag aus P1.10 wird endlich bedient.** `IRegionStateStore` wurde
  seinerzeit für genau diesen Tag angelegt.
- **Wiederherstellen ist kein Nacherleben.** Es wird Zustand gesetzt, sonst
  nichts: kein Regenerationsereignis, kein Ton, keine doppelten Pflanzen,
  keine erzwungene Lösungsanimation. Möglich, weil `FinsterwaldRegeneration`
  ihr `Apply` von ihrem `Regenerate` trennt.
- **Atomar geschrieben.** Erst vollständig daneben, dann die bisherige Fassung
  zur Sicherung, dann tauschen. In die Zieldatei hinein zu schreiben hieße,
  den einzigen gültigen Stand als Erstes zu zerstören.
- **Beschädigte Stände** werden nie stillschweigend als gültig behandelt: erst
  die Sicherung, sonst ein leerer Start mit Warnung — und die kaputten Dateien
  bleiben zur Untersuchung liegen. Ein Stand aus einer **neueren** Fassung
  wird weder geladen noch überschrieben; ihn zu überschreiben wäre
  Datenverlust für den, der zurückwechselt.
- **Nicht lebensnotwendig.** Fällt das Speichern aus, läuft das Spiel weiter
  und verliert nur Fortschritt. Ein Speichersystem, ohne das sich das Spiel
  nicht mehr starten lässt, wäre ein schlechterer Zustand als gar keines.
- **Echter Neustart-Nachweis.** `Save()` gefolgt von `Load()` im selben Prozess
  beweist wenig — ein statisches Feld könnte die Daten halten, ohne dass die
  Datei je gelesen wird. `SaveRestartProof` läuft deshalb in **zwei getrennten
  Unity-Prozessen**; dazwischen existiert nichts als die Datei.
- **Nicht gespeichert:** Position, Ausdauer, Leben, Gegnerzustand, Cooldowns,
  Audiozustände. Tod, Respawn und Heilung bleiben Creative Gates — das
  Speichersystem trifft diese Entscheidungen nicht nebenbei.
- **Offen für P1.13B:** `spawnId` steht im Format bereit, bleibt aber leer. Wo
  ein Laden den Spieler absetzt, ist eine Leveldesign-Entscheidung; ein
  geratener Startpunkt wäre schlechter als gar keiner.

## [Unreleased] — Der Bogen als ein Stück (P1.14)

Zum ersten Mal läuft der Kernbogen als zusammenhängender Test: Begegnung →
Resonanzzone → Erinnerung → Rätsel → Antwort des Waldes, im echten
Finsterwald.

- **Echte Wege statt gesetzter Zustände.** Der Kampf läuft über simulierte
  Geräte und die ausgelieferte Tastenbelegung. Jede Interaktion läuft über den
  `InteractionDetector`: Aren wird an das Objekt gestellt, es muss von selbst
  zum aktiven Ziel werden, und erst dann fällt der Tastendruck. Kein Test ruft
  `Interact` direkt auf, keiner setzt einen Rätselzustand von Hand, und die
  Lösung wird bei `BridgePuzzleRules` erfragt statt abgeschrieben.
- **Die Reihenfolge ist echt erzwungen.** Die Memory Site schaltet über
  `AnyActivationCompleted` das Rätsel frei — vor der Erinnerung lässt sich
  kein Anker drehen. Das war vorher nirgends geprüft; jetzt ist es die
  tragende Zusicherung des Bogens.
- **Was der erste Lauf zutage förderte.** Fünf Fehlschläge, keiner davon ein
  Produktfehler: drei kamen daher, dass `MemorySessionState`,
  `PuzzleSessionState` und `RegionRegenerationState` einen Szenenwechsel
  bewusst überleben — in einer Testreihe prüfte damit jeder Test auf dem
  Ergebnis seines Vorgängers, und der Wald hatte geantwortet, bevor der Bogen
  begann. Einer war ein Fehler im Testgerüst selbst (`null == null` galt als
  „Ziel erreicht" und drückte danach ins Leere). Der fünfte war lehrreich:
  eine „falsche" Ankerstellung, die zufällig keinen Anker bewegt, lässt das
  Rätsel vor `Configuring` stehen — der Stamm ist dann gar nicht freigebbar.
- **`MemorySessionState.Forget`** ergänzt, symmetrisch zu den beiden
  Geschwistern, die es schon hatten. Im Spiel wird es nicht aufgerufen.
- **Nicht enthalten:** die Wegöffnung. `blockedPath` bleibt unverdrahtet;
  welcher Weg sich öffnet, entscheidet das Leveldesign nach der menschlichen
  Abnahme.

## [Unreleased] — Nachtregression: zwei blinde Flecken in der Prüfung

Kein Spielinhalt. Diese Runde hat die Prüfung selbst geprüft — und zweimal
festgestellt, dass sie weniger belegt hat, als sie behauptet hat.

- **Der Kampftest hat in Frames gewartet, aber Sekunden gemeint.**
  `FinsterwaldFirstEncounterRuntimeTests.WaitUntil` zählte 900 Frames. Im
  Editor sind das bei rund 60 Bildern je Sekunde zufällig etwa 15 s, und alles
  lief grün. Im Batchmode laufen Frames ohne Bildsynchronisation um ein
  Vielfaches schneller — dieselben 900 Frames waren dort kürzer als die 2 s
  lange Verdachtsphase des Gegners. Fünf Tests meldeten daraufhin einen
  Gegner, der nicht reagiert; in Wahrheit war er nur noch nicht an der Reihe.
  Die Wartezeit ist jetzt zeitbasiert; die Frameschranke bleibt als Notbremse
  gegen eine stehende Uhr. **Kein Defekt am Spiel** — ein Defekt an der
  Messung, und einer, der die Suite außerhalb des Editors unbrauchbar machte.
- **Das Szenenprofil war älter als der halbe Slice.** Die Integritätsprüfung
  forderte Einmaligkeit für HUD, Eingabe und Erlebnisschicht, aber für nichts
  aus P1.5 bis P1.10. Ein Baumeisterlauf, der einen zweiten Wurzelstreifer,
  einen zweiten Link oder eine zweite Regeneration angelegt hätte, wäre grün
  durchgekommen — gedeckt war das bisher nur durch Handarbeit und einzelne
  PlayMode-Tests. Gegner, Link, Rätsel, Resonanzzone und Regeneration stehen
  jetzt im Profil; die drei Anker und die fünf Sitzpunkte bewusst nicht, die
  sind absichtlich mehrfach.
- **Alle Validatoren in einem Lauf.** `NightRegressionRunner` ruft die
  vorhandenen neun Einstiegspunkte nacheinander auf und zählt, was sie melden.
  Er fügt keine Prüfung hinzu; er spart acht Unity-Starts und legt das
  Ergebnis in ein Log statt in neun.
- **Compiler-Gate mit Neubau-Nachweis.** `Tools/QA/Run-CompilerGate.ps1`
  schlägt bei Errors **und** bei Warnungen fehl. Der erste Entwurf war zu
  gutgläubig: er löschte nur die Assemblies, worauf Unity sie aus dem
  Build-Cache wiederherstellte, ohne den Compiler zu starten — frische
  Zeitstempel, kein einziger Compilerlauf, und eine absichtlich eingebaute
  `CS0414`-Warnung blieb unsichtbar. Jetzt fliegt `Library/Bee` mit weg, und
  das Gate verlangt zusätzlich den Nachweis `Finished compiling graph … ToBuild`
  im Log. Gegengeprüft: mit Warnung Exit 1, ohne Warnung Exit 0. Es
  unterdrückt nichts und filtert nichts weg. Aufruf in
  `Technical/QA_GATES.md`.
- **Frames oder Sekunden, jetzt als Regel.** Zwei weitere Wartestellen
  umgestellt: das Warten auf einen Szenenwechsel in
  `RegionTravelSpawnRuntimeTests` (ein Ladevorgang dauert Zeit, keine Frames)
  und beide Ruhe-Erkennungen, deren Budget als „eine Sekunde" gemeint war. Die
  Ruhe-Messung selbst zählt weiterhin Frames — sie misst die Bewegung zwischen
  zwei aufeinanderfolgenden Bildern, und das ist genau die richtige Einheit.
  Ebenso bleibt das Durchlaufenlassen von fünf Frames unverändert.

## [Unreleased] — Der Wald antwortet (P1.10)

Zum ersten Mal verändert Arens Verstehen etwas in der Welt.

- **Zwei Bedingungen, nicht eine.** Rätsel gelöst **und** Memory Site gesehen.
  Rätsel und Erinnerung laufen im Code getrennt, also werden beide verlangt.
  „Trigger betreten" genügt ausdrücklich nicht — sonst reagierte der Wald auf
  einen Schritt statt auf ein Verstehen.
- **Leise.** Ein paar Triebe erscheinen, wenige Pflanzen bekommen Farbe, das
  Wasser wird klarer, das Licht etwas wärmer. Kein Blitz, keine Welle. Der
  Spieler soll zuerst „Moment — hier ist etwas anders" denken und erst danach
  „der Wald reagiert".
- **Nur um die Brücke herum,** Radius 13 m, höchstens zwölf Pflanzen. Der Wald
  wird nicht grün, er ist an einer Stelle anders.
- **Idempotent.** Mehrfaches Auslösen, Szenenwechsel und Wiederherstellung
  führen zu demselben Ergebnis; das Ereignis feuert genau einmal, und eine
  Wiederherstellung feuert es gar nicht — sonst klänge der Ton bei jedem
  Betreten erneut.
- **Kein Collider wird verändert.** Die Wegöffnung ist vorbereitet, aber
  **bewusst nicht verdrahtet**: welcher Weg sich öffnen soll, ist eine
  Level-Entscheidung, und ein geratener Eingriff beschädigt handgebaute
  Wegführung.
- **Färbung über `MaterialPropertyBlock`** — direkt aufs Material zu schreiben
  hätte jede andere Pflanze im Wald mitgefärbt.
- **Vertrag für P1.13:** `IRegionStateStore` mit zwei Methoden. Bis das
  Speichersystem existiert, läuft alles über die Sitzung; wer P1.13 baut,
  hängt sich ein, ohne P1.10 anzufassen. Keine Slots, keine Cloud, kein
  Profilmanager.
- **Audio über die vorhandene `SfxLibrary`**, ein neuer Hinweis, Lautstärke
  0,55 — leiser als alles andere.
- **Tests:** PlayMode 151 → **157**. Darunter: nur eine Bedingung reicht nicht,
  mehrfaches Auslösen bleibt folgenlos, Wiederherstellung ohne neues Ereignis,
  der Speichervertrag wird benutzt, und Portale, Spawns und Memory Sites
  bleiben unangetastet.

**Neu:** `AUDIO_AUDITION.md` — die Hörprobenliste. Alle Töne sind nach
Benennung und Länge gewählt; gehört hat sie niemand. Bleibt **HUMAN QA OPEN**.

## [Unreleased] — Lore und Narration (P1.9)

Nur der bestätigte Kanon. Kein Name fällt, keine Herkunft wird erklärt.

- **Alle Texte hinter Lokalisierungsschlüsseln** in einem `NarrationCatalog`.
  Kein Text im Code, keiner in einem Inspectorfeld einer Szene — sonst müsste
  eine Übersetzung die Szene durchsuchen, und ein Text in einem Prefab wird
  zuverlässig übersehen.
- **Die Stimme bleibt unbekannt.** Die freigegebene Zeile „Du suchst immer nach
  dem Weg, Aren." fällt nach der Erinnerung, mit dem Sprecher **„Unbekannte
  Stimme"**. Kein Name.
- **Aus der Erinnerung nimmt Aren einen Satz mit: „Ich war hier."** Nicht wer
  bei ihm war, nicht warum, nicht wann, nicht wer seine Erinnerungen genommen
  hat. Ein Test verlangt den Satz und verbietet die Erklärungen.
- **Ein wiederkehrendes Zeichen** — drei Linien, die sich nicht kreuzen — an
  zwei Orten, ausdrücklich unerklärt.
- **Drei optionale Umgebungstexte.** Klein, freiwillig, ohne neue Eigennamen.
- **Verbotene Begriffe sind testgesichert.** Kanon, der nur in einem Dokument
  steht, überlebt den ersten beiläufigen Textnachtrag nicht.
- **Gameplay ohne Lore:** ein Test entfernt jedes Loreobjekt und löst das
  Rätsel trotzdem vollständig.
- **Tests:** PlayMode 141 → **151**.

## [Unreleased] — Link, die Eule (P1.8)

Arens Begleiter ist zum ersten Mal im Finsterwald anwesend.

- **Link spricht nicht, zeigt nicht und führt nicht.** Er sitzt, sieht hin,
  wechselt gelegentlich den Platz und ruft manchmal. Kein Questmarker, kein
  Pfeil, keine Lösungsanzeige, keine menschliche Sprache, kein Untertitel für
  seinen Ruf.
- **Die Platzwahl kann die Lösung gar nicht verraten.** `LinkCompanion` hat
  keinen Verweis auf Rätsel oder Anker — die Information liegt dort schlicht
  nicht vor. Ein Test fährt alle 27 Ankerstellungen durch und verlangt
  dieselbe Wahl.
- **Kein Collider.** Link kann Aren nicht im Weg stehen.
- **Fünf Sitzpunkte** an Startbereich, Rastplatz, Lichtung und Brücke. Der
  Sitzpunkt an der Brücke schaut auf die Mitte der Lücke — auf den räumlichen
  Zusammenhang, nicht auf einen Anker.
- **Rückfall:** ohne brauchbaren Sitzpunkt hält Link sich in Arens Nähe, statt
  zu verschwinden oder hängenzubleiben.
- **Modell ist ein Primitiv-Platzhalter.** Im Bestand gibt es keinen Vogel —
  das Tierpack kennt Wolf, Hirsch, Fuchs und Vieh. Nichts beschafft, keine
  Meshy-Ausgabe. Finale Link-Kunst kommt später.
- **Tests:** PlayMode 132 → **141**. Darunter: Sitzpunktwechsel, Rückfall, kein
  Softlock bei nur einem Sitzpunkt, kein Collider, genau eine Instanz in der
  Szene, und das Rätsel bleibt ohne Link vollständig lösbar.

### Nebenbefund: 120 Compilerwarnungen, die niemand gesehen hat

Beim Gate-Lauf zeigte das Editor-Log **120 Warnungen** (alle CS0618, veraltete
`FindObjectsSortMode`-Überladung) in sechs Dateien — drei davon längst auf
`developer`. Die Unity-Console über MCP hatte sie nie ausgegeben, und frühere
Berichte haben deshalb „0 Warnungen" gemeldet, obwohl es keine null waren.
Alle sind jetzt behoben; die Warnungsfreiheit ist erstmals gegen das Editor-Log
belegt statt gegen die Console.

## [Unreleased] — Lesbarkeit am Brückenrätsel

Noch keine finale Kunst. Nur: man muss sehen können, was man tut.

- **Die Kerbenzahl war überhaupt nicht ablesbar.** Jeder Anker trug eine
  einzige Kerbe, und seine Stellung stand allein in der Drehung des Steins —
  ausgerechnet die Zahl, von der die Lösung spricht, war unsichtbar. Jeder
  Anker hat jetzt drei Kerbengruppen mit einer, zwei und drei Kerben.
- **Sichtbar ist immer nur die Gruppe der aktuellen Stellung.** Mit allen drei
  gleichzeitig sah man aus jedem Blickwinkel Teile mehrerer Gruppen; der Stein
  wirkte umwickelt statt gekerbt. Die Drehung bleibt als spürbare Rückmeldung.
- **Die Kerben liegen auf der Oberseite,** nicht am Mantel. Ein stehender
  Spieler blickt auf diese Steine hinab; am Mantel liefen sie um den Zylinder
  herum und waren aus keinem Winkel zu zählen.
- **Die drei Anker unterscheiden sich in der Form:** der Süd-Tiefanker ist
  breit und niedrig, der Seitenanker ein schlanker hoher Pfosten, der Nordanker
  liegt dazwischen. Drei gleiche Zylinder wären ordentlich und unbrauchbar
  gewesen.
- **Seilbock und Stammfreigabe sind als Funktionsobjekte erkennbar:** der
  Seilbock als zwei schräge Pfosten mit einem Seil darüber, die Freigabe als
  Pfosten mit deutlich abgewinkeltem Hebel. Vorher waren beide derselbe Würfel.
- **Der Seitenanker steckte sichtbar im gefallenen Stamm.** Beide sind jetzt
  auseinandergerückt.
- **Vier einfache Materialien** (Stein, Kerbe, Holz, Seil), damit sich die
  Funktionsobjekte vom Wald absetzen. Keine Meshy-Assets, keine Beschaffung.
- **Das Memory Echo bleibt unverändert:** es zeigt die alte Brücke, also die
  Beziehung — nie die Ankerstellungen.
- **Tests:** PlayMode 130 → **132**. Neu: jede Stellung zeigt genau eine
  Gruppe mit der passenden Kerbenzahl, und die drei Anker sind in Form und
  Abstand unterscheidbar.

**Anmerkung zum Vorgehen:** Dieser Durchgang lief zunächst gegen eine stale
Editor-Assembly. Die Unity-Console meldete null Fehler, während die
Kompilierung tatsächlich fehlschlug; erst das Editor-Log zeigte zwei
Compilerfehler. Kompiliersauberkeit wird seitdem gegen das Editor-Log geprüft,
nicht gegen die Console allein.

## [Unreleased] — Gesundheitsfeedback am Wurzelstreifer

Der Gegner zeigt jetzt, wie es um ihn steht.

- **Aus PR #30 gerettet, nicht gemergt.** Jener PR stand auf einem alten
  `developer`-Stand. Übernommen wurden `EnemyHealthBar`,
  `EnemyHealthBarVisual`, der zugehörige Validator und die 19 Laufzeittests —
  gegen den **heutigen** `EnemyHealth` und den heutigen Wurzelstreifer. Die
  veraltete `TASK_QUEUE.md` aus jenem PR blieb draußen.
- **Verhalten:** bei voller unberührter Gesundheit verborgen, nach dem ersten
  Treffer sichtbar, Verhältnis korrekt, nach dem Tod wieder verborgen.
- **Kameraausrichtung:** parallel zur Bildebene statt zur Kameraposition — so
  kippt die Leiste am Bildrand nicht weg.
- **Tatsächlich am Gegner integriert:** eigenes Kindobjekt `Lebensanzeige` im
  Wurzelstreifer-Prefab, Höhe 1,25 m. Der Vorgabewert von 2,1 m stammt von
  einer aufrechten Figur und hätte über einem 0,9 m hohen Vierbeiner in der
  Luft geschwebt.
- **Keine doppelte HUD-Struktur:** der Gegner bringt genau einen eigenen
  World-Space-Canvas mit, und der entsteht erst zur Laufzeit. Die Szene hat
  unverändert zwei Canvas-Strukturen.
- **Optik bleibt Blockout.** Die Darstellung kennt weder `EnemyHealth` noch
  Sichtbarkeitsregeln; die endgültige Optik ist dadurch austauschbar.
- **Tests:** PlayMode 109 → **130**. Darunter zwei neue Prüfungen am echten
  Exemplar auf der Lichtung: die Anzeige hängt am Gegner und verdoppelt das
  HUD nicht.

## [Unreleased] — Hörbare Spielregeln (P1.11A)

Kein Sounddesign, sondern Lesbarkeit. Der Encounter-Plan verlangt Angriffe
„durch Bewegung **und Ton**" lesbar; das Brückenrätsel verlangt einen Holz-
gegen einen Steinton. Beides fehlte.

- **Kein neues Audio-System.** Alles hängt an der bereits vorhandenen
  `SfxLibrary`, die schon zuvor zentral auf Spielereignisse gehört hat. Neu
  sind acht Hinweise und die statischen Ereignisse, über die sie ankommen —
  dem Muster von `PlayerCombat.AttackPerformed` folgend.
- **Telegraph:** eigener Ton vor dem Sprungbiss. `creak1` misst 0,661 s und
  endet damit fast genau, wenn der Biss landet — der Telegraph bleibt hörbar,
  auch wenn die Silhouette gerade verdeckt steht.
- **Treffer am Gegner:** leicht und schwer klingen unterschiedlich; der
  schwere Ton ist zugleich der hörbare Stagger.
- **Der tödliche Treffer bekommt keinen Trefferton.** Sonst lägen Treffer und
  Beruhigung im selben Moment übereinander, und der Spieler hörte zwei
  Ereignisse, wo eines stattfindet.
- **Block:** geblockt klingt gedämpft, ungeblockt hart. Ein Block, der klingt
  wie ein voller Treffer, lehrt nichts.
- **Brückenrätsel:** trägt die Ankerstellung, klingt Holz und Seil; trägt sie
  nicht, reibt Stein.
- **Der Ton verrät nie, WELCHER Anker falsch steht.** Das Ereignis führt genau
  ein `bool` — was nicht übergeben wird, kann nicht hörbar werden. Alle 26
  falschen Kombinationen sind einzeln geprüft: gleicher Ton, gleiche Anzahl.
- **Ein einziger Platzhalter:** `PLACEHOLDER_Stein_Reiben.wav`, erzeugt und im
  Namen als solcher gekennzeichnet. Der Bestand kennt Aufschläge auf Stein
  (`impactMining`), aber kein *Reiben*; ein Aufschlag würde als „etwas ist
  zerbrochen" gelesen statt als „das trägt nicht". Deterministisch erzeugt,
  damit ein erneuter Lauf keine Scheinänderung schreibt.
- **Alle übrigen Clips stammen aus dem vorhandenen Kenney-Bestand** (CC0, je
  eigene `License.txt`). Keine Beschaffung, kein Download, keine Kosten.
- **Tests:** PlayMode 97 → **109**. Darunter: jeder Hinweis feuert genau
  einmal, kein Doppeltrigger, korrekte Zustände lösen korrekte Hinweise aus,
  und die Szene hat alle acht Clips verdrahtet.
- **Dokumentiert:** `Technical/KNOWN_TOOLCHAIN_MESSAGES.md` nennt Version und
  Ursache der einen tolerierten Input-System-Meldung. Die Toleranz bleibt auf
  genau einen Test begrenzt.

**Nicht geprüft:** wie die Clips tatsächlich klingen. Die Auswahl folgt der
Benennung des Kenney-Bestands und den gemessenen Längen; ein Mensch sollte sie
abhören.

## [Unreleased] — Die geteilte Brücke (P1.7)

Das erste Memory-Watch-Rätsel. Nach dem ersten Kampf bewusst ein anderes
Gameplay: Kampf → Erkundung → Rätsel.

- **Neu — Rätselkern:** Zustandstabelle und Lösung liegen in
  `BridgePuzzleRules` ohne Unity-Abhängigkeit und sind damit vollständig
  prüfbar, ohne eine Szene zu laden.
- **Neu — Ablauf:** Resonanzzone → Echo sehen → drei Anker drehen → Seilbock
  prüfen → Stamm freigeben → zwei Bohlen legen → Brücke trägt.
- **Regel:** Die Watch zeigt nur Beziehungen. Die Anker wissen selbst nicht,
  ob sie richtig stehen, und der Seilbock meldet nur, **dass** etwas nicht
  trägt — nie **welcher** Anker falsch steht. Ein Hinweis, der den falschen
  Anker benennt, wäre die Lösung in Raten.
- **Lösung (Prototyp):** Süd-Tiefanker 1 Kerbe, Seitenanker 2, Nordanker 3 —
  dem Lastverlauf des Echos folgend, nicht der Zahlenreihe. Wer stumpf 1-2-3
  in Leserichtung einstellt, liegt falsch; genau dieser Irrtum ist vorgesehen.
- **Jeder Fehlversuch ist umkehrbar.** Alle 26 falschen Kombinationen sind
  einzeln geprüft: der Stamm verkantet, 1,2 s Sperre, zurück in die
  Konfiguration. Kein Softlock, kein Verlust.
- **Furt (`OFFEN` → `ENTSCHIEDEN`):** schadensloses Zurücksetzen. Kein Tod,
  kein Health-Penalty, kein unsichtbarer Todesrand. Tod und Respawn sind noch
  nicht kanonisch entschieden — dieses Rätsel darf das nicht nebenbei tun.
- **Erst die Bohlen machen die Brücke begehbar.** Die Lauffläche bleibt
  abgeschaltet, bis beide liegen; sonst wären die Bohlen Zierde.
- **Kein globaler Save-Manager.** Der Stand hält über Szenenwechsel innerhalb
  der Sitzung (`PuzzleSessionState`, nach dem Vorbild von
  `MemorySessionState`). Übergangszustände fallen dabei immer auf ihren
  letzten stabilen Stand zurück — ein Laden mitten in der Stammbewegung darf
  niemals Zwischengeometrie herstellen. Persistenz über das Programmende
  hinaus wartet auf das Save-System.
- **Szene:** Neuer Knoten `Environment/Brueckenraetsel` plus ein
  Strömungsvolumen unter der vorhandenen `Furt`. Objektbilanz gegen den
  Vorstand geprüft: nichts verloren, 19 neue Objekte, 0 fehlende Skripte.
- **Tests:** EditMode 37 → **51**, PlayMode 78 → **97**. Darunter eine Abnahme
  im echten Finsterwald, die das Rätsel von Anfang bis Ende löst, und der
  Nachweis, dass die Memory Site innerhalb der Resonanzzone liegt — läge sie
  außerhalb, käme der Spieler nie über den ersten Schritt hinaus.

**Offen:** ob Spieler die Lösung herleiten oder die 27 Kombinationen
durchprobieren. Das entscheidet der Usability-Test.

## [Unreleased] — Wurzelstreifer, der erste echte Gegner (P1.5)

Der Punkt, an dem der Finsterwald-Slice aufhört, Infrastruktur zu sein.

- **Neu — Damage Contract:** Jeder Treffer am Spieler läuft über einen
  einzigen `PlayerDamageReceiver`, der den Kontext bestimmt (`Normal` /
  `Blocked`). Der Angreifer weiß nicht, ob geblockt wird; das entscheidet der
  Verteidiger. Vorher rief jeder Angreifer `PlayerVitals.TakeDamage` direkt
  auf — die Blockregel hätte in jedem Gegner einzeln nachgebaut werden müssen.
- **Neu — Blockregel:** Block reduziert Gesundheitsschaden um **70 %**; 30 %
  kommen durch. Block negiert bewusst nicht vollständig, damit er eine gültige,
  aber nicht die dominante Antwort ist.
- **Neu — Wurzelstreifer:** 9 m Sicht, 5 m Gehör, 2 s Verdacht, verliert Aren
  nach 4 s. Muster: beobachten → seitlicher Schritt → **0,7 s Telegraph** →
  Sprungbiss → **1,2 s Erholung**. 40 LP, 10 Schaden, einmaliger Rückzug unter
  30 %. Genau ein Exemplar auf der Lichtung.
- **Neu — Lesbarkeit:** Der Schaden entsteht erst am **Ende** des Telegraphs
  und nur, wenn das Ziel dann noch in Reichweite steht. Wer währenddessen aus
  den 2,2 m rollt, wird nicht getroffen; der Gegner steht danach offen.
- **Neu — Bindung an die Begegnung (`EnemyLeash`):** Jenseits von 12 m kehrt
  der Gegner zur Lichtung zurück und nimmt bis dahin kein Ziel wahr. Ohne das
  stand er nach einer Flucht des Spielers fünfzehn Meter neben der Lichtung —
  die nächste Begegnung hätte nicht mehr dort stattgefunden, wo sie entworfen
  wurde. **Von einem Abnahmetest gefunden, nicht vermutet.**
- **Neu — Rückzug (`EnemyRetreat`):** einmalig pro Leben, damit der Kampf
  lesbar bleibt statt zäh zu werden.
- **Erweitert — Gegnergrundlage:** Gehörradius, Verdachtsphase, seitlicher
  Schritt, Telegraph-/Erholungsfenster, getrennte Dauer für leichten Flinch
  (0,18 s) und schweres Straucheln (0,8 s), Zustand `Retreat`. Alle neuen
  Werte sind mit 0 vorbelegt — ein Gegner ohne eigenes Profil verhält sich
  unverändert.
- **Blockout, keine Art Direction:** Das Modell ist der vorhandene
  Quaternius-Wolf aus `Ultimate Animated Animals` (CC0, `License.txt` im Pack,
  im Asset-Katalog unabhängig bestätigt). Er dient nur Größe, Bewegung,
  Hitboxen, Telegraph, Kamera und Timing. Keine Beschaffung, kein Download,
  keine Meshy-Generierung. Der finale Entwurf steht in
  `07_Enemies/WURZELSTREIFER_CONCEPT_BRIEF.md`.
- **Geändert (Drittanbieter):** Für den Wolf wurde ein Avatar erzeugt
  (Importeinstellung, `Wolf.fbx.meta`) — ohne ihn sind die zwölf
  mitgelieferten Clips nicht abspielbar. Die Datei selbst bleibt unverändert.
- **Szene:** Rein additiv — die Finsterwald-Szene wächst um den Knoten
  `Erste Begegnung`; Spawn, Portal, Memory Site, Wege, IDs, Startführung und
  Erlebnisschicht sind unverändert. 0 fehlende Skripte.
- **Tests:** EditMode 29 → **37**, PlayMode 54 → **78**. Darunter eine Abnahme
  im echten Finsterwald über die ausgelieferte Tastenbelegung, Tastatur und
  Gamepad.
- **Werte vorläufig** gemäß der Balancing-Konvention der Slice-Planung.

## [Unreleased] — Ausdauer an Rolle, Block und Angriff (P1.2)

Erster Produktionsbaustein des freigegebenen Finsterwald Vertical Slice.

- **Neu:** Rolle (20), leichter Angriff (8), schwerer Angriff (18) und Blocken
  (10/s) verbrauchen Ausdauer. Bisher tat das nur der Sprint.
- **Verhalten:** Reicht die Ausdauer nicht, unterbleibt die Aktion vollständig.
  Eine halb bezahlte Rolle oder ein halber Schlag existiert nicht. Blocken
  bricht bei Erschöpfung zusammen — sonst wäre Halten die dominante Antwort auf
  jeden Angriff.
- **Geändert:** Der Zeitpunkt des letzten Ausdauerverbrauchs liegt jetzt in
  `PlayerVitals` statt in `PlayerMovement`. Mit mehreren verbrauchenden Systemen
  hätte ein eigener Zähler je System dazu geführt, dass die Regeneration schon
  wieder anläuft, während ein anderes System gerade zahlt.
- **Belegt im Spiel:** Rolle bei Spielzeit 26.565 ausgelöst, Messung bei 28.355.
  100 − 20 + (0.79 s × 22/s) = **97.4** — gemessen 97.40, HUD-Balken 0.972.
- **Tests:** EditMode 22 → 29. Neu: Abbuchung, Ablehnung ohne Teilabbuchung,
  exakt ausreichende Ausdauer, Verbrauchszeitpunkt gesetzt beziehungsweise
  nicht gesetzt bei Ablehnung und bei Nullverbrauch.
- **Werte vorläufig** gemäß der Balancing-Konvention der Slice-Planung.

## [Unreleased] — Project Bible / Canon Consolidation

- **Struktur:** Die verbindliche Dokumentenhierarchie steht jetzt in
  `docs/README.md` und ist in jedem betroffenen Dokument benannt:
  Project Bible → Game Design Document → World → Story Bible → Characters →
  Gameplay → Architecture.
- **Aufgelöst:** Drei Doppelstrukturen. Das Game Design lag doppelt vor
  (inhaltlich im Root, als leeres Gerüst unter `00_Project/`); die technische
  Architektur dreifach (`ARCHITECTURE.md`, `Technical/ARCHITECTURE.md`,
  `10_Unity/Architecture.md`); der Erzählkanon verteilt auf `GAME_BIBLE.md` und
  `LORE_BIBLE.md`.
- **Migriert, nicht gelöscht:** Inhalte wurden zuerst vollständig in die
  Hierarchie überführt, erst danach wurden die alten Pfade zu Weiterleitungen.
  Jede Weiterleitung nennt, wohin welcher Abschnitt gegangen ist.
- **Gefüllt aus vorhandenen Quellen:** `02_Story/StoryBible.md`,
  `01_World/WorldBible.md`, `01_World/Lore.md`, `03_Characters/Hero.md`,
  `03_Characters/NPCs.md`, `03_Characters/Enemies.md`,
  `04_Gameplay/Combat.md`, `Puzzles.md`, `Weapons.md`, `Items.md`. Diese
  Dateien enthielten zuvor nur eine Überschrift.
- **Ergänzt:** `04_Gameplay/MEMORY_WATCH.md` um die achtstufige
  Fähigkeitsprogression und die verbindlichen Präzisierungen; diese standen
  bisher nur in den beiden Bibeln.
- **Ergänzt:** `docs/ARCHITECTURE.md` um Save-Bereiche, Singleton-Regel, stabile
  IDs als Savegame-Vertrag und die Qualitätsanforderung je System — die
  einzigen Inhalte, die es nur in `Technical/ARCHITECTURE.md` gab.
- **Nichts erfunden.** Ungeklärtes ist als `OFFEN` oder `NOCH ZU ENTSCHEIDEN`
  markiert, Widersprüche sind dokumentiert statt still aufgelöst.
- **Belegt:** Ein Abdeckungsvergleich gegen die Originalfassungen aus
  `372d380` zeigt, dass jeder inhaltstragende Begriff der vier Quelldokumente
  im neuen Bestand weiterhin vorkommt. Sieben Formulierungsabweichungen wurden
  einzeln geprüft und begründet.

### Dokumentierte Widersprüche

- **Zwölftes Kapitel:** `GAME_BIBLE.md` nennt zwölf Gebiete namentlich, die
  aktuelle Vorgabe bezeichnet den zwölften Eintrag als nicht eindeutig
  bestätigt. Bleibt offen.
- **Kapitelreihenfolge:** Erzähl- und Produktionsreihenfolge widersprechen sich
  beim Nebelmoor. Bleibt offen.
- **Kartograf-Prämisse:** von `GAME_BIBLE.md` als ersetzt geführt, durch die
  spätere Entscheidung wieder bestätigt. Story Bible ist maßgeblich.
- **Ersetzter Kanon:** Wunschring und „Großes Vergessen" bleiben ersetzt; beide
  sind festgehalten, damit sie nicht versehentlich zurückkehren.
- **Externe Quelle:** `GAME_BIBLE.md` erklärte eine `.docx` zur maßgeblichen
  Quelle, die **nicht im Repository liegt** und damit weder versioniert noch
  überprüfbar ist. Als `OFFEN` vermerkt.

## [Unreleased] — Zero Warning / QA Hygiene

- **Behoben:** Die 27 CS0618-Warnungen sind auf **0**, im Batchmode nachgemessen.
  25 davon kamen aus 13 `FindObjects*`-Aufrufstellen in sechs Dateien: zwölf
  übergaben `FindObjectsSortMode.None`, und die parameterlosen Überladungen sind
  ebenfalls unsortiert — die Ersetzung ist damit verhaltensgleich. Keine
  einzige Stelle übergab `InstanceID`, wo das Weglassen die Reihenfolge geändert
  hätte, und keine wertet die Reihenfolge überhaupt aus. Die dreizehnte war
  `FindFirstObjectByType<EventSystem>(...) == null` — eine reine
  Existenzprüfung, für die `FindAnyObjectByType` der dokumentierte Ersatz ist.
  Die letzten 2 stammten aus `Object.GetInstanceID()` in
  `RegionTravelSpawnRuntimeTests`; beide vergleichen nur Identität (altes gegen
  neues Spielerobjekt nach einem Regionswechsel), nutzen den Wert weder als
  Sortierschlüssel noch persistent, und wurden auf `GetEntityId()` umgestellt.
- **Abgesichert:** `RegionSceneIntegrityValidator` öffnet beim Prüfen Szenen und
  ließ die zuletzt geprüfte aktiv zurück. Genau daran ist bei der Abnahme von
  PR #32 ein Play-Mode-Test unbemerkt auf der falschen Region gestartet. Der
  Validator stellt die ursprünglich offene Szene jetzt wieder her und meldet das
  im Report. Hat die offene Szene ungespeicherte Änderungen, bricht er ab,
  statt sie zu verwerfen.
- **Neu:** `.gitattributes` nimmt Unity-YAML-Assets gezielt aus der
  Trailing-Space-Prüfung. Unity serialisiert leere Strings als `key: ` mit
  Leerzeichen am Zeilenende; ein einziger Szenendiff erzeugte darüber 1646
  Meldungen und machte `git diff --check` als Gate wertlos.
  **Gegenprobe dokumentiert:** Mit der Regel meldet `git diff --check` weiterhin
  Trailing Whitespace in `.cs` und `.md`, und in `.unity` weiterhin
  `space before tab` — abgeschaltet ist ausschließlich die eine Prüfung, die der
  Serializer auslöst, nicht die Whitespace-Prüfung insgesamt.

## [Unreleased] — Input Unification

- **Behoben:** `Interact` und das Inventar lagen am Controller beide auf
  `buttonNorth`. Ein Tastendruck öffnete das Inventar **und** untersuchte
  gleichzeitig das Objekt davor. Das Inventar hat jetzt eine eigene
  `Inventory`-Action (Taste I, Gamepad Select).
- **Behoben:** Das Tutorial prüfte `leftCtrl` für die Rolle, während das
  Actions-Asset sie auf `C` legt. Wer die Rolle wie vorgesehen auslöste, kam im
  Tutorial nicht weiter. Die Fortschrittserkennung liest jetzt die Action.
- **Behoben:** `PlayerCombat` hörte auf Maustaste und rechten Trigger, die im
  Actions-Asset gar nicht als `Attack` standen. Belegung und Asset stimmen
  jetzt überein; `Block` (Q / linker Trigger) ist ebenfalls eine echte Action.
- **Geändert:** `PlayerCombat`, `IntroSequence`, `TutorialSequence` und
  `InventoryUI` lesen keine Geräte mehr direkt. `PlayerInputReader` ist die
  einzige Stelle im Projekt, die `Keyboard.current`, `Gamepad.current` oder
  `Mouse.current` anfasst.
- **Geändert:** Das Intro wird nicht mehr über „beliebige Taste" übersprungen,
  sondern über Sprung, Interagieren oder Angriff. `anyKey` reagierte auch auf
  Tasten, die im Spiel etwas völlig anderes tun.
- **Neu:** `InputBindingCollisionTests` prüfen das ausgelieferte Actions-Asset
  selbst: kein Gamepad-Control darf zwei Aktionen bedienen, `Interact` und
  `Inventory` teilen kein Control, die bisherigen Belegungen bleiben erhalten,
  und `<Gamepad>/start` bleibt für Pause frei.
- **Behoben (Testabdeckung):** Eine neue `InputTestFixture` ließ den bestehenden
  Regressionstest `PlayerInputReaderProjectAssetTests` still durchrutschen —
  die Fixture setzt das Input-System zurück und löscht dabei
  `InputSystem.actions`, worauf der ältere Test sich selbst übersprang.
  `ProjectInputActions` hält die Referenz jetzt beim Start der Wiedergabe fest.
  Zurückschreiben ist keine Option, `InputSystem.actions` wirft im Play Mode.
- **Tests:** EditMode 22 unverändert, PlayMode 37 → 54, alle grün, keine Skips.
- **Keine Szenenänderung:** Intro, Tutorial und Inventar-UI finden den Reader
  zur Laufzeit, damit keine bestehende Szene neu verdrahtet werden musste.

## [Unreleased] — Region Experience Hardening

- **Behoben:** Sonnenfelder und Nebelmoor trugen dieselbe Fehlerklasse wie
  Finsterwald, nur noch nicht ausgelöst. `SfxLibrary` und die einzige
  `AudioSource` hingen dort am UI-Canvas `PrototypeHUD`. Der Canvas war aktiv,
  also fiel nichts auf — hätte ihn jemand deaktiviert, wäre wie in Finsterwald
  der komplette Ton mitgegangen. Beide Regionen sind jetzt in
  `ElyndorExperienceUI` (sichtbar) und `ElyndorExperience` (nicht-visuell)
  getrennt.
- **Behoben:** `Bootstrap` serialisierte keinen `PlayerInputReader`, obwohl
  `PlayerMovement` ihn per `RequireComponent` fordert. Unity legte ihn bei jedem
  Laden neu an und warnte dabei. Die Komponente ist jetzt Teil der Szene.
- **Bewusst nicht gemacht:** Sonnenfelder und Nebelmoor haben weiterhin kein
  HUD-Fundament, kein Intro und kein Tutorial. Diese Systeme wurden **nicht**
  ergänzt — sie sind Designentscheidung der Region, kein Defekt.
- **Neu:** `RegionSceneIntegrityProfiles` und `RegionSceneIntegrityValidator`.
  Ein QA-Durchlauf prüft Finsterwald, Sonnenfelder, Nebelmoor und Bootstrap
  gegen je ein eigenes Profil, lädt jede Szene selbst und ist batchmode-fähig.
- **Neu:** `SceneIntegrityProfile.AllowOptionalActive` — regionsabhängige
  Systeme dürfen fehlen, aber nicht wirkungslos vorhanden sein. Ohne diese
  Stufe müsste man entweder drei Regionen künstlich angleichen oder die
  Prüfung löchrig machen.
- **Neu:** `RegionScenes` hält die Szenenpfade an einer Stelle, damit Migrator,
  Prüfung und Tests nicht auseinanderlaufen.
- **Neu:** `ElyndorTestRunner` startet EditMode- und PlayMode-Suite aus dem
  Editor und schreibt das Ergebnis in eine Datei, die auch einen Domain-Reload
  übersteht.
- **Repariert:** `HudPolishValidator` war nicht batchmode-tauglich — er prüfte
  die gerade offene Szene und hatte weder Scene-Argument noch Exit-Code. Im
  Batchmode ist beim Start keine Szene geladen; er fand nichts und meldete
  trotzdem Erfolg. Er lädt seine Szene jetzt selbst (`ValidateScene`,
  `ValidateBatch`).
- **Verbessert:** `ExperienceLayerMigrator` speichert nur noch, wenn er
  tatsächlich etwas verändert hat. Vorher schrieb jeder Lauf auch eine längst
  migrierte Szene neu und erzeugte einen aussagelosen Diff. Zusätzlich gibt es
  Einstiegspunkte je Region und über alle Regionen.
- **Tests:** EditMode 8 → 22, PlayMode unverändert 37. Neu sind unter anderem
  je Szene ein Profiltest, der Nachweis dass `SfxLibrary` in keiner Region am
  Canvas hängt, dass kein `PrototypeHUD` mehr existiert, sowie die Gegenprobe,
  dass das Finsterwald-Profil gegen Nebelmoor korrekt durchfällt.
- **Offen, nicht blockierend:** Das Interaktions-Prompt-Panel überlappt in
  Finsterwald am unteren Rand mit der Quickslot-Leiste (Text bleibt lesbar).
  `InventoryUI` liest Gamepad `buttonNorth` direkt, worauf auch die
  `Interact`-Action liegt — am Gamepad lösen beide gleichzeitig aus.

## [Unreleased] — Finsterwald Runtime Recovery

- **Behoben:** Die Erlebnisschicht in Finsterwald war wirkungslos. Der Canvas
  `PrototypeHUD` stand auf inaktiv und trug zehn Komponenten — darunter das
  Erinnerungsuhr-Overlay, Narration, Kompass, Intro, Tutorial und die
  komplette `SfxLibrary`. Auf einem deaktivierten GameObject läuft weder
  `Awake` noch `Update`; in der Szene gab es dadurch weder Interaktions-Prompts
  noch Ton.
- **Ursache:** Beim HUD-Umbau entstand ein neuer `InteractionPromptController`,
  der weiterhin das *alte* Panel unter `PrototypeHUD` steuert. Anschließend
  wurde dessen Container deaktiviert — der neue Controller zeigte seitdem auf
  ein totes Panel.
- **Neue Struktur:** `ElyndorExperienceUI` (Canvas, sichtbare Systeme) und
  `ElyndorExperience` (Runtime-Root, nicht-visuelle Systeme) sind getrennt und
  unabhängig aktiv. Ein abgeschaltetes UI-Panel kann den Ton nicht mehr
  mitnehmen. Die doppelte `InteractionPromptUI` wurde entfernt.
- **Neu:** `SceneIntegrityAnalyzer` mit datengetriebenem Profil findet genau
  diese Fehlerklasse: Pflichtsysteme unter deaktivierten Vorfahren, doppelte
  Einzelsysteme, UI-Wurzeln mit Scale 0, fehlende Scripts und nicht gesetzte
  Pflichtreferenzen. Er hat die doppelte `InteractionPromptUI` selbst gefunden.
- **Neu:** `ExperienceLayerMigrator` (idempotent, batchmode-fähig) und die
  erste EditMode-Testsuite des Projekts.
- **Neu:** Sechs Regressionstests für `PlayerInputReader`. Der bestehende
  Test-Helper ließ die `Look`-Aktion weg und erreichte dadurch nie den
  Asset-Pfad, sondern immer nur die Fallback-Belegung.
- **Korrektur zu Report 0:** Der dort als P0 gemeldete Input-Runtime-Fehler ist
  **kein** Funktionsausfall. Die vier `Debug.Assert`-Meldungen traten nur beim
  ersten Play-Start einer Sitzung auf; `Debug.Assert` loggt, wirft aber nicht.
  Die Eingabe ist nachweislich funktionsfähig. Kein Fix vorgenommen.

## [Unreleased] — World Visual Overhaul Foundation

- Additiver, idempotenter Visual-Layer für Finsterwald, Sonnenfelder und
  Nebelmoor; bestehende Gameplay-Roots, Portale, Spawns und Memory Sites
  bleiben unangetastet.
- Neue Landmarken-, Ruhe-, Spielerführungs- und Regenerationsgruppen sowie
  regionale Light-Probe-Raster ergänzt.
- Neuer `WorldVisualOverhaulBuilder` mit Build- und Validierungsmenüs.
- Visuelle Richtung, Regionslayout, Assetbestand, Performance-Risiken und
  manueller Testplan unter `docs/` dokumentiert.

## [0.0.9] — 2026-07-25 — Visual Uplift Stufe 1 + Wald-Aufräumen

- **Rendering:** SSAO (PC-Renderer), Schattendistanz 90 m mit 3 Kaskaden,
  dezentes Bloom in allen Regionsprofilen.
- **Neue Shader:** `Elyndor/FoliageWind` (Blattwerk wiegt sich im Wind,
  höhenabhängig, inkl. Schattenwurf) auf allen Cutout-Materialien;
  `Elyndor/StylizedWater` (Wellenbewegung + wandernde Glanzstreifen)
  auf allen Wasserflächen.
- **Atmosphären-Partikel:** Schwebstaub (Finsterwald), Glühwürmchen
  (Memory Site, Lichtung), Nebelschwaden + Irrlichter (Moor),
  Pollenstaub (Sonnenfelder).
- **Kamera:** Blick jetzt bis fast auf Bodenhöhe absenkbar (minPitch 6°),
  mit Terrain-Schutz gegen Eintauchen ins Gelände.
- **Wald-Aufräumen:** „Gestürzte Baumriesen" entfernt (lasen sich als
  Platzierungsfehler); per Modell-Audit (`AuditTreeLineup`, messbare
  Schräglage) stark lehnende Varianten aus den Streu-Pools genommen
  (TST-Variante 10, USN-Kiefer 4).


## [0.0.8] — 2026-07-25 — Sprint „Kapitel 1: Der Finsterwald" (M3.4–M3.6)

- **Rückkehr-Tore** (Lore-Bibel Rückkehrstruktur): Schwarzer See mit
  versunkenen Stufen (SO), verriegelte Waldhütte mit sichtbarem Tagebuch
  und Zahnrad (NW), Baum ohne Schatten mit Watch-Vertiefung (N) —
  sichtbar ab Kapitel 1, nutzbar erst mit späteren Watch-Fähigkeiten.
- **Erwachens-Sequenz** (`IntroSequence`): Schwarzblende mit den
  fragmentierten Erinnerungszeilen des Kapitel-1-Auftakts, überspringbar,
  einmal pro Sitzung, nur im Finsterwald.
- **Quest-Fundament + vier Nebenquests**: `QuestState`/`QuestGiver`/
  `QuestObjective`; Maela (Korb), Borin (Axt), Teren (Packtier),
  Ilya (Wegmarke) als Platzhalter-NPCs mit Lore-Texten.
- Jeweils verifiziert per Unity-Batch-Build: 0 Fehler, 0 Warnungen.


## [0.0.7] — 2026-07-25 — Kanon-Sync auf Lore-Bibel V1

- **Story-Kanon V1 übernommen** (Lore- und Game-Bibel V1): Aren erwacht
  ohne Erinnerungen, Soren als Antagonist/früher Begleiter, Elian als
  Vater und Watch-Schöpfer, 12-Kapitel-Struktur. `GAME_BIBLE.md` ersetzt
  die alte Prämisse (Kartograf, Ring, Großes Vergessen).
- **Flüsterwald → Finsterwald**: Szene (`Finsterwald.unity`), Builder,
  Terrain-/Profil-Assets, Site-ID, Schildertexte und Build Settings
  umbenannt; alte Fluesterwald-Assets entfernt.
- **Sonnenfelder** bleibt als optionales Nebenquest-Gebiet erhalten
  (kein Kapitelgebiet; Entscheidung Lars).
- Encoding-Reparatur: doppelt kodierte Umlaute/Anführungszeichen in
  drei Editor-Skripten korrigiert (Ursache: frühere PowerShell-Edits
  ohne explizites UTF-8; alle Skript-Edits laufen jetzt mit
  `-Encoding utf8`).
- Verifiziert per Unity-Batch-Build: 0 Fehler, 0 Warnungen.


Format angelehnt an [Keep a Changelog](https://keepachangelog.com/de/).
Neueste Einträge oben.

---

## [0.0.6] — 2026-07-25 — Sprint „Weltwerkstatt I: Größerer Wald + Handarbeit-Schicht"

### Flüsterwald auf 200 × 200 m (~2× Fläche)
- Drei neue Story-Orte im Außenring: **Verfallener Wachturm** (NO-Kuppe,
  abgetragene statt eingestürzte Ostwand), **Stiller Teich** (Westsenke
  mit Schilf), **Verlassenes Jägerlager** (SW, nie entzündetes Feuer) —
  jeweils mit Anbindungspfad und untersuchbarem Detail.
- Bach bis an die neuen Kartenränder verlängert; Vegetations-Budgets
  ungefähr verdoppelt (Bäume, Gras, Büsche, Waldrand).

### Handarbeit-Schicht (Weltwerkstatt Stufe 3, Grundgerüst)
- Beide Builder erneuern beim Regenerieren nur noch die generierten
  Wurzeln (`Environment`, `*_Terrain`, `MemoryVisionVolume`,
  `PrototypeHUD`). Die neue **`[Handarbeit]`-Wurzel wird nie angetastet**:
  Dort platzierte Objekte (Deko, Charaktere, Feintuning) überleben jede
  Regenerierung.
- Weltwerkstatt Stufe 1+2 (gemeinsame Builder-Basis + Regionen als
  ScriptableObject-Daten) als Folge-Sprint aufgesetzt.

---

## [0.0.5] — 2026-07-24 — Sprint „Elyndor öffnet sich"

Branch: `feature/whispering-forest-foundation`

### Zwei neue Regionen (RegionSceneBuilder)
- **Sonnenfelder** (`Sonnenfelder.unity`): warmes Licht, goldene Wiesen,
  drei Kornfelder mit gepflanzten Reihen, verlassener Hof (untersuchbar),
  Memory Site „Alte Mühle" — die Geister-Windmühle erhebt sich über dem
  Steinfundament. Wege nach Westen (Flüsterwald) und Norden (Nebelmoor).
- **Nebelmoor** (`Nebelmoor.unity`): dichter Nebel (0.045), gedämpftes
  Licht, fünf dunkle Tümpel in Senken, nur tote/verdrehte Bäume,
  versunkener Karren (untersuchbar, verweist auf die Mühlen-Geschichte),
  Memory Site „Alter Steg" — Geisterplanken über dem schwarzen Wasser.
- Jede Region: eigenes Terrain, eigene Post-Processing-Profile, eigene
  Skybox/Fog/Ambient-Stimmung, eigener HUD-Aufbau.

### Regions-Verbindungen (`Elyndor.World`)
- `RegionPortal` (Trigger → Szenenwechsel), `RegionTravel` (statischer
  Reise-Zustand), `RegionSpawnPoint` (Spawn nach Ankunft).
- Flüsterwald ↔ Sonnenfelder ↔ Nebelmoor als zusammenhängender Kontinent-
  Ausschnitt; Memory-Site-Zustände überleben Regionswechsel
  (MemorySessionState war bereits szenenwechselfest).
- Wegweiser-Schilder kündigen die Nachbarregion erzählerisch an.

### Spieler unter Baumkronen sichtbar
- Neuer Shader `Elyndor/OccludedSilhouette` (ZTest Greater): zeichnet
  ausschließlich auf verdeckten Pixeln.
- `PlayerSilhouette` hängt das Silhouetten-Material an alle Renderer des
  Spielers — verdeckt ihn eine Baumkrone oder ein Hügel, erscheint eine
  sanfte hellblaue Silhouette (Tunic-Stil). GPU-seitig, ohne Raycasts.

### Bekannte Schulden
- RegionSceneBuilder dupliziert HUD-/Terrain-Helfer des Forest-Builders —
  gemeinsame Builder-Basis steht aus.

---

## [0.0.4] — 2026-07-22 — Sprint „Der wachsende Flüsterwald"

Branch: `feature/whispering-forest-foundation` — der Wald wächst auf
140 × 140 m (~5× Fläche), organisch statt flächig.

### Welt
- **Unity-Terrain statt flacher Plane**: sanfte Hügel (Perlin-Noise),
  Senken, ein Aussichtshügel (+7 m), abgeflachte Lichtung, erhöhtes
  Ruinen-Plateau; Terrain-Layer (Wald/Pfad/Fels/Wiese) mit prozeduralen
  Noise-Texturen, Pfade und Zonen per Alphamap gezeichnet.
- **Acht fließend verbundene Bereiche**: Startbereich, Waldpfad,
  kleine Lichtung, alter Bach, Steinkreis, versteckter Pfad,
  kleine Ruine, Aussichtspunkt.
- **Kurvige Splines-Pfade** (Catmull-Rom) statt gerader Wege; ein
  schmaler, schwächer gezeichneter versteckter Pfad zur Ruine.
- **Der alte Bach** quert die Karte, ist eingegraben und nur an zwei
  entdeckbaren Stellen passierbar: umgestürzter Stamm (Westen) und
  flache Furt mit Trittsteinen (Osten). Die zerstörte Brücke am
  Hauptweg bleibt der Memory-Watch-Moment — und ist vom Nordufer aus
  später erneut sichtbar (Rundweg).
- **Landmarken zur Blickführung**: großer alter Baum (Lichtung),
  markanter Fels (Weggabelung), toter Baum als Silhouette
  (Aussichtspunkt), Steinkreis auf Anhöhe, Bachlauf als Leitlinie,
  alte Wegmarkierungen an Entscheidungspunkten.
- **Drei neue untersuchbare Details** (bestehendes System, keine neue
  Mechanik): Steinkreis, verwitterte Inschrift in der Ruine,
  Aussichtspunkt-Schild.

### Natur Pack integriert
- Analyse: Low-Poly-Naturset (FBX + Texturen inkl. Normal Maps),
  keine Prefabs/Materialien enthalten; Modelle in 4 Formaten dupliziert.
- Eigene URP-Materialien für alle Pack-Texturen (Bark/Leaves/Rocks/
  Grass/Flowers, Alpha-Cutout für Blattwerk, Normal Maps aktiviert);
  Material-Remapping beim Platzieren über die FBX-Materialnamen.
- **Maßstabs-Normalisierung**: Jedes Modell wird vermessen und auf
  Zielhöhen skaliert (Laubbäume ~8 m, Kiefern ~11 m, Büsche ~1,1 m …).
- Kollider automatisch: Kapsel-Stammkollider für Bäume; Kleinvegetation
  bewusst ohne Kollider.
- ~250 Bäume in Baumgruppen + Einzelbäumen (Artenmix nach Standort:
  Kiefern auf Höhen, verdrehte Bäume am Bach, tote Bäume als Akzente),
  dichter Waldrand (~110 Bäume) statt sichtbarer Grenzmauern,
  Büsche, Gras, Farne, Kiesel, Pilze, Blumen — deterministisch verteilt.

### Technik
- `PlayerMovement`: **Gravitation ergänzt** (geplante Schuld aus der
  Roadmap) — nötig für Höhenunterschiede; Bewegung/Sprint/Roll sonst
  unverändert.
- Asset-Reorganisation: Packs von `Assets/_Elyndor/Objects/` nach
  `Assets/ThirdParty/` (ADR-002); Format-Duplikate (OBJ/glTF/.blend,
  Nicht-Unity-FBX) entfernt; leerer Medieval-Village-Ordner entfernt
  (Import war fehlgeschlagen).
- Alte Primitive-Prefabs und nicht mehr genutzte Prototyp-Materialien
  entfernt (durch Natur-Pack-Modelle bzw. Terrain-Layer ersetzt).
- Unsichtbare Weltgrenzen-Collider + dichter Baumrand statt
  Platzhalter-Mauern.

---

## [0.0.3] — 2026-07-22 — Sprint M2.5 „The First Breath of Elyndor"

Branch: `feature/whispering-forest-foundation` — reiner Atmosphäre-Pass,
keine neuen Gameplay-Systeme.

### Atmosphäre (Unity-Einstellungen, per Scene Builder)
- **Fog:** ExponentialSquared, Dichte 0.018, graugrüner Nebelton —
  ruhige Tiefenwirkung, der Wald „verliert sich" in der Ferne.
- **Licht:** Tiefstehende, warme Morgensonne (38°/-32°), weiche Schatten
  (Stärke 0.8) für lange Schattenwürfe quer zum Weg.
- **Ambient:** Trilight (kühler Himmel, gedämpfte Mitten, dunkler Boden).
- **Skybox:** Neues prozedurales Skybox-Material (`Proto_Skybox`),
  entsättigter Himmelston; als `RenderSettings.sun` verknüpft.
- **Post-Processing:** `Fluesterwald_Base_Profile` (dezente Vignette 0.18,
  Sättigung -8, Kontrast +6) auf dem Global Volume.
- **Farbpalette** aller Prototyp-Materialien entsättigt und harmonisiert
  (Vergessen / Ruhe / Neugier statt Spielzeug-Farben).

### Memory-Watch-Moment
- Neu: `MemoryVisionEffect` (Runtime) blendet während der Aktivierung ein
  zweites Volume (`Fluesterwald_MemoryVision_Profile`: Abdunklung,
  Entsättigung -45, kühle Farbtemperatur, Vignette 0.38) weich ein und
  nach dem Erscheinen der Erinnerung wieder aus.
- Aktivierungs-Overlay fadet jetzt sanft über eine CanvasGroup
  (`MemoryWatchActivationUI` überarbeitet), Abdunklung statt Farbfläche.
- Geister-Brücke: sanfteres Einblenden (2.5 s) und dezentes Eigenleuchten
  (Emission) — „Hier war einmal etwas."

### Ergänzte Objekte
- Vegetation als Prefabs (`Prefabs/Prototype`): zwei Baumvarianten
  (Moos/Verblasst), Busch, Stein, Grasbüschel, zwei Blumenarten —
  deterministisch verteilt (28 Bäume inkl. Ufer-Gegenseite, 16 Büsche,
  12 Steine, 55 Grasbüschel).
- Erzählerische Details ohne Mechanik: umgestürzter Baum mit Wurzelballen,
  alte Wegmarkierung mit verblasster Farbmarke, halb versunkene Steinreste
  an Flussufer und Erinnerungsort, stiller Blumenring um den Steinkreis.

### Performance
- Alle statischen Umgebungsobjekte mit `BatchingStatic` markiert
  (Geister-Brücke bewusst ausgenommen wegen Alpha-Fade).
- Vegetation als Prefab-Instanzen, saubere Hierarchie
  (Environment → Vegetation/Erzaehl-Details/Bereichsgrenzen).
- Keine neuen Update()-Methoden — alles event- und coroutine-basiert.

### Bewusst ausgelassen
- Audio: Im Projekt existieren keine Audio-Assets; statt ungeprüfter
  externer Sounds bleibt Audio ein dokumentierter Platzhalter
  (modulare Einbindung folgt, sobald geeignete Assets vorliegen).

---

## [0.0.2] — 2026-07-22 — Sprint „Whispering Forest Foundation"

Branch: `feature/whispering-forest-foundation`

### Hinzugefügt
- **Interaktionssystem** (`Elyndor.Interaction`): `IInteractable`,
  `InteractableBase`, `InteractionDetector` (ein aktives Ziel,
  `TargetChanged`-Event, Taste E / Gamepad West), `ExaminableObject`.
- **Memory-Watch-Prototyp** (`Elyndor.Memory`): `MemorySite`
  (Aktivierungssequenz, Zustand, statische Events), `MemoryEcho`
  (visuelles Einblenden per Alpha-Fade), `MemorySessionState`
  (Sitzungszustand, szenenwechselfest).
- **Narration-Kanal** (`Elyndor.Core.NarrationEvents`) zur Entkopplung
  von Weltobjekten und UI.
- **Prototyp-HUD** (`Elyndor.UI`): `InteractionPromptUI`, `NarrationUI`,
  `MemoryWatchActivationUI` (Legacy-UGUI-Text, bewusst minimal).
- **Editor-Tooling** (`Elyndor.Editor`-Assembly):
  `WhisperingForestSceneBuilder` erzeugt `Fluesterwald_Prototype.unity`
  deterministisch aus Bootstrap (Weg, Bäume, Fluss, Brückenreste,
  Geister-Brücke, Wegschild, Erinnerungsort, HUD) inkl.
  Prototyp-Materialien unter `Art/Materials/Prototype`.
- **Docs**: `GAME_BIBLE.md`, `ARCHITECTURE.md`, `CHANGELOG.md`;
  Roadmap um Phasenübersicht ergänzt.

### Geändert
- `Elyndor.Runtime.asmdef`: Referenz auf `UnityEngine.UI` ergänzt.
- `EditorBuildSettings`: Testszene zusätzlich zur Bootstrap-Szene
  aufgenommen (durch den Scene Builder).

### Unverändert (bewusst)
- `PlayerMovement`, `PlayerState`, `CameraFollow`, `PlayerAnimator`,
  `Bootstrap.unity` — bestehende Player-/Kamera-/Animationssysteme
  wurden nicht angefasst.

---

## [0.0.1] — 2026-07-22 — M2.3 Struktur-Cleanup

### Hinzugefügt
- `Elyndor.Runtime.asmdef`, Namespaces (`Elyndor.Player`, `Elyndor.Cameras`)
- `Docs/Roadmap.md` (Sprints M3–M12), `Docs/DECISIONS.md` (ADR-Log)

### Geändert
- Build Settings von SampleScene auf `Bootstrap.unity` korrigiert
- `InputSystem_Actions` → `_Elyndor/Input/`, Mixamo → `_Elyndor/Animation/`
- Scripts in Domänen-Ordner (`Player/`, `Camera/`)

### Entfernt
- Unity-Template-Reste (SampleScene, TutorialInfo, Readme.asset),
  `desktop.ini`-Dateien, Stub-Duplikate in `Docs/`
