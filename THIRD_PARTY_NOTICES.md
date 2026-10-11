# Third-party notices

## Umbra Menu

The original menu code retains the MIT notice in the root `LICENSE` (Copyright 2022 Acherons_). Existing attribution comments have been preserved. This suite adds launcher source, maintenance tooling and fixes around the existing implementation.

## SharpMonoInjector

Vendored from [warbler/SharpMonoInjector](https://github.com/warbler/SharpMonoInjector) at commit `73566c1be1e8e1bb25ab60683557958368b2cd47`.

MIT License, Copyright (c) 2017 Biney. The complete license is in `vendor/SharpMonoInjector/LICENSE`, and release packages carry it as `SharpMonoInjector.LICENSE.txt`. See `vendor/SharpMonoInjector/PATCHES.md` for changes from upstream.

## Runtime and game references

.NET Framework 4.8 is a Windows prerequisite, not vendored source. Risk of Rain 2, Unity, RoR2, Rewired and Addressables libraries are referenced from the user's installed game. Game DLLs are not redistributed in the repository or release bundle. Item and equipment icons are obtained from their exact live catalog definitions at runtime. Portal photographs are embedded thumbnails with separate attribution below.

The older launcher's Octokit, SharpZipLib and command-line `smi.exe` are not dependencies of this suite. Harmony was removed from the menu dependency chain by using the existing reflection helpers.

## Portal photographs

Fourteen portal thumbnails in `src/Menu/Assets/PortalIcons/` are derived from the portal pictures referenced by the [Risk of Rain 2 Wiki Interactables page](https://riskofrain2.wiki.gg/wiki/Interactables) and its [interactable data](https://riskofrain2.wiki.gg/wiki/Module:Interactables/Data). The screenshots depict Risk of Rain 2, owned by its respective rights holders, and are not covered by this project's MIT code license. Refer to each original file page for uploader, revision history and image rights information.

The images were resized into 128 x 128 transparent canvases without cropping, recoloring or distorting their aspect ratios. They are embedded in `UmbraMenu.dll` and require no runtime web requests. Original and thumbnail SHA-256 values are recorded in `src/Menu/Assets/PortalIcons/sources.json`. Keep this notice with source and release copies.

| Portal key | Original wiki file |
| --- | --- |
| blue | [Blue Portal (icon).png](https://riskofrain2.wiki.gg/wiki/File:Blue_Portal_%28icon%29.png) |
| cele | [Celestial Portal (icon).png](https://riskofrain2.wiki.gg/wiki/File:Celestial_Portal_%28icon%29.png) |
| gold | [Gold Portal (icon).png](https://riskofrain2.wiki.gg/wiki/File:Gold_Portal_%28icon%29.png) |
| artifact | [Artifact Portal (icon).png](https://riskofrain2.wiki.gg/wiki/File:Artifact_Portal_%28icon%29.png) |
| null | [Null Portal (icon).png](https://riskofrain2.wiki.gg/wiki/File:Null_Portal_%28icon%29.png) |
| void | [Void Portal (icon).png](https://riskofrain2.wiki.gg/wiki/File:Void_Portal_%28icon%29.png) |
| deepvoid | [Deep Void Portal (icon).png](https://riskofrain2.wiki.gg/wiki/File:Deep_Void_Portal_%28icon%29.png) |
| infinite | [Infinite Portal (icon).png](https://riskofrain2.wiki.gg/wiki/File:Infinite_Portal_%28icon%29.png) |
| green | [Green Portal (icon).png](https://riskofrain2.wiki.gg/wiki/File:Green_Portal_%28icon%29.png) |
| destination | [Destination Portal (icon).png](https://riskofrain2.wiki.gg/wiki/File:Destination_Portal_%28icon%29.png) |
| virtual | [Virtual Portal.png](https://riskofrain2.wiki.gg/wiki/File:Virtual_Portal.png) |
| encrypted | [Encrypted Portal.png](https://riskofrain2.wiki.gg/wiki/File:Encrypted_Portal.png) |
| decrypted | [Decrypted Portal.png](https://riskofrain2.wiki.gg/wiki/File:Decrypted_Portal.png) |
| mainline | [Mainline Portal.png](https://riskofrain2.wiki.gg/wiki/File:Mainline_Portal.png) |
