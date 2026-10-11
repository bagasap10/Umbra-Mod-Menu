using System.Collections.Generic;
using UnityEngine;
using System;

namespace UmbraMenu
{
    public class Menu
    {
        private IMenu _menu;

        public Menu(IMenu menu)
        {
            _menu = menu;
        }

        public void SetWindow()
        {
            _menu.SetWindow();
        }

        public virtual void Draw()
        {
            _menu.Draw();
        }

        public void AddButtons(List<Button> buttonList)
        {
            _menu.Buttons = buttonList;
            _menu.NumberOfButtons = 0;
            foreach (Button button in buttonList)
                _menu.NumberOfButtons = Math.Max(_menu.NumberOfButtons, button.GetId());
        }

        public int GetId()
        {
            return _menu.Id;
        }
        public bool IsEnabled()
        {
            return _menu.Enabled;
        }
        
        public Rect GetRect()
        {
            return _menu.Rect;
        }
        
        public string GetTitle()
        {
            return _menu.Title;
        }

        public void SetTitle(string newTitle)
        {
            _menu.Title = newTitle;
        }

        public List<Button> GetButtons()
        {
            return _menu.Buttons;
        }

        public void ToggleMenu()
        {
            SetEnabled(!_menu.Enabled);
        }

        public virtual void SetEnabled(bool value)
        {
            _menu.Enabled = value;
        }

        public void SetIfDragged(bool value)
        {
            _menu.IfDragged = value;
        }

        public void SetRect(Rect rect)
        {
            _menu.Rect = rect;
        }

        public int GetNumberOfButtons()
        {
            return _menu.NumberOfButtons;
        }

        public void SetNumberOfButtons(int value)
        {
            _menu.NumberOfButtons = value;
        }

        public float GetWidthSize()
        {
            return _menu.WidthSize;
        }

        public float GetContentOffset()
        {
            return (_menu as ListMenu)?.ContentOffset ?? 0f;
        }

        public bool IsSearchFocused()
        {
            return (_menu as SearchableListMenu)?.IsSearchFocused ?? false;
        }

        protected string SearchText => (_menu as SearchableListMenu)?.SearchText ?? "";

        protected void ConfigureSearch(Action<string> onChanged)
        {
            ((SearchableListMenu)_menu).SearchChanged = onChanged;
        }

        public void SetActivatingButton(Button button)
        {
            _menu.ActivatingButton = button;
        }

        public Button GetActivatingButton()
        {
            return _menu.ActivatingButton;
        }

        public virtual void Reset()
        {
            _menu.Reset();
        }

        public int GetPrevMenuId()
        {
            return _menu.PrevMenuId;
        }

        public void SetPrevMenuId(int value)
        {
            _menu.PrevMenuId = value;
        }

        public void SetScrollPosition(Vector2 value) 
        {
            _menu.CurrentScrollPosition = value;
        }

        public void SetWidthSize(float value)
        {
            _menu.WidthSize = value;
        }
    }
}
