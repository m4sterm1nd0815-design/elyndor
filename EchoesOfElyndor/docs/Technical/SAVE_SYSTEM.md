# Speichersystem (P1.13A)

Stand: 15. August 2026
Save-Version: **1**

Persistenz für den Finsterwald-Slice. Bewusst klein: der Fortschritt soll
einen Programmstart überleben, mehr nicht.

## Aufbau

```
Sitzungszustände          Persistenz                    Ablage
─────────────────         ──────────────                ──────
MemorySessionState   ─┐
PuzzleSessionState   ─┼──►  SaveService  ──►  ISaveStore  ──►  FileSaveStore
RegionRegenerationState┘         │                              MemorySaveStore
                                 └──►  SaveData (versioniert)
```

Keine Szene und kein Spielsystem kennt das Speichersystem. Die drei
Sitzungszustände melden über ein `Changed`-Ereignis nur, **dass** sich etwas
geändert hat; der `SaveService` entscheidet, was damit geschieht. Umgekehrt
weiß der `SaveService` nichts über Dateien — das ist die Aufgabe des
`ISaveStore`, und genau an dieser Naht hängen die Tests einen temporären
Speicher ein.

Für den Regionszustand wird zusätzlich der Vertrag `IRegionStateStore`
bedient, den P1.10 für diesen Tag angelegt hatte.

**Gestartet** wird über `SaveBootstrap` per `RuntimeInitializeOnLoadMethod`
(`AfterSceneLoad`) — **ohne Änderung an einer Szene**. Der Vertical Slice ist
menschlich abgenommen; Persistenz nachzurüsten darf ihn nicht anfassen. Ein
bereits eingehängter Dienst wird nicht ersetzt; daran hängt die Testbarkeit.

## Was gespeichert wird

| Zustand | Inhalt |
|---|---|
| Erinnerungsorte | IDs der gesehenen Memory Sites |
| Brückenrätsel | je Rätsel-ID der letzte **stabile** Zustandsname |
| Regionen | IDs der Abschnitte, die bereits geantwortet haben |
| `spawnId` | **leer** — Feld existiert für P1.13B, wird nicht gefüllt |

Zustandsnamen statt Zahlen: eine Zahl hätte sich beim Umsortieren der
Aufzählung still verschoben und alte Spielstände auf einen falschen Zustand
gesetzt.

## Was ausdrücklich **nicht** gespeichert wird

Nichts davon, bis eine Game-Director-Entscheidung vorliegt:

- Gegner-Lebenspunkte, Kampfzustand, Verdacht, Telegraph
- Abklingzeiten
- Ausdauer und Spielerleben
- Interaktionsziele
- Audiozustände
- Spielerposition

**Kein GameObject, keine Szene, keine Transform-Hierarchie.** Nur fachlicher
Zustand.

Tod, Respawn und Heilungswirtschaft sind weiterhin Creative Gates. Das
Speichersystem trifft diese Entscheidungen nicht nebenbei.

## Wann gespeichert wird

Nach jedem **bestätigten Fortschritt** — nicht pro Bild. Die Zustände melden
sich nur bei stabilen Übergängen, also höchstens ein paar Mal je Sitzung:

- eine Memory Site wurde erstmals aktiviert,
- ein Rätsel hat einen stabilen Zustand erreicht,
- eine Region hat geantwortet.

Bei dieser Häufigkeit ist sofortiges Schreiben die einfachste verlässliche
Lösung. Alles mit Verzögerung verlöre beim Absturz genau das Ereignis, dessen
Verlust am meisten schmerzt.

## Wo gespeichert wird

`Application.persistentDataPath/elyndor_progress.json`

Nie im Projektordner, nie unter einem festen Pfad. Tests biegen den Ort über
den `ISaveStore` um und fassen **niemals** einen echten Spielstand an; dafür
sorgt `SaveIsolationSetup` für die gesamte PlayMode-Suite.

Daneben liegen `…json.backup` (die letzte gültige Fassung) und während des
Schreibens kurzzeitig `…json.tmp`.

## Atomares Schreiben

1. Vollständig nach `.tmp` schreiben.
2. Die bisherige gültige Datei nach `.backup` sichern.
3. Erst dann tauschen.

