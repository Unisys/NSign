param(
    $TestProjectPattern = '*.UnitTests',
    $TestParams = @('--nologo', '--no-restore'),
    $GeneratorParams = @('-filefilters:-*.g.cs')
)

$base = $PSScriptRoot
$coverageDir = "$base/Coverage"
$tempResultsDir = "$coverageDir/temp"
$historyDir = "$coverageDir/history"

Remove-Item -Recurse $tempResultsDir -EA SilentlyContinue

Get-ChildItem -Path $base -Recurse -Filter $TestProjectPattern -Directory -Name |
    ForEach-Object {
        dotnet test "$base/$_" $TestParams --collect:'XPlat Code Coverage' -r $tempResultsDir
    }

reportgenerator -reports:"$tempResultsDir/**/coverage.cobertura.xml" -reporttypes:'Html;Cobertura' `
    -targetdir:"$coverageDir" -assemblyfilters:"-$TestProjectPattern" $GeneratorParams -verbosity:Warning `
    -historydir:"$historyDir"

Write-Host
Write-Host "Finished report generation to $coverageDir/index.html"
