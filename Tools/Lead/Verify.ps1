param(
    [Parameter(Mandatory = $true)][string]$UnityEditor,
    [switch]$Build,
    [switch]$Smoke
)

$ErrorActionPreference = 'Stop'
$leadProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$leadLogRoot = Join-Path $leadProjectRoot 'Logs'
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw 'UnityEditor must point to Unity.exe (6000.3.13f1).' }
New-Item -ItemType Directory -Path $leadLogRoot -Force | Out-Null

function Invoke-LeadProcess([string]$Executable, [string[]]$Arguments) {
    # Start-Process joins ArgumentList; explicitly quote each path/token for Windows.
    $leadQuotedArguments = $Arguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }
    $leadProcess = Start-Process -FilePath $Executable -ArgumentList $leadQuotedArguments -WorkingDirectory $leadProjectRoot -WindowStyle Hidden -PassThru
    $leadProcess.WaitForExit()
    $leadProcess.Refresh()
    if ($leadProcess.ExitCode -ne 0) { throw "Process failed ($($leadProcess.ExitCode)). Inspect Logs in $leadProjectRoot." }
}

Push-Location $leadProjectRoot
try {
    Invoke-LeadProcess $UnityEditor @('-batchmode', '-nographics', '-projectPath', $leadProjectRoot,
        '-executeMethod', 'GameUp.Lead.Editor.LeadProjectSetup.EnsureScene', '-quit', '-logFile', (Join-Path $leadLogRoot 'lead-setup.log'))

    $leadResults = Join-Path $leadLogRoot 'lead-editmode-results.xml'
    Invoke-LeadProcess $UnityEditor @('-batchmode', '-nographics', '-projectPath', $leadProjectRoot,
        '-runTests', '-testPlatform', 'EditMode', '-testFilter', 'GameUp.Lead.Tests', '-testResults', $leadResults,
        '-logFile', (Join-Path $leadLogRoot 'lead-tests.log'))
    [xml]$leadTestReport = Get-Content -LiteralPath $leadResults -Raw
    if ($leadTestReport.'test-run'.result -ne 'Passed') { throw 'EditMode tests did not pass.' }
    Write-Output ("EditMode: {0} passed / {1} total" -f $leadTestReport.'test-run'.passed, $leadTestReport.'test-run'.total)

    if ($Build -or $Smoke) {
        Invoke-LeadProcess $UnityEditor @('-batchmode', '-nographics', '-projectPath', $leadProjectRoot,
            '-executeMethod', 'GameUp.Lead.Editor.LeadProjectSetup.BuildWindows', '-quit',
            '-logFile', (Join-Path $leadLogRoot 'lead-build.log'))
        Write-Output 'Windows prototype: Builds\LeadPrototype\GameUP32.exe'
    }
    if ($Smoke) {
        $leadSmokeOutput = Join-Path $leadLogRoot ('Smoke-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
        Invoke-LeadProcess (Join-Path $leadProjectRoot 'Builds\LeadPrototype\GameUP32.exe') @(
            '-batchmode', '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
            '-logFile', (Join-Path $leadLogRoot 'lead-player.log'), '--lead-smoke-test', ('--lead-output=' + $leadSmokeOutput))
        $leadSmokeResult = Get-Content -LiteralPath (Join-Path $leadSmokeOutput 'result.json') -Raw | ConvertFrom-Json
        if ($leadSmokeResult.result -ne 'PASS') { throw 'Player smoke test did not pass.' }
        Write-Output ("Player smoke: PASS; screenshots and result in {0}" -f $leadSmokeOutput)
    }
}
finally { Pop-Location }
