# AGENTS.md

Diese Datei ist die verbindliche Arbeitsanweisung fuer Codex im Repository
`Echoes of Elyndor`. Sie gilt fuer das gesamte Repository. Untergeordnete
`AGENTS.md` duerfen sie fuer ihren Bereich praezisieren, aber nicht abschwaechen.

## Verbindliche Wissensreihenfolge

Vor Produktionsarbeit sind vollstaendig zu lesen:

1. `AGENTS.md`
2. `EchoesOfElyndor/docs/PROJECT_BIBLE.md`
3. `EchoesOfElyndor/docs/LORE_BIBLE.md`
4. `EchoesOfElyndor/docs/GAME_DESIGN_DOCUMENT.md`
5. `EchoesOfElyndor/docs/WORLD_ROADMAP.md`
6. `EchoesOfElyndor/docs/GAMEPLAY_ROADMAP.md`
7. `EchoesOfElyndor/docs/ASSET_PIPELINE.md`
8. `EchoesOfElyndor/docs/DEVELOPMENT_PLAN.md`
9. `EchoesOfElyndor/docs/TASK_QUEUE.md`

Bei Widerspruechen gilt in dieser Reihenfolge: ausdrueckliche aktuelle
Nutzerentscheidung, diese Produktionsstruktur, `PROJECT_BIBLE.md`, fachlich
zustaendige Dokumentation, stabiler Code. Widersprueche werden dokumentiert,
nicht stillschweigend aufgeloest. Unbestaetigte Inhalte sind als `OFFEN`,
`VORSCHLAG` oder `NOCH ZU ENTSCHEIDEN` zu kennzeichnen.

## Autonomie

- Innerhalb des Repositorys selbststaendig und ohne gewoehnliche Rueckfragen
  arbeiten.
- Dateioperationen, Terminal, Git, Unity-Batch-Modus, Tests, Validatoren,
  Feature-Branches, Commits, Pushes, Pull Requests gegen `developer`, GitHub
  CLI und Meshy-MCP sind fuer freigegebene Aufgaben erlaubt.
- Eigene Fehler selbststaendig beheben.
- Vor normalen Aenderungen, Builds, Commits, Pushes oder PR-Aktualisierungen
  nicht um Zustimmung bitten.
- Fremde oder parallele Arbeit zuerst identifizieren und unangetastet lassen.

Nur unterbrechen bei:

- drohendem Verlust fremder Arbeit;
- direktem Eingriff in `main`;
- destruktivem Force-Push;
- groesseren Loeschaktionen;
- Secrets oder Zugangsdaten;
- kostenpflichtigen Kaeufen;
- unkontrolliertem externem Credit-Verbrauch;
- unklaren Asset-Lizenzen;
- grundlegender Aenderung von Story, Kernmechanik oder Art Direction;
- ausdruecklich gekennzeichnetem Meilenstein-Gate.

## Git

- `main` niemals direkt bearbeiten oder pushen.
- Feature-Branches basieren auf `developer`; Pull Requests zielen auf
  `developer`, sofern ein Gate nichts anderes freigibt.
- Kein normales `--force`. Nach einem notwendigen Rebase ausschliesslich
  `--force-with-lease`.
- Keine fremden Aenderungen verwerfen, ueberschreiben, staschen oder in den
  eigenen Commit aufnehmen.
- `.claude/`, Secrets, API-Keys, `Library/`, `Temp/`, `Logs/`, `UserSettings/`
  und lokale Konfigurationen nicht committen.
- Vor jedem Commit Branch, Status, vollstaendigen Diff und Scope pruefen.
- Nur aufgabenzugehoerige Dateien explizit stagen.
- Branch, Commit, PR und manuellen Testbedarf in `TASK_QUEUE.md` nachfuehren.

## Unity-Qualitaet

Vor Abschluss jedes technischen Pakets:

- Unity-Batch-Kompilierung mit der im Projekt festgelegten Unity-Version;
- `git diff --check`;
- passende automatisierte Tests und Editor-Validatoren;
- Pruefung auf fehlende Scripts in veraenderten Szenen und Prefabs;
- Pruefung auf unbeabsichtigte Aenderungen ausserhalb des Scopes;
- erforderliche Play-Mode-Tests als `MANUELL OFFEN` dokumentieren.

Generierte Szenen duerfen bestehende Handarbeit, Gameplay-Roots, Portale,
Spawns und persistente IDs nicht unbeabsichtigt ersetzen.

## Meshy und externe Assets

- Vor einer Beschaffung vorhandene Assets und freigegebene CC0-Quellen pruefen.
- Meshy nur fuer individuelle Hero-Assets und unverwechselbare Regionsobjekte
  einsetzen.
- Ohne neue Freigabe maximal zwei Generierungsversuche pro Asset.
- Vor kostenpflichtigen Folgeschritten Guthaben und erwarteten Verbrauch
  pruefen.
- Scale, Pivot, Ausrichtung, Topologie, Polycount, UVs, Materialien, Collider,
  LODs und Lizenz-/Herkunftsdaten pruefen.
- API-Keys niemals ausgeben, dokumentieren oder committen.
- Die Regeln in `EchoesOfElyndor/docs/ASSET_PIPELINE.md` sind verbindlich.

## Aufgabensteuerung

- `EchoesOfElyndor/docs/TASK_QUEUE.md` ist die verbindliche
  Ausfuehrungsreihenfolge.
- Immer nur Aufgaben mit Status `FREIGEGEBEN` oder `IN ARBEIT` bearbeiten.
- Abhaengigkeiten und Scope vor Beginn pruefen.
- Immer nur bis zum naechsten ausdruecklichen Gate arbeiten.
- Gate-Kriterien nicht selbst freigeben; am Gate mit Belegen stoppen.
- Bereits integrierte oder parallel laufende Arbeit nicht als neue Aufgabe
  nachbauen.
