# Speichern — Fünf-Minuten-Prüfung

Stand: 16. August 2026

Die Maschine kann belegen, dass der Fortschritt in eine Datei kommt und aus
ihr zurück. Sie kann nicht belegen, dass es sich beim tatsächlichen Spielen
richtig anfühlt. Dafür ist diese Seite da.

> ## Ergebnis: OFFEN — diese Prüfung muss neu durchgeführt werden
>
> Am 16.08.2026 wurde beim Brückenrätsel ein Fehler gefunden, der genau diese
> Prüfung betrifft: Der Spielstand wurde erst geladen, **nachdem** die Szene
> sich aufgebaut hatte. Die Brücke stand nach jedem echten Programmstart wieder
> auf Anfang, obwohl die Datei den Fortschritt korrekt enthielt — und wer
> weiterspielen wollte, kam am Rätsel nicht mehr vorbei, weil die bereits
> gesehene Erinnerung sich nicht noch einmal ansehen ließ.
>
> Behoben und automatisch geprüft
> (`SavePersistenceTests`, `FinsterwaldBridgeResumeRuntimeTests`). Ein grüner
> Test belegt aber ausdrücklich **nicht**, dass die Datei im echten Ablauf
> zweier Programmstarts tut, was sie soll — genau das ist der Punkt dieser
> Seite. Sie gilt deshalb **nicht** als bestanden, bis ein Mensch sie erneut
> durchgeführt hat. Punkt 4.2 (ist die Brücke weiterhin begehbar?) ist dabei
> der entscheidende.

**Dauer:** rund fünf Minuten. Kein Vorwissen nötig, keine Rätsellösung
enthalten.

---

## Vorbereitung

| | |
|---|---|
| **Szene** | `Assets/_Elyndor/Scenes/Finsterwald.unity` |
| **Start** | Szene öffnen, Play drücken |

---

## 1. Fortschritt herstellen

Spiel bis mindestens **einem** davon:

- eine **Memory Site** benutzt, **oder**
- das **Brückenrätsel** gelöst.

Beides zusammen ist besser — dann lässt sich auch die Regeneration prüfen.

`Erreicht: ☐ Erinnerung  ☐ Rätsel  ☐ Regeneration`

## 2. Vollständig beenden

Play stoppen **und den Unity-Editor schließen**.

> Nur Play zu stoppen genügt nicht. Es geht ausdrücklich um den echten
> Programmstart — sonst prüfst du den Arbeitsspeicher statt die Datei.

## 3. Neu öffnen

Unity starten, Szene öffnen, **Play** drücken.

## 4. Prüfen

1. Ist die benutzte **Memory Site** weiterhin benutzt — bleibt das Echo
   sichtbar, statt sich neu aktivieren zu lassen?
2. Ist die **Brücke** weiterhin begehbar, falls du sie gelöst hattest?
3. Falls die Regeneration schon eingesetzt hatte: **ist der Wald weiterhin
   verändert — und erklingt der Ton dabei NICHT erneut?**

> **Punkt 3 ist der wichtigste.** Wiederherstellen ist kein Nacherleben. Ein
> Ton, der bei jedem Start neu erklingt, entwertet genau den Moment, den er
> begleiten soll.

`PASS / FAIL / ANMERKUNG:`

## 5. Gegenprobe: neu anfangen

Es gibt bewusst noch **kein Menü** dafür. Wer den Spielstand loswerden will,
löscht die Datei:

```
%USERPROFILE%\AppData\LocalLow\<Firma>\<Projekt>\elyndor_progress.json
```

Danach muss der nächste Start wieder bei null beginnen.

`PASS / FAIL / ANMERKUNG:`

---

## Was du bewusst ignorieren darfst

- **Die Position wird nicht gespeichert.** Du startest immer im Startbereich,
  nicht dort, wo du aufgehört hast. Wo ein Laden absetzen soll, ist eine
  Leveldesign-Entscheidung und noch offen (P1.13B).
- **Ausdauer, Leben, Gegnerzustand** werden nicht gespeichert — Tod, Respawn
  und Heilung sind weiterhin offen.
- **Kein Save-Menü, keine Slots, keine Cloud.**

## Wenn etwas schiefgeht

Ein kaputter Spielstand darf das Spiel **nicht** am Start hindern. Sollte das
Spiel gar nicht mehr starten, ist das ein Fehler — bitte melden, das ist
ausdrücklich nicht so gedacht.

## Verwandte Dokumente

- `Technical/SAVE_SYSTEM.md` — was, wann, wo und wie
- `FINSTERWALD_HUMAN_ACCEPTANCE.md` — die Abnahme des Kernbogens
