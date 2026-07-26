[CmdletBinding()]
param(
    [string]$Version,
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot "ryzen-smu-cli\ryzen-smu-cli.csproj"

if ([string]::IsNullOrWhiteSpace($Version)) {
    $versionNode = Select-Xml -LiteralPath $projectPath `
        -XPath "/Project/PropertyGroup/Version" |
        Select-Object -First 1
    if ($null -eq $versionNode) {
        throw "The project does not define a Version property."
    }

    $Version = $versionNode.Node.InnerText
}

$Version = $Version -replace "^v", ""
if ($Version -notmatch "^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$") {
    throw "Version '$Version' is not a supported Semantic Version."
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot "artifacts\release"
}

$env:GIT_CONFIG_COUNT = "1"
$env:GIT_CONFIG_KEY_0 = "safe.directory"
$env:GIT_CONFIG_VALUE_0 = "*"
$env:GIT_CONFIG_GLOBAL = "NUL"

function Invoke-DotNet {
    param([Parameter(ValueFromRemainingArguments)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$stagingRoot = Join-Path ([IO.Path]::GetTempPath()) (
    "ryzen-smu-cli-package-" + [Guid]::NewGuid().ToString("N"))
$frameworkDependentDirectory = Join-Path $stagingRoot "framework-dependent"
$selfContainedDirectory = Join-Path $stagingRoot "self-contained"
$symbolsDirectory = Join-Path $stagingRoot "symbols"

$frameworkDependentArchive = Join-Path $OutputDirectory (
    "ryzen-smu-cli-v$Version-win-x64-framework-dependent.zip")
$selfContainedArchive = Join-Path $OutputDirectory (
    "ryzen-smu-cli-v$Version-win-x64-self-contained.zip")
$symbolsArchive = Join-Path $OutputDirectory (
    "ryzen-smu-cli-v$Version-symbols.zip")
$checksumsPath = Join-Path $OutputDirectory "checksums-sha256.txt"

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $frameworkDependentDirectory -Force |
    Out-Null
New-Item -ItemType Directory -Path $selfContainedDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $symbolsDirectory -Force | Out-Null

try {
    Push-Location $repositoryRoot
    try {
        Invoke-DotNet publish $projectPath `
            --configuration Release `
            --no-restore `
            --self-contained false `
            --output $frameworkDependentDirectory

        Invoke-DotNet publish $projectPath `
            --configuration Release `
            --self-contained true `
            "-p:EnableCompressionInSingleFile=true" `
            --output $selfContainedDirectory
    }
    finally {
        Pop-Location
    }

    Get-ChildItem -LiteralPath $frameworkDependentDirectory -Filter "*.pdb" |
        Copy-Item -Destination $symbolsDirectory
    Get-ChildItem -LiteralPath $frameworkDependentDirectory -Filter "*.pdb" |
        Remove-Item -Force
    Get-ChildItem -LiteralPath $selfContainedDirectory -Filter "*.pdb" |
        Remove-Item -Force

    $dotnetRoot = Split-Path -Parent (Get-Command dotnet).Source
    $dotnetLicense = Join-Path $dotnetRoot "LICENSE.txt"
    $dotnetNotices = Join-Path $dotnetRoot "ThirdPartyNotices.txt"
    if (-not (Test-Path -LiteralPath $dotnetLicense) -or
        -not (Test-Path -LiteralPath $dotnetNotices)) {
        throw "The installed .NET SDK does not contain its distribution notices."
    }

    Copy-Item -LiteralPath $dotnetLicense `
        -Destination (Join-Path $selfContainedDirectory "dotnet-LICENSE.txt")
    Copy-Item -LiteralPath $dotnetNotices `
        -Destination (
            Join-Path $selfContainedDirectory "dotnet-ThirdPartyNotices.txt")

    foreach ($archive in @(
            $frameworkDependentArchive,
            $selfContainedArchive,
            $symbolsArchive,
            $checksumsPath)) {
        if (Test-Path -LiteralPath $archive) {
            Remove-Item -LiteralPath $archive -Force
        }
    }

    Compress-Archive `
        -Path (Join-Path $frameworkDependentDirectory "*") `
        -DestinationPath $frameworkDependentArchive `
        -CompressionLevel Optimal
    Compress-Archive `
        -Path (Join-Path $selfContainedDirectory "*") `
        -DestinationPath $selfContainedArchive `
        -CompressionLevel Optimal
    Compress-Archive `
        -Path (Join-Path $symbolsDirectory "*") `
        -DestinationPath $symbolsArchive `
        -CompressionLevel Optimal

    $checksumLines = foreach ($archive in @(
            $frameworkDependentArchive,
            $selfContainedArchive,
            $symbolsArchive)) {
        $file = Get-Item -LiteralPath $archive
        $hash = Get-FileHash -LiteralPath $archive -Algorithm SHA256
        "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $file.Name
    }
    $checksumLines | Set-Content -LiteralPath $checksumsPath -Encoding Ascii

    Write-Host "Release packages:"
    Get-ChildItem -LiteralPath $OutputDirectory |
        Sort-Object Name |
        Format-Table Name, Length -AutoSize
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}
