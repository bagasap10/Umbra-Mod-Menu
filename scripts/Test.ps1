. (Join-Path $PSScriptRoot 'Tools.ps1')
$suiteRoot = Split-Path $PSScriptRoot -Parent
$csc = Join-Path (Split-Path (Find-MSBuild) -Parent) 'Roslyn\csc.exe'
$outDir = Join-Path $suiteRoot 'artifacts\tests'
New-Item -ItemType Directory -Path (Join-Path $outDir 'previews') -Force | Out-Null
function Run-Checks {
    param([string]$Name,[string[]]$Sources,[string[]]$References = @('System.Drawing.dll'))
    $exe = Join-Path $outDir "$Name.exe"
    $arguments = @('/nologo','/langversion:latest','/platform:x64',"/out:$exe")
    $arguments += $References | ForEach-Object { "/r:$_" }
    if ($Name -eq 'GameChecks') {
        foreach ($image in Get-ChildItem -LiteralPath (Join-Path $suiteRoot 'src\Menu\Assets\PortalIcons') -Filter '*.png') {
            $arguments += "/resource:$($image.FullName),UmbraMenu.PortalIcons.$($image.Name)"
        }
    }
    $arguments += $Sources | ForEach-Object { Join-Path $suiteRoot $_ }
    Invoke-Checked $csc $arguments
    Invoke-Checked $exe @()
}
$uiSources = @('MenuUI.cs','UITheme.cs','Navigation.cs','MenuTypes\Menu.cs','MenuTypes\NormalMenu.cs','MenuTypes\ListMenu.cs','MenuTypes\SearchableListMenu.cs','Interfaces\IMenu.cs','Interfaces\IButton.cs','ButtonTypes\Button.cs','ButtonTypes\NormalButton.cs','ButtonTypes\PickupButton.cs','ButtonTypes\MulButton.cs','ButtonTypes\TogglableButton.cs','ButtonTypes\TogglableMulButton.cs','ButtonTypes\Text.cs','ButtonTypes\DividerButton.cs') | ForEach-Object { "src\Menu\$_" }
Push-Location $suiteRoot
try {
    # Generate the tested constructor/fields from today's production Items source, not a stale copied snapshot.
    $itemsSource = [IO.File]::ReadAllText((Join-Path $suiteRoot 'src\Menu\Menus\Items.cs'))
    $endConstructor = $itemsSource.IndexOf('#region Drop Item Handle', [StringComparison]::Ordinal)
    if ($endConstructor -lt 0) { throw 'The Items constructor boundary could not be found.' }
    $fragment = [IO.File]::ReadAllText((Join-Path $suiteRoot 'tests\Menu\ItemsActions.fragment'))
    [IO.File]::WriteAllText((Join-Path $outDir 'ItemsConstructor.cs'), $itemsSource.Substring(0,$endConstructor) + $fragment)
    foreach ($menuName in @('Player','Movement')) {
        $sourcePath = Join-Path $suiteRoot ("src\Menu\Menus\$menuName.cs")
        $menuSource = [IO.File]::ReadAllText($sourcePath)
        $boundary = $menuSource.IndexOf('        public override void Draw()', [StringComparison]::Ordinal)
        if ($boundary -lt 0) { throw "The $menuName constructor boundary could not be found." }
        $actionFragment = [IO.File]::ReadAllText((Join-Path $suiteRoot "tests\Menu\${menuName}Actions.fragment"))
        [IO.File]::WriteAllText((Join-Path $outDir "${menuName}Constructor.cs"), $menuSource.Substring(0,$boundary) + $actionFragment)
    }
    Run-Checks 'UiChecks' ($uiSources + (@('MockUnity.cs','IconMock.cs','Checks.cs','ResizeChecks.cs','BackShortcutChecks.cs') | ForEach-Object { "tests\Menu\$_" }))
    $gameSources = @('WorldCheats.cs','PlayerCheats.cs','Menus\Main.cs','Menus\CheatsMenu.cs','CatalogGrouping.cs','Reflections.cs','CatalogIcons.cs','ItemPacks.cs','PortalCatalog.cs','PortalIcons.cs','EspTargets.cs','InventorySafety.cs','Menus\Teleporter.cs','Menus\ItemList.cs','Menus\InventoryItemList.cs','Menus\EquipmentList.cs','Menus\ChestItemList.cs','Menus\ItemPackList.cs','Menus\ItemPackDetails.cs') | ForEach-Object { "src\Menu\$_" }
    Run-Checks 'GameChecks' ($uiSources + $gameSources + @('artifacts\tests\ItemsConstructor.cs','artifacts\tests\PlayerConstructor.cs','artifacts\tests\MovementConstructor.cs') + (@('GameMockUnity.cs','GameChecks.cs','GameExtraChecks.cs','ChestMock.cs','WorldCheatsMock.cs','WorldCheatsChecks.cs','PlayerCheatsMock.cs','PlayerCheatsChecks.cs','CatalogSearchChecks.cs','GameIconMock.cs','PackChecks.cs','PortalIconChecks.cs','StabilityChecks.cs') | ForEach-Object { "tests\Menu\$_" }))
    $runtimeSources = @('Loader.cs','SafeRoutine.cs','MenuSettings.cs','Textures.cs','Reflections.cs','InventorySafety.cs') | ForEach-Object { "src\Menu\$_" }
    Run-Checks 'RuntimeChecks' ($runtimeSources + @('tests\Menu\RuntimeMock.cs','tests\Menu\RuntimeChecks.cs')) @('System.Core.dll')
    $backend = Join-Path $suiteRoot 'vendor\SharpMonoInjector\bin\Release\SharpMonoInjector.dll'
    if (!(Test-Path -LiteralPath $backend)) { throw 'Run scripts\Build.ps1 before testing the injector.' }
    Copy-Item -LiteralPath $backend -Destination $outDir -Force
    Run-Checks 'InjectorChecks' @('src\Shared\SessionProtocol.cs','src\Injector\Bundle.cs','src\Injector\GameProcess.cs','src\Injector\InjectionSession.cs','tests\Injector\InjectorChecks.cs') @('System.Core.dll','System.Xml.dll',$backend)
    $previewSources = @(Get-ChildItem -LiteralPath (Join-Path $suiteRoot 'src\Injector') -Filter '*.cs' | Where-Object Name -ne 'Program.cs' | ForEach-Object { $_.FullName.Substring($suiteRoot.Length + 1) })
    Run-Checks 'LauncherPreview' ($previewSources + @('src\Shared\SessionProtocol.cs','tests\Injector\LauncherPreview.cs')) @('System.Core.dll','System.Xml.dll','System.Drawing.dll','System.Windows.Forms.dll',$backend)
    Write-Host 'All checks passed. No game process was launched or injected by these checks.'
}
finally { Pop-Location }
