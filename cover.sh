#!/bin/bash

BASE=$(dirname $0)
COVERAGE_DIR="$BASE/Coverage"
TEMP_RESULTS_DIR="$COVERAGE_DIR/temp"
HISTORY_DIR="$COVERAGE_DIR/history"

TEST_PROJECT_PATTERN=${TEST_PROJECT_PATTERN-'*.UnitTests.csproj'}
TEST_PARAMS=${TEST_PARAMS-"--nologo --no-restore"}
GENERATOR_PARAMS=${GENERATOR_PARAMS-"-filefilters:-*.g.cs"}

rm -rf $TEMP_RESULTS_DIR || true

if ! command -v reportgenerator &> /dev/null
then
    echo reportgenerator could not be found
    echo You may need to install it with:
    echo
    echo dotnet tool install -g dotnet-reportgenerator-globaltool
    exit 1
fi

find $BASE -iname $TEST_PROJECT_PATTERN \
    -exec dotnet test "{}" $TEST_PARAMS \
        --collect:'XPlat Code Coverage;Format=opencover' \
        --results-directory $TEMP_RESULTS_DIR \;

reportgenerator \
    -reports:"$TEMP_RESULTS_DIR/**/coverage.opencover.xml" \
    -reporttypes:'Html;Cobertura' \
    -targetdir:"$COVERAGE_DIR" \
    -assemblyfilters:"-$TEST_PROJECT_PATTERN" \
    -verbosity:Warning \
    -historydir:"$HISTORY_DIR" \
    "$GENERATOR_PARAMS"

echo
echo Finished report generation. Report is located here: $COVERAGE_DIR/index.html
