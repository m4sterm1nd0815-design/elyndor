[CmdletBinding()]
param(
    [string]$Repository = "m4sterm1nd0815-design/elyndor",
    [string]$WorkingDirectory = "",
    [ValidateRange(15, 3600)]
    [int]$PollSeconds = 60,
    [switch]$Once,
    [switch]$DryRun,
    [switch]$AllowPendingReview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) {
    $WorkingDirectory = Split-Path -Parent $PSScriptRoot
}

$WorkingDirectory = [System.IO.Path]::GetFullPath($WorkingDirectory)

if ([string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
    $stateBase = [System.IO.Path]::GetTempPath()
}
else {
    $stateBase = $env:LOCALAPPDATA
}

$script:StateRoot = Join-Path $stateBase "ElyndorClaudeRunner"
$script:LogRoot = Join-Path $script:StateRoot "logs"
$script:StateFile = Join-Path $script:StateRoot "state.json"

New-Item -ItemType Directory -Path $script:LogRoot -Force | Out-Null
$script:LogFile = Join-Path $script:LogRoot ("runner-{0}.log" -f (Get-Date -Format "yyyy-MM-dd"))

function Write-RunnerLog {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,

        [ValidateSet("INFO", "WARN", "ERROR")]
        [string]$Level = "INFO"
    )

    $line = "[{0}] [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message
    Write-Host $line
    Add-Content -LiteralPath $script:LogFile -Value $line -Encoding UTF8
}

function Write-RunnerState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Status,

        [Nullable[int]]$IssueNumber = $null,

        [string]$Details = ""
    )

    $state = [ordered]@{
        updatedAt = (Get-Date).ToString("o")
        status = $Status
        repository = $Repository
        workingDirectory = $WorkingDirectory
        issueNumber = $IssueNumber
        details = $Details
    }

    $state | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $script:StateFile -Encoding UTF8
}

function Get-StableHash {
    param([Parameter(Mandatory = $true)][string]$Value)

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
        $hashBytes = $sha.ComputeHash($bytes)
        return ([System.BitConverter]::ToString($hashBytes)).Replace("-", "").Substring(0, 20)
    }
    finally {
        $sha.Dispose()
    }
}

function Assert-CommandAvailable {
    param([Parameter(Mandatory = $true)][string]$Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw "Required command '$Name' was not found in PATH."
    }
}

function Invoke-GhJson {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $stderrPath = Join-Path $script:StateRoot ("gh-{0}.stderr" -f ([guid]::NewGuid().ToString("N")))

    try {
        $output = & gh @Arguments 2> $stderrPath
        $exitCode = $LASTEXITCODE

        $stderrText = ""
        if (Test-Path -LiteralPath $stderrPath) {
            $stderrRaw = Get-Content -LiteralPath $stderrPath -Raw -ErrorAction SilentlyContinue
            if ($null -ne $stderrRaw) {
                $stderrText = $stderrRaw.Trim()
            }
        }

        if ($exitCode -ne 0) {
            $argumentText = $Arguments -join " "
            throw "gh $argumentText failed with exit code $exitCode. $stderrText"
        }

        $jsonText = ($output -join [Environment]::NewLine).Trim()
        if ([string]::IsNullOrWhiteSpace($jsonText)) {
            return @()
        }

        return ($jsonText | ConvertFrom-Json)
    }
    finally {
        Remove-Item -LiteralPath $stderrPath -Force -ErrorAction SilentlyContinue
    }
}

function Get-OpenIssuesByLabel {
    param([Parameter(Mandatory = $true)][string]$Label)

    return @(
        Invoke-GhJson -Arguments @(
            "issue", "list",
            "--repo", $Repository,
            "--state", "open",
            "--label", $Label,
            "--limit", "100",
            "--json", "number,title,url,createdAt,labels"
        )
    )
}

function Get-IssueDetails {
    param([Parameter(Mandatory = $true)][int]$IssueNumber)

    return Invoke-GhJson -Arguments @(
        "issue", "view", [string]$IssueNumber,
        "--repo", $Repository,
        "--json", "number,title,url,state,createdAt,labels"
    )
}

function Get-IssueLabelNames {
    param([Parameter(Mandatory = $true)]$Issue)

    if ($null -eq $Issue.labels) {
        return @()
    }

    return @($Issue.labels | ForEach-Object { [string]$_.name })
}

