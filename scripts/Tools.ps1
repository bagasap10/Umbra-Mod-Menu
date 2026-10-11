$ErrorActionPreference = 'Stop'
function Find-MSBuild {
    $found = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $found = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe'
        if ($found) { return @($found)[0] }
    }
    throw 'Install Visual Studio Build Tools with .NET desktop development and the .NET Framework 4.8 targeting pack.'
}
function Find-GameDirectory {
    param([string]$Requested)
    if ($Requested) { $candidates = @($Requested) }
    else {
        $steam = (Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
        $libraries = @($steam)
        if ($steam) {
            $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
            if (Test-Path -LiteralPath $vdf) {
                foreach ($line in Get-Content -LiteralPath $vdf) {
                    if ($line -match '"path"\s+"([^"]+)"') { $libraries += $Matches[1].Replace('\\','\') }
                }
            }
        }
        $candidates = @($libraries | Where-Object { $_ } | ForEach-Object { Join-Path $_ 'steamapps\common\Risk of Rain 2' })
    }
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath (Join-Path $candidate 'Risk of Rain 2_Data\Managed\RoR2.dll')) { return (Resolve-Path -LiteralPath $candidate).Path }
    }
    throw 'Risk of Rain 2 was not found. Pass -GameDir with your installation directory.'
}
function Invoke-Checked {
    param([string]$Program, [string[]]$Arguments)
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Program failed with exit code $LASTEXITCODE." }
}
