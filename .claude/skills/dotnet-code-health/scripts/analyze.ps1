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
changed (default) | all | error | warning | info | deadcode | <diagnostic code, e.g. CA2263>
deadcode = only the dead-code diagnostics (whole solution) plus the report-only Roslynator candidate list.
all = every finding in the solution, plus the same report-only Roslynator candidate list.

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
$script:PathComparer = if ($script:IsWin) { [StringComparer]::OrdinalIgnoreCase } else { [StringComparer]::Ordinal }
$script:Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$script:EditorConfigs = $null
$script:DeadCodeBuildIncomplete = $false
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

# Dead-code diagnostics. The IDE rules are off or hidden in most repos, so they are enabled for one extra build through
# generated files under -OutDir (never by editing .editorconfig). The CS codes are compiler warnings the normal build reports.
$DeadCodeIdeCodes = 'IDE0005', 'IDE0051', 'IDE0052', 'IDE0060'
$DeadCodeCodes = $DeadCodeIdeCodes + 'CS0168', 'CS0169', 'CS0219', 'CS0414'

# True when the scope is answered only by the dead-code build, so a failed or skipped build must not read as "no findings".
function Test-ScopeNeedsDeadCodeBuild([string]$ScopeMode, [string]$ScopeName) {
    return $ScopeMode -eq 'deadcode' -or ($ScopeMode -eq 'code' -and $DeadCodeIdeCodes -contains $ScopeName)
}

# True when the scope filter drops compiler/build errors (warning, info, or a non-build code): analyzers need a compiling
# solution, so with a failed build an empty result would read as "clean". changed, all, error and CS/MSB/NU codes keep the errors.
function Test-ScopeHidesBuildErrors([string]$ScopeMode, [string]$ScopeName) {
    if ($ScopeMode -eq 'deadcode') { return $true }
    if ($ScopeMode -eq 'severity') { return $ScopeName -ne 'error' }
    if ($ScopeMode -eq 'code') { return $ScopeName -notmatch '^(CS|MSB|NU)\d+$' }
    return $false
}

# Criticality within a severity: lower rank is fixed first. references/severity-order.md documents this table.
$CategoryRules = @(
    @{ Rank = 0; Name = 'build'; Pattern = '^(CS|MSB|NU)\d+$' }
    @{ Rank = 1; Name = 'security'; Pattern = '^CA(3\d{3}|5\d{3}|2(100|109|119|153|3\d{2}|4\d{2}))$' }
    @{ Rank = 2; Name = 'reliability'; Pattern = '^CA2\d{3}$' }
    @{ Rank = 3; Name = 'performance'; Pattern = '^CA18\d{2}$' }
    @{ Rank = 4; Name = 'maintainability'; Pattern = '^CA\d{4}$' }
    @{ Rank = 5; Name = 'deadcode'; Pattern = '^(IDE0005|IDE0051|IDE0052|IDE0060)$' }
    @{ Rank = 6; Name = 'style'; Pattern = '^IDE\d{4}$' }
    @{ Rank = 8; Name = 'format'; Pattern = '^(WHITESPACE|FINALNEWLINE|ENDOFLINE|CHARSET|IMPORTS)$' }
)

function Get-Category([string]$Code) {
    foreach ($rule in $CategoryRules) {
        if ($Code -match $rule.Pattern) { return $rule }
    }
    return @{ Rank = 7; Name = 'other' }
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
    $seen = [System.Collections.Generic.HashSet[string]]::new($script:PathComparer)
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
    $map = [System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[int[]]]]::new($script:PathComparer)
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

# Converts an .editorconfig section glob to an anchored regex over a path relative to the config's directory.
# Supports *, **, ?, [seq], [!seq], {a,b} (nestable), {n..m} (any integer) and \ escapes. A glob without '/' matches at any depth.
function ConvertTo-EditorConfigRegex([string]$Glob) {
    $body = ConvertTo-EditorConfigRegexBody ($Glob.TrimStart('/'))
    $prefix = if ($Glob.Contains('/')) { '' } else { '(?:.*/)?' }
    $options = if ($script:IsWin) { 'IgnoreCase' } else { 'None' }
    return [regex]::new("^$prefix$body$", $options)
}

