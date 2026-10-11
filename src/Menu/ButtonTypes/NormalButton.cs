using System;
using UnityEngine;

namespace UmbraMenu
{
    public class NormalButton : IButton
    {
        public Menu ParentMenu { get; set; }
        public int Position { get; set; }
        public string Text { get; set; }
        public bool Enabled { get; set; }
        public Action Action { get; set; }

        public string OnText { get; set; }
        public string OffText { get; set; }
        public Action IncreaseAction { get; set; }
        public Action DecreaseAction { get; set; }

        public NormalButton(Menu parentMenu, int position, string text, Action Action) 
        {
            this.ParentMenu = parentMenu;
            this.Position = position;
            this.Text = text;
            this.Action = Action;
        }

        public void Draw() => MenuUI.DrawButton(this);
    }
}
