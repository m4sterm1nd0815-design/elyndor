# Finsterwald — Human Vertical Slice Acceptance

Stand: 15. August 2026
Basis: `origin/developer` bei `8a4fa87`
Dauer: **10–15 Minuten**

Diese Anleitung ist für **eine** Sitzung gedacht: einmal durch den Kernbogen,
mit gezielten Fragen an den Stellen, die nur ein Mensch beantworten kann.

## Vorbereitung

| | |
|---|---|
| **Szene** | `Assets/_Elyndor/Scenes/Finsterwald.unity` |
| **Ausgangspunkt** | Arens Startpose im Startbereich, rund `(-3, 0, -82)` — dort steht auch das Wegschild |
| **Start** | Szene öffnen, Play drücken. Kein Bootstrap nötig. |
| **Eingabe** | Tastatur/Maus **und** Gamepad sind belegt. Bitte beides kurz anfassen. |

**Wichtig:** Diese Anleitung enthält **keine Rätsellösung**. Punkt 8 ist
ausdrücklich ein Test ohne Vorwissen — wer die Lösung kennt, kann diesen Punkt
nicht mehr beurteilen.

**Zum Ausfüllen:** je Punkt **PASS / FAIL / ANMERKUNG**.

---

## 1. Start, Bewegung, Kamera

Geh vom Startbereich nach Norden los. Probier Gehen, Sprinten und die Rolle.

1. Fühlt sich die Bewegung kontrolliert an, oder rutschig?
2. Bleibt die Kamera in engen Stellen brauchbar?
3. Merkst du, dass Sprint und Rolle Ausdauer kosten — ohne auf die Leiste zu
   schauen?

`PASS / FAIL / ANMERKUNG:`

---

## 2. Link beobachten

Link ist eine Eule. Sie sitzt auf Ästen, wechselt gelegentlich den Platz und
folgt dir grob. Bleib einen Moment stehen und sieh ihr zu.

1. Wirkt sie wie ein Begleiter oder wie eine Deko?
2. Lenkt sie deinen Blick irgendwohin — und falls ja: fühlt sich das nach
   „schau mal da" an oder nach „hier ist die Lösung"?

> **HUMAN QA OPEN** · Modell ist ein Primitiv-Platzhalter. Bitte nur Verhalten
> beurteilen, nicht Aussehen.

`PASS / FAIL / ANMERKUNG:`

---

## 3. Wurzelstreifer bekämpfen

Auf der kleinen Lichtung im Westen, rund `(-26, 0, -17)`, steht genau ein
Gegner. Du brauchst eine Waffe in der Haupthand.

1. Bemerkt er dich nachvollziehbar — und siehst du, dass er dich erst
   *beobachtet*, bevor er handelt?
2. Ist der Kampf ein Unterricht oder eine Prüfung?

`PASS / FAIL / ANMERKUNG:`

---

## 4. Angriffstelegraph und Audio

Der Gegner setzt sichtbar an, bevor er zubeißt.

1. Erkennst du das Ansetzen **früh genug**, um zu reagieren?
2. Hörst du es auch dann, wenn du ihn gerade nicht ansiehst?
3. Der Telegraphton ist `creak1`, 0,661 s gegen 0,7 s Ansatz — endet er
   gefühlt genau dann, wenn der Biss landet?

> **AUDIO QA OPEN** · Diese Clips hat noch nie jemand gehört. Details in
> `AUDIO_AUDITION.md`.

`PASS / FAIL / ANMERKUNG:`

---

## 5. Block und Rolle

Probier beides gegen denselben Angriff.

1. Fühlt sich der Block wie eine gültige Antwort an — spürbar billiger als eine
   Rolle, aber nie umsonst?
2. Klingt ein geblockter Treffer deutlich anders als ein durchgekommener?
3. Vermeidet die Rolle den Biss zuverlässig, wenn du rechtzeitig rollst?

> **Hinweis:** Die Rolle folgt deiner Bewegungsrichtung. Ohne Eingabe rollst du
> nach vorn.

`PASS / FAIL / ANMERKUNG:`

---

## 6. Gegner-Lebensanzeige

1. Ist sie vor deinem ersten Treffer unsichtbar — und erscheint sie danach?
2. Steht sie gut lesbar über dem Tier, oder schwebt sie zu hoch/zu tief?
3. Kippt sie am Bildschirmrand weg?

> **HUMAN QA OPEN** · Optik ist Blockout.

`PASS / FAIL / ANMERKUNG:`

---

## 7. Memory Watch

Geh zur zerstörten Brücke, rund `(2, 0, -1)`. In der Nähe liegt eine Memory
Site.

