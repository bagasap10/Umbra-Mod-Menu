using System;
using UnityEngine;

namespace UmbraMenu
{
    public class TogglableButton : IButton
    {
        public Menu ParentMenu { get; set; }
        public int Position { get; set; }
        public string Text { get; set; }
        public Action Action { get; set; }
        public Action OnAction, OffAction;
        public Action IncreaseAction { get; set; }
        public Action DecreaseAction { get; set; }
        private bool enabled = false;
        public string OnText { get; set; }
        public string OffText { get; set; }


        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
                if (enabled)
                {
                    Text = OnText;
                    Action = OnAction;
                }
                else
                {
                    Text = OffText;
                    Action = OffAction;
                }
            }
        }

        public TogglableButton(Menu parentMenu, int position, string offText, string onText, Action OffAction, Action OnAction, bool defaultEnable = false)
        {
            Enabled = defaultEnable;
            this.ParentMenu = parentMenu;
            this.Position = position;
            Text = offText;
            this.OffText = offText;
            this.OnText = onText;
            Action = OffAction;
            this.OffAction = OffAction;
            this.OnAction = OnAction;

            if (defaultEnable)
            {
                Text = onText;
                Action = OnAction;
            }
        }

        public void Draw() => MenuUI.DrawButton(this);
    }
}
