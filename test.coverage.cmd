@echo off

setlocal ENABLEDELAYEDEXPANSION

rem The pattern to identify test project directories and assemblies likewise.
set TESTPROJECTS_PATTERN=*.UnitTests
rem Do not restore NuGet packages (but build the solution/projects) as part of testing.
set ADDTL_TESTPARAMS=--nologo --no-restore
rem Do not generate report data for generated code files (*.g.cs)
set ADDTL_GENERATORPARAMS=-filefilters:-*.g.cs

set BASE=%~dp0
set COVERAGE_DIR=%BASE%\Coverage
set TEMPRESULTS_DIR=%COVERAGE_DIR%\Temp

rem Delete the directory with temporary coverage results (per project) first. We don't want to include older coverage data.
rd /q /s %TEMPRESULTS_DIR% 2>nul

rem Run tests for all test projects.
for /F "tokens=* usebackq" %%p in (`dir /b /ad /s %BASE%\%TESTPROJECTS_PATTERN%`) do (
    dotnet test %%p --collect:"XPlat Code Coverage" --results-directory %TEMPRESULTS_DIR% %ADDTL_TESTPARAMS%
)

rem Run the individual coverage reports through the reportgenerator tool to combine and generate HTML output.
reportgenerator -reports:%TEMPRESULTS_DIR%\**\coverage.cobertura.xml -reporttypes:Html;Cobertura ^
    -targetdir:%COVERAGE_DIR% -assemblyfilters:-%TESTPROJECTS_PATTERN% %ADDTL_GENERATORPARAMS% -verbosity:Warning

echo.
echo Finished generating report to %COVERAGE_DIR%\index.html

endlocal
