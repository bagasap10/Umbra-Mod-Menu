# Third-party notices

## Umbra Menu

The original menu code retains the MIT notice in the root `LICENSE` (Copyright 2022 Acherons_). Existing attribution comments have been preserved. This suite adds launcher source, maintenance tooling and fixes around the existing implementation.

## SharpMonoInjector

Vendored from [warbler/SharpMonoInjector](https://github.com/warbler/SharpMonoInjector) at commit `73566c1be1e8e1bb25ab60683557958368b2cd47`.

MIT License, Copyright (c) 2017 Biney. The complete license is in `vendor/SharpMonoInjector/LICENSE`, and release packages carry it as `SharpMonoInjector.LICENSE.txt`. See `vendor/SharpMonoInjector/PATCHES.md` for changes from upstream.

## Runtime and game references

.NET Framework 4.8 is a Windows prerequisite, not vendored source. Risk of Rain 2, Unity, RoR2, Rewired and Addressables libraries are referenced from the user's installed game. They and game assets are not redistributed in the repository or release bundle. Icons are obtained from the exact live catalog definitions at runtime.

The older launcher's Octokit, SharpZipLib and command-line `smi.exe` are not dependencies of this suite. Harmony was removed from the menu dependency chain by using the existing reflection helpers.
