<#
.SYNOPSIS
Enterprise Power BI Analytics Platform - Release & Git Tagging Orchestrator (PowerShell)
.DESCRIPTION
Implements atomic commit -> SemVer calculation -> tag -> push -> verify flow.
Supports:
  1. Normal release: stage, commit, tag, push commit & tag, verify
  2. Untagged-commit recovery: tag & push existing pushed commit without amending
  3. Tag retry: retry failed tag push without creating duplicate commits
#>

[CmdletBinding()]
param(
    [Alias("m")]
    [string]$Message,

    [Alias("c")]
    [string]$Commit,

    [Alias("t")]
    [string]$Tag,

    [string]$TagMessage,

    [Alias("r")]
    [string]$Remote = "origin",

    [Alias("b")]
    [string]$Branch = "main",

    [switch]$TagOnly,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Write-Info([string]$msg) {
    Write-Host "[INFO] $msg" -ForegroundColor Cyan
}

function Write-Success([string]$msg) {
    Write-Host "[SUCCESS] $msg" -ForegroundColor Green
}

function Write-Warning([string]$msg) {
    Write-Host "[WARN] $msg" -ForegroundColor Yellow
}

function Write-ErrorMsg([string]$msg) {
    Write-Host "[ERROR] $msg" -ForegroundColor Red
}

# STEP 1: Pre-flight Validations
$isInsideRepo = (git rev-parse --is-inside-work-tree 2>$null)
if ($LASTEXITCODE -ne 0 -or $isInsideRepo -ne "true") {
    Write-ErrorMsg "Not inside a valid Git repository."
    exit 1
}

$currentBranch = (git rev-parse --abbrev-ref HEAD 2>$null)
if ($currentBranch) { $currentBranch = $currentBranch.Trim() }
if ($currentBranch -eq "HEAD") {
    Write-ErrorMsg "Repository is in detached HEAD state. Please checkout $Branch first."
    exit 1
}

if ($Branch -and $currentBranch -ne $Branch -and -not $DryRun) {
    Write-Warning "Current branch is '$currentBranch', expected '$Branch'."
}

$remoteUrl = (git remote get-url $Remote 2>$null)
if ($LASTEXITCODE -ne 0 -or -not $remoteUrl) {
    Write-ErrorMsg "Configured remote '$Remote' does not exist."
    exit 1
}

# STEP 2: Handle Changes & Establish Target Commit SHA
$dirtyChanges = @(git status --porcelain 2>$null)
$hasDirtyChanges = [bool]($dirtyChanges.Count -gt 0 -and (-not [string]::IsNullOrWhiteSpace($dirtyChanges[0])))
$newCommitCreated = $false

if ($TagOnly) {
    if ($Commit) {
        $commitSha = (git rev-parse $Commit 2>$null)
        if ($LASTEXITCODE -ne 0 -or -not $commitSha) {
            Write-ErrorMsg "Invalid commit SHA: $Commit"
            exit 1
        }
        $commitSha = $commitSha.Trim()
    } else {
        $commitSha = (git rev-parse HEAD).Trim()
    }
    Write-Info "Tag-only mode selected for existing commit: $commitSha"
} else {
    if ($hasDirtyChanges) {
        if (-not $Message) {
            Write-ErrorMsg "Working tree has unstaged/staged changes, but no commit message (-Message / -m) was provided."
            exit 1
        }
        Write-Info "Staging all changes..."
        if ($DryRun) {
            Write-Info "[DRY-RUN] git add -A && git commit -m `"$Message`""
            $commitSha = "DRY_RUN_COMMIT_SHA"
        } else {
            git add -A
            git commit -m $Message
            if ($LASTEXITCODE -ne 0) {
                Write-ErrorMsg "Failed to commit changes."
                exit 1
            }
            $commitSha = (git rev-parse HEAD).Trim()
            $newCommitCreated = $true
            Write-Success "Created new commit: $commitSha ($Message)"
        }
    } else {
        $commitSha = (git rev-parse HEAD).Trim()
        Write-Info "Working tree clean. Targeting current HEAD commit: $commitSha"
    }
}

# STEP 3: SemVer Version Determination
function Get-NextSemVer([string]$targetSha) {
    $latestTag = (git describe --tags --abbrev=0 --match "v[0-9]*.[0-9]*.[0-9]*" $targetSha 2>$null)
    if (-not $latestTag) {
        $latestTag = (git tag -l "v[0-9]*.[0-9]*.[0-9]*" --sort=-v:refname 2>$null | Select-Object -First 1)
    }
    if (-not $latestTag) {
        return "v1.0.0"
    }
    $latestTag = $latestTag.Trim()

    if ($latestTag -notmatch '^v(\d+)\.(\d+)\.(\d+)$') {
        return "v1.0.0"
    }
    $major = [int]$Matches[1]
    $minor = [int]$Matches[2]
    $patch = [int]$Matches[3]

    $tagTargetSha = (git rev-list -n 1 $latestTag 2>$null)
    if ($tagTargetSha) { $tagTargetSha = $tagTargetSha.Trim() }
    if ($tagTargetSha -eq $targetSha) {
        return $latestTag
    }

    $logOutput = (git log --format="%s%n%b" "${latestTag}..${targetSha}" 2>$null)
    $logText = ($logOutput -join "`n")

    $bump = "patch"
    if ($logText -match '(?m)(BREAKING CHANGE:|BREAKING-CHANGE:|^[a-zA-Z]+(\([^\)]+\))?!:)') {
        $bump = "major"
    } elseif ($logText -match '(?m)^feat(\([^\)]+\))?:') {
        $bump = "minor"
    }

    switch ($bump) {
        "major" { return "v$($major + 1).0.0" }
        "minor" { return "v$($major).$($minor + 1).0" }
        "patch" { return "v$($major).$($minor).$($patch + 1)" }
    }
}

if ($Tag) {
    $nextTag = $Tag.Trim()
    Write-Info "Using overridden tag: $nextTag"
} else {
    if ($DryRun -and $commitSha -eq "DRY_RUN_COMMIT_SHA") {
        $latestTag = (git tag -l "v[0-9]*.[0-9]*.[0-9]*" --sort=-v:refname 2>$null | Select-Object -First 1)
        if ($latestTag -and ($latestTag.Trim() -match '^v(\d+)\.(\d+)\.(\d+)$')) {
            $major = [int]$Matches[1]
            $minor = [int]$Matches[2]
            $patch = [int]$Matches[3]
            $bump = "patch"
            if ($Message -match '(?m)(BREAKING CHANGE:|BREAKING-CHANGE:|^[a-zA-Z]+(\([^\)]+\))?!:)') {
                $bump = "major"
            } elseif ($Message -match '(?m)^feat(\([^\)]+\))?:') {
                $bump = "minor"
            }
            switch ($bump) {
                "major" { $nextTag = "v$($major + 1).0.0" }
                "minor" { $nextTag = "v$($major).$($minor + 1).0" }
                "patch" { $nextTag = "v$($major).$($minor).$($patch + 1)" }
            }
        } else {
            $nextTag = "v1.0.0"
        }
        Write-Info "[DRY-RUN] Calculated SemVer tag: $nextTag"
    } else {
        $nextTag = Get-NextSemVer $commitSha
        Write-Info "Calculated SemVer tag: $nextTag (for commit $commitSha)"
    }
}

if ($nextTag -notmatch '^v\d+\.\d+\.\d+$') {
    Write-ErrorMsg "Tag '$nextTag' does not conform to 'vMAJOR.MINOR.PATCH' SemVer specification."
    exit 1
}

# STEP 4: Collision Guard & Existing Tag Verification
$tagAlreadyExistsLocally = $false
$localTagSha = (git rev-parse -q --verify "refs/tags/$nextTag" 2>$null)
if ($LASTEXITCODE -eq 0 -and $localTagSha) {
    $tagAlreadyExistsLocally = $true
    $resolvedLocalSha = (git rev-list -n 1 $nextTag 2>$null).Trim()
    if ($resolvedLocalSha -ne $commitSha -and -not $DryRun) {
        Write-ErrorMsg "Tag '$nextTag' already exists locally pointing to $resolvedLocalSha, but target is $commitSha. Refusing to overwrite tag."
        exit 1
    } else {
        Write-Warning "Tag '$nextTag' already exists locally and points to $commitSha."
    }
}

$tagAlreadyExistsRemotely = $false
if (-not $DryRun) {
    $remoteTagRef = (git ls-remote --tags $Remote "refs/tags/$nextTag" 2>$null)
    if ($remoteTagRef) {
        $tagAlreadyExistsRemotely = $true
        Write-Warning "Tag '$nextTag' is already present on remote '$Remote'."
    }
}

# STEP 5: Create Annotated Tag
if (-not $tagAlreadyExistsLocally) {
    $tagMsg = if ($TagMessage) { $TagMessage } else { "Release $($nextTag): Power BI Analytics Platform release for commit $commitSha" }
    Write-Info "Creating annotated tag '$nextTag' -> $commitSha"
    if ($DryRun) {
        Write-Info "[DRY-RUN] git tag -a `"$nextTag`" `"$commitSha`" -m `"$tagMsg`""
    } else {
        git tag -a $nextTag $commitSha -m $tagMsg
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMsg "Failed to create local annotated tag '$nextTag'."
            exit 1
        }
        Write-Success "Annotated tag '$nextTag' created."
    }
}

# STEP 6: Push Commit to Remote
if ($DryRun) {
    Write-Info "[DRY-RUN] git push `"$Remote`" `"$Branch`""
    Write-Info "[DRY-RUN] git push `"$Remote`" `"refs/tags/$nextTag`""
    Write-Success "[DRY-RUN] Simulation complete: Commit -> $nextTag -> $Remote verified."
    exit 0
}

if ($newCommitCreated) {
    Write-Info "Pushing new commit to $Remote/$Branch..."
    git push $Remote $Branch
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMsg "Failed to push commit $commitSha to remote $Remote/$Branch. Tag was created locally ($nextTag) but not pushed."
        exit 1
    }
    Write-Success "Commit $commitSha pushed to $Remote/$Branch."
}

# STEP 7: Push Tag to Remote
if (-not $tagAlreadyExistsRemotely) {
    Write-Info "Pushing tag '$nextTag' to remote '$Remote'..."
    git push $Remote "refs/tags/$nextTag"
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorMsg "PARTIAL COMPLETION: Commit $commitSha was pushed, but tag '$nextTag' failed to push to $Remote."
        Write-ErrorMsg "Retry pushing the tag using: git push $Remote refs/tags/$nextTag"
        exit 2
    }
    Write-Success "Tag '$nextTag' successfully pushed to $Remote."
} else {
    Write-Info "Tag '$nextTag' already pushed to remote. Proceeding to verification."
}

