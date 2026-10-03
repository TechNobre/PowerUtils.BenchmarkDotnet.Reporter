#Requires -Version 5.1
<#
.SYNOPSIS
Deterministic build + format analysis for a .NET repository (Windows PowerShell 5.1 or PowerShell 7+, any OS).

.DESCRIPTION
Run mode (default): builds with --no-incremental, runs `dotnet format --verify-no-changes --severity info`,
parses and de-duplicates every diagnostic, filters by -Scope, sorts by severity then criticality, and writes
the results under -OutDir. Report mode (-Report): compares the baseline of the first run against the latest
run and writes/prints report.md.

Exit codes: 0 = no findings in scope, 1 = findings remain in scope, 2 = tooling/usage error.

.PARAMETER Scope
changed (default) | all | error | warning | info | <diagnostic code, e.g. CA2263>

.PARAMETER Reset
Start a new baseline (use on the first run of every invocation).
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [string]$Scope = 'changed',
    [string]$Solution,
    [string]$Base,
    [string]$OutDir = 'artifacts/code-health',
    [int]$Top = 100,
    [string[]]$Decisions = @(),
    [Parameter(ValueFromRemainingArguments)][string[]]$ExtraDecisions = @(),
    [switch]$Reset,
    [switch]$Report
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# With -File, `-Decisions "a" "b"` binds only "a"; the rest arrive as remaining arguments.
$Decisions = @(@($Decisions) + @($ExtraDecisions) | Where-Object { $_ })

# Windows PowerShell 5.1 has no $IsWindows.
$script:IsWin = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
$script:Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
try { [Console]::OutputEncoding = $script:Utf8NoBom } catch { Write-Verbose 'Console encoding not changed.' }

function Stop-Tool([string]$Message) {
    [Console]::Error.WriteLine("dotnet-code-health: $Message")
    exit 2
}

# Native stderr must not become a terminating error on Windows PowerShell 5.1.
function Invoke-Git {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $out = & git @args 2>$null }
    finally { $ErrorActionPreference = $previous }
    if ($LASTEXITCODE -ne 0) { return $null }
    return $out
}

function Write-TextFile([string]$Path, [string[]]$Lines) {
    [System.IO.File]::WriteAllText($Path, (($Lines -join "`n") + "`n"), $script:Utf8NoBom)
}

function Read-Lines([string]$Path) {
    return [System.IO.File]::ReadAllLines($Path)
}

# Runs a native command, stores its combined output as UTF-8 and returns its exit code.
function Invoke-Logged([string]$LogPath, [string[]]$CommandLine) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $exe = $CommandLine[0]
        $rest = @($CommandLine | Select-Object -Skip 1)
        $lines = @(& $exe @rest 2>&1 | ForEach-Object { "$_" })
    }
    finally { $ErrorActionPreference = $previous }
    $exit = $LASTEXITCODE
    Write-TextFile $LogPath $lines
    return $exit
}

$FindingHeader = 'Severity', 'Rank', 'Category', 'Code', 'File', 'Line', 'Col', 'Message', 'Project'
$SeverityOrder = @{ error = 0; warning = 1; info = 2 }

# Criticality within a severity: lower rank is fixed first. references/severity-order.md documents this table.
$CategoryRules = @(
    @{ Rank = 0; Name = 'build'; Pattern = '^(CS|MSB|NU)\d+$' }
    @{ Rank = 1; Name = 'security'; Pattern = '^CA(3\d{3}|5\d{3}|2(100|109|119|153|3\d{2}|4\d{2}))$' }
    @{ Rank = 2; Name = 'reliability'; Pattern = '^CA2\d{3}$' }
    @{ Rank = 3; Name = 'performance'; Pattern = '^CA18\d{2}$' }
    @{ Rank = 4; Name = 'maintainability'; Pattern = '^CA\d{4}$' }
    @{ Rank = 5; Name = 'style'; Pattern = '^IDE\d{4}$' }
    @{ Rank = 7; Name = 'format'; Pattern = '^(WHITESPACE|FINALNEWLINE|ENDOFLINE|CHARSET|IMPORTS)$' }
)

