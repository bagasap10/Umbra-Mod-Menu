param([string]$GameDir, [string]$Version = '4.1.1')
. (Join-Path $PSScriptRoot 'Tools.ps1')
$suiteRoot = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'Build.ps1') -GameDir $GameDir -Version $Version
& (Join-Path $PSScriptRoot 'Test.ps1')
$artifacts = Join-Path $suiteRoot 'artifacts'
$releaseZip = Join-Path $artifacts "Umbra-Suite-$Version-win-x64.zip"
$releaseFiles = @('UmbraInjector.exe','UmbraInjector.exe.config','UmbraMenu.dll','SharpMonoInjector.dll','bundle.xml','START-HERE.md','LICENSE','SharpMonoInjector.LICENSE.txt','THIRD_PARTY_NOTICES.md') | ForEach-Object { Join-Path $artifacts "release\$_" }
Compress-Archive -LiteralPath $releaseFiles -DestinationPath $releaseZip -Force
# Staging uses a source allowlist rather than copying a checkout, caches or local game dependencies.
$stageDir = Join-Path $artifacts ('source-' + [Guid]::NewGuid().ToString('N'))
$sourceDir = Join-Path $stageDir 'Umbra-Suite'
New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null
$rootFiles = @('README.md','LICENSE','THIRD_PARTY_NOTICES.md','.gitignore','.editorconfig','Directory.Build.props','Umbra-Suite.sln')
foreach ($name in $rootFiles) { Copy-Item -LiteralPath (Join-Path $suiteRoot $name) -Destination $sourceDir -Force }
foreach ($directory in @('src','vendor','scripts','tests','docs')) {
    $base = Join-Path $suiteRoot $directory
    foreach ($file in Get-ChildItem -LiteralPath $base -Recurse -File) {
        $relative = $file.FullName.Substring($suiteRoot.Length + 1)
        if ($relative -match '(^|[\\/])(bin|obj)([\\/]|$)' -or $file.Extension -in @('.exe','.dll','.pdb','.log','.tmp','.ini')) { continue }
        $destination = Join-Path $sourceDir $relative
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
    }
}
$sourceZip = Join-Path $artifacts "Umbra-Suite-$Version-source.zip"
Compress-Archive -LiteralPath $sourceDir -DestinationPath $sourceZip -Force
$hashes = foreach ($zip in @($releaseZip,$sourceZip)) { $hash = Get-FileHash -LiteralPath $zip -Algorithm SHA256; "$($hash.Hash)  $([IO.Path]::GetFileName($zip))" }
$hashes | Set-Content -LiteralPath (Join-Path $artifacts 'SHA256SUMS.txt') -Encoding ASCII
# Only remove this run's freshly created staging directory, after checking its absolute workspace boundary.
$resolvedStage = (Resolve-Path -LiteralPath $stageDir).Path
$resolvedArtifacts = (Resolve-Path -LiteralPath $artifacts).Path.TrimEnd('\') + '\'
if (!$resolvedStage.StartsWith($resolvedArtifacts, [StringComparison]::OrdinalIgnoreCase)) { throw 'Source staging escaped the artifacts directory.' }
Remove-Item -LiteralPath $resolvedStage -Recurse -Force
Write-Host "Release: $releaseZip"
Write-Host "GitHub source: $sourceZip"
