# QA-Gates

Stand: 15. August 2026

Alle Gates laufen im Batchmode und liefern einen eindeutigen Exit-Code. Der
Unity-Editor muss dafür **geschlossen** sein; er hält sonst die Projektsperre.

Reihenfolge wie unten: erst das Compiler-Gate, dann die Suiten, dann die
Validatoren. Ein rotes Compiler-Gate macht jedes spätere Ergebnis wertlos.

---

## 1. Compiler-Gate

```
powershell -NoProfile -File Tools/QA/Run-CompilerGate.ps1 `
    -LogFile C:\tmp\qa\compilergate.log
```

| Exit | Bedeutung |
|---|---|
| **0** | frisch gebaut, 0 Errors, 0 Warnings |
| **1** | Compiler-Errors oder Compiler-Warnings gefunden |
| **2** | der Neubau hat nicht stattgefunden — Ergebnis wertlos |
| **3** | Unity nicht startbar oder kein Log |

**Warum Exit 2 existiert.** Ein Unity-Batchlauf benutzt vorhandene Assemblies
wieder. Läuft die Testsuite grün durch, ohne dass etwas kompiliert wurde,
belegt das über den aktuellen Quelltext nichts — im Log steht dann keine
einzige Compilerzeile, und genau das sieht aus wie „0 Fehler, 0 Warnungen".

Das Gate entfernt deshalb `Library/ScriptAssemblies` **und** `Library/Bee`.
Nur die Assemblies zu löschen genügt nicht: Unity stellt sie dann aus dem
Build-Cache wieder her, ohne den Compiler zu starten. Die Dateien sind danach
frisch datiert — eine absichtlich eingebaute Warnung blieb trotzdem
unsichtbar. Deshalb verlangt das Gate zusätzlich den positiven Nachweis
`Finished compiling graph: … N ToBuild` im Log.

**Was das Gate nicht tut:** Es unterdrückt nichts, filtert nichts weg, setzt
keine Compilerschalter, ändert keine asmdef und führt keine Ausnahmeliste. Es
löscht Cache und liest Log.

**Selbstprüfung.** Am 15.08.2026 gegengeprüft: mit einer absichtlich
eingebauten `CS0414`-Warnung meldet es **FAIL / Exit 1**, nach dem Entfernen
**PASS / Exit 0**.

## 2. EditMode

```
Unity.exe -batchmode -nographics -projectPath <Projekt> `
    -runTests -testPlatform EditMode `
    -testResults C:\tmp\qa\editmode.xml -logFile C:\tmp\qa\editmode.log
```

Kein `-quit`. Ergebnis steht als NUnit-XML in `testResults`; das Attribut
`failed` im Wurzelknoten ist maßgeblich.

## 3. PlayMode

```
Unity.exe -batchmode -projectPath <Projekt> `
    -runTests -testPlatform PlayMode `
    -testResults C:\tmp\qa\playmode.xml -logFile C:\tmp\qa\playmode.log
```

**Ohne** `-nographics` — PlayMode braucht ein Grafikgerät.

## 4. Validatoren

```
Unity.exe -batchmode -projectPath <Projekt> `
    -executeMethod Elyndor.EditorTools.NightRegressionRunner.RunAllBatch `
    -logFile C:\tmp\qa\validators.log
```

Läuft alle neun Validatoren in einem Editor-Start und meldet
`Elyndor Validatoren: n/n sauber`. Exit 0 nur, wenn alle sauber sind.

Vor jedem einzelnen Validator wird Finsterwald neu geöffnet, weil die
szenenöffnenden Validatoren sonst eine fremde Region als aktive Szene
hinterlassen — daran ist schon einmal ein Lauf auf der falschen Region
gestartet.

---

## Frames oder Sekunden — eine Regel für Tests

Wartet ein Test auf einen **zeitgesteuerten Spielzustand** (Telegraph,
Verdachtsphase, Ladevorgang), dann wird in **Sekunden** gewartet. Eine
Framezahl bedeutet im Editor und im Batchmode Verschiedenes: dort rund 60
Bilder je Sekunde, hier ohne Bildsynchronisation ein Vielfaches davon.

Wartet ein Test darauf, dass **Bilder durchlaufen** (Start-Rückrufe,
Bewegung zwischen zwei aufeinanderfolgenden Bildern), dann sind Frames die
richtige Einheit und bleiben es.

Eine Frameschranke darf zusätzlich als Notbremse gegen eine stehende Uhr
stehenbleiben. Sie muss so hoch liegen, dass sie vor der Zeitschranke nie
greift.

## Verwandte Dokumente

- `KNOWN_TOOLCHAIN_MESSAGES.md` — einzeln begründete Toleranzen
- `../FINSTERWALD_VERTICAL_SLICE_STATUS.md` — Stand je Arbeitspaket