function Get-Category([string]$Code) {
    foreach ($rule in $CategoryRules) {
        if ($Code -match $rule.Pattern) { return $rule }
    }
    return @{ Rank = 6; Name = 'other' }
}

$DiagnosticRegex = [regex]'^\s*(?<file>.+?)(?:\((?<line>\d+)(?:,(?<col>\d+))?(?:,\d+,\d+)?\))?: (?<sev>error|warning|info) (?<code>[A-Za-z]+\d*): (?<msg>.*?)(?: \[(?<proj>[^\]]+)\])?\s*$'

# Repo-relative with '/' on every OS, so findings compare equal wherever they were produced.
function Get-RelativePath([string]$Path) {
    $p = $Path.Trim() -replace '\\', '/'
    $root = ($script:RepoRoot -replace '\\', '/').TrimEnd('/') + '/'
    $comparison = if ($script:IsWin) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    if ($p.StartsWith($root, $comparison)) { return $p.Substring($root.Length) }
    return $p
}

function Read-Diagnostics([string]$LogPath, [string]$ForcedSeverity) {
    $items = [System.Collections.Generic.List[object]]::new()
    foreach ($line in (Read-Lines $LogPath)) {
        $m = $DiagnosticRegex.Match($line)
        if (-not $m.Success) { continue }
        $code = $m.Groups['code'].Value.ToUpperInvariant()
        $category = Get-Category $code
        $severity = if ($ForcedSeverity) { $ForcedSeverity } else { $m.Groups['sev'].Value }
        $items.Add([pscustomobject]@{
                Severity = $severity
                Rank     = $category.Rank
                Category = $category.Name
                Code     = $code
                File     = Get-RelativePath $m.Groups['file'].Value
                Line     = if ($m.Groups['line'].Success) { [int]$m.Groups['line'].Value } else { 0 }
                Col      = if ($m.Groups['col'].Success) { [int]$m.Groups['col'].Value } else { 0 }
                Message  = ($m.Groups['msg'].Value -replace '[\t\r\n]+', ' ').Trim()
                Project  = if ($m.Groups['proj'].Success) { Get-RelativePath $m.Groups['proj'].Value } else { '' }
            })
    }
    return $items
}

function Select-Unique($Items) {
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($i in $Items) {
        if ($seen.Add("$($i.File)|$($i.Line)|$($i.Col)|$($i.Code)")) { $i }
    }
}

# Culture-independent so the order is identical on every machine.
function Compare-Text([string]$A, [string]$B) {
    $r = [string]::Compare($A, $B, [StringComparison]::OrdinalIgnoreCase)
    if ($r -ne 0) { return $r }
    return [string]::CompareOrdinal($A, $B)
}

function Compare-Finding($A, $B) {
    $r = $SeverityOrder[$A.Severity] - $SeverityOrder[$B.Severity]
    if ($r -ne 0) { return $r }
    $r = $A.Rank - $B.Rank
    if ($r -ne 0) { return $r }
    $r = Compare-Text $A.Code $B.Code
    if ($r -ne 0) { return $r }
    $r = Compare-Text $A.File $B.File
    if ($r -ne 0) { return $r }
    $r = $A.Line - $B.Line
    if ($r -ne 0) { return $r }
    $r = $A.Col - $B.Col
    if ($r -ne 0) { return $r }
    return Compare-Text $A.Message $B.Message
}

function Sort-Findings($Items) {
    $list = [System.Collections.Generic.List[object]]::new()
    foreach ($i in @($Items)) { if ($null -ne $i) { $list.Add($i) } }
    $list.Sort([Comparison[object]] { param($a, $b) [int](Compare-Finding $a $b) })
    return $list.ToArray()
}

function Get-SortedUnique([string[]]$Items) {
    $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($i in $Items) { if ($i) { [void]$set.Add($i) } }
    $list = [System.Collections.Generic.List[string]]::new($set)
    $list.Sort([StringComparer]::Ordinal)
    return @($list)
}

