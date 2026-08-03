# Claude Queue Runner

Der Runner ueberwacht die bestehende GitHub-Queue fuer `m4sterm1nd0815-design/elyndor` und startet Claude Code automatisch fuer genau ein freigegebenes Issue.

Claude selbst arbeitet weiterhin strikt nach `CLAUDE_QUEUE.md`: ein Issue, ein Feature-Branch, ein Draft-PR gegen `developer`, danach Stopp. Der Runner startet erst in einem spaeteren Polling-Zyklus einen neuen Claude-Prozess.

## Sicherheitsmodell

Standardmaessig startet der Runner **keine weitere Aufgabe**, solange mindestens ein offenes Issue eines dieser Labels traegt:

- `claude-in-progress`
- `claude-review`
- `claude-blocked`

Damit entstehen nicht unbegrenzt ungepruefte Draft-PRs. `claude-blocked` bleibt immer blockierend.

Mit `-AllowPendingReview` kann nur das Review-Gate bewusst aufgehoben werden. `claude-in-progress` und `claude-blocked` bleiben auch dann blockierend.

Der Runner:

- mergt keine Pull Requests;
- veraendert Queue-Labels nicht selbst;
- verwendet kein `--dangerously-skip-permissions`;
- fuehrt kein Reset, Clean oder Stash aus;
- startet bei einem unsauberen Working Tree nicht;
- speichert Logs ausserhalb des Repositorys.

## Voraussetzungen

Im Checkout muessen funktionieren:

```powershell
git --version
gh --version
gh auth status
claude --version
```

Erwarteter Checkout:

```text
C:\tmp\elyndor-gate0-developer
```

In `.claude/settings.local.json` muss fuer den Headless-Betrieb die exakte Freigabe `Bash` vorhanden sein. Beispiel:

```json
{
  "permissions": {
    "allow": [
      "Read",
      "Edit",
      "Write",
      "Glob",
      "Grep",
      "Bash",
      "mcp__meshy__*"
    ],
    "deny": [
      "Bash(git push --force:*)",
      "Bash(git reset --hard:*)",
      "Bash(git clean:*)",
      "Bash(rm -rf:*)",
      "Bash(del /s:*)"
    ],
    "defaultMode": "acceptEdits"
  }
}
```

Die lokale `.claude`-Konfiguration wird nicht committed.

## Einmaliger Test

Vom Repository-Root:

```powershell
.\tools\start-claude-queue-runner.cmd -Once -DryRun
```

Der Test prueft Repository, Tools, GitHub-Anmeldung, lokalen Claude-Zugriff, Working Tree und Queue. Claude wird dabei nicht gestartet.

## Dauerbetrieb

```powershell
.\tools\start-claude-queue-runner.cmd
```

Standardintervall: 60 Sekunden.

Ein anderes Intervall:

```powershell
.\tools\start-claude-queue-runner.cmd -PollSeconds 120
```

Stoppen mit `Ctrl+C`.

## Bewusstes Pipelining

Nur verwenden, wenn weitere Aufgaben trotz offener `claude-review`-Issues begonnen werden sollen:

```powershell
.\tools\start-claude-queue-runner.cmd -AllowPendingReview
```

`claude-in-progress` und `claude-blocked` stoppen den Runner weiterhin.

## Autostart bei Windows-Anmeldung

Installation fuer den aktuellen Benutzer, ohne erhoehte Ausfuehrung:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\manage-claude-runner-task.ps1 -StartNow
```

Mit anderem Polling-Intervall:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\manage-claude-runner-task.ps1 -PollSeconds 120 -StartNow
```

Autostart entfernen:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\manage-claude-runner-task.ps1 -Uninstall
```

Je nach lokaler Windows-Richtlinie kann das Anlegen geplanter Aufgaben eingeschraenkt sein. In diesem Fall den Runner normal in einem Terminal starten.

## Logs und Status

Logs:

```text
%LOCALAPPDATA%\ElyndorClaudeRunner\logs
```

Letzter Status:

```text
%LOCALAPPDATA%\ElyndorClaudeRunner\state.json
```

Es werden keine Environment-Dumps, Tokens oder Secrets protokolliert.

## Verhalten bei Problemen

### Dirty Working Tree

Der Runner stoppt vor dem Claude-Aufruf. Er verwirft, stasht oder bereinigt nichts automatisch. Lokale Aenderungen zuerst manuell pruefen.

### GitHub-Authentifizierung

Bei einem Fehler von `gh auth status` den Zugriff lokal erneuern und den Runner neu starten.

### Issue bleibt `claude-in-progress`

Der Runner korrigiert Labels nicht eigenmaechtig und startet keine weitere Aufgabe. Issue, Claude-Log und Draft-PR manuell pruefen.

### Zweite Runner-Instanz

Pro Repository und Checkout ist nur eine Runner-Instanz erlaubt. Eine zweite Instanz beendet sich kontrolliert.

## Queue-Auswahl

Bei mehreren `claude-ready`-Issues wird deterministisch das aelteste Issue gewaehlt. Bei identischem Erstellungszeitpunkt gewinnt die niedrigere Issue-Nummer.
