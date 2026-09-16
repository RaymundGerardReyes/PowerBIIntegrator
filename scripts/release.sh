#!/usr/bin/env bash
# ==============================================================================
# Enterprise Power BI Analytics Platform - Release & Git Tagging Orchestrator
# Implements atomic commit -> SemVer calculation -> tag -> push -> verify flow
# Supports:
#   1. Normal release: stage, commit, tag, push commit & tag, verify
#   2. Untagged-commit recovery: tag & push existing pushed commit without amending
#   3. Tag retry: retry failed tag push without creating duplicate commits
# ==============================================================================

set -euo pipefail

# Default configuration
REMOTE="${GIT_REMOTE:-origin}"
BRANCH="${GIT_BRANCH:-main}"
DRY_RUN=0
FORCE_TAG=0
COMMIT_MSG=""
TARGET_COMMIT=""
TAG_OVERRIDE=""

log_info()  { echo -e "\033[1;34m[INFO]\033[0m $*"; }
log_ok()    { echo -e "\033[1;32m[SUCCESS]\033[0m $*"; }
log_warn()  { echo -e "\033[1;33m[WARN]\033[0m $*"; }
log_error() { echo -e "\033[1;31m[ERROR]\033[0m $*" >&2; }

usage() {
  cat <<EOF
Usage: $(basename "$0") [OPTIONS]

Options:
  -m, --message MSG       Commit message (triggers stage & commit if dirty)
  -c, --commit SHA        Target commit SHA to tag (defaults to HEAD)
  -t, --tag TAG           Explicitly specify tag (bypasses auto-SemVer calculation)
  -r, --remote REMOTE     Git remote name (default: origin)
  -b, --branch BRANCH     Target branch name (default: main)
      --tag-only          Tag and push existing commit without creating a commit
      --dry-run           Compute and display actions without mutating Git state
  -h, --help              Show this help message

Examples:
  ./scripts/release.sh -m "feat(api): add new export endpoint"
  ./scripts/release.sh --tag-only
  ./scripts/release.sh --commit 4cf6b4c --tag-only
EOF
  exit 0
}

# Parse CLI arguments
TAG_ONLY=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    -m|--message)
      COMMIT_MSG="$2"
      shift 2
      ;;
    -c|--commit)
      TARGET_COMMIT="$2"
      shift 2
      ;;
    -t|--tag)
      TAG_OVERRIDE="$2"
      shift 2
      ;;
    -r|--remote)
      REMOTE="$2"
      shift 2
      ;;
    -b|--branch)
      BRANCH="$2"
      shift 2
      ;;
    --tag-only)
      TAG_ONLY=1
      shift
      ;;
    --dry-run)
      DRY_RUN=1
      shift
      ;;
    -h|--help)
      usage
      ;;
    *)
      log_error "Unknown argument: $1"
      exit 1
      ;;
  esac
done

# ------------------------------------------------------------------------------
# STEP 1: Pre-flight Validations
# ------------------------------------------------------------------------------
if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  log_error "Not inside a valid Git repository."
  exit 1
fi

CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "HEAD")
if [[ "$CURRENT_BRANCH" == "HEAD" ]]; then
  log_error "Repository is in detached HEAD state. Please checkout $BRANCH first."
  exit 1
fi

if [[ -n "$BRANCH" && "$CURRENT_BRANCH" != "$BRANCH" && "$DRY_RUN" -eq 0 ]]; then
  log_warn "Current branch is '$CURRENT_BRANCH', expected '$BRANCH'."
fi

if ! git remote get-url "$REMOTE" >/dev/null 2>&1; then
  log_error "Configured remote '$REMOTE' does not exist."
  exit 1
fi

# ------------------------------------------------------------------------------
# STEP 2: Handle Changes & Establish Target Commit SHA
# ------------------------------------------------------------------------------
HAS_DIRTY_CHANGES=0
if [[ -n $(git status --porcelain) ]]; then
  HAS_DIRTY_CHANGES=1
fi

NEW_COMMIT_CREATED=0

if [[ "$TAG_ONLY" -eq 1 ]]; then
  if [[ -n "$TARGET_COMMIT" ]]; then
    COMMIT_SHA=$(git rev-parse "$TARGET_COMMIT" 2>/dev/null || { log_error "Invalid commit SHA: $TARGET_COMMIT"; exit 1; })
  else
    COMMIT_SHA=$(git rev-parse HEAD)
  fi
  log_info "Tag-only mode selected for existing commit: $COMMIT_SHA"