function ConvertTo-EditorConfigRegexBody([string]$Glob) {
    $sb = [System.Text.StringBuilder]::new()
    $i = 0
    while ($i -lt $Glob.Length) {
        $c = $Glob[$i]
        switch ($c) {
            '*' {
                if ($i + 1 -lt $Glob.Length -and $Glob[$i + 1] -eq '*') { [void]$sb.Append('.*'); $i++ } else { [void]$sb.Append('[^/]*') }
            }
            '?' { [void]$sb.Append('[^/]') }
            '\' {
                if ($i + 1 -lt $Glob.Length) { $i++; [void]$sb.Append([regex]::Escape([string]$Glob[$i])) } else { [void]$sb.Append('\\') }
            }
            '[' {
                $close = $Glob.IndexOf(']', $i + 1)
                if ($close -lt 0) { [void]$sb.Append('\[') }
                else {
                    $set = $Glob.Substring($i + 1, $close - $i - 1)
                    if ($set.StartsWith('!')) { $set = '^' + $set.Substring(1) }
                    [void]$sb.Append('[' + ($set -replace '\\', '\\\\') + ']')
                    $i = $close
                }
            }
            '{' {
                # Find the matching close brace, honouring nesting.
                $depth = 0
                $close = -1
                for ($j = $i; $j -lt $Glob.Length; $j++) {
                    if ($Glob[$j] -eq '{') { $depth++ }
                    elseif ($Glob[$j] -eq '}') { $depth--; if ($depth -eq 0) { $close = $j; break } }
                }
                if ($close -lt 0) { [void]$sb.Append('\{') }
                else {
                    $inner = $Glob.Substring($i + 1, $close - $i - 1)
                    if ($inner -match '^-?\d+\.\.-?\d+$') { [void]$sb.Append('-?\d+') }
                    else {
                        # Split on top-level commas only.
                        $parts = [System.Collections.Generic.List[string]]::new()
                        $depth = 0
                        $start = 0
                        for ($j = 0; $j -lt $inner.Length; $j++) {
                            if ($inner[$j] -eq '{') { $depth++ }
                            elseif ($inner[$j] -eq '}') { $depth-- }
                            elseif ($inner[$j] -eq ',' -and $depth -eq 0) { $parts.Add($inner.Substring($start, $j - $start)); $start = $j + 1 }
                        }
                        $parts.Add($inner.Substring($start))
                        if ($parts.Count -eq 1) { [void]$sb.Append('\{' + (ConvertTo-EditorConfigRegexBody $inner) + '\}') }
                        else { [void]$sb.Append('(?:' + (($parts | ForEach-Object { ConvertTo-EditorConfigRegexBody $_ }) -join '|') + ')') }
                    }
                    $i = $close
                }
            }
            default { [void]$sb.Append([regex]::Escape([string]$c)) }
        }
        $i++
    }
    return $sb.ToString()
}

