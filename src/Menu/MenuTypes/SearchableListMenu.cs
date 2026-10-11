using System;
using UnityEngine;

namespace UmbraMenu
{
    public sealed class SearchableListMenu : ListMenu
    {
        private string searchText = "";
        private bool searchFocused;
        private readonly string searchPlaceholder;

        public Action<string> SearchChanged { get; set; }
        public string SearchText => searchText;
        public bool IsSearchFocused => Enabled && searchFocused;
        public override float ContentOffset => 52f;

        public override bool Enabled
        {
            get => base.Enabled;
            set
            {
                base.Enabled = value;
                if (!value)
                    searchFocused = false;
            }
        }

        public SearchableListMenu(int id, Rect rect, string title, string placeholder = "Search item name...") : base(id, rect, title)
        { searchPlaceholder = placeholder; }

        public void SetSearchText(string value)
        {
            value = value ?? "";
            if (searchText == value)
                return;

            searchText = value;
            CurrentScrollPosition = Vector2.zero;
            if (UmbraMenu.navigationToggle && Navigation.menuIndex == Id)
            {
                Navigation.buttonIndex = 1;
                UmbraMenu.scrolled = false;
            }
            SearchChanged?.Invoke(searchText);
        }

        internal void DrawSearch(Rect row)
        {
            string controlName = "UmbraItemSearch_" + Id;
            Rect searchRect = new Rect(row.x, row.y, row.width - 76, row.height);
            Rect clearRect = new Rect(row.xMax - 66, row.y, 66, row.height);
            Event currentEvent = Event.current;
            bool focused = GUI.GetNameOfFocusedControl() == controlName;

            if (focused && currentEvent.type == EventType.MouseDown && !searchRect.Contains(currentEvent.mousePosition))
                GUI.FocusControl(null);
            else if (focused && currentEvent.type == EventType.KeyDown
                && (currentEvent.keyCode == KeyCode.Escape || currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
            {
                GUI.FocusControl(null);
                currentEvent.Use();
            }

            GUI.SetNextControlName(controlName);
            SetSearchText(GUI.TextField(searchRect, searchText, 128, UITheme.Search));
            if (GUI.Button(clearRect, "Clear", UITheme.SmallButton))
            {
                SetSearchText("");
                GUI.FocusControl(controlName);
            }

            searchFocused = GUI.GetNameOfFocusedControl() == controlName;
            if (searchText.Length == 0 && !searchFocused)
                GUI.Label(searchRect, searchPlaceholder, UITheme.SearchPlaceholder);
        }

    }
}