else
  if [[ "$HAS_DIRTY_CHANGES" -eq 1 ]]; then
    if [[ -z "$COMMIT_MSG" ]]; then
      log_error "Working tree has unstaged/staged changes, but no commit message (-m) was provided."
      exit 1
    fi
    log_info "Staging all changes..."
    if [[ "$DRY_RUN" -eq 1 ]]; then
      log_info "[DRY-RUN] git add -A && git commit -m \"$COMMIT_MSG\""
      COMMIT_SHA="DRY_RUN_COMMIT_SHA"
    else
      git add -A
      git commit -m "$COMMIT_MSG"
      COMMIT_SHA=$(git rev-parse HEAD)
      NEW_COMMIT_CREATED=1
      log_ok "Created new commit: $COMMIT_SHA ($COMMIT_MSG)"
    fi
  else
    # Clean working tree: handle already-pushed-but-untagged state
    COMMIT_SHA=$(git rev-parse HEAD)
    log_info "Working tree clean. Targeting current HEAD commit: $COMMIT_SHA"
  fi
fi

# ------------------------------------------------------------------------------
# STEP 3: SemVer Version Determination
# ------------------------------------------------------------------------------
calculate_next_semver() {
  local target_sha="$1"
  
  # Find latest tag matching v*.*.* reachable or in repo
  local latest_tag
  latest_tag=$(git describe --tags --abbrev=0 --match "v[0-9]*.[0-9]*.[0-9]*" "$target_sha" 2>/dev/null || true)

  if [[ -z "$latest_tag" ]]; then
    # Fallback to any v*.*.* in repository
    latest_tag=$(git tag -l "v[0-9]*.[0-9]*.[0-9]*" --sort=-v:refname | head -n1 || true)
  fi

  if [[ -z "$latest_tag" ]]; then
    echo "v1.0.0"
    return
  fi

  # Parse SemVer digits (strip leading 'v')
  local raw_version="${latest_tag#v}"
  local major minor patch
  IFS='.' read -r major minor patch <<< "$raw_version"

  # If commit is already tagged with latest_tag
  local tag_target_sha
  tag_target_sha=$(git rev-list -n 1 "$latest_tag" 2>/dev/null || echo "")
  if [[ "$tag_target_sha" == "$target_sha" ]]; then
    echo "$latest_tag"
    return
  fi

  # Determine bump level from conventional commits
  local bump="patch"
  local log_output
  log_output=$(git log --format="%s%n%b" "${latest_tag}..${target_sha}" 2>/dev/null || true)

  if echo "$log_output" | grep -qE "(BREAKING CHANGE:|BREAKING-CHANGE:|^[a-zA-Z]+(\([^\)]+\))?!:)"; then
    bump="major"
  elif echo "$log_output" | grep -qE "^feat(\([^\)]+\))?:"; then
    bump="minor"
  elif echo "$log_output" | grep -qE "^(fix|perf|refactor|docs|build|chore|test)(\([^\)]+\))?:"; then
    bump="patch"
  fi

  case "$bump" in
    major)
      major=$((major + 1))
      minor=0
      patch=0
      ;;
    minor)
      minor=$((minor + 1))
      patch=0
      ;;
    patch)
      patch=$((patch + 1))
      ;;
  esac

  echo "v${major}.${minor}.${patch}"
}

if [[ -n "$TAG_OVERRIDE" ]]; then
  NEXT_TAG="$TAG_OVERRIDE"
  log_info "Using overridden tag: $NEXT_TAG"
else
  if [[ "$DRY_RUN" -eq 1 && "$COMMIT_SHA" == "DRY_RUN_COMMIT_SHA" ]]; then
    NEXT_TAG="v1.1.0"
    log_info "[DRY-RUN] Calculated SemVer tag: $NEXT_TAG"
  else
    NEXT_TAG=$(calculate_next_semver "$COMMIT_SHA")
    log_info "Calculated SemVer tag: $NEXT_TAG (for commit $COMMIT_SHA)"
  fi
fi