# Tracked .editorconfig files parsed once: directory (repo-relative, '' = root) -> Root flag and ordered sections, each with
# its glob regex and the dead-code rule severities it assigns (lowercase, last assignment in the section wins).
function Get-EditorConfigs {
    if ($script:EditorConfigs) { return $script:EditorConfigs }
    $configs = @{}
    $files = @(Invoke-Git ls-files --cached --others --exclude-standard -- '.editorconfig' '*/.editorconfig')
    foreach ($file in $files) {
        if (-not $file -or -not (Test-Path -LiteralPath $file)) { continue }
        $dir = if ($file.Contains('/')) { $file.Substring(0, $file.LastIndexOf('/')) } else { '' }
        $config = @{ Root = $false; Sections = [System.Collections.Generic.List[object]]::new() }
        $section = $null
        foreach ($line in (Read-Lines (Convert-Path -LiteralPath $file))) {
            $header = [regex]::Match($line, '^\s*\[(?<glob>.+)\]\s*(?:[#;].*)?$')
            if ($header.Success) {
                $section = @{ Regex = (ConvertTo-EditorConfigRegex $header.Groups['glob'].Value.Trim()); Rules = @{} }
                $config.Sections.Add($section)
                continue
            }
            if (-not $section) {
                if ($line -match '^\s*root\s*=\s*true\b') { $config.Root = $true }
                continue
            }
            $m = [regex]::Match($line, '^\s*dotnet_diagnostic\.(?<code>[A-Za-z]+\d+)\.severity\s*=\s*(?<sev>none|silent|suggestion|warning|error)\b', 'IgnoreCase')
            if (-not $m.Success) { continue }
            $code = $m.Groups['code'].Value.ToUpperInvariant()
            if ($DeadCodeIdeCodes -contains $code) { $section.Rules[$code] = $m.Groups['sev'].Value.ToLowerInvariant() }
        }
        $configs[$dir] = $config
    }
    $script:EditorConfigs = $configs
    return $configs
}

# A .globalconfig has lower precedence than .editorconfig, so the severity the repo sets for a dead-code IDE rule wins over the
# generated one. Resolves it for one file like the compiler does: the closest .editorconfig wins, up to the one with root = true,
# and inside a file the last matching section wins. Returns none/silent/suggestion/warning/error, or $null when not configured.
function Get-EffectiveDeadCodeSeverity([string]$RelativeFile, [string]$Code) {
    $configs = Get-EditorConfigs
    $dir = if ($RelativeFile.Contains('/')) { $RelativeFile.Substring(0, $RelativeFile.LastIndexOf('/')) } else { '' }
    while ($true) {
        if ($configs.ContainsKey($dir)) {
            $config = $configs[$dir]
            $target = if ($dir) { $RelativeFile.Substring($dir.Length + 1) } else { $RelativeFile }
            for ($i = $config.Sections.Count - 1; $i -ge 0; $i--) {
                $section = $config.Sections[$i]
                if ($section.Rules.ContainsKey($Code) -and $section.Regex.IsMatch($target)) { return $section.Rules[$Code] }
            }
            if ($config.Root) { return $null }
        }
        if (-not $dir) { return $null }
        $dir = if ($dir.Contains('/')) { $dir.Substring(0, $dir.LastIndexOf('/')) } else { '' }
    }
}

# Dead-code IDE rules that resolve to none/silent for at least one source file: the extra build cannot turn them back on.
function Get-UnscannedDeadCodeRules {
    $sources = @(Invoke-Git ls-files --cached --others --exclude-standard -- '*.cs' '*.vb')
    $unscanned = [System.Collections.Generic.List[string]]::new()
    foreach ($code in $DeadCodeIdeCodes) {
        foreach ($source in $sources) {
            if ((Get-EffectiveDeadCodeSeverity $source $code) -in 'none', 'silent') { $unscanned.Add($code); break }
        }
    }
    return @($unscanned)
}

