param([string]$RepoPath="C:\Users\Lars\Desktop\Echos of Elyndor")
$ErrorActionPreference="Stop"
$source=Join-Path $PSScriptRoot "Files"
if(-not(Test-Path(Join-Path $RepoPath ".git"))){throw "Repository nicht gefunden"}
Set-Location $RepoPath
if((git branch --show-current) -ne "docs/project-bible"){git switch docs/project-bible}
Copy-Item -Path (Join-Path $source "*") -Destination $RepoPath -Recurse -Force
git add EchoesOfElyndor/docs
git diff --cached --check
git status
Write-Host 'Danach: git commit -m "Add project documentation foundation"'
Write-Host 'Dann:   git push'
