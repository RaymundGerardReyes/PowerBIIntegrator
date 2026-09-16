# ==============================================================================
# Automated Test Suite for scripts/release.ps1
# Tests:
#   1. Normal flow: stage -> commit -> SemVer -> tag -> push commit & tag -> verify
#   2. Existing pushed commit with missing tag (no duplicate commit)
#   3. Existing tag collision detection (refuse silent overwrite)
#   4. SemVer conventional commit progression rules (patch, minor, major)
#   5. Exact commit SHA referencing verification
# ==============================================================================

$ErrorActionPreference = "Continue"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$releaseScript = Join-Path (Split-Path -Parent $scriptDir) "release.ps1"
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("release-ps1-test-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null

$passed = 0
$failed = 0

function Assert-Equal($expected, $actual, [string]$msg) {
    if ($expected -ne $actual) {
        Write-Host "  [FAIL] $msg : expected '$expected', got '$actual'" -ForegroundColor Red
        $script:failed++
    } else {
        Write-Host "  [PASS] $msg" -ForegroundColor Green
        $script:passed++
    }
}

function Assert-True([bool]$condition, [string]$msg) {
    if ($condition) {
        Write-Host "  [PASS] $msg" -ForegroundColor Green
        $script:passed++
    } else {
        Write-Host "  [FAIL] $msg" -ForegroundColor Red
        $script:failed++
    }
}

function Setup-Sandbox([string]$name) {
    $sandbox = Join-Path $testRoot $name
    New-Item -ItemType Directory -Path $sandbox -Force | Out-Null
    Set-Location $sandbox

    git init -q --bare remote.git
    git init -q repo
    Set-Location (Join-Path $sandbox "repo")

    git config user.name "Test Author"
    git config user.email "test@example.com"
    git remote add origin ../remote.git
    git checkout -q -B main *>$null

    "initial" | Out-File -FilePath "file.txt" -Encoding utf8
    git add file.txt
    git commit -q -m "feat(core): initial baseline v1.0.0"
    git tag -a v1.0.0 -m "Release v1.0.0"
    git push -q origin main --tags *>$null
}

try {
    Write-Host "========================================================================"
    Write-Host "Running PowerShell Release Script Test Suite"
    Write-Host "========================================================================"

    # TEST 1: Normal Release Flow
    Write-Host "Test 1: Normal release flow (add -> commit -> SemVer -> tag -> push)"
    Setup-Sandbox "test1_normal_flow"

    "new feature code" | Add-Content -Path "file.txt"
    & $releaseScript -Message "feat(api): add analytics query endpoint" -Remote origin -Branch main | Out-Null

    $commitSha = (git rev-parse HEAD).Trim()
    $localTag = (git tag --points-at HEAD).Trim()
    Assert-Equal "v1.1.0" $localTag "HEAD is tagged with v1.1.0"
    Assert-Equal $commitSha (git rev-list -n 1 v1.1.0).Trim() "Local tag points to commit SHA"

    $remoteTagRaw = (git ls-remote --tags origin "refs/tags/v1.1.0*")
    $remoteTagSha = ""
    foreach ($line in ($remoteTagRaw -split "`r?`n")) {
        if ($line -like "*^{}") {
            $remoteTagSha = ($line -split "\s+")[0]
            break
        }
    }
    Assert-Equal $commitSha $remoteTagSha "Remote tag points to commit SHA"

    # TEST 2: Existing Pushed Commit with Missing Tag
    Write-Host "Test 2: Existing pushed commit with missing tag recovery"
    Setup-Sandbox "test2_untagged_recovery"

    "gap closure" | Add-Content -Path "file.txt"
    git add file.txt
    git commit -m "feat(reporting): document generation engine" | Out-Null
    git push origin main *>$null | Out-Null

    $origSha = (git rev-parse HEAD).Trim()
    $preTag = (git tag --points-at HEAD)
    Assert-True (-not $preTag) "Precondition: HEAD has no tag"

    & $releaseScript -TagOnly -Remote origin -Branch main | Out-Null
    $afterSha = (git rev-parse HEAD).Trim()
    Assert-Equal $origSha $afterSha "No duplicate commit created"
    Assert-Equal "v1.1.0" (git tag --points-at HEAD).Trim() "Existing commit received tag v1.1.0"

    # TEST 3: Collision Guard
    Write-Host "Test 3: Existing tag collision guard"
    Setup-Sandbox "test3_tag_collision"

    "collision test" | Add-Content -Path "file.txt"
    git add file.txt
    git commit -m "fix(test): collision test commit" | Out-Null

    & $releaseScript -Tag "v1.0.0" -Remote origin -Branch main *>$null
    $exitCode = $LASTEXITCODE
    Assert-Equal 1 $exitCode "Refuses to overwrite existing v1.0.0 tag"

    # TEST 4: SemVer Progression Rules
    Write-Host "Test 4: Conventional Commit SemVer progression rules"
    Setup-Sandbox "test4_semver"

    # fix -> patch
    "fix line" | Add-Content -Path "file.txt"
    git add file.txt
    git commit -m "fix: resolve memory leak" | Out-Null
    git push origin main *>$null | Out-Null
    & $releaseScript -TagOnly -Remote origin -Branch main | Out-Null
    Assert-Equal "v1.0.1" (git tag --points-at HEAD).Trim() "fix bumps patch (v1.0.0 -> v1.0.1)"

    # feat -> minor
    "feat line" | Add-Content -Path "file.txt"
    git add file.txt
    git commit -m "feat(mcp): add mcp server host" | Out-Null
    git push origin main *>$null | Out-Null
    & $releaseScript -TagOnly -Remote origin -Branch main | Out-Null
    Assert-Equal "v1.1.0" (git tag --points-at HEAD).Trim() "feat bumps minor (v1.0.1 -> v1.1.0)"

    # breaking -> major
    "breaking line" | Add-Content -Path "file.txt"
    git add file.txt
    git commit -m "feat(core)!: overhaul IR entity" | Out-Null
    git push origin main *>$null | Out-Null
    & $releaseScript -TagOnly -Remote origin -Branch main | Out-Null
    Assert-Equal "v2.0.0" (git tag --points-at HEAD).Trim() "breaking bumps major (v1.1.0 -> v2.0.0)"

    # TEST 5: Exact SHA Verification
    Write-Host "Test 5: Verify remote tag points exactly to target commit SHA"
    Setup-Sandbox "test5_exact_sha"

    "audit log" | Add-Content -Path "file.txt"
    & $releaseScript -Message "feat(audit): add structured JSON logger" -Remote origin -Branch main | Out-Null
    $createdSha = (git rev-parse HEAD).Trim()
    $remoteTagRaw = (git ls-remote --tags origin "refs/tags/v1.1.0*")
    $remoteTagSha = ""
    foreach ($line in ($remoteTagRaw -split "`r?`n")) {
        if ($line -like "*^{}") {
            $remoteTagSha = ($line -split "\s+")[0]
            break
        }
    }
    Assert-Equal $createdSha $remoteTagSha "Remote tag references the exact commit SHA created by operation"

    Write-Host "========================================================================"
    Write-Host "PowerShell Results: $passed passed, $failed failed"
    Write-Host "========================================================================"
} finally {
    Set-Location $scriptDir
    if (Test-Path $testRoot) {
        Remove-Item -Path $testRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($failed -gt 0) {
    exit 1
}
