using System.Collections.Generic;
using UnityEngine;

namespace UmbraMenu
{
    // Shared, cached IMGUI resources. No texture or style allocations per row/frame.
    internal static class UITheme
    {
        public const float RowHeight = 40, RowPitch = 46;
        public static readonly Color32 Background = new Color32(18, 29, 39, 158);
        public static readonly Color32 Panel = new Color32(27, 42, 53, 60);
        public static readonly Color32 Surface = new Color32(125, 158, 178, 28);
        public static readonly Color32 Hover = new Color32(152, 192, 210, 58);
        public static readonly Color32 Selected = new Color32(88, 196, 169, 65);
        public static readonly Color32 Accent = new Color32(111, 224, 192, 255);
        public static readonly Color32 Text = new Color32(230, 237, 242, 255);
        public static readonly Color32 Muted = new Color32(185, 203, 215, 255);
        public static readonly Color32 Line = new Color32(175, 207, 223, 35);
        public static readonly Color32 GlassRim = new Color32(211, 241, 255, 75);
        public static readonly Color32 Warning = new Color32(241, 179, 113, 255);

        private static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
        private static readonly List<Texture2D> textures = new List<Texture2D>();
        private static Texture2D pixel;
        private static GUISkin menuSkin;
        private static float opacity = -1;
        private static readonly Dictionary<int, Texture2D> cursors = new Dictionary<int, Texture2D>();

        // Refresh background textures only when the opacity setting changes.
        public static void Prepare()
        {
            float next = Mathf.Clamp(UmbraMenu.GlassOpacity, .35f, .9f);
            if (next == opacity) return;
            ReleaseStyles();
            opacity = next;
        }
        public static GUISkin MenuSkin
        {
            get
            {
                if (!menuSkin)
                {
                    menuSkin = Object.Instantiate(GUI.skin);
                    menuSkin.hideFlags = HideFlags.HideAndDontSave;
                    menuSkin.verticalScrollbar = ScrollTrack;
                    menuSkin.verticalScrollbarThumb = ScrollThumb;
                    menuSkin.verticalScrollbarUpButton = GUIStyle.none;
                    menuSkin.verticalScrollbarDownButton = GUIStyle.none;
                }
                return menuSkin;
            }
        }

        public static GUIStyle Label => Style("label", Text, 14);
        public static GUIStyle Caption => Style("caption", Muted, 11);
        public static GUIStyle Hint => Style("hint", Muted, 12);
        public static GUIStyle Badge => Style("badge", Muted, 12, false, null, null, true);
        public static GUIStyle Title => Style("title", Text, 23, true);
        public static GUIStyle Brand => Style("brand", Text, 22, true);
        public static GUIStyle CompactBrand => Style("compactBrand", Text, 16, true);
        public static GUIStyle Logo => Style("logo", new Color32(18, 29, 39, 255), 18, true, Accent, Accent, true);
        public static GUIStyle Section => Style("section", Accent, 11, true);
        public static GUIStyle Button => Style("button", Text, 14, false, Surface, Hover);
        public static GUIStyle SelectedButton => Style("selected", Text, 14, false, Selected, Hover);
        public static GUIStyle Sidebar => Style("sidebar", Muted, 14, false, Panel, Surface);
        public static GUIStyle SidebarSelected => Style("sidebarSelected", Accent, 14, true, Selected, Hover);
        public static GUIStyle SmallButton => Style("small", Text, 13, false, Surface, Hover, true);
        public static GUIStyle DangerButton => Style("danger", Warning, 14, false, Surface, Hover);
        public static GUIStyle Card => Style("card", Text, 15, true, Surface, Hover);
        public static GUIStyle SelectedCard => Style("selectedCard", Text, 15, true, Selected, Hover);
        public static GUIStyle SwitchOff => Style("switchOff", Muted, 10, true, Line, Line, true);
        public static GUIStyle SwitchOn => Style("switchOn", Background, 10, true, Accent, Accent, true);
        public static GUIStyle BackgroundStyle => Style("background", Text, 14, false, new Color32(Background.r, Background.g, Background.b, (byte)(Mathf.Clamp(UmbraMenu.GlassOpacity, .35f, .9f) * 255)), Background);
        public static GUIStyle PanelStyle => Style("panel", Text, 14, false, Panel, Panel);
        public static GUIStyle GroupStyle => Style("group", Text, 14, true, Panel, Panel);
        public static GUIStyle Search => Style("search", Text, 14, false, Surface, Selected);
        public static GUIStyle SearchPlaceholder => Style("placeholder", Muted, 14);
        public static GUIStyle ScrollTrack => Style("scrollTrack", Text, 12, false, Panel, Panel);
        public static GUIStyle ScrollThumb => Style("scrollThumb", Text, 12, false, GlassRim, Accent);