# Plain tab-separated text: Export-Csv quoting differs between PowerShell 5.1 and 7.
function Write-Tsv($Items, [string]$Path) {
    $lines = @($FindingHeader -join "`t")
    foreach ($item in @($Items)) {
        $lines += (($FindingHeader | ForEach-Object { "$($item.$_)" }) -join "`t")
    }
    Write-TextFile $Path $lines
}

function Import-Findings([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return @() }
    $result = [System.Collections.Generic.List[object]]::new()
    foreach ($line in @(Read-Lines $Path | Select-Object -Skip 1)) {
        if (-not $line) { continue }
        $f = @($line.Split("`t"))
        while ($f.Count -lt $FindingHeader.Count) { $f += '' }
        $result.Add([pscustomobject]@{
                Severity = $f[0]
                Rank     = [int]$f[1]
                Category = $f[2]
                Code     = $f[3]
                File     = $f[4]
                Line     = [int]$f[5]
                Col      = [int]$f[6]
                Message  = $f[7]
                Project  = $f[8]
            })
    }
    return $result.ToArray()
}

function ConvertTo-JsonObject($Map) {
    $parts = foreach ($k in $Map.Keys) {
        $value = ("$($Map[$k])" -replace '\\', '\\') -replace '"', '\"'
        '  "' + $k + '": "' + $value + '"'
    }
    return "{`n" + ($parts -join ",`n") + "`n}"
}

function Get-ChangedRanges([string]$MergeBase) {
    $map = [System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[int[]]]]::new([StringComparer]::OrdinalIgnoreCase)
    $file = $null
    $inHeader = $false
    foreach ($line in @(& git -c core.quotepath=off diff --unified=0 --no-color --no-ext-diff $MergeBase)) {
        if ($line -match '^diff --git ') { $inHeader = $true; $file = $null; continue }
        if ($inHeader -and $line -match '^\+\+\+ (?:b/)?(.+?)\t?$') {
            $file = if ($Matches[1] -eq '/dev/null') { $null } else { $Matches[1] }
            continue
        }
        if ($line -match '^@@ -\d+(?:,\d+)? \+(?<s>\d+)(?:,(?<c>\d+))? @@') {
            $inHeader = $false
            if (-not $file) { continue }
            $count = if ($Matches['c']) { [int]$Matches['c'] } else { 1 }
            if ($count -eq 0) { continue }
            if (-not $map.ContainsKey($file)) { $map[$file] = [System.Collections.Generic.List[int[]]]::new() }
            $start = [int]$Matches['s']
            $map[$file].Add(@($start, ($start + $count - 1)))
        }
    }
    foreach ($untracked in @(& git -c core.quotepath=off ls-files --others --exclude-standard)) {
        if (-not $untracked) { continue }
        $map[$untracked] = [System.Collections.Generic.List[int[]]]::new()
        $map[$untracked].Add(@(1, [int]::MaxValue))
    }
    return , $map
}

function Test-InChangedRanges($Finding, $Ranges) {
    if (-not $Ranges.ContainsKey($Finding.File)) {
        # A finding with no file on disk (e.g. a compiler-level error) can't be attributed to a diff, so keep it.
        return -not (Test-Path -LiteralPath $Finding.File)
    }
    if ($Finding.Line -le 0) { return $true }
    foreach ($r in $Ranges[$Finding.File]) {
        if ($Finding.Line -ge $r[0] -and $Finding.Line -le $r[1]) { return $true }
    }
    return $false
}

function Get-FindingKey($Finding) { "$($Finding.File)|$($Finding.Code)|$($Finding.Message)" }

