[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$PublishDirectory
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot

# Windows Git treats a repository reached through WSL's UNC share as
# non-local. Supply a process-scoped safe-directory value so ZenStates-Core's
# version target can inspect its own Git metadata without changing user config.
$env:GIT_CONFIG_COUNT = "1"
$env:GIT_CONFIG_KEY_0 = "safe.directory"
$env:GIT_CONFIG_VALUE_0 = "*"
$env:GIT_CONFIG_GLOBAL = "NUL"

if ([string]::IsNullOrWhiteSpace($PublishDirectory)) {
    $PublishDirectory = Join-Path $repositoryRoot "artifacts\win-x64"
}

function Invoke-DotNet {
    param([Parameter(ValueFromRemainingArguments)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repositoryRoot
try {
    Invoke-DotNet restore ".\ryzen-smu-cli.sln"
    Invoke-DotNet build ".\ryzen-smu-cli.sln" `
        --configuration $Configuration `
        --no-restore `
        "-p:TargetFrameworks=net8.0-windows" `
        "-p:IsWindows=false"
    Invoke-DotNet test ".\ryzen-smu-cli.sln" `
        --configuration $Configuration `
        --no-build
    Invoke-DotNet publish ".\ryzen-smu-cli\ryzen-smu-cli.csproj" `
        --configuration $Configuration `
        --no-restore `
        --output $PublishDirectory
}
finally {
    Pop-Location
}
