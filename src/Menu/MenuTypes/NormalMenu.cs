using System.Collections.Generic;
using UnityEngine;

namespace UmbraMenu
{
    public class NormalMenu : IMenu
    {
        public float WidthSize { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public bool enabled { get; set; }
        public Rect Rect { get; set; }
        public bool IfDragged { get; set; }
        public int NumberOfButtons { get; set; }
        public Button ActivatingButton { get; set; }
        public int PrevMenuId { get; set; }
        public List<Button> Buttons { get; set; }

        public Vector2 CurrentScrollPosition { get; set; }

        public bool Enabled 
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
                if (value) UmbraMenu.previousMenuOpen = Id;
                if (UmbraMenu.navigationToggle && value)
                {
                    Navigation.menuIndex = Id;
                }

                if (enabled)
                {
                    ActivatingButton?.SetEnabled(true);
                }
                else
                {
                    ActivatingButton?.SetEnabled(false);
                }
            }
        }


        public NormalMenu(int id, Rect rect, string title)
        {
            Id = id;
            Rect = rect;
            Title = title;
            NumberOfButtons = 0;
            WidthSize = UmbraMenu.Width;
        }

        // Page chrome and dragging belong to the shared MenuUI window.
        public void SetWindow() { }

        public virtual void Draw() => MenuUI.DrawRows(this);

        public virtual void Reset()
        {
            Enabled = false;
            IfDragged = false;
            CurrentScrollPosition = Vector2.zero;
            if (Buttons != null)
                foreach (Button button in Buttons) button.SetEnabled(false);
        }
    }
}