function Test-WorkingTreeClean {
    $output = @(& git -C $WorkingDirectory status --porcelain --untracked-files=normal 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "git status failed: $($output -join [Environment]::NewLine)"
    }

    $dirtyLines = @($output | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    return ($dirtyLines.Count -eq 0)
}

function Assert-LocalClaudeSettings {
    $settingsPath = Join-Path $WorkingDirectory ".claude\settings.local.json"

    if (-not (Test-Path -LiteralPath $settingsPath -PathType Leaf)) {
        throw "Missing local Claude settings: $settingsPath"
    }

    try {
        $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    }
    catch {
        throw "Could not parse $settingsPath as JSON: $($_.Exception.Message)"
    }

    if ($null -eq $settings.permissions) {
        throw "$settingsPath does not contain a permissions object."
    }

    $allowed = @($settings.permissions.allow)
    if (-not ($allowed -contains "Bash")) {
        throw "Automatic mode requires the exact allow entry 'Bash' in $settingsPath. Narrow Bash rules can still trigger prompts in headless mode."
    }

    $defaultMode = [string]$settings.permissions.defaultMode
    if ($defaultMode -eq "bypassPermissions") {
        Write-RunnerLog -Level "WARN" -Message "Local settings use bypassPermissions. The runner does not add --dangerously-skip-permissions, but this local mode still bypasses prompts."
    }

    $requiredDenies = @(
        "Bash(git push --force:*)",
        "Bash(git reset --hard:*)",
        "Bash(git clean:*)",
        "Bash(rm -rf:*)",
        "Bash(del /s:*)"
    )

    $denied = @($settings.permissions.deny)
    $missingDenies = @($requiredDenies | Where-Object { $denied -notcontains $_ })

    if ($missingDenies.Count -gt 0) {
        Write-RunnerLog -Level "WARN" -Message ("Local settings are missing recommended deny rules: {0}" -f ($missingDenies -join ", "))
    }
}

function Assert-Preflight {
    param([switch]$BeforeClaude)

    if (-not (Test-Path -LiteralPath $WorkingDirectory -PathType Container)) {
        throw "Working directory does not exist: $WorkingDirectory"
    }

    Assert-CommandAvailable -Name "git"
    Assert-CommandAvailable -Name "gh"
    Assert-CommandAvailable -Name "claude"

    $inside = @(& git -C $WorkingDirectory rev-parse --is-inside-work-tree 2>&1)
    if ($LASTEXITCODE -ne 0 -or (($inside -join "").Trim() -ne "true")) {
        throw "Not a Git working tree: $WorkingDirectory"
    }

    $originOutput = @(& git -C $WorkingDirectory remote get-url origin 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "Could not read origin URL: $($originOutput -join [Environment]::NewLine)"
    }

    $origin = (($originOutput -join "").Trim()) -replace "\\", "/"
    if ($origin -notmatch "github\.com[:/]m4sterm1nd0815-design/elyndor(?:\.git)?/?$") {
        throw "Unexpected origin URL '$origin'. Expected m4sterm1nd0815-design/elyndor."
    }

    foreach ($requiredFile in @("CLAUDE_QUEUE.md", "AGENTS.md")) {
        $path = Join-Path $WorkingDirectory $requiredFile
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required file is missing: $path"
        }
    }

    Assert-LocalClaudeSettings

    $authOutput = @(& gh auth status --hostname github.com 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "GitHub CLI authentication failed: $($authOutput -join [Environment]::NewLine)"
    }

    if (-not (Test-WorkingTreeClean)) {
        throw "Working tree is not clean. The runner will not stash, reset, clean, or discard changes."
    }

    if ($BeforeClaude) {
        $fetchOutput = @(& git -C $WorkingDirectory fetch origin --prune 2>&1)
        if ($LASTEXITCODE -ne 0) {
            throw "git fetch origin --prune failed: $($fetchOutput -join [Environment]::NewLine)"
        }
    }
}

function Get-QueueSnapshot {
    return [pscustomobject]@{
        InProgress = @(Get-OpenIssuesByLabel -Label "claude-in-progress")
        Review = @(Get-OpenIssuesByLabel -Label "claude-review")
        Blocked = @(Get-OpenIssuesByLabel -Label "claude-blocked")
        Ready = @(Get-OpenIssuesByLabel -Label "claude-ready")
    }
}

function Format-IssueList {
    param([object[]]$Issues)

    if ($null -eq $Issues -or $Issues.Count -eq 0) {
        return "none"
    }

    return (($Issues | ForEach-Object { "#{0} {1}" -f $_.number, $_.title }) -join "; ")
}

function Select-ReadyIssue {
    param([Parameter(Mandatory = $true)][object[]]$Issues)

    return $Issues |
        Sort-Object `
            @{ Expression = { [datetime]$_.createdAt }; Ascending = $true }, `
            @{ Expression = { [int]$_.number }; Ascending = $true } |
        Select-Object -First 1
}

function Test-SelectedIssueStillReady {
    param([Parameter(Mandatory = $true)][int]$IssueNumber)

    $issue = Get-IssueDetails -IssueNumber $IssueNumber
    $labels = @(Get-IssueLabelNames -Issue $issue)

    if ([string]$issue.state -ne "OPEN") {
        return $false
    }

    return ($labels -contains "claude-ready")
}