function ConvertTo-MdTable($Items, [string[]]$Columns) {
    $rows = @($Items)
    if ($rows.Count -eq 0) { return 'None.' }
    $esc = { param($v) ("$v" -replace '\|', '\|') }
    $out = @(('| ' + ($Columns -join ' | ') + ' |'), ('|' + ('---|' * $Columns.Count)))
    foreach ($r in $rows) {
        $cells = foreach ($c in $Columns) {
            if ($c -eq 'Location') { & $esc ($r.File + $(if ($r.Line -gt 0) { ":$($r.Line)" } else { '' })) }
            else { & $esc $r.$c }
        }
        $out += '| ' + ($cells -join ' | ') + ' |'
    }
    return $out -join "`n"
}

# ---------------------------------------------------------------- setup

$script:RepoRoot = Invoke-Git rev-parse --show-toplevel
if (-not $script:RepoRoot) { Stop-Tool 'not inside a git repository.' }
Set-Location $script:RepoRoot
# .NET file APIs resolve relative paths against the process directory, not the PowerShell location.
[Environment]::CurrentDirectory = $script:RepoRoot

$scopeText = $Scope.Trim()
$mode = if ($scopeText -ieq 'changed') { 'changed' }
elseif ($scopeText -ieq 'all') { 'all' }
elseif ($scopeText -match '^(?i:error|warning|info)$') { 'severity' }
elseif ($scopeText -match '^[A-Za-z]+\d*$') { 'code' }
else { Stop-Tool "invalid scope '$Scope'. Use changed, all, error, warning, info or a diagnostic code." }
$scopeText = if ($mode -eq 'code') { $scopeText.ToUpperInvariant() } else { $scopeText.ToLowerInvariant() }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$buildLog = Join-Path $OutDir 'build.log'
$formatLog = Join-Path $OutDir 'format.log'
$findingsPath = Join-Path $OutDir 'findings.tsv'
$allPath = Join-Path $OutDir 'findings-all.tsv'
$baselinePath = Join-Path $OutDir 'baseline.tsv'
$statePath = Join-Path $OutDir 'state.json'
$reportPath = Join-Path $OutDir 'report.md'

if (-not $Base) {
    $head = Invoke-Git symbolic-ref --short refs/remotes/origin/HEAD
    $candidates = @($head, 'origin/main', 'main', 'origin/master', 'master') | Where-Object { $_ }
    $Base = $candidates | Where-Object { Invoke-Git rev-parse --verify --quiet $_ } | Select-Object -First 1
}
if (-not $Base) { Stop-Tool 'could not detect the default branch. Pass -Base <branch>.' }
$mergeBase = Invoke-Git merge-base HEAD $Base
if (-not $mergeBase) { Stop-Tool "no merge-base between HEAD and '$Base'." }
$branch = Invoke-Git rev-parse --abbrev-ref HEAD

# ---------------------------------------------------------------- report mode