# STEP 8: Verify Remote Tag
Write-Info "Verifying remote tag alignment on $Remote..."
$remoteTagRaw = (git ls-remote --tags $Remote "refs/tags/$nextTag*" 2>$null)
$remoteResolvedSha = ""
foreach ($line in ($remoteTagRaw -split "`r?`n")) {
    if ($line -like "*^{}") {
        $remoteResolvedSha = ($line -split "\s+")[0]
        break
    }
}
if (-not $remoteResolvedSha) {
    foreach ($line in ($remoteTagRaw -split "`r?`n")) {
        if ($line -like "*refs/tags/$nextTag*") {
            $remoteResolvedSha = ($line -split "\s+")[0]
            break
        }
    }
}

if (-not $remoteResolvedSha) {
    Write-ErrorMsg "Verification failed: Tag '$nextTag' was not found on remote '$Remote'."
    exit 1
}

if ($remoteResolvedSha -ne $commitSha) {
    Write-ErrorMsg "ALIGNMENT MISMATCH: Remote tag '$nextTag' points to '$remoteResolvedSha', expected '$commitSha'."
    exit 1
}

Write-Success "Release complete & verified!"
Write-Host "------------------------------------------------------------------------"
Write-Host "  Commit SHA : $commitSha"
Write-Host "  SemVer Tag : $nextTag"
Write-Host "  Remote     : $Remote"
Write-Host "  Alignment  : Remote tag points directly to commit SHA ($commitSha)"
Write-Host "------------------------------------------------------------------------"