# One extra build with the dead-code IDE rules enabled through generated files (EditorConfigFiles item), repo files untouched.
function Invoke-DeadCodeBuild([string]$SolutionPath, [string]$LogPath, [string]$Dir) {
    $configPath = [System.IO.Path]::GetFullPath((Join-Path $Dir 'deadcode.globalconfig'))
    $targetsPath = [System.IO.Path]::GetFullPath((Join-Path $Dir 'deadcode.targets'))
    $rules = @('is_global = true') + @($DeadCodeIdeCodes | ForEach-Object { "dotnet_diagnostic.$_.severity = warning" })
    Write-TextFile $configPath $rules
    # The path lands in an MSBuild item Include inside an XML attribute: escape MSBuild specials first, then XML.
    $msbuildPath = $configPath.Replace('%', '%25').Replace(';', '%3B').Replace('$', '%24').Replace('@', '%40')
    $xmlConfigPath = [System.Security.SecurityElement]::Escape($msbuildPath)
    Write-TextFile $targetsPath @(
        '<Project>'
        '  <Target Name="DeadCodeInjectRules" AfterTargets="GenerateMSBuildEditorConfigFile" BeforeTargets="CoreCompile">'
        '    <ItemGroup>'
        "      <EditorConfigFiles Include=`"$xmlConfigPath`" />"
        '    </ItemGroup>'
        '  </Target>'
        '</Project>'
    )
    $commandLine = @('dotnet', 'build', $SolutionPath, '--no-incremental', '-nologo', '-v:minimal',
        '-p:EnforceCodeStyleInBuild=true', '-p:GenerateDocumentationFile=true', '-p:TreatWarningsAsErrors=false',
        "-p:CustomAfterMicrosoftCommonTargets=$targetsPath")
    $exit = Invoke-Logged $LogPath $commandLine
    $parsed = @(Read-Diagnostics $LogPath $null)
    # A repo can set a rule to error explicitly, which fails this build with a real finding; only an unparsable failure is a tooling error.
    if ($exit -ne 0 -and -not ($parsed | Where-Object Severity -eq 'error')) {
        Read-Lines $LogPath | Select-Object -Last 20 | Write-Host
        Stop-Tool "dead-code build failed (exit $exit) without a parsable diagnostic; see $LogPath."
    }
    # An error outside the dead-code rules (e.g. only hit with documentation/code-style enforcement on) means the compilation
    # did not finish cleanly, so the dead-code diagnostics may be missing. Errors from the dead-code rules themselves are findings.
    $script:DeadCodeBuildIncomplete = $exit -ne 0 -and [bool]($parsed | Where-Object { $_.Severity -eq 'error' -and $DeadCodeIdeCodes -notcontains $_.Code })
    # The generated rules run as warnings, which only exist to surface them in the log. Where the repo configures the rule as
    # warning or error for the finding's own file that severity is kept; otherwise (not configured, or suggestion) it is reported as info.
    foreach ($d in $parsed) {
        if ($d.Severity -eq 'error') { continue }
        $repoSeverity = Get-EffectiveDeadCodeSeverity $d.File $d.Code
        $d.Severity = if ($repoSeverity -in 'warning', 'error') { 'warning' } else { 'info' }
    }
    return @($parsed | Where-Object { $_.Severity -eq 'error' -or $DeadCodeIdeCodes -contains $_.Code })
}

# Report-only: unused internal/public symbols across the solution (Roslynator). Best effort; returns a status and the rows.
function Invoke-DeadCodeCandidates([string]$SolutionPath, [string]$Dir) {
    $toolDir = [System.IO.Path]::GetFullPath((Join-Path $Dir 'tools'))
    $exe = Join-Path $toolDir $(if ($script:IsWin) { 'roslynator.exe' } else { 'roslynator' })
    if (-not (Test-Path -LiteralPath $exe)) {
        $installLog = Join-Path $Dir 'roslynator-install.log'
        [void](Invoke-Logged $installLog @('dotnet', 'tool', 'install', 'roslynator.dotnet.cli', '--tool-path', $toolDir))
        if (-not (Test-Path -LiteralPath $exe)) { return @{ Status = 'unavailable'; Rows = @() } }
    }
    $log = Join-Path $Dir 'roslynator.log'
    $exit = Invoke-Logged $log @($exe, 'find-symbol', $SolutionPath, '--unused', '--visibility', 'public', 'internal', '--verbosity', 'normal')
    if ($exit -ne 0) { return @{ Status = 'unavailable'; Rows = @() } }
    $rows = [System.Collections.Generic.List[object]]::new()
    foreach ($line in (Read-Lines $log)) {
        $m = [regex]::Match($line, '^(?<kind>[a-z-]+)\s+(?<symbol>\S.*)$')
        if (-not $m.Success -or $m.Groups['kind'].Value -notin 'class', 'struct', 'interface', 'enum', 'delegate', 'method', 'property', 'indexer', 'field', 'const', 'event', 'enum-field') { continue }
        $symbol = $m.Groups['symbol'].Value.Trim()
        # Test projects and constructors are noise: test classes are discovered by the runner and never referenced.
        if ($symbol -match '(?i)\.tests?\.' -or $symbol -match '\.(\w+)\.\1\(') { continue }
        $rows.Add([pscustomobject]@{ Kind = $m.Groups['kind'].Value; Symbol = $symbol })
    }
    return @{ Status = 'ok'; Rows = $rows.ToArray() }
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
elseif ($scopeText -ieq 'deadcode') { 'deadcode' }
elseif ($scopeText -match '^(?i:error|warning|info)$') { 'severity' }
elseif ($scopeText -match '^[A-Za-z]+\d*$') { 'code' }
else { Stop-Tool "invalid scope '$Scope'. Use changed, all, error, warning, info, deadcode or a diagnostic code." }
$scopeText = if ($mode -eq 'code') { $scopeText.ToUpperInvariant() } else { $scopeText.ToLowerInvariant() }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$buildLog = Join-Path $OutDir 'build.log'
$formatLog = Join-Path $OutDir 'format.log'
$deadLog = Join-Path $OutDir 'deadcode.log'
$candidatesPath = Join-Path $OutDir 'deadcode-candidates.tsv'
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

    $unscanned = @(if ($state.PSObject.Properties['unscannedRules']) { $state.unscannedRules | Where-Object { $_ } })
    $unscannedText = if ($unscanned.Count -gt 0) { "Rules disabled in .editorconfig and NOT scanned: $($unscanned -join ', '). A clean result does not cover them." } else { 'None.' }

    $candidateStatus = if ($state.PSObject.Properties['deadCodeCandidates']) { "$($state.deadCodeCandidates)" } else { 'not-run' }
    $deadCodeSection = switch ($candidateStatus) {
        'ok' {
            $rows = @(Read-Lines $candidatesPath | Select-Object -Skip 1 | Where-Object { $_ } | ForEach-Object {
                    $f = $_.Split("`t")
                    [pscustomobject]@{ Kind = $f[0]; Symbol = $f[1] }
                })
            'Unused public/internal symbols found by Roslynator. Never deleted automatically: reflection, DI, serialization, framework entry points and public APIs look unused but are not.' + "`n`n" + (ConvertTo-MdTable $rows 'Kind', 'Symbol')
        }
        'unavailable' { 'Roslynator was unavailable (offline or install failed); candidates were not collected.' }
        default { 'Not run. Use the `deadcode` or `all` scope to collect candidates.' }
    }

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
        '## Rules not scanned'
        ''
        $unscannedText
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
        '## Dead code candidates (report only)'
        ''
        $deadCodeSection
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

