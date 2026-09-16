#!/usr/bin/env bash
# ==============================================================================
# Automated Test Suite for scripts/release.sh
# Tests:
#   1. Normal flow: stage -> commit -> SemVer -> tag -> push commit & tag -> verify
#   2. Existing pushed commit with missing tag (no duplicate commit)
#   3. Existing tag collision detection (refuse silent overwrite)
#   4. Tag push failure reporting (partial completion) and retry
#   5. SemVer conventional commit progression rules (patch, minor, major)
#   6. Exact commit SHA referencing verification
# ==============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RELEASE_SCRIPT="$(cd "$SCRIPT_DIR/.." && pwd)/release.sh"

TEST_ROOT=$(mktemp -d -t release-test-XXXXXX)
trap 'rm -rf "$TEST_ROOT"' EXIT

PASSED=0
FAILED=0

assert_eq() {
  local expected="$1"
  local actual="$2"
  local msg="$3"
  if [[ "$expected" != "$actual" ]]; then
    echo "  [FAIL] $msg: expected '$expected', got '$actual'"
    FAILED=$((FAILED + 1))
    return 1
  else
    echo "  [PASS] $msg"
    PASSED=$((PASSED + 1))
    return 0
  fi
}

assert_true() {
  local condition="$1"
  local msg="$2"
  if eval "$condition"; then
    echo "  [PASS] $msg"
    PASSED=$((PASSED + 1))
  else
    echo "  [FAIL] $msg"
    FAILED=$((FAILED + 1))
    return 1
  fi
}

setup_sandbox() {
  local test_name="$1"
  local sandbox_dir="$TEST_ROOT/$test_name"
  mkdir -p "$sandbox_dir"
  cd "$sandbox_dir"

  # Create bare remote repository
  git init --bare remote.git >/dev/null

  # Create work repository
  git init repo >/dev/null
  cd repo
  git config user.name "Test Author"
  git config user.email "test@example.com"
  git remote add origin ../remote.git
  git checkout -b main >/dev/null 2>&1 || git checkout main >/dev/null 2>&1

  # Create initial commit and tag v1.0.0
  echo "initial" > file.txt
  git add file.txt
  git commit -m "feat(core): initial baseline v1.0.0" >/dev/null
  git tag -a v1.0.0 -m "Release v1.0.0"
  git push origin main --tags >/dev/null 2>&1
}

echo "========================================================================"
echo "Running Release Script Test Suite"
echo "========================================================================"

# ------------------------------------------------------------------------------
# TEST 1: Normal Flow
# ------------------------------------------------------------------------------
echo "Test 1: Normal release flow (add -> commit -> SemVer -> tag -> push)"
setup_sandbox "test1_normal_flow"

echo "new feature code" >> file.txt
bash "$RELEASE_SCRIPT" -m "feat(api): add analytics query endpoint" -r origin -b main >/dev/null

COMMIT_SHA=$(git rev-parse HEAD)
assert_eq "v1.1.0" "$(git tag --points-at HEAD)" "HEAD is tagged with v1.1.0"
assert_eq "$COMMIT_SHA" "$(git rev-list -n 1 v1.1.0)" "Local tag points to commit SHA"

# Verify remote receives commit and tag
REMOTE_COMMIT=$(git ls-remote origin refs/heads/main | awk '{print $1}')
REMOTE_TAG_COMMIT=$(git ls-remote --tags origin "refs/tags/v1.1.0*" | grep -F '^{}' | awk '{print $1}')
assert_eq "$COMMIT_SHA" "$REMOTE_COMMIT" "Remote main branch has commit SHA"
assert_eq "$COMMIT_SHA" "$REMOTE_TAG_COMMIT" "Remote tag v1.1.0 points to commit SHA"

# ------------------------------------------------------------------------------
# TEST 2: Existing Pushed Commit with Missing Tag (Already-Pushed Recovery)
# ------------------------------------------------------------------------------
echo "Test 2: Existing pushed commit with missing tag recovery"
setup_sandbox "test2_untagged_recovery"

# Create a commit and push it directly without tagging
echo "gap closure" >> file.txt
git add file.txt
git commit -m "feat(reporting): document generation engine" >/dev/null
git push origin main >/dev/null 2>&1

ORIGINAL_COMMIT_SHA=$(git rev-parse HEAD)
assert_true "[[ -z '$(git tag --points-at HEAD)' ]]" "Precondition: HEAD has no tag"

# Run release with --tag-only on clean tree
bash "$RELEASE_SCRIPT" --tag-only -r origin -b main >/dev/null

AFTER_COMMIT_SHA=$(git rev-parse HEAD)
assert_eq "$ORIGINAL_COMMIT_SHA" "$AFTER_COMMIT_SHA" "No duplicate commit created"
assert_eq "v1.1.0" "$(git tag --points-at HEAD)" "Existing commit received tag v1.1.0"

REMOTE_TAG_COMMIT=$(git ls-remote --tags origin "refs/tags/v1.1.0*" | grep -F '^{}' | awk '{print $1}')
assert_eq "$ORIGINAL_COMMIT_SHA" "$REMOTE_TAG_COMMIT" "Remote tag points directly to existing commit SHA"

# ------------------------------------------------------------------------------
# TEST 3: Existing Tag Collision Guard
# ------------------------------------------------------------------------------
echo "Test 3: Existing tag collision guard"
setup_sandbox "test3_tag_collision"

