# Bekannte Werkzeugmeldungen

Stand: 15. August 2026

Meldungen aus Unity oder aus Paketen, die im QA-Lauf auftauchen, **nicht** von
Elyndor-Code stammen und deshalb einzeln bewertet wurden.

Diese Datei ist die einzige Stelle, an der eine Toleranz begründet werden darf.
Es gibt keine pauschale Unterdrückung: jede Toleranz steht in genau einem Test
und nennt hier ihre Ursache.

---

## 1. „Cached unprocessed value unexpectedly became outdated"

| | |
|---|---|
| **Meldungstyp** | Error |
| **Paket** | `com.unity.inputsystem` **1.19.0** |
| **Fundstelle** | `InputSystem/Controls/InputControl.cs:1410` |
| **Unity** | 6000.4.5f1 |
| **Betroffener Test** | `FinsterwaldFirstEncounterRuntimeTests.DieRolle_IstEineGueltigeAntwort` |
| **Erstmals dokumentiert** | 15.08.2026 (P1.11A-QA-Lauf) |

### Ursache

Die Zeile steht in einem `#if DEBUG`-Block hinter dem internen Kennzeichen
`paranoidReadValueCachingChecksEnabled`. Dieses Kennzeichen wird an genau einer
Stelle gesetzt:

```
Tests/TestFixture/InputTestFixture.cs:156
    InputSystem.settings.SetInternalFeatureFlag(
        InputFeatureNames.kParanoidReadValueCachingChecks, true);
```

Die Selbstprüfung läuft also **ausschließlich unter `InputTestFixture`** und
niemals im Spiel oder in einem Player-Build.

Sie liest den Steuerwert zusätzlich frisch aus dem Zustandspuffer und meldet
einen Error, wenn er vom zwischengespeicherten Wert abweicht, obwohl das
Veraltet-Kennzeichen nicht gesetzt war. Genau das tritt in unserem Rolltest auf,
weil dieser Test zusätzlich eine Szene lädt: der Gerätezustand wechselt dabei
über einen Pfad, der das Kennzeichen des Paket-Caches nicht setzt.

### Bewertung

Kein Elyndor-Defekt und keine Auswirkung auf das Spiel. Die Zusicherungen des
betroffenen Tests laufen unverändert durch — die Rolle vermeidet den Biss, und
der Test würde bei einem echten Fehler weiterhin rot.

### Umgang

`LogAssert.ignoreFailingMessages = true` **nur innerhalb dieses einen Tests**,
zurückgesetzt im `TearDown`. Die übrige Suite prüft weiterhin ausdrücklich auf
rote Meldungen; mehrere Tests haben eigene Zusicherungen darauf.

### Bei einem Paket-Update zu tun

1. Toleranz versuchsweise entfernen.
2. Vollständigen PlayMode-Lauf durchführen.
3. Bleibt die Suite grün, Toleranz und diesen Eintrag löschen.
4. Bleibt sie rot, hier die neue Paketversion nachtragen.