# The dead-code IDE rules only run in the extra build, which needs a clean main build. Those scopes filter out the build errors,
# so continuing would report "no findings" for a scan that never ran.
if ($buildExit -ne 0 -and (Test-ScopeHidesBuildErrors $mode $scopeText)) {
    Stop-Tool "analysis incomplete: the build has errors and scope '$scopeText' does not report them, so an empty result would not mean clean. Fix them and re-run; see $buildLog."
}

# Format-only findings are the info items the build hides; anything the build already reported keeps its severity.
$buildKeys = [System.Collections.Generic.HashSet[string]]::new($script:PathComparer)
foreach ($b in $buildFindings) { [void]$buildKeys.Add("$($b.File)|$($b.Line)|$($b.Code)") }
$formatOnly = $formatFindings | Where-Object { -not $buildKeys.Contains("$($_.File)|$($_.Line)|$($_.Code)") }

# Dead code: the IDE rules need their own build (see Invoke-DeadCodeBuild); skipped when the main build already failed.
$deadFindings = @()
$unscannedRules = @()
if ($buildExit -eq 0) {
    $unscannedRules = @(Get-UnscannedDeadCodeRules)
    if ($unscannedRules.Count -gt 0) {
        Write-Warning "Dead-code rules disabled in .editorconfig cannot be re-enabled by the scan and were NOT checked: $($unscannedRules -join ', ')."
    }
    $deadFindings =@(Invoke-DeadCodeBuild $Solution $deadLog $OutDir | Where-Object { -not $buildKeys.Contains("$($_.File)|$($_.Line)|$($_.Code)") })
    if ($script:DeadCodeBuildIncomplete) {
        if (Test-ScopeNeedsDeadCodeBuild $mode $scopeText) {
            Stop-Tool "analysis incomplete: the dead-code build has errors outside the dead-code rules, so scope '$scopeText' may be missing findings. Fix them and re-run; see $deadLog."
        }
        Write-Warning "The dead-code build has errors outside the dead-code rules; dead-code findings may be incomplete. See $deadLog."
    }
}
else {
    Write-Warning 'Dead-code build skipped: the build has errors. Fix them and re-run.'
}

