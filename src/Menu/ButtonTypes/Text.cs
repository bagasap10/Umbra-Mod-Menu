using System;
using UnityEngine;

namespace UmbraMenu
{
    public class TextButton : IButton
    {
        public Menu ParentMenu { get; set; }
        public int Position { get; set; }
        public string Text { get; set; }
        public Action Action { get; set; }
        public bool Enabled { get; set; }

        public string OnText { get; set; }
        public string OffText { get; set; }
        public Action IncreaseAction { get; set; }
        public Action DecreaseAction { get; set; }

        public TextButton(Menu parentMenu, int position, string text)
        {
            this.ParentMenu = parentMenu;
            this.Position = position;
            this.Text = text;
        }

        public void Draw() => MenuUI.DrawButton(this);
    }
}
