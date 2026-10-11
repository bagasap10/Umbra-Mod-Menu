using System;
using UnityEngine;

namespace UmbraMenu
{
    // Uses a list row for layout, but is a label rather than an actionable button.
    internal sealed class DividerButton : IButton
    {
        public Menu ParentMenu { get; set; }
        public int Position { get; set; }
        public string Text { get; set; }
        public bool Enabled { get; set; }
        public string OnText { get; set; }
        public string OffText { get; set; }
        public Action Action { get; set; }
        public Action IncreaseAction { get; set; }
        public Action DecreaseAction { get; set; }

        private readonly Color32 color;
        private readonly bool rarityHeading;
        private GUIStyle style;

        public DividerButton(Menu parentMenu, int position, string text, Color32 color, bool rarityHeading)
        {
            ParentMenu = parentMenu;
            Position = position;
            Text = text;
            this.color = color;
            this.rarityHeading = rarityHeading;
        }

        public void Draw() => MenuUI.DrawButton(this);

        internal void DrawHeading(Rect rect)
        {
            if (string.IsNullOrEmpty(Text))
            {
                UITheme.Fill(new Rect(rect.x + 14, rect.y + rect.height / 2, rect.width - 28, 1), UITheme.GlassRim);
                return;
            }
            if (style == null)
            {
                style = new GUIStyle(UITheme.Label)
                {
                    fontSize = rarityHeading ? 15 : 12,
                    fontStyle = rarityHeading ? FontStyle.Bold : FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true
                };
                style.normal.textColor = color;
            }

            if (rarityHeading)
            {
                GUI.Box(rect, "", UITheme.GroupStyle);
                UITheme.Fill(new Rect(rect.x, rect.y + 10, 3, 20), color);
            }
            else
                UITheme.Fill(new Rect(rect.x + 14, rect.y + 2, rect.width - 28, 1), UITheme.Line);

            GUI.Label(rect, Text, style);
        }
    }
}