$all = @(Sort-Findings (Select-Unique (@($buildFindings) + @($deadFindings) + @($formatOnly))))

$inScope = switch ($mode) {
    'changed' {
        $ranges = Get-ChangedRanges $mergeBase
        $all | Where-Object { Test-InChangedRanges $_ $ranges }
    }
    'all' { $all }
    'deadcode' { $all | Where-Object { $DeadCodeCodes -contains $_.Code } }
    'severity' { $all | Where-Object Severity -eq $scopeText }
    'code' { $all | Where-Object Code -eq $scopeText }
}
$inScope = @($inScope)

Write-Tsv $all $allPath
Write-Tsv $inScope $findingsPath

$startNew = $Reset -or -not (Test-Path -LiteralPath $baselinePath) -or -not (Test-Path -LiteralPath $statePath)
$started = (Get-Date).ToUniversalTime().ToString('o')
$candidatesStatus = 'not-run'
if (-not $startNew) {
    $previous = [System.IO.File]::ReadAllText($statePath) | ConvertFrom-Json
    if ($previous.scope -ne $scopeText) {
        Stop-Tool "scope changed from '$($previous.scope)' to '$scopeText' since the baseline; re-run with -Reset."
    }
    $started = "$($previous.startedUtc)"
    # The candidate list is report-only and computed once per invocation; re-scans keep it.
    if ($previous.PSObject.Properties['deadCodeCandidates']) { $candidatesStatus = "$($previous.deadCodeCandidates)" }
}
else {
    Write-Tsv $inScope $baselinePath
    Remove-Item -LiteralPath $candidatesPath -ErrorAction SilentlyContinue
    if ($mode -in 'deadcode', 'all') {
        $candidates = Invoke-DeadCodeCandidates $Solution $OutDir
        $candidatesStatus = $candidates.Status
        if ($candidatesStatus -eq 'ok') {
            $lines = @("Kind`tSymbol") + @($candidates.Rows | ForEach-Object { "$($_.Kind)`t$($_.Symbol)" })
            Write-TextFile $candidatesPath $lines
        }
        else {
            Write-Warning 'Roslynator is unavailable (offline or install failed); dead-code candidates were not collected.'
        }
    }
}
$stateText = ConvertTo-JsonObject ([ordered]@{
        scope      = $scopeText
        base       = $Base
        mergeBase  = $mergeBase
        solution   = $Solution
        startedUtc = $started
        deadCodeCandidates = $candidatesStatus
        unscannedRules     = @($unscannedRules)
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
if ($unscannedRules.Count -gt 0) { Write-Host "Not scanned (disabled in .editorconfig): $($unscannedRules -join ', '). A clean result does not cover these rules." }
Write-Host "Findings: $findingsPath | All: $allPath | Baseline: $baselinePath"
exit $(if ($inScope.Count -eq 0) { 0 } else { 1 })
