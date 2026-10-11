using System;
using UnityEngine;

namespace UmbraMenu
{
    public class MulButton : IButton
    {
        public Menu ParentMenu { get; set; }
        public int Position { get; set; }
        public string Text { get; set; }
        public bool Enabled { get; set; }
        public Action Action { get; set; }
        public Action IncreaseAction { get; set; }
        public Action DecreaseAction{ get; set; }

        public string OnText { get; set; }
        public string OffText { get; set; }

        public MulButton(Menu parentMenu, int position, string text, Action Action, Action IncreaseAction, Action DecreaseAction)
        {
            this.ParentMenu = parentMenu;
            this.Position = position;
            this.Text = text;
            this.Action = Action;
            this.IncreaseAction = IncreaseAction;
            this.DecreaseAction = DecreaseAction;
        }

        public void Draw() => MenuUI.DrawButton(this);
    }
}
