param(
    [Parameter(Mandatory = $true)][string]$UnityEditor,
    [switch]$Author,
    [switch]$Build,
    [switch]$Visuals
)
$ErrorActionPreference = 'Stop'
$gameProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$gameLogRoot = Join-Path $gameProjectRoot 'Logs'
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw 'Use Unity.exe 6000.3.13f1.' }
New-Item -ItemType Directory -Path $gameLogRoot -Force | Out-Null
function Invoke-Unity([string[]]$Arguments) {
    $gameArguments = $Arguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }
    $gameProcess = Start-Process -FilePath $UnityEditor -ArgumentList $gameArguments -WorkingDirectory $gameProjectRoot -WindowStyle Hidden -PassThru
    $gameProcess.WaitForExit()
    $gameProcess.Refresh()
    if ($gameProcess.ExitCode -ne 0) { throw "Unity failed ($($gameProcess.ExitCode)). Inspect $gameLogRoot." }
}
Push-Location $gameProjectRoot
try {
    if ($Author) {
        Invoke-Unity @('-batchmode', '-nographics', '-projectPath', $gameProjectRoot, '-executeMethod',
            'GameUp.Editor.ProductionProjectSetup.Author', '-quit', '-logFile', (Join-Path $gameLogRoot 'production-author.log'))
    }
    $gameResults = Join-Path $gameLogRoot 'production-tests.xml'
    $gameTestArguments = @('-batchmode', '-projectPath', $gameProjectRoot, '-runTests', '-testPlatform', 'EditMode',
        '-testResults', $gameResults, '-logFile', (Join-Path $gameLogRoot 'production-tests.log'))
    if (-not $Visuals) { $gameTestArguments += '-nographics' }
    Invoke-Unity $gameTestArguments
    [xml]$gameReport = Get-Content -LiteralPath $gameResults -Raw
    if ($gameReport.'test-run'.result -ne 'Passed') { throw 'Tests failed. Inspect production-tests.xml.' }
    Write-Output ("Tests: {0} passed / {1} total" -f $gameReport.'test-run'.passed, $gameReport.'test-run'.total)
    if ($Build) {
        Invoke-Unity @('-batchmode', '-nographics', '-projectPath', $gameProjectRoot, '-executeMethod',
            'GameUp.Editor.ProductionProjectSetup.BuildWindows', '-quit', '-logFile', (Join-Path $gameLogRoot 'production-build.log'))
        Write-Output 'Windows: Builds\Windows\AntalyaDenetimMasasi.exe'
    }
}
finally { Pop-Location }
