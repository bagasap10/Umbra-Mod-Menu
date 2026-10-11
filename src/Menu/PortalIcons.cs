using System;
using System.Collections.Generic;
using UnityEngine;

namespace UmbraMenu
{
    // Wiki thumbnails are embedded in the menu; drawing never downloads or loads a portal prefab.
    internal static class PortalIcons
    {
        internal const int ImageSize = 128;
        internal const int MaximumImageBytes = 65536;
        private static readonly Dictionary<string, Sprite> images = new Dictionary<string, Sprite>();

        public static Sprite Get(PortalCatalog.Definition portal)
        {
            if (portal == null || !ReferenceEquals(PortalCatalog.Find(portal.Key), portal)) return null;
            if (images.TryGetValue(portal.Key, out var cached)) return cached;
            // Cache missing/failed entries too, so repaint does not keep attempting to load them.
            images.Add(portal.Key, null);
            Texture2D texture = null;
            Sprite sprite = null;
            try
            {
                using (var stream = typeof(PortalIcons).Assembly.GetManifestResourceStream("UmbraMenu.PortalIcons." + portal.Key + ".png"))
                {
                    if (stream == null) return null;
                    if (stream.Length < 24 || stream.Length > MaximumImageBytes) throw new InvalidOperationException("Portal image size is invalid.");
                    byte[] bytes = new byte[(int)stream.Length];
                    int read = 0;
                    while (read < bytes.Length)
                    {
                        int count = stream.Read(bytes, read, bytes.Length - read);
                        if (count == 0) throw new InvalidOperationException("Portal image was truncated.");
                        read += count;
                    }
                    if (!ValidPng(bytes)) throw new InvalidOperationException("Portal image dimensions are invalid.");
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    if (!ImageConversion.LoadImage(texture, bytes, true)) throw new InvalidOperationException("Portal image could not be decoded.");
                    sprite = Sprite.Create(texture, new Rect(0, 0, ImageSize, ImageSize), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
                    if (!sprite) throw new InvalidOperationException("Portal image sprite could not be created.");
                    sprite.hideFlags = HideFlags.HideAndDontSave;
                    images[portal.Key] = sprite;
                    return sprite;
                }
            }
            catch (Exception error)
            {
                if (sprite) UnityEngine.Object.Destroy(sprite);
                if (texture) UnityEngine.Object.Destroy(texture);
                Debug.LogWarning("Umbra: portal image unavailable for " + portal.Key + ": " + error.Message);
                return null;
            }
        }

        internal static bool ValidPng(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 24 || bytes.Length > MaximumImageBytes) return false;
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            for (int i = 0; i < signature.Length; i++) if (bytes[i] != signature[i]) return false;
            return bytes[8] == 0 && bytes[9] == 0 && bytes[10] == 0 && bytes[11] == 13
                && bytes[12] == 'I' && bytes[13] == 'H' && bytes[14] == 'D' && bytes[15] == 'R'
                && ReadDimension(bytes, 16) == ImageSize && ReadDimension(bytes, 20) == ImageSize;
        }

        private static uint ReadDimension(byte[] bytes, int offset) =>
            ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];

        public static void Release()
        {
            foreach (Sprite sprite in images.Values)
            {
                if (!sprite) continue;
                try
                {
                    Texture2D texture = sprite.texture;
                    UnityEngine.Object.Destroy(sprite);
                    if (texture) UnityEngine.Object.Destroy(texture);
                }
                catch (Exception error) { Debug.LogWarning("Umbra: portal image cleanup failed: " + error.Message); }
            }
            images.Clear();
        }
    }
}