# Portal images

The Teleporter / Portals menu shows a 32 x 32 thumbnail beside each spawn row and retains its base-game / DLC sections. Fourteen photographs come from the [wiki Interactables reference](https://riskofrain2.wiki.gg/wiki/Interactables). Each image is matched to its portal key; there are no generic recolored replacements.

The wiki images are embedded in the menu DLL. No extra image folder, game-time download, portal prefab loading or network spawning is needed to display them. Photographs keep their aspect ratios within transparent 128 x 128 canvases. Images decode only when their rows are visible, are cached once per menu lifetime, and their owned sprites/textures are released on unload.

At the time these images were checked, the wiki did not provide photographs at the expected filenames for these seven entries. They show the user-selected neutral `?` placeholder and remain fully clickable:

- Void Outro Portal
- Solutional Portal
- Anima Portal
- Anima Exit / Leave Winnowing
- Ritual Portal
- Mortal Portal
- Veil of Memory

An image load failure also shows the placeholder and does not retry every repaint. Reopening a new menu lifetime permits loading again. Unknown or non-catalog portal definitions cannot load an image.

For future images, add a verified `128 x 128` PNG named exactly after the portal key to `src/Menu/Assets/PortalIcons/`, keep it at or below 64 KiB, and update `sources.json`, the available-image test fixture, and `THIRD_PARTY_NOTICES.md`. The build embeds these PNGs automatically; source packaging includes the assets and release packaging includes them inside the menu DLL. See [third-party notices](../THIRD_PARTY_NOTICES.md) for the original image file pages and rights attribution.