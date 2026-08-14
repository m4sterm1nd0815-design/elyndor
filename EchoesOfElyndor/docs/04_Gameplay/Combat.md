# Kampf

Stand: 14. August 2026
Status: Verbindlicher Kanon mit gekennzeichneten offenen Punkten

Gameplay-Kanon (`docs/04_Gameplay/`). Konsolidiert aus
`00_Project/GAME_DESIGN_DOCUMENT.md`, `PROJECT_BIBLE.md` und `GAME_BIBLE.md`.
Die Kanonregeln zu Gegnern stehen in `docs/03_Characters/Enemies.md`.

## Grundsätze

- **Keine HP-Schwämme.** Gegner werden über Verhalten und klare Fenster gelernt.
- Erste Grundlage: leichte und schwere Angriffe, Blocken, Schaden und Reaktion.
- Der erste Gegnertyp braucht Telegraphing, Trefferreaktion, Niederlage und
  einen nachvollziehbaren Weltbezug.
- Menschen können besiegt werden, ohne getötet zu werden.

## Ausdauer

Sprint und Rolle verbrauchen **echte** Ausdauer; keine parallele
Scheinressource.

Angebunden sind **Sprint, Rolle, Angriff und Block**. Ein Angriff oder eine
Rolle wird nur ausgeführt, wenn die Kosten vollständig bezahlt werden können —
es gibt keine halb bezahlte Aktion. Blocken verbraucht laufend Ausdauer und
bricht bei Erschöpfung zusammen, statt kostenlos weiterzulaufen.

Vorläufige Werte: Sprint 18/s, Rolle 20 einmalig, leichter Angriff 8, schwerer
Angriff 18, Block 10/s. Regeneration 22/s nach 1 s ohne Verbrauch. Die Werte
sind Balancing-Vorschläge und stehen unter Vorbehalt des Balancing-Pakets.

## Block

**Block reduziert eingehenden Gesundheitsschaden um 70 %.** Der Spieler nimmt
also 30 % Chip Damage: 10 Schaden ungeblockt ergeben 3 Schaden geblockt.
Zusätzlich läuft der Ausdauerverbrauch beim Blocken weiter. Bricht die Ausdauer
zusammen, endet der Block, und der nächste Treffer verursacht vollen Schaden.

Block negiert bewusst **nicht** vollständig. Damit haben die drei defensiven
Antworten unterschiedliche Kosten:

| Antwort | Kosten | Wirkung |
|---|---|---|
| Rolle | Ausdauer, hohes Timing-Risiko | vermeidet vollständig |
| Block | Ausdauer **und** etwas Leben | leicht auszuführen, nie kostenlos |
| Abstand | keine Ausdauer | gibt Position und Angriffsfenster auf |

Noch **nicht** vorhanden und bewusst nicht vorgezogen: Perfect Parry, Perfect
Block, Guard Counter, Poise- und Impact-Stamina-Systeme. Sie kommen nur, wenn
das Kampfgefühl später einen klaren Bedarf zeigt.

Technisch läuft jeder Treffer am Spieler über einen einzigen Empfänger
(`PlayerDamageReceiver`), der den Kontext bestimmt. Der Angreifer weiß nicht,
ob geblockt wird — das entscheidet der Verteidiger. Ohne diese Bündelung hätte
die Blockregel in jedem Gegner einzeln nachgebaut werden müssen.

## Eingabe

Controller First, Maus und Tastatur vollständig unterstützt. Die verbindliche
Belegung steht in `docs/ARCHITECTURE.md` unter *Eingabe* und ist durch Tests
abgesichert.

## Offen

- `NOCH ZU ENTSCHEIDEN`: Tod und Respawn, Heilungswirtschaft, Lock-on und
  Parieren. Solange Tod und Respawn offen sind, ist der Schaden bei 0 Leben
  folgenlos außer dass Aktionen blockieren.
- `OFFEN`: finale Geschwindigkeiten und Roll-Unverwundbarkeit.
