param([Parameter(Mandatory=$true)][string]$SuiteRoot, [Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
function Get-SourceSection([string]$Source, [string]$Start, [string]$End) {
    $first = $Source.IndexOf($Start, [StringComparison]::Ordinal)
    if ($first -lt 0) { throw "Missing lobby test source boundary: $Start" }
    $last = $Source.IndexOf($End, $first + $Start.Length, [StringComparison]::Ordinal)
    if ($last -lt 0) { throw "Missing lobby test source boundary: $End" }
    $Source.Substring($first, $last - $first)
}
# Exercise the current production input routines without executing Unity's native runtime.
$runtime = [IO.File]::ReadAllText((Join-Path $SuiteRoot 'src\Menu\UmbraMenu.cs')).Replace("`r`n", "`n")
$methods = foreach ($range in @(
    @('        private void CheckInputs()', '        private void VisualKeybinds()'),
    @('        private void VisualKeybinds()', '        private void Keybinds()'),
    @('        private void SetKeybindRoutine()', '        private void ForceFullMenuRoutine()'),
    @('        private void UpdateMainButtonsRoutine()', '        private void UpdateKeybindButtonRoutine()'),
    @('        private void UpdateKeybindButtonRoutine()', '        private void UpdateNavIndexRoutine()')
)) { Get-SourceSection $runtime $range[0] $range[1] }
$utility = [IO.File]::ReadAllText((Join-Path $SuiteRoot 'src\Menu\Utility.cs')).Replace("`r`n", "`n")
$lookup = Get-SourceSection $utility '        public static Button GetEnabledKeybindButton()' '        #endregion'
$generated = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UmbraMenu;
using Menus = UmbraMenu.Menus;
using static UmbraMenu.UmbraMenu;

internal sealed class LobbyRuntimeDriver
{
    private static bool IsItemSearchFocused() => MenuUI.IsSearchFocused;
    private void Keybinds() { if (!chatOpen) VisualKeybinds(); }
    public void TickInputs() => CheckInputs();
    public void TickCapture() => SetKeybindRoutine();
    public void TickKeybindButtons() { UpdateMainButtonsRoutine(); UpdateKeybindButtonRoutine(); }
$($methods -join "`n")
}
namespace UmbraMenu
{
    public static partial class Utility
    {
$lookup
    }
}
"@
[IO.File]::WriteAllText($OutputPath, $generated)
