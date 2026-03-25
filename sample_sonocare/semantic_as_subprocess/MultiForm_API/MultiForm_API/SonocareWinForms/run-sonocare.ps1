param(
    [string]$Configuration = "Debug"
)

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $projectDir "SonocareWinForms.csproj"
$sourceOutput = Join-Path $projectDir "bin\$Configuration\net48"
$safeOutput = Join-Path $env:USERPROFILE "Documents\SonocareWinForms_Run\net48"
$targetExe = Join-Path $safeOutput "SonocareWinForms.exe"

Write-Host "Building SonocareWinForms ($Configuration)..."
dotnet build $projectFile -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit $LASTEXITCODE
}

if (!(Test-Path $sourceOutput)) {
    Write-Error "Build output not found: $sourceOutput"
    exit 1
}

Write-Host "Copying build output to: $safeOutput"
New-Item -ItemType Directory -Path $safeOutput -Force | Out-Null
Copy-Item -Path (Join-Path $sourceOutput "*") -Destination $safeOutput -Recurse -Force

if (!(Test-Path $targetExe)) {
    Write-Error "Executable not found after copy: $targetExe"
    exit 1
}

Write-Host "Starting: $targetExe"
Start-Process -FilePath $targetExe
