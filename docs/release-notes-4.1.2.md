# Umbra Suite 4.1.2 - Hallowed Concepts

Game-version differences now show a warning and allow injection. This fix preserves the Cheats page, character and world cheats, portal images, catalog search and other 4.1.1 features.

## Game update compatibility

- A different Risk of Rain 2 build shows an amber warning in the package card.
- **Inject menu** remains enabled; clicking it proceeds without an additional confirmation dialog.
- The launcher, injection service and helper all allow the version mismatch.
- The warning stays visible during routine status refreshes and clears when the game matches the package.
- Missing game assemblies, invalid release files and existing injection-session errors still report errors.

An older menu can still have features that fail if a game update changed their APIs. Rebuilding against the current installation remains recommended when that happens.

## Installation and clean source

Restart the game, extract the full **Umbra-Suite-4.1.2-win-x64.zip**, and run **UmbraInjector.exe**. Open the menu with **Insert / Numpad 0**; **Numpad 1** goes back. Existing custom keys and settings are preserved.

The menu, injector, application manifest and package metadata use **4.1.2**, following the existing DLC count.feature update.fix numbering.

**Umbra-Suite-4.1.2-source.zip** contains the complete buildable source and licenses, without Git metadata, generated binaries, game assemblies, logs or personal settings. Release binaries remain separate from the source repository.

## Validation

Regression checks cover matching game builds, advisory mismatches, beginning an injection attempt after a mismatch, missing game files, release-file validation and the actual launcher's enabled button and warning layout. Automated checks do not launch or inject a game; live injection and gameplay still require an in-game pass.

See the [4.1.1 release notes](release-notes-4.1.1.md) for the retained Cheats features.
