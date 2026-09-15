#!/usr/bin/env bash
set -euo pipefail
echo "== Backend tests =="
(cd backend && ./run-tests.sh all)
echo "== Frontend tests =="
(cd frontend && npm run test:unit && npm run test:integration && npm run test:path && npm run test:regression && npm run test:security)
echo "All test suites completed."