In die Zieldatei hinein zu schreiben hieße, den einzigen gültigen Stand als
Erstes zu zerstören. Ein Abbruch mitten im Schreiben kostet so höchstens den
**neuen** Stand, niemals den alten.

## Restore

**Wiederherstellen ist kein Nacherleben.** Es wird nur Zustand gesetzt; die
Welt liest ihn beim Aufbau und zeigt ihn an.

Es wird **nicht**:

- ein Regenerationsereignis erneut gefeuert,
- ein Ton erneut gespielt,
- eine Pflanze doppelt gesetzt,
- eine Lösungsanimation erzwungen.

Möglich ist das, weil `FinsterwaldRegeneration` ihr `Apply()` von ihrem
`Regenerate()` trennt — die Wiederherstellung nimmt den stillen Weg. Während
des Wiederherstellens ist das Speichern gesperrt; sonst schriebe jedes Laden
sofort wieder zurück.

## Wenn etwas schiefgeht

| Fall | Verhalten | `LoadOutcome` |
|---|---|---|
| kein Spielstand | sauberer Anfang | `NoSave` |
| gültig | geladen | `Loaded` |
| unlesbar, Sicherung gültig | Sicherung wird genutzt, Warnung | `RecoveredFromBackup` |
| beides unlesbar | leerer Start, Warnung, **Dateien bleiben liegen** | `Corrupt` |
| neuere Version | **nicht geladen, nicht überschrieben**, Warnung | `FromNewerVersion` |

Ein beschädigter Stand wird **nie stillschweigend als gültig** behandelt. Ein
Stand aus einer neueren Fassung wird nicht angetastet — ihn zu überschreiben
wäre Datenverlust für den, der zurückwechselt.

**Das Speichersystem ist nicht lebensnotwendig.** Fällt es aus — kein
Schreibrecht, volle Platte, kaputte Datei —, läuft das Spiel weiter und
verliert nur seinen Fortschritt. Ein Speichersystem, ohne das sich das Spiel
nicht mehr starten lässt, wäre ein schlechterer Zustand als gar keines.

## Reset

`SaveService.ResetProgress()` löscht Spielstand, Sicherung und allen
laufenden Fortschritt. Das ist „Neues Spiel" auf der Ebene der Daten; ein
Menü dafür gibt es bewusst noch nicht.

## Nachweis über einen echten Neustart

`Save()` gefolgt von `Load()` im selben Prozess beweist wenig — ein statisches
Feld könnte die Daten halten, ohne dass die Datei je gelesen wird.

`SaveRestartProof` läuft deshalb in **zwei getrennten Unity-Prozessen**:

```
Unity.exe -batchmode -quit -projectPath <p> \
  -executeMethod Elyndor.EditorTools.SaveRestartProof.WriteProgress

Unity.exe -batchmode -quit -projectPath <p> \
  -executeMethod Elyndor.EditorTools.SaveRestartProof.VerifyProgress
```

Der zweite Aufruf endet mit 0, wenn er den Fortschritt vorfindet. Zwischen
beiden existiert nichts als die Datei. Er benutzt einen eigenen Ordner und
fasst den Spielstand eines Menschen nicht an; `CleanUp` räumt ihn weg.

## Offen für P1.13B

- **`spawnId` wird nicht gefüllt.** Wo ein Laden den Spieler absetzt, ist eine
  Leveldesign-Entscheidung. Sie wurde nicht getroffen, und ein geratener
  Startpunkt wäre schlechter als gar keiner. Das Feld steht bereit.
- Kein Save-Slot-Menü, kein Hauptmenü, keine Cloud, keine Plattformanbindung.
- Keine Migration zwischen Versionen — die Architektur **erkennt** ältere,
  neuere und beschädigte Stände, wandelt sie aber nicht um.

## Verwandte Dokumente

- `QA_GATES.md` — wie geprüft wird
- `../SAVE_ACCEPTANCE.md` — die Fünf-Minuten-Prüfung von Hand
- `../FINSTERWALD_VERTICAL_SLICE_STATUS.md` — Stand je Arbeitspaket
