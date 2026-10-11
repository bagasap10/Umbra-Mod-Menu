using System;
using UnityEngine;
using UnityEngine.Sprites;

namespace UmbraMenu
{
    // The sprite is resolved for the exact pickup or portal used by this row's action.
    internal sealed class PickupButton : NormalButton
    {
        private readonly Func<Sprite> resolveIcon;
        private Sprite cachedSprite;
        private Texture icon;
        private Rect iconUV;
        internal string MissingIconText { get; }

        public PickupButton(Menu menu, int position, string text, Action action, Sprite sprite)
            : this(menu, position, text, action, () => sprite) { }

        public PickupButton(Menu menu, int position, string text, Action action, Func<Sprite> resolveIcon, string missingIconText = null)
            : base(menu, position, text, action) { this.resolveIcon = resolveIcon; MissingIconText = missingIconText; }

        public bool TryGetIcon(out Texture texture, out Rect uvRect)
        {
            Sprite sprite = resolveIcon?.Invoke();
            if (sprite && sprite.texture && sprite != cachedSprite)
            {
                Vector4 uv = DataUtility.GetOuterUV(sprite);
                cachedSprite = sprite;
                icon = sprite.texture;
                iconUV = new Rect(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y);
            }
            texture = icon;
            uvRect = iconUV;
            return texture;
        }
    }
}