1. Merkst du, dass hier etwas zu benutzen ist — ohne dass es dir gesagt wird?
2. Wirkt die Erinnerung wie eine Spur oder wie ein Film?

`PASS / FAIL / ANMERKUNG:`

---

## 8. Das Brückenrätsel — ohne Vorwissen

**Nicht weiterlesen, wenn du das Rätsel selbst lösen willst.** Hier steht
keine Lösung, aber auch kein Hinweis.

Du findest drei drehbare Anker, einen Seilbock, eine Stammfreigabe und zwei
lose Bohlen. Nimm dir die Zeit, die du brauchst.

1. Verstehst du **von selbst**, was die Kerben bedeuten sollen?
2. Hast du die Lösung **hergeleitet** — oder durchprobiert? (Beides ist ein
   gültiges Ergebnis. Bitte ehrlich.)
3. Fühlt sich ein Fehlversuch folgenlos an, oder ärgerlich?

> **HUMAN QA OPEN** · Das ist die wichtigste offene Frage des Slice. Ist Raten
> offensichtlich schneller als Verstehen, wird **nicht** ein Hinweis
> nachgelegt — dann stimmt der Rätselentwurf nicht.

`PASS / FAIL / ANMERKUNG:`
`Gelöst durch: ☐ Herleiten  ☐ Durchprobieren  ☐ gar nicht`
`Gebrauchte Zeit: ______`

---

## 9. Puzzle-Audio

1. Klingt eine tragende Ankerstellung eindeutig anders als eine, die nicht
   trägt?
2. Der „trägt"-Ton (`creak3`) und der Kampf-Telegraph (`creak1`) sind **beide
   Creaks** — verwechselst du sie?
3. Verrät dir irgendein Ton, *welcher* Anker falsch steht? (Das darf er nicht.)

> **AUDIO QA OPEN** · Frage 2 ist der wahrscheinlichste Tauschkandidat.

`PASS / FAIL / ANMERKUNG:`

---

## 10. Lore und Narration

Verstreut liegen kleine untersuchbare Dinge: eine Gravur (zweimal), ein
Rastplatz, Steine im Bach, ein Brückenrest. Nach der Erinnerung meldet sich
außerdem eine Stimme.

1. Erfährst du genug, um neugierig zu werden — und wenig genug, um es zu
   bleiben?
2. Fühlt sich irgendein Text wie eine Textwand oder wie eine Erklärung zu viel
   an?
3. Bleibt offen, **wer** spricht?

`PASS / FAIL / ANMERKUNG:`

---

## 11. Regeneration

Sie setzt erst ein, wenn du **beides** getan hast: das Rätsel gelöst *und* die
Erinnerung gesehen. Schau dich danach an der Brücke um.

1. Merkst du überhaupt, dass sich etwas geändert hat?
2. War dein erster Gedanke eher „hier ist etwas anders" oder sofort „der Wald
   reagiert"? (Ersteres ist das Ziel.)
3. Ist es zu leise, zu laut, oder richtig?

> **HUMAN QA OPEN** · Der Ton ist ein erzeugter Platzhalter und bewusst der
> leiseste im Spiel.

`PASS / FAIL / ANMERKUNG:`

---

## 12. Gesamtgefühl

1. Trägt der Bogen — Bewegung → Begegnung → Erinnerung → Rätsel → Veränderung —
   als **eine** Erfahrung?
2. Was war der stärkste Moment?
3. Was war der schwächste — und ist er ein Entwurfsproblem oder nur fehlende
   Kunst?

`PASS / FAIL / ANMERKUNG:`

---

## Was du bewusst ignorieren darfst

Alles davon ist bekannt und keine Rückmeldung wert:

- **Blockout-Optik** bei Wurzelstreifer, Link, Ankern, Seilbock, Bohlen und
  Trieben.
- **Keine Ambience** — kein Wald-, Wasser- oder Nachtklang.
- **Link ruft nicht hörbar** — der Ruf ist im Code vorgesehen, hat aber
  bewusst noch keinen Clip.
- **Kein Tod und kein Respawn** — bei 0 Leben blockieren nur Aktionen.
- **Kein Speichern** — alles gilt nur für die laufende Sitzung.
- **Kein Weg öffnet sich** bei der Regeneration; welcher es sein soll, ist noch
  nicht entschieden.

## Verwandte Dokumente

- `AUDIO_AUDITION.md` — jeder Ton mit Ereignis, Stelle und gewünschter Wirkung
- `FINSTERWALD_VERTICAL_SLICE_STATUS.md` — Stand je Arbeitspaket
- `Technical/KNOWN_TOOLCHAIN_MESSAGES.md` — tolerierte Werkzeugmeldungen
