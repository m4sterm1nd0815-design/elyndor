<#
.SYNOPSIS
    Compiler-Gate: baut alle Skript-Assemblies frisch und schlaegt bei jedem
    Compiler-Fehler UND bei jeder Compiler-Warnung fehl.

.DESCRIPTION
    Warum es dieses Skript gibt:

    Ein Unity-Batchlauf benutzt vorhandene Assemblies aus
    'Library/ScriptAssemblies' wieder. Laeuft die Testsuite gruen durch, ohne
    dass etwas kompiliert wurde, belegt das ueber den aktuellen Quelltext
    nichts — im Log steht dann keine einzige Compilerzeile, und genau das sieht
    aus wie "0 Fehler, 0 Warnungen". Am 15.08.2026 war das der Fall.

    Dieses Gate raeumt die Assemblies deshalb weg und erzwingt einen
    vollstaendigen Neubau. Anschliessend prueft es zweierlei:

      1. dass der Neubau wirklich stattgefunden hat (die Elyndor-Assemblies
         existieren und sind juenger als der Startzeitpunkt),
      2. dass dabei weder 'error CS' noch 'warning CS' aufgetreten ist.

    Ohne Punkt 1 waere Punkt 2 wertlos: ein Lauf, der nichts kompiliert,
    meldet auch nichts.

    Es wird nichts unterdrueckt und nichts weggefiltert. Das Skript setzt
    keine Compilerschalter, veraendert keine asmdef und kennt keine
    Ausnahmeliste. Es liest nur das Log.

.PARAMETER ProjectPath
    Pfad zum Unity-Projekt (der Ordner mit 'Assets' und 'ProjectSettings').

.PARAMETER UnityExe
    Pfad zur Unity.exe. Standard ist die im Projekt verwendete Version.

.PARAMETER LogFile
    Zielpfad des Batch-Logs.

.EXAMPLE
    powershell -NoProfile -File Tools/QA/Run-CompilerGate.ps1

.EXAMPLE
    powershell -NoProfile -File Tools/QA/Run-CompilerGate.ps1 `
        -ProjectPath C:\tmp\elyndor\EchoesOfElyndor `
        -LogFile C:\tmp\qa\compilergate.log

.OUTPUTS
    Exit 0  — Neubau nachgewiesen, 0 Errors, 0 Warnings.
    Exit 1  — Compiler-Errors oder Compiler-Warnings gefunden.
    Exit 2  — der Neubau hat nicht stattgefunden; das Ergebnis ist wertlos.
    Exit 3  — Unity liess sich nicht starten oder das Log fehlt.

.NOTES
    Der Unity-Editor muss geschlossen sein; er haelt sonst die Projektsperre.
#>

[CmdletBinding()]
param(
    [string] $ProjectPath,
    [string] $UnityExe = 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe',
    [string] $LogFile = (Join-Path ([System.IO.Path]::GetTempPath()) 'elyndor-compilergate.log')
)

$ErrorActionPreference = 'Stop'

# In Windows PowerShell 5.1 ist $PSScriptRoot beim Binden der Parameter noch
# leer, deshalb wird der Projektpfad erst hier bestimmt: zwei Ebenen ueber
# Tools/QA liegt das Projekt.
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

# Die Assemblies, deren Neubau nachgewiesen werden muss. Fehlt eine davon
# hinterher, hat Unity nicht gebaut, und das Ergebnis zaehlt nicht.
$RequiredAssemblies = @(
    'Elyndor.Runtime.dll',
    'Elyndor.Editor.dll',
    'Elyndor.EditModeTests.dll',
    'Elyndor.Runtime.PlayModeTests.dll'
)

function Write-Section([string] $Text) {
    Write-Output ''
    Write-Output "== $Text"
}

Write-Section 'Compiler-Gate'
Write-Output "Projekt: $ProjectPath"

if (-not (Test-Path (Join-Path $ProjectPath 'Assets'))) {
    Write-Output "FEHLER: '$ProjectPath' sieht nicht nach einem Unity-Projekt aus."
    exit 3
}

if (-not (Test-Path $UnityExe)) {
    Write-Output "FEHLER: Unity nicht gefunden unter '$UnityExe'."
    exit 3
}

$assemblyDir = Join-Path $ProjectPath 'Library\ScriptAssemblies'
$buildCacheDir = Join-Path $ProjectPath 'Library\Bee'

# Erst nachsehen, ob das Projekt offen ist — dann erst loeschen.
#
# Die erste Fassung hat in umgekehrter Reihenfolge gearbeitet: Assemblies und
# Build-Cache weg, dann Unity starten, dann feststellen, dass eine andere
# Instanz das Projekt haelt. Ergebnis war ein sauberer Abbruch mit Code 2 —
# und ein geoeffneter Editor, dem gerade unter den Haenden die Assemblies
# entfernt worden waren. Ein Pruefwerkzeug darf die Arbeit eines Menschen
# nicht beschaedigen, nur weil es selbst nicht laufen kann.
$lockFile = Join-Path $ProjectPath 'Temp\UnityLockfile'

