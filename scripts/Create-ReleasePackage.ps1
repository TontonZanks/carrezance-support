param(
    [string]$Version
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

$appInfoPath = Join-Path $repoRoot "Carrezance.Support.App\Helpers\AppInfo.cs"
$projectPath = Join-Path $repoRoot "Carrezance.Support.App\Carrezance.Support.App.csproj"
$publishExe = Join-Path $repoRoot "Carrezance.Support.App\bin\Release\net8.0-windows\win-x64\publish\Carrezance Support.exe"

if ([string]::IsNullOrWhiteSpace($Version)) {
    $appInfoContent = Get-Content $appInfoPath -Raw
    $match = [regex]::Match($appInfoContent, 'Version\s*=\s*"(?<version>[^"]+)"')
    if (-not $match.Success) {
        throw "Impossible de lire la version depuis AppInfo.cs."
    }

    $Version = $match.Groups["version"].Value
}

$versionTag = if ($Version.StartsWith("v")) { $Version } else { "v$Version" }
$plainVersion = $versionTag.TrimStart("v")

Write-Host "Version release : $versionTag"

dotnet publish $projectPath -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=false

if (-not (Test-Path $publishExe)) {
    throw "Executable publie introuvable : $publishExe"
}

$artifactRoot = Join-Path $repoRoot "artifacts\$versionTag"
$zipFolder = Join-Path $artifactRoot "zip-content"
$releaseExeName = "CarrezanceSupport-$versionTag-win-x64.exe"
$releaseZipName = "CarrezanceSupport-$versionTag-win-x64.zip"
$releaseExePath = Join-Path $artifactRoot $releaseExeName
$releaseZipPath = Join-Path $artifactRoot $releaseZipName
$sha256SumsPath = Join-Path $artifactRoot "SHA256SUMS.txt"

if (Test-Path $artifactRoot) {
    Remove-Item $artifactRoot -Recurse -Force
}

New-Item -ItemType Directory -Force $artifactRoot | Out-Null
New-Item -ItemType Directory -Force $zipFolder | Out-Null

Copy-Item $publishExe $releaseExePath -Force
Copy-Item $publishExe (Join-Path $zipFolder "Carrezance Support.exe") -Force

foreach ($doc in @("README.md", "CHANGELOG.md", "RELEASE.md")) {
    $docPath = Join-Path $repoRoot $doc
    if (-not (Test-Path $docPath)) {
        throw "Documentation manquante : $doc"
    }

    Copy-Item $docPath (Join-Path $zipFolder $doc) -Force
}

Compress-Archive -Path (Join-Path $zipFolder "*") -DestinationPath $releaseZipPath -Force

$exeHash = (Get-FileHash $releaseExePath -Algorithm SHA256).Hash.ToLowerInvariant()
$zipHash = (Get-FileHash $releaseZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$sha256Content = @(
    "$exeHash  $releaseExeName",
    "$zipHash  $releaseZipName"
)
$sha256Content | Set-Content -Path $sha256SumsPath -Encoding ASCII

Write-Host ""
Write-Host "Assets de release generes :"
Write-Host "EXE : $releaseExePath"
Write-Host "ZIP : $releaseZipPath"
Write-Host "SHA256 : $sha256SumsPath"
Write-Host ""
Write-Host "Ces fichiers doivent etre ajoutes uniquement a GitHub Releases, pas au depot Git."
