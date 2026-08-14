---
Project: Echoes of Elyndor
Version: 0.0.2
Status: Active
Owner: Lars Becker
Last Updated: 22.07.2026
---



# CHANGELOG

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
