#!/usr/bin/env bash
set -euo pipefail
CATEGORY="${1:-all}"
SOLUTION="AnalyticsPlatform"

run() { echo "==> dotnet test tests/$SOLUTION.$1"; dotnet test "tests/$SOLUTION.$1" --no-build --nologo; }

case "$CATEGORY" in
  unit)        run UnitTests ;;
  integration) run IntegrationTests ;;
  path)        run PathTests ;;
  regression)  run RegressionTests ;;
  e2e)         run E2ETests ;;
  security)    run SecurityTests ;;
  all)
    run UnitTests
    run IntegrationTests
    run PathTests
    run RegressionTests
    run SecurityTests
    run E2ETests
    ;;
  *) echo "Usage: ./run-tests.sh [unit|integration|path|regression|e2e|security|all]"; exit 1 ;;
esac