        private static GUIStyle Style(string key, Color32 color, int size, bool bold = false,
            Color32? background = null, Color32? hover = null, bool centered = false)
        {
            if (styles.TryGetValue(key, out GUIStyle cached))
                return cached;

            var style = new GUIStyle
            {
                fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = centered ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft,
                padding = new RectOffset(centered ? 0 : 14, centered ? 0 : 14, 0, 0),
                border = new RectOffset(8, 8, 8, 8), wordWrap = true, richText = true
            };
            style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
            if (background.HasValue)
            {
                style.normal.background = Rounded(background.Value);
                style.hover.background = Rounded(hover ?? background.Value);
                style.active.background = style.focused.background = Rounded(Selected);
            }
            if (key == "card" || key == "selectedCard")
            {
                style.alignment = TextAnchor.UpperLeft;
                style.padding.top = 14;
            }
            if (key == "brand" || key == "compactBrand") style.padding = new RectOffset();
            if (key == "scrollTrack" || key == "scrollThumb")
            {
                style.fixedWidth = 8;
                style.padding = new RectOffset();
            }
            styles.Add(key, style);
            return style;
        }

        private static Texture2D Rounded(Color32 color)
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(8 - (x + .5f), x + .5f - (size - 8));
                float dy = Mathf.Max(8 - (y + .5f), y + .5f - (size - 8));
                float coverage = Mathf.Clamp01(8.5f - Mathf.Sqrt(Mathf.Max(0, dx) * Mathf.Max(0, dx) + Mathf.Max(0, dy) * Mathf.Max(0, dy)));
                    // A soft upper reflection keeps the translucent layers from looking flat.
                    float reflection = Mathf.Max(0, 1 - y / 16f) * 9;
                    pixels[y * size + x] = new Color32((byte)Mathf.Min(255, color.r + reflection), (byte)Mathf.Min(255, color.g + reflection), (byte)Mathf.Min(255, color.b + reflection), (byte)(color.a * coverage));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            textures.Add(texture);
            return texture;
        }

        public static void Fill(Rect rect, Color color)
        {
            if (!pixel)
            {
                pixel = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
                textures.Add(pixel);
            }
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }

        public static Texture2D ResizeCursor(int direction)
        {
            if (cursors.TryGetValue(direction, out Texture2D cached)) return cached;
            float dx = direction == 0 ? 1 : direction == 1 ? 0 : .7071f;
            float dy = direction == 0 ? 0 : direction == 1 ? 1 : direction == 2 ? .7071f : -.7071f;
            var lines = new List<Vector2[]> { new[] { new Vector2(12 - dx * 9, 12 - dy * 9), new Vector2(12 + dx * 9, 12 + dy * 9) } };
            for (int end = -1; end <= 1; end += 2)
            {
                Vector2 tip = new Vector2(12 + end * dx * 9, 12 + end * dy * 9);
                for (int side = -1; side <= 1; side += 2)
                    lines.Add(new[] { tip, new Vector2(tip.x - end * dx * 4 - side * dy * 3, tip.y - end * dy * 4 + side * dx * 3) });
            }
            var cursorPixels = new Color32[24 * 24];
            for (int y = 0; y < 24; y++)
            for (int x = 0; x < 24; x++)
            {
                float distance = 100;
                foreach (Vector2[] line in lines)
                {
                    float vx = line[1].x - line[0].x, vy = line[1].y - line[0].y;
                    float t = Mathf.Clamp01(((x - line[0].x) * vx + (y - line[0].y) * vy) / (vx * vx + vy * vy));
                    float ox = x - line[0].x - t * vx, oy = y - line[0].y - t * vy;
                    distance = Mathf.Min(distance, Mathf.Sqrt(ox * ox + oy * oy));
                }
                cursorPixels[y * 24 + x] = distance < 1 ? new Color32(230, 255, 249, 255) : distance < 2 ? new Color32(8, 20, 24, 255) : new Color32(0, 0, 0, 0);
            }
            var texture = new Texture2D(24, 24, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(cursorPixels);
            texture.Apply(false, false);
            cursors.Add(direction, texture);
            return texture;
        }

        private static void ReleaseStyles()
        {
            foreach (Texture2D texture in textures)
                if (texture) Object.Destroy(texture);
            textures.Clear();
            styles.Clear();
            pixel = null;
            if (menuSkin) Object.Destroy(menuSkin);
            menuSkin = null;
        }

        public static void Release()
        {
            ReleaseStyles();
            foreach (Texture2D cursor in cursors.Values) if (cursor) Object.Destroy(cursor);
            cursors.Clear();
            opacity = -1;
        }
    }
}