if ($Report) {
    foreach ($required in $baselinePath, $findingsPath, $allPath, $statePath) {
        if (-not (Test-Path -LiteralPath $required)) { Stop-Tool "missing '$required'. Run the analysis first." }
    }
    $state = [System.IO.File]::ReadAllText($statePath) | ConvertFrom-Json
    $baseline = @(Import-Findings $baselinePath)
    $current = @(Import-Findings $findingsPath)
    $outside = @(Import-Findings $allPath).Count - $current.Count

    $currentCount = @{}
    foreach ($c in $current) { $currentCount[(Get-FindingKey $c)] = 1 + [int]$currentCount[(Get-FindingKey $c)] }
    $baselineCount = @{}
    foreach ($b in $baseline) { $baselineCount[(Get-FindingKey $b)] = 1 + [int]$baselineCount[(Get-FindingKey $b)] }

    $fixed = [System.Collections.Generic.List[object]]::new()
    $budget = $currentCount.Clone()
    foreach ($b in $baseline) {
        $k = Get-FindingKey $b
        if ([int]$budget[$k] -gt 0) { $budget[$k]-- } else { $fixed.Add($b) }
    }
    $introduced = [System.Collections.Generic.List[object]]::new()
    $budget = $baselineCount.Clone()
    foreach ($c in $current) {
        $k = Get-FindingKey $c
        if ([int]$budget[$k] -gt 0) { $budget[$k]-- } else { $introduced.Add($c) }
    }

    $summaryRows = foreach ($sev in 'error', 'warning', 'info') {
        [pscustomobject]@{
            Severity  = $sev
            Initial   = @($baseline | Where-Object Severity -eq $sev).Count
            Fixed     = @($fixed | Where-Object Severity -eq $sev).Count
            Remaining = @($current | Where-Object Severity -eq $sev).Count
            New       = @($introduced | Where-Object Severity -eq $sev).Count
        }
    }

    $testPattern = '(?i)(^|/)[^/]*tests?[^/]*/|tests?\.cs$'
    $changedFiles = Get-SortedUnique (@(Invoke-Git -c core.quotepath=off diff --name-only $state.mergeBase) + @(Invoke-Git -c core.quotepath=off ls-files --others --exclude-standard))
    $testFiles = @($changedFiles | Where-Object { $_ -match $testPattern })
    $stat = @(Invoke-Git diff --stat $state.mergeBase) -join "`n"
    $untrackedList = @(Invoke-Git -c core.quotepath=off ls-files --others --exclude-standard) | Where-Object { $_ }

    $md = @(
        '# Code health report'
        ''
        "- Date (UTC): $((Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm'))"
        "- Branch: $branch"
        "- Scope: $($state.scope)"
        "- Base: $($state.base) (merge-base $($state.mergeBase.Substring(0, 8)))"
        "- Solution: $($state.solution)"
        ''
        '## Summary'
        ''
        (ConvertTo-MdTable $summaryRows 'Severity', 'Initial', 'Fixed', 'Remaining', 'New')
        ''
        "Open findings outside the scope: $outside"
        ''
        '## Fixed issues'
        ''
        (ConvertTo-MdTable $fixed 'Severity', 'Code', 'Category', 'Location', 'Message')
        ''
        '## Remaining issues'
        ''
        (ConvertTo-MdTable $current 'Severity', 'Code', 'Category', 'Location', 'Message')
        ''
        '## New issues'
        ''
        (ConvertTo-MdTable $introduced 'Severity', 'Code', 'Category', 'Location', 'Message')
        ''
        '## Files changed'
        ''
        '```text'
        $(if ($stat) { $stat } else { '(no tracked changes)' })
        $(if ($untrackedList) { "Untracked:`n" + ($untrackedList -join "`n") })
        '```'
        ''
        '## Test files touched'
        ''
        $(if ($testFiles.Count -gt 0) { ($testFiles | ForEach-Object { "- $_" }) -join "`n" } else { 'None.' })
        ''
        '## Decisions needed'
        ''
        $(if ($Decisions.Count -gt 0) { ($Decisions | ForEach-Object { "- $_" }) -join "`n" } else { 'None.' })
    ) -join "`n"

    Write-TextFile $reportPath $md
    Write-Host "Report written to $reportPath"
    Write-Host ''
    Read-Lines $reportPath | Write-Host
    exit $(if ($current.Count -eq 0) { 0 } else { 1 })
}

# ---------------------------------------------------------------- run mode

if (-not $Solution) {
    $rootFiles = @(Get-ChildItem -File)
    $candidates = @($rootFiles | Where-Object { $_.Extension -in '.slnx', '.sln' })
    if ($candidates.Count -eq 0) { $candidates = @($rootFiles | Where-Object { $_.Extension -eq '.csproj' }) }
    if ($candidates.Count -ne 1) {
        Stop-Tool "expected exactly one solution/project at the repo root, found $($candidates.Count). Pass -Solution <path>."
    }
    $Solution = $candidates[0].Name
}
if (-not (Test-Path -LiteralPath $Solution)) { Stop-Tool "solution '$Solution' not found." }

$previousPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& git check-ignore -q (Join-Path $OutDir '.probe') 2>$null
$ignoreExit = $LASTEXITCODE
$ErrorActionPreference = $previousPreference
if ($ignoreExit -ne 0) { Write-Warning "'$OutDir' is not git-ignored; add it to .gitignore." }