function Invoke-ClaudeForIssue {
    param([Parameter(Mandatory = $true)]$Issue)

    $prompt = @"
Arbeite nach CLAUDE_QUEUE.md.

Bearbeite ausschliesslich das offene Issue #$($Issue.number) im Repository $Repository.

Pruefe unmittelbar vor jeder Aenderung:
- Issue #$($Issue.number) ist weiterhin offen und traegt weiterhin claude-ready.
- Kein anderes offenes Issue traegt claude-in-progress.
- Der Working Tree ist sauber.
- Der Arbeitsbranch basiert auf dem aktuellen origin/developer.

Wenn sich der Queue-Zustand geaendert hat, antworte QUEUE_STATE_CHANGED, veraendere nichts und beende den Lauf.

Ansonsten:
- Uebernimm nur Issue #$($Issue.number) nach CLAUDE_QUEUE.md.
- Bearbeite kein zweites Issue.
- Erstelle einen eigenen Feature-Branch von origin/developer.
- Bleibe strikt im freigegebenen Scope.
- Erstelle nur einen Draft-PR gegen developer.
- Merge niemals selbst.
- Aktualisiere Issue-Kommentar und Labels nach CLAUDE_QUEUE.md.
- Stoppe direkt nach dem Draft-PR oder bei claude-blocked.
"@

    $runLog = Join-Path $script:LogRoot (
        "claude-issue-{0}-{1}.log" -f $Issue.number, (Get-Date -Format "yyyyMMdd-HHmmss")
    )

    $claudeArguments = @(
        "-p",
        $prompt,
        "--output-format",
        "text",
        "--permission-mode",
        "acceptEdits",
        "--disallowedTools",
        "Bash(git push --force:*)",
        "Bash(git reset --hard:*)",
        "Bash(git clean:*)",
        "Bash(rm -rf:*)",
        "Bash(del /s:*)"
    )

    Write-RunnerLog -Message "Starting Claude for issue #$($Issue.number): $($Issue.title)"
    Write-RunnerState -Status "claude-running" -IssueNumber ([int]$Issue.number) -Details ([string]$Issue.title)

    Push-Location $WorkingDirectory
    try {
        & claude @claudeArguments 2>&1 | Tee-Object -FilePath $runLog -Append
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    Write-RunnerLog -Message "Claude exited for issue #$($Issue.number) with exit code $exitCode. Output: $runLog"
    return $exitCode
}

function Verify-IssueResult {
    param(
        [Parameter(Mandatory = $true)][int]$IssueNumber,
        [Parameter(Mandatory = $true)][int]$ClaudeExitCode
    )

    $issue = Get-IssueDetails -IssueNumber $IssueNumber
    $labels = @(Get-IssueLabelNames -Issue $issue)

    if ($labels -contains "claude-review") {
        Write-RunnerLog -Message "Issue #$IssueNumber reached claude-review."
        Write-RunnerState -Status "review" -IssueNumber $IssueNumber -Details "Claude exit code: $ClaudeExitCode"
        return
    }

    if ($labels -contains "claude-blocked") {
        Write-RunnerLog -Level "WARN" -Message "Issue #$IssueNumber is claude-blocked and requires manual attention."
        Write-RunnerState -Status "blocked" -IssueNumber $IssueNumber -Details "Claude exit code: $ClaudeExitCode"
        return
    }

    if ($labels -contains "claude-in-progress") {
        Write-RunnerLog -Level "ERROR" -Message "Issue #$IssueNumber is still claude-in-progress after Claude exited. The runner will not change labels."
        Write-RunnerState -Status "stuck-in-progress" -IssueNumber $IssueNumber -Details "Claude exit code: $ClaudeExitCode"
        return
    }

    if ($labels -contains "claude-ready") {
        Write-RunnerLog -Level "WARN" -Message "Issue #$IssueNumber is still claude-ready after Claude exited."
        Write-RunnerState -Status "ready-unchanged" -IssueNumber $IssueNumber -Details "Claude exit code: $ClaudeExitCode"
        return
    }

    Write-RunnerLog -Level "WARN" -Message "Issue #$IssueNumber did not reach an expected queue status."
    Write-RunnerState -Status "unexpected-status" -IssueNumber $IssueNumber -Details "Claude exit code: $ClaudeExitCode"
}

function Invoke-QueueCycle {
    $snapshot = Get-QueueSnapshot

    if ($snapshot.InProgress.Count -gt 0) {
        $text = Format-IssueList -Issues $snapshot.InProgress
        Write-RunnerLog -Message "Waiting: claude-in-progress exists: $text"
        Write-RunnerState -Status "waiting-in-progress" -Details $text
        return
    }

    if ($snapshot.Blocked.Count -gt 0) {
        $text = Format-IssueList -Issues $snapshot.Blocked
        Write-RunnerLog -Level "WARN" -Message "Waiting: claude-blocked requires manual attention: $text"
        Write-RunnerState -Status "waiting-blocked" -Details $text
        return
    }

    if (-not $AllowPendingReview -and $snapshot.Review.Count -gt 0) {
        $text = Format-IssueList -Issues $snapshot.Review
        Write-RunnerLog -Message "Waiting for review gate: $text"
        Write-RunnerState -Status "waiting-review" -Details $text
        return
    }

    if ($snapshot.Ready.Count -eq 0) {
        Write-RunnerLog -Message "Queue empty: no open claude-ready issue."
        Write-RunnerState -Status "queue-empty"
        return
    }

    $selected = Select-ReadyIssue -Issues $snapshot.Ready
    Write-RunnerLog -Message "Selected oldest ready issue #$($selected.number): $($selected.title)"

    if ($DryRun) {
        Write-RunnerLog -Message "DryRun active: Claude will not be started."
        Write-RunnerState -Status "dry-run-selected" -IssueNumber ([int]$selected.number) -Details ([string]$selected.title)
        return
    }

    Assert-Preflight -BeforeClaude

    $secondSnapshot = Get-QueueSnapshot

    if ($secondSnapshot.InProgress.Count -gt 0) {
        Write-RunnerLog -Level "WARN" -Message "Queue changed before start: claude-in-progress appeared. Claude will not be started."
        Write-RunnerState -Status "race-in-progress"
        return
    }

    if ($secondSnapshot.Blocked.Count -gt 0) {
        Write-RunnerLog -Level "WARN" -Message "Queue changed before start: claude-blocked appeared. Claude will not be started."
        Write-RunnerState -Status "race-blocked"
        return
    }

    if (-not $AllowPendingReview -and $secondSnapshot.Review.Count -gt 0) {
        Write-RunnerLog -Level "WARN" -Message "Queue changed before start: claude-review appeared. Claude will not be started."
        Write-RunnerState -Status "race-review"
        return
    }

    if (-not (Test-SelectedIssueStillReady -IssueNumber ([int]$selected.number))) {
        Write-RunnerLog -Level "WARN" -Message "Issue #$($selected.number) is no longer open with claude-ready. Claude will not be started."
        Write-RunnerState -Status "race-selected-changed" -IssueNumber ([int]$selected.number)
        return
    }

    if (-not (Test-WorkingTreeClean)) {
        Write-RunnerLog -Level "WARN" -Message "Working tree became dirty before Claude start. No automatic cleanup will be attempted."
        Write-RunnerState -Status "dirty-before-start" -IssueNumber ([int]$selected.number)
        return
    }

    $claudeExitCode = Invoke-ClaudeForIssue -Issue $selected
    Verify-IssueResult -IssueNumber ([int]$selected.number) -ClaudeExitCode $claudeExitCode
}

$mutex = $null
$ownsMutex = $false
$exitCode = 0

try {
    $mutexKey = Get-StableHash -Value ("{0}|{1}" -f $Repository.ToLowerInvariant(), $WorkingDirectory.ToLowerInvariant())
    $mutexName = "Local\ElyndorClaudeRunner_$mutexKey"
    $createdNew = $false
    $mutex = [System.Threading.Mutex]::new($true, $mutexName, [ref]$createdNew)

    if (-not $createdNew) {
        Write-RunnerLog -Level "ERROR" -Message "Another runner instance already owns $WorkingDirectory."
        Write-RunnerState -Status "duplicate-runner"
        exit 3
    }

    $ownsMutex = $true

    Write-RunnerLog -Message "Runner started. Repository=$Repository; WorkingDirectory=$WorkingDirectory; PollSeconds=$PollSeconds; Once=$Once; DryRun=$DryRun; AllowPendingReview=$AllowPendingReview"
    Assert-Preflight
    Write-RunnerState -Status "started"

    do {
        try {
            Invoke-QueueCycle
        }
        catch {
            Write-RunnerLog -Level "ERROR" -Message $_.Exception.Message
            Write-RunnerState -Status "cycle-error" -Details $_.Exception.Message

            if ($Once) {
                throw
            }
        }

        if ($Once) {
            break
        }

        Start-Sleep -Seconds $PollSeconds
    }
    while ($true)
}
catch {
    $exitCode = 1
    Write-RunnerLog -Level "ERROR" -Message ("Runner stopped because of an error: {0}" -f $_.Exception.Message)
    Write-RunnerState -Status "stopped-error" -Details $_.Exception.Message
}
finally {
    if ($ownsMutex -and $null -ne $mutex) {
        try {
            $mutex.ReleaseMutex()
        }
        catch {
        }
    }

    if ($null -ne $mutex) {
        $mutex.Dispose()
    }

    Write-RunnerLog -Message "Runner stopped."
}

exit $exitCode
