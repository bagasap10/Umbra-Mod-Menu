param([string]$GameDir, [string]$Version = '3.0.0')
. (Join-Path $PSScriptRoot 'Tools.ps1')
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$') { throw 'Use a semantic version, for example 2.0.0.' }
$suiteRoot = Split-Path $PSScriptRoot -Parent
$GameDir = Find-GameDirectory $GameDir
$msbuild = Find-MSBuild
Invoke-Checked $msbuild @((Join-Path $suiteRoot 'Umbra-Suite.sln'), '/t:Rebuild', '/m', '/p:Configuration=Release', '/p:Platform=x64', "/p:GameDir=$GameDir", '/warnaserror', '/v:minimal', '/nologo')
$releaseDir = Join-Path $suiteRoot 'artifacts\release'
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
# Release assembly uses an explicit allowlist; game DLLs and personal settings never enter the bundle.
foreach ($name in @('UmbraInjector.exe','UmbraInjector.exe.config','SharpMonoInjector.dll')) {
    Copy-Item -LiteralPath (Join-Path $suiteRoot "src\Injector\bin\Release\$name") -Destination $releaseDir -Force
}
$menuPath = Join-Path $suiteRoot 'src\Menu\bin\Release\UmbraMenu.dll'
Copy-Item -LiteralPath $menuPath -Destination $releaseDir -Force
# Inspect metadata in a small .NET Framework helper: ReflectionOnlyLoad is unavailable in PowerShell 7.
$toolDir = Join-Path $suiteRoot 'artifacts\tools'
New-Item -ItemType Directory -Path $toolDir -Force | Out-Null
$inspector = Join-Path $toolDir 'InspectAssembly.exe'
$csc = Join-Path (Split-Path $msbuild -Parent) 'Roslyn\csc.exe'
Invoke-Checked $csc @('/nologo','/platform:x64',"/out:$inspector",(Join-Path $PSScriptRoot 'InspectAssembly.cs'))
$module = & $inspector $menuPath
if ($LASTEXITCODE -ne 0 -or ![Guid]::TryParse($module, [ref]([Guid]::Empty))) { throw 'Menu metadata inspection failed.' }
$manifest = New-Object Xml.XmlDocument
$root = $manifest.CreateElement('umbra'); $manifest.AppendChild($root) | Out-Null
$root.SetAttribute('protocol','1'); $root.SetAttribute('version',$Version)
$root.SetAttribute('module',$module)
$root.SetAttribute('gameSha256',(Get-FileHash -LiteralPath (Join-Path $GameDir 'Risk of Rain 2_Data\Managed\RoR2.dll') -Algorithm SHA256).Hash)
foreach ($name in @('UmbraMenu.dll','SharpMonoInjector.dll')) {
    $file = $manifest.CreateElement('file'); $file.SetAttribute('name',$name)
    $file.SetAttribute('sha256',(Get-FileHash -LiteralPath (Join-Path $releaseDir $name) -Algorithm SHA256).Hash)
    $root.AppendChild($file) | Out-Null
}
$manifest.Save((Join-Path $releaseDir 'bundle.xml'))
Copy-Item -LiteralPath (Join-Path $suiteRoot 'LICENSE') -Destination $releaseDir -Force
Copy-Item -LiteralPath (Join-Path $suiteRoot 'THIRD_PARTY_NOTICES.md') -Destination $releaseDir -Force
Copy-Item -LiteralPath (Join-Path $suiteRoot 'vendor\SharpMonoInjector\LICENSE') -Destination (Join-Path $releaseDir 'SharpMonoInjector.LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $suiteRoot 'docs\USAGE.md') -Destination (Join-Path $releaseDir 'START-HERE.md') -Force
Write-Host "Built $releaseDir. Run UmbraInjector.exe from that folder."
