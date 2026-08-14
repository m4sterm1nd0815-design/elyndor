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

Stand: `PlayerVitals` ist vorhanden, die echte Anbindung von Sprint und Rolle an
den Ausdauerverbrauch ist als offene Aufgabe geführt.

## Eingabe

Controller First, Maus und Tastatur vollständig unterstützt. Die verbindliche
Belegung steht in `docs/ARCHITECTURE.md` unter *Eingabe* und ist durch Tests
abgesichert.

## Offen

- `NOCH ZU ENTSCHEIDEN`: Tod und Respawn, Heilungswirtschaft, Lock-on und
  Parieren.
- `OFFEN`: finale Geschwindigkeiten und Roll-Unverwundbarkeit.