if (Test-Path $lockFile) {
    $locked = $false

    try {
        $stream = [System.IO.File]::Open(
            $lockFile, 'Open', 'ReadWrite', 'None')
        $stream.Close()
    } catch {
        $locked = $true
    }

    if ($locked) {
        Write-Output ''
        Write-Output 'GATE ABBRUCH — das Projekt ist in Unity geoeffnet.'
        Write-Output 'Es wurde nichts geloescht und nichts veraendert.'
        Write-Output 'Bitte den Editor schliessen und erneut starten.'
        exit 3
    }
}

# Beides muss weg. Nur die Assemblies zu loeschen genuegt nicht: Unity stellt
# sie dann aus dem Build-Cache wieder her, ohne den Compiler laufen zu lassen.
# Die Dateien sind danach frisch datiert, im Log steht aber keine einzige
# Compilerzeile — eine absichtlich eingebaute Warnung blieb so unentdeckt.
Write-Section 'Assemblies und Build-Cache entfernen'
foreach ($dir in @($assemblyDir, $buildCacheDir)) {
    if (Test-Path $dir) {
        Remove-Item -Recurse -Force $dir
        Write-Output "Entfernt: $dir"
    } else {
        Write-Output "Nicht vorhanden: $dir"
    }
}

# Der Startzeitpunkt ist die Messlatte fuer "frisch gebaut". Eine Sekunde
# Vorlauf, damit Dateisystemauflösung nicht gegen uns arbeitet.
$startedAt = (Get-Date).AddSeconds(-1)

if (Test-Path $LogFile) {
    Remove-Item -Force $LogFile
}

Write-Section 'Unity im Batchmode starten'
$arguments = @(
    '-batchmode'
    '-nographics'
    '-quit'
    '-projectPath', $ProjectPath
    '-logFile', $LogFile
)

$process = Start-Process -FilePath $UnityExe -ArgumentList $arguments -Wait -PassThru -NoNewWindow
Write-Output "Unity beendet mit Code $($process.ExitCode)."

if (-not (Test-Path $LogFile)) {
    Write-Output 'FEHLER: Es wurde kein Log geschrieben.'
    exit 3
}

Write-Section 'Nachweis, dass wirklich gebaut wurde'
$missing = @()
$stale = @()

foreach ($name in $RequiredAssemblies) {
    $path = Join-Path $assemblyDir $name

    if (-not (Test-Path $path)) {
        $missing += $name
        continue
    }

    $writtenAt = (Get-Item $path).LastWriteTime
    if ($writtenAt -lt $startedAt) {
        $stale += "$name ($writtenAt)"
        continue
    }

    Write-Output "  neu gebaut: $name"
}

if ($missing.Count -gt 0 -or $stale.Count -gt 0) {
    if ($missing.Count -gt 0) {
        Write-Output "FEHLT: $($missing -join ', ')"
    }
    if ($stale.Count -gt 0) {
        Write-Output "NICHT NEU: $($stale -join ', ')"
    }
    Write-Output ''
    Write-Output 'GATE FAIL — der Neubau hat nicht stattgefunden.'
    Write-Output 'Ein Ergebnis ohne Neubau sagt ueber den Quelltext nichts aus.'
    exit 2
}

# Frische Zeitstempel allein beweisen nichts: aus dem Build-Cache
# wiederhergestellte Assemblies sind ebenfalls frisch datiert. Nur diese Zeile
# belegt, dass der Compiler tatsaechlich gelaufen ist — und 'ToBuild' sagt,
# wie viel er gebaut hat.
$compileEvidence = @(
    Get-Content $LogFile | Select-String -Pattern 'Finished compiling graph:.*?(\d+) ToBuild'
)

if ($compileEvidence.Count -eq 0) {
    Write-Output ''
    Write-Output 'GATE FAIL — im Log steht keine einzige Compilerzeile.'
    Write-Output 'Unity hat die Assemblies aus dem Cache genommen, statt zu bauen.'
    Write-Output 'Eine Warnung im Quelltext waere so unsichtbar geblieben.'
    exit 2
}

$builtNodes = [int] $compileEvidence[0].Matches[0].Groups[1].Value
Write-Output "  Compiler gelaufen: $builtNodes Knoten gebaut"

if ($builtNodes -le 0) {
    Write-Output ''
    Write-Output 'GATE FAIL — der Compiler hat nichts gebaut.'
    exit 2
}

Write-Section 'Compilermeldungen im Log'
$logLines = Get-Content $LogFile

$errors = @($logLines | Select-String -Pattern 'error CS\d+' -SimpleMatch:$false)
$warnings = @($logLines | Select-String -Pattern 'warning CS\d+' -SimpleMatch:$false)
$failed = @($logLines | Select-String -Pattern 'Compilation failed' -SimpleMatch)

foreach ($line in ($errors + $warnings | Select-Object -First 40)) {
    Write-Output "  $($line.Line.Trim())"
}

Write-Output ''
Write-Output "Errors:   $($errors.Count)"
Write-Output "Warnings: $($warnings.Count)"
Write-Output "Log:      $LogFile"

if ($errors.Count -gt 0 -or $warnings.Count -gt 0 -or $failed.Count -gt 0) {
    Write-Output ''
    Write-Output 'GATE FAIL — Compiler-Errors oder -Warnings vorhanden.'
    exit 1
}

Write-Output ''
Write-Output 'GATE PASS — frisch gebaut, 0 Errors, 0 Warnings.'
exit 0