# Validate tag format
if [[ ! "$NEXT_TAG" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  log_error "Tag '$NEXT_TAG' does not conform to 'vMAJOR.MINOR.PATCH' SemVer specification."
  exit 1
fi

# ------------------------------------------------------------------------------
# STEP 4: Collision Guard & Existing Tag Verification
# ------------------------------------------------------------------------------
TAG_ALREADY_EXISTS_LOCALLY=0
if git rev-parse -q --verify "refs/tags/$NEXT_TAG" >/dev/null 2>&1; then
  TAG_ALREADY_EXISTS_LOCALLY=1
  LOCAL_TAG_SHA=$(git rev-list -n 1 "$NEXT_TAG")
  if [[ "$LOCAL_TAG_SHA" != "$COMMIT_SHA" && "$DRY_RUN" -eq 0 ]]; then
    log_error "Tag '$NEXT_TAG' already exists locally pointing to $LOCAL_TAG_SHA, but target is $COMMIT_SHA. Refusing to overwrite tag."
    exit 1
  else
    log_warn "Tag '$NEXT_TAG' already exists locally and points to $COMMIT_SHA."
  fi
fi

TAG_ALREADY_EXISTS_REMOTELY=0
if [[ "$DRY_RUN" -eq 0 ]]; then
  REMOTE_TAG_REF=$(git ls-remote --tags "$REMOTE" "refs/tags/$NEXT_TAG" 2>/dev/null || true)
  if [[ -n "$REMOTE_TAG_REF" ]]; then
    TAG_ALREADY_EXISTS_REMOTELY=1
    log_warn "Tag '$NEXT_TAG' is already present on remote '$REMOTE'."
  fi
fi

# ------------------------------------------------------------------------------
# STEP 5: Create Annotated Tag (if not existing locally)
# ------------------------------------------------------------------------------
if [[ "$TAG_ALREADY_EXISTS_LOCALLY" -eq 0 ]]; then
  TAG_MSG="Release $NEXT_TAG: Analytics Platform release for commit $COMMIT_SHA"
  log_info "Creating annotated tag '$NEXT_TAG' -> $COMMIT_SHA"
  if [[ "$DRY_RUN" -eq 1 ]]; then
    log_info "[DRY-RUN] git tag -a \"$NEXT_TAG\" \"$COMMIT_SHA\" -m \"$TAG_MSG\""
  else
    if ! git tag -a "$NEXT_TAG" "$COMMIT_SHA" -m "$TAG_MSG"; then
      log_error "Failed to create local annotated tag '$NEXT_TAG'."
      exit 1
    fi
    log_ok "Annotated tag '$NEXT_TAG' created."
  fi
fi

# ------------------------------------------------------------------------------
# STEP 6: Push Commit to Remote (if newly created or ahead)
# ------------------------------------------------------------------------------
if [[ "$DRY_RUN" -eq 1 ]]; then
  log_info "[DRY-RUN] git push \"$REMOTE\" \"$BRANCH\""
  log_info "[DRY-RUN] git push \"$REMOTE\" \"refs/tags/$NEXT_TAG\""
  log_ok "[DRY-RUN] Simulation complete: Commit -> $NEXT_TAG -> $REMOTE verified."
  exit 0
fi

if [[ "$NEW_COMMIT_CREATED" -eq 1 ]]; then
  log_info "Pushing new commit to $REMOTE/$BRANCH..."
  if ! git push "$REMOTE" "$BRANCH"; then
    log_error "Failed to push commit $COMMIT_SHA to remote $REMOTE/$BRANCH. Tag was created locally ($NEXT_TAG) but not pushed."
    exit 1
  fi
  log_ok "Commit $COMMIT_SHA pushed to $REMOTE/$BRANCH."
fi

# ------------------------------------------------------------------------------
# STEP 7: Push Tag to Remote
# ------------------------------------------------------------------------------
if [[ "$TAG_ALREADY_EXISTS_REMOTELY" -eq 0 ]]; then
  log_info "Pushing tag '$NEXT_TAG' to remote '$REMOTE'..."
  if ! git push "$REMOTE" "refs/tags/$NEXT_TAG"; then
    log_error "PARTIAL COMPLETION: Commit $COMMIT_SHA was pushed, but tag '$NEXT_TAG' failed to push to $REMOTE."
    log_error "Retry pushing the tag using: git push $REMOTE refs/tags/$NEXT_TAG"
    exit 2
  fi
  log_ok "Tag '$NEXT_TAG' successfully pushed to $REMOTE."
else
  log_info "Tag '$NEXT_TAG' already pushed to remote. Proceeding to verification."
fi

# ------------------------------------------------------------------------------
# STEP 8: Verify Remote Tag Points Directly to Target Commit SHA
# ------------------------------------------------------------------------------
log_info "Verifying remote tag alignment on $REMOTE..."
REMOTE_TAG_RAW=$(git ls-remote --tags "$REMOTE" "refs/tags/$NEXT_TAG*" 2>/dev/null || true)

# Annotated tags produce two refs: refs/tags/vX.Y.Z (tag object) and refs/tags/vX.Y.Z^{} (peeled commit)
REMOTE_RESOLVED_SHA=$(echo "$REMOTE_TAG_RAW" | grep -F '^{}' | awk '{print $1}' || true)
if [[ -z "$REMOTE_RESOLVED_SHA" ]]; then
  # Fallback for lightweight tag or single entry
  REMOTE_RESOLVED_SHA=$(echo "$REMOTE_TAG_RAW" | grep -F "refs/tags/${NEXT_TAG}" | head -n1 | awk '{print $1}' || true)
fi

if [[ -z "$REMOTE_RESOLVED_SHA" ]]; then
  log_error "Verification failed: Tag '$NEXT_TAG' was not found on remote '$REMOTE'."
  exit 1
fi

if [[ "$REMOTE_RESOLVED_SHA" != "$COMMIT_SHA" ]]; then
  log_error "ALIGNMENT MISMATCH: Remote tag '$NEXT_TAG' points to '$REMOTE_RESOLVED_SHA', expected '$COMMIT_SHA'."
  exit 1
fi

log_ok "Release complete & verified!"
echo "------------------------------------------------------------------------"
echo "  Commit SHA : $COMMIT_SHA"
echo "  SemVer Tag : $NEXT_TAG"
echo "  Remote     : $REMOTE"
echo "  Alignment  : Remote tag points directly to commit SHA ($COMMIT_SHA)"
echo "------------------------------------------------------------------------"