Write-Host "Solution: $Solution | Scope: $scopeText | Base: $Base | Merge-base: $($mergeBase.Substring(0, 8))"

$buildExit = Invoke-Logged $buildLog @('dotnet', 'build', $Solution, '--no-incremental', '-nologo', '-v:minimal')
$buildFindings = @(Read-Diagnostics $buildLog $null)

$formatExit = Invoke-Logged $formatLog @('dotnet', 'format', $Solution, '--verify-no-changes', '--severity', 'info', '--no-restore', '--verbosity', 'normal')
$formatFindings = @(Read-Diagnostics $formatLog 'info')

if ($buildExit -ne 0 -and -not ($buildFindings | Where-Object Severity -eq 'error')) {
    Read-Lines $buildLog | Select-Object -Last 20 | Write-Host
    Stop-Tool "build failed (exit $buildExit) without a parsable diagnostic; see $buildLog."
}
if ($buildExit -eq 0 -and $formatExit -notin 0, 2) {
    Read-Lines $formatLog | Select-Object -Last 20 | Write-Host
    Stop-Tool "dotnet format failed (exit $formatExit); see $formatLog."
}
Write-Host "Build exit: $buildExit | Format exit: $formatExit"

# Format-only findings are the info items the build hides; anything the build already reported keeps its severity.
$buildKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($b in $buildFindings) { [void]$buildKeys.Add("$($b.File)|$($b.Line)|$($b.Code)") }
$formatOnly = $formatFindings | Where-Object { -not $buildKeys.Contains("$($_.File)|$($_.Line)|$($_.Code)") }

$all = @(Sort-Findings (Select-Unique (@($buildFindings) + @($formatOnly))))

$inScope = switch ($mode) {
    'changed' {
        $ranges = Get-ChangedRanges $mergeBase
        $all | Where-Object { Test-InChangedRanges $_ $ranges }
    }
    'all' { $all }
    'severity' { $all | Where-Object Severity -eq $scopeText }
    'code' { $all | Where-Object Code -eq $scopeText }
}
$inScope = @($inScope)

Write-Tsv $all $allPath
Write-Tsv $inScope $findingsPath

$startNew = $Reset -or -not (Test-Path -LiteralPath $baselinePath) -or -not (Test-Path -LiteralPath $statePath)
$started = (Get-Date).ToUniversalTime().ToString('o')
if (-not $startNew) {
    $previous = [System.IO.File]::ReadAllText($statePath) | ConvertFrom-Json
    if ($previous.scope -ne $scopeText) {
        Stop-Tool "scope changed from '$($previous.scope)' to '$scopeText' since the baseline; re-run with -Reset."
    }
    $started = "$($previous.startedUtc)"
}
else {
    Write-Tsv $inScope $baselinePath
}
$stateText = ConvertTo-JsonObject ([ordered]@{
        scope      = $scopeText
        base       = $Base
        mergeBase  = $mergeBase
        solution   = $Solution
        startedUtc = $started
    })
Write-TextFile $statePath $stateText

Write-Host ''
Write-Host 'Severity  InScope  Total'
foreach ($sev in 'error', 'warning', 'info') {
    $row = '{0,-9} {1,7}  {2,5}' -f $sev, @($inScope | Where-Object Severity -eq $sev).Count, @($all | Where-Object Severity -eq $sev).Count
    Write-Host $row
}
Write-Host ''
if ($inScope.Count -gt 0) {
    $shown = @($inScope | Select-Object -First $Top)
    Write-Host (($shown | Format-Table Severity, Category, Code, File, Line, Message -AutoSize | Out-String -Width 4096).TrimEnd())
    if ($inScope.Count -gt $shown.Count) { Write-Host "... $($inScope.Count - $shown.Count) more in $findingsPath" }
}
else {
    Write-Host 'No findings in scope.'
}
Write-Host ''
Write-Host "Findings: $findingsPath | All: $allPath | Baseline: $baselinePath"
exit $(if ($inScope.Count -eq 0) { 0 } else { 1 })