# Attempt to create tag that already exists for a different target
echo "some fix" >> file.txt
git add file.txt
git commit -m "fix(compiler): fix TMDL indentation" >/dev/null

set +e
bash "$RELEASE_SCRIPT" --tag v1.0.0 -r origin -b main >/dev/null 2>&1
EXIT_CODE=$?
set -e

assert_eq "1" "$EXIT_CODE" "Refuses to overwrite existing v1.0.0 tag"
V100_TARGET=$(git rev-list -n 1 v1.0.0)
HEAD_SHA=$(git rev-parse HEAD)
assert_true "[[ '$V100_TARGET' != '$HEAD_SHA' ]]" "v1.0.0 tag target remains uncorrupted"

# ------------------------------------------------------------------------------
# TEST 4: Tag Push Failure & Partial Completion
# ------------------------------------------------------------------------------
echo "Test 4: Tag push failure reporting and retry path"
setup_sandbox "test4_push_failure"

# Set up pre-receive hook in remote that rejects tags
cat > ../remote.git/hooks/pre-receive <<'EOF'
#!/bin/sh
while read oldrev newrev refname; do
  case "$refname" in
    refs/tags/*)
      echo "Remote rejected tag push for testing" >&2
      exit 1
      ;;
  esac
done
EOF
chmod +x ../remote.git/hooks/pre-receive

echo "patch fix" >> file.txt
set +e
bash "$RELEASE_SCRIPT" -m "fix(rules): adjust boundary check" -r origin -b main >/dev/null 2>&1
EXIT_CODE=$?
set -e

assert_eq "2" "$EXIT_CODE" "Reports exit code 2 (partial completion) when tag push fails"
COMMIT_SHA=$(git rev-parse HEAD)
assert_eq "$COMMIT_SHA" "$(git rev-parse origin/main)" "Commit was successfully pushed to remote"
assert_eq "v1.0.1" "$(git tag --points-at HEAD)" "Local tag v1.0.1 exists on commit"
assert_true "[[ -z '$(git ls-remote --tags origin refs/tags/v1.0.1)' ]]" "Remote tag does not exist yet"

# Now remove the rejecting hook to simulate retry
rm -f ../remote.git/hooks/pre-receive

# Retry pushing tag
bash "$RELEASE_SCRIPT" --tag-only -r origin -b main >/dev/null

REMOTE_TAG_COMMIT=$(git ls-remote --tags origin "refs/tags/v1.0.1*" | grep -F '^{}' | awk '{print $1}')
assert_eq "$COMMIT_SHA" "$REMOTE_TAG_COMMIT" "Retry successfully pushed tag v1.0.1 to remote"

# ------------------------------------------------------------------------------
# TEST 5: SemVer Progression Rules
# ------------------------------------------------------------------------------
echo "Test 5: Conventional Commit SemVer progression rules"
setup_sandbox "test5_semver_rules"

# 5a: Patch increment (fix:)
echo "fix1" >> file.txt
git add file.txt
git commit -m "fix: resolve memory leak in streaming reader" >/dev/null
git push origin main >/dev/null 2>&1
bash "$RELEASE_SCRIPT" --tag-only -r origin -b main >/dev/null
assert_eq "v1.0.1" "$(git tag --points-at HEAD)" "fix commit bumps patch (v1.0.0 -> v1.0.1)"

# 5b: Minor increment (feat:)
echo "feat1" >> file.txt
git add file.txt
git commit -m "feat(mcp): add mcp server host" >/dev/null
git push origin main >/dev/null 2>&1
bash "$RELEASE_SCRIPT" --tag-only -r origin -b main >/dev/null
assert_eq "v1.1.0" "$(git tag --points-at HEAD)" "feat commit bumps minor (v1.0.1 -> v1.1.0)"

# 5c: Major increment (BREAKING CHANGE:)
echo "breaking1" >> file.txt
git add file.txt
git commit -m "feat(compiler)!: redesign intermediate representation" >/dev/null
git push origin main >/dev/null 2>&1
bash "$RELEASE_SCRIPT" --tag-only -r origin -b main >/dev/null
assert_eq "v2.0.0" "$(git tag --points-at HEAD)" "breaking change bumps major (v1.1.0 -> v2.0.0)"

# ------------------------------------------------------------------------------
# TEST 6: Exact Commit SHA Referencing Verification
# ------------------------------------------------------------------------------
echo "Test 6: Verify remote tag points exactly to target commit SHA"
setup_sandbox "test6_exact_sha"

echo "audit log" >> file.txt
bash "$RELEASE_SCRIPT" -m "feat(audit): add structured JSON logger" -r origin -b main >/dev/null
CREATED_COMMIT=$(git rev-parse HEAD)
RESOLVED_TAG_COMMIT=$(git ls-remote --tags origin "refs/tags/v1.1.0*" | grep -F '^{}' | awk '{print $1}')
assert_eq "$CREATED_COMMIT" "$RESOLVED_TAG_COMMIT" "Remote tag references the exact commit SHA created by operation"

# ------------------------------------------------------------------------------
# SUMMARY
# ------------------------------------------------------------------------------
echo "========================================================================"
echo "Results: $PASSED passed, $FAILED failed"
echo "========================================================================"

if [[ "$FAILED" -gt 0 ]]; then
  exit 1
fi
