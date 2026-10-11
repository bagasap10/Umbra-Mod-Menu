using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace UmbraMenu
{
    // One window owns all coordinates, page selection and scrolling.
    internal static class MenuUI
    {
        private static readonly string[] names = { "Overview", "Player", "Movement", "Items", "Spawn", "Teleporter", "Visuals", "Settings", "Modify stats", "Live stats", "Characters", "Buffs", "Spawn items", "Spawn equipment", "Chest items", "Spawn catalog", "Keybinds", "Remove items", "Item packs", "Pack details", "World cheats" };
        private static readonly string[] descriptions = {
            "Your tools, in one place.", "Resources, abilities and character tools.", "Move through the world your way.",
            "Manage your inventory and equipment.", "Create enemies and interactables.", "Stage controls and portals, grouped by expansion.",
            "Keep track of enemies, interactables and active mods.", "Customize the menu and your controls.",
            "Adjust values with the minus and plus controls.", "Your current character stats, updated live.",
            "Choose a character to play.", "Apply a buff to your character.", "Search by name. Select an item to spawn it.",
            "Equipment, grouped by type and expansion.", "Choose an item for the nearest chest.",
            "Select an entry to spawn it.", "Select a binding, then press a key to replace it.",
            "Your current inventory. Each click removes one stack.",
            "General packs and CHEF recipes, sorted by value.", "Review the contents, then give the complete pack.", "Host controls for stage loot, shopping and combat." };

        private static Rect window;
        private static Rect body;
        private static float rowWidth;
        private static float visibleRowTop, visibleRowBottom;
        private static Vector2 sidebarScroll;
        private static bool initialized;
        private static bool clearFocus;
        [Flags]
        internal enum ResizeEdges { None = 0, Left = 1, Right = 2, Top = 4, Bottom = 8 }
        public const float MinWidth = 560, MaxWidth = 1200, MinHeight = 360, MaxHeight = 900;
        private static ResizeEdges resizing, hoveredEdge;
        private static Rect resizeStart;
        private static Vector2 resizePointer;
        private static int resizeControl, cursorDirection = -1;
        public static int ActiveMenuId { get; private set; }
        public static bool CanUsePages => UmbraMenu.characterCollected || UmbraMenu.forceFullModMenu;
        public static float ConfiguredWidth => Mathf.Clamp(UmbraMenu.WindowWidth, MinWidth, MaxWidth);
        public static float ConfiguredHeight => Mathf.Clamp(UmbraMenu.WindowHeight, MinHeight, MaxHeight);

        public static string ToggleShortcut
        {
            get
            {
                KeyCode key = UmbraMenu.keybindDict.TryGetValue("MAIN MENU", out Keybind binding) ? binding.KeyCode : KeyCode.Insert;
                if (key == KeyCode.None || key == KeyCode.Keypad0) return "Num 0";
                string name = key.ToString();
                if (name.StartsWith("Keypad", StringComparison.Ordinal)) name = "Num " + name.Substring(6);
                return name + " / Num 0";
            }
        }

        public static void DrawWatermark()
        {
            UITheme.Prepare();
            float width = Mathf.Min(390, Mathf.Max(0, Screen.width - 16));
            float left = Screen.width - width - 8;
            string status = Loader.devBuild ? " / DEV" : Loader.updateAvailable ? " / UPDATE AVAILABLE" : "";
            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            try
            {
                // The open window can cover the badge on small screens.
                GUI.depth = -99;
                GUI.color = Color.white;
                GUI.Box(new Rect(left, 8, width, 52), "", UITheme.GroupStyle);
                GUI.Label(new Rect(left + 2, 9, width - 4, 25), "UMBRA / v" + UmbraMenu.VERSION + " / " + UmbraMenu.DLC_NAME + status, UITheme.Label);
                GUI.Label(new Rect(left + 2, 32, width - 4, 22), ToggleShortcut + "  |  " + (UmbraMenu.mainMenu.IsEnabled() ? "Close menu" : "Open menu"), UITheme.Hint);
            }
            finally { GUI.depth = previousDepth; GUI.color = previousColor; }
        }

        public static Rect FitWindow(Rect rect, float width, float height, float screenWidth, float screenHeight)
        {
            rect.width = Mathf.Min(Mathf.Clamp(width, MinWidth, MaxWidth), Mathf.Max(0, screenWidth - 16));
            rect.height = Mathf.Min(Mathf.Clamp(height, MinHeight, MaxHeight), Mathf.Max(0, screenHeight - 16));
            rect.x = Mathf.Clamp(rect.x, 0, Mathf.Max(0, screenWidth - rect.width));
            rect.y = Mathf.Clamp(rect.y, 0, Mathf.Max(0, screenHeight - rect.height));
            return rect;
        }

        internal static ResizeEdges HitResizeEdges(Rect rect, Vector2 pointer)
        {
            if (!rect.Contains(pointer)) return ResizeEdges.None;
            float x = pointer.x - rect.x, y = pointer.y - rect.y;
            bool nearSide = x < 18 || x > rect.width - 18;
            bool nearEnd = y < 18 || y > rect.height - 18;
            ResizeEdges edges = ResizeEdges.None;
            if (x < 8 || (x < 18 && nearEnd)) edges |= ResizeEdges.Left;
            if (x > rect.width - 8 || (x > rect.width - 18 && nearEnd)) edges |= ResizeEdges.Right;
            if (y < 8 || (y < 18 && nearSide)) edges |= ResizeEdges.Top;
            if (y > rect.height - 8 || (y > rect.height - 18 && nearSide)) edges |= ResizeEdges.Bottom;
            return edges;
        }

        internal static Rect ResizeWindow(Rect start, Vector2 delta, ResizeEdges edges, float screenWidth, float screenHeight)
        {
            float left = start.x, right = start.xMax, top = start.y, bottom = start.yMax;
            float minimumWidth = Mathf.Min(MinWidth, Mathf.Max(0, screenWidth - 16)), minimumHeight = Mathf.Min(MinHeight, Mathf.Max(0, screenHeight - 16));
            if ((edges & ResizeEdges.Left) != 0) left = Mathf.Clamp(left + delta.x, Mathf.Max(0, right - MaxWidth), right - minimumWidth);
            if ((edges & ResizeEdges.Right) != 0) right = Mathf.Clamp(right + delta.x, left + minimumWidth, Mathf.Min(screenWidth, left + MaxWidth));
            if ((edges & ResizeEdges.Top) != 0) top = Mathf.Clamp(top + delta.y, Mathf.Max(0, bottom - MaxHeight), bottom - minimumHeight);
            if ((edges & ResizeEdges.Bottom) != 0) bottom = Mathf.Clamp(bottom + delta.y, top + minimumHeight, Mathf.Min(screenHeight, top + MaxHeight));
            return new Rect(left, top, right - left, bottom - top);
        }

        public static void ChangeSize(float widthDelta, float heightDelta)
        {
            UmbraMenu.WindowWidth = Mathf.Clamp(ConfiguredWidth + widthDelta, MinWidth, MaxWidth);
            UmbraMenu.WindowHeight = Mathf.Clamp(ConfiguredHeight + heightDelta, MinHeight, MaxHeight);
            Utility.SaveSettings();
        }

        private static void EndResize(bool commit)
        {
            if (resizing != ResizeEdges.None)
            {
                if (commit)
                {
                    UmbraMenu.WindowWidth = window.width;
                    UmbraMenu.WindowHeight = window.height;
                    Utility.SaveSettings();
                }
                else window = resizeStart;
            }
            if (resizeControl != 0 && GUIUtility.hotControl == resizeControl) GUIUtility.hotControl = 0;
            resizing = ResizeEdges.None;
        }

        private static void HandleResize()
        {
            resizeControl = GUIUtility.GetControlID(198705, FocusType.Passive);
            Event current = Event.current;
            hoveredEdge = HitResizeEdges(window, current.mousePosition);
            if (resizing != ResizeEdges.None)
            {
                if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
                {
                    EndResize(false);
                    current.Use();
                }
                else if (current.type == EventType.MouseDrag)
                {
                    window = ResizeWindow(resizeStart, new Vector2(current.mousePosition.x - resizePointer.x, current.mousePosition.y - resizePointer.y), resizing, Screen.width, Screen.height);
                    current.Use();
                }
                else if (current.type == EventType.MouseUp && current.button == 0)
                {
                    EndResize(true);
                    current.Use();
                }
                else if (current.type == EventType.Ignore || GUIUtility.hotControl != resizeControl) EndResize(true);
            }
            else if (current.type == EventType.MouseDown && current.button == 0 && hoveredEdge != ResizeEdges.None && GUIUtility.hotControl == 0)
            {
                resizeStart = window;
                resizePointer = current.mousePosition;
                resizing = hoveredEdge;
                GUIUtility.hotControl = resizeControl;
                GUI.FocusControl(null);
                current.Use();
            }
            if (current.type == EventType.Repaint) UpdateResizeCursor(resizing != ResizeEdges.None ? resizing : hoveredEdge);
        }

        private static void UpdateResizeCursor(ResizeEdges edges)
        {
            bool horizontal = (edges & (ResizeEdges.Left | ResizeEdges.Right)) != 0;
            bool vertical = (edges & (ResizeEdges.Top | ResizeEdges.Bottom)) != 0;
            int direction = edges == ResizeEdges.None ? -1 : !vertical ? 0 : !horizontal ? 1 : edges == (ResizeEdges.Left | ResizeEdges.Top) || edges == (ResizeEdges.Right | ResizeEdges.Bottom) ? 2 : 3;
            if (direction == cursorDirection && direction < 0) return;
            Cursor.SetCursor(direction < 0 ? null : UITheme.ResizeCursor(direction), direction < 0 ? Vector2.zero : new Vector2(12, 12), CursorMode.Auto);
            cursorDirection = direction;
        }

        public static void Release()
        {
            EndResize(false);
            UpdateResizeCursor(ResizeEdges.None);
        }

        public static void ResetWindow()
        {
            Release();
            initialized = false;
        }

        public static void SyncPage()
        {
            if (UmbraMenu.menus == null || UmbraMenu.menus.Count == 0) return;
            if (!CanUsePages) { ActiveMenuId = 0; return; }
            int requested = UmbraMenu.previousMenuOpen;
            if (requested > 0 && requested < UmbraMenu.menus.Count && UmbraMenu.menus[requested].IsEnabled())
            {
                if (requested != ActiveMenuId) NavigateTo(requested);
            }
            else if (ActiveMenuId >= UmbraMenu.menus.Count || (ActiveMenuId > 0 && !UmbraMenu.menus[ActiveMenuId].IsEnabled()))
            {
                int parent = ActiveMenuId < UmbraMenu.menus.Count ? UmbraMenu.menus[ActiveMenuId].GetPrevMenuId() : 0;
                NavigateTo(parent > 0 && UmbraMenu.menus[parent].IsEnabled() ? parent : 0);
            }
        }

        public static void NavigateTo(int id)
        {
            if (id < 0 || id >= UmbraMenu.menus.Count || (id > 0 && !CanUsePages)) return;
            bool[] branch = new bool[UmbraMenu.menus.Count];
            int ancestor = id;
            for (int depth = 0; ancestor > 0 && depth < branch.Length; depth++)
            {
                branch[ancestor] = true;
                ancestor = UmbraMenu.menus[ancestor].GetPrevMenuId();
                if (ancestor < 0 || ancestor >= branch.Length) break;
            }
            clearFocus = true;
            CancelKeybindCapture();
            for (int i = 1; i < branch.Length; i++)
                if (!branch[i] && UmbraMenu.menus[i].IsEnabled()) UmbraMenu.menus[i].SetEnabled(false);
            // Ancestors remain available to Back, but only the selected page is drawn.
            for (int i = 1; i < branch.Length; i++)
                if (branch[i]) UmbraMenu.menus[i].SetEnabled(true);
            ActiveMenuId = id;
            UmbraMenu.previousMenuOpen = id;
            Navigation.menuIndex = id;
            Navigation.buttonIndex = 1;
            UmbraMenu.scrolled = false;
        }

        public static void Back()
        {
            if (ActiveMenuId > 0)
            {
                Menu current = UmbraMenu.menus[ActiveMenuId];
                int parent = current.GetPrevMenuId();
                int parentButton = current.GetActivatingButton()?.GetId() ?? 1;
                NavigateTo(parent);
                Navigation.buttonIndex = parentButton;
            }
        }

        public static bool IsSearchFocused => UmbraMenu.menus.Any(menu => menu.IsSearchFocused());

        public static bool HandleBackShortcut()
        {
            if (!UmbraMenu.mainMenu.IsEnabled() || UmbraMenu.listenForKeybind || UmbraMenu.chatOpen || IsSearchFocused
                || !Input.GetKeyDown(KeyCode.Keypad1)) return false;
            Back();
            return true;
        }

        public static void CancelKeybindCapture()
        {
            UmbraMenu.listenForKeybind = false;
            if (UmbraMenu.menus.Count > 16)
                foreach (Button button in UmbraMenu.menus[16].GetButtons() ?? Enumerable.Empty<Button>()) button.SetEnabled(false);
        }

        public static void Draw()
        {
            SyncPage();
            if (!UmbraMenu.mainMenu.IsEnabled()) { EndResize(true); UpdateResizeCursor(ResizeEdges.None); return; }
            UITheme.Prepare();
            if (!initialized)
            {
                window = new Rect(Mathf.Max(8, (Screen.width - ConfiguredWidth) / 2f), Mathf.Max(8, (Screen.height - ConfiguredHeight) / 2f), ConfiguredWidth, ConfiguredHeight);
                initialized = true;
            }
            if (resizing == ResizeEdges.None) window = FitWindow(window, ConfiguredWidth, ConfiguredHeight, Screen.width, Screen.height);
            HandleResize();
            // Isolate the menu skin so the game's IMGUI and the ESP keep their own styles.
            GUISkin previousSkin = GUI.skin;
            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            try
            {
                GUI.skin = UITheme.MenuSkin;
                GUI.depth = -100;
                GUI.color = Color.white;
                window = GUI.Window(198704, window, DrawWindow, "", GUIStyle.none);
            }
            finally { GUI.skin = previousSkin; GUI.depth = previousDepth; GUI.color = previousColor; }
        }

        private static void DrawWindow(int id)
        {
            if (clearFocus) { GUI.FocusControl(null); clearFocus = false; }
            float rail = window.width < 640 ? 142 : 178;
            GUI.Box(new Rect(0, 0, window.width, window.height), "", UITheme.BackgroundStyle);
            GUI.Box(new Rect(1, 1, rail - 1, window.height - 2), "", UITheme.PanelStyle);
            UITheme.Fill(new Rect(rail, 0, 1, window.height), UITheme.Line);
            UITheme.Fill(new Rect(rail, 88, window.width - rail, 1), UITheme.Line);
            UITheme.Fill(new Rect(0, window.height - 44, window.width, 1), UITheme.Line);

            GUI.Box(new Rect(20, 23, 32, 32), "U", UITheme.Logo);
            GUI.Label(new Rect(60, 17, rail - 64, 31), "UMBRA", rail < 160 ? UITheme.CompactBrand : UITheme.Brand);
            GUI.Label(new Rect(6, 58, rail - 12, 20), "RISK OF RAIN 2", UITheme.Caption);

            int category = CategoryFor(ActiveMenuId);
            string breadcrumb = ActiveMenuId > 7 ? names[category] + " / " + names[ActiveMenuId] : "WORKSPACE / " + names[ActiveMenuId];
            GUI.Label(new Rect(rail + 10, 9, window.width - rail - 118, 20), breadcrumb, UITheme.Caption);
            GUI.Label(new Rect(rail + 10, 29, window.width - rail - 118, 31), ActiveMenuId == 19 ? UmbraMenu.menus[ActiveMenuId].GetTitle() : names[ActiveMenuId], UITheme.Title);
            GUI.Label(new Rect(rail + 10, 60, window.width - rail - 22, 22), descriptions[ActiveMenuId], UITheme.Hint);
            if (ActiveMenuId > 0 && GUI.Button(new Rect(window.width - 112, 27, 56, 30), "Back", UITheme.SmallButton)) Back();
            if (GUI.Button(new Rect(window.width - 46, 27, 28, 30), "x", UITheme.SmallButton))
            {
                GUI.FocusControl(null);
                UmbraMenu.mainMenu.SetEnabled(false);
                CancelKeybindCapture();
            }

            GUI.Label(new Rect(6, 91, rail - 12, 24), "WORKSPACE", UITheme.Caption);
            Rect sidebarViewport = new Rect(12, 122, rail - 24, Mathf.Max(40, window.height - 240));
            sidebarScroll = GUI.BeginScrollView(sidebarViewport, sidebarScroll, new Rect(0, 0, rail - 38, 8 * 36), false, false, GUIStyle.none, UITheme.ScrollTrack);
            for (int page = 0; page <= 7; page++)
            {
                bool selected = category == page;
                bool oldEnabled = GUI.enabled;
                GUI.enabled = oldEnabled && (page == 0 || CanUsePages);
                if (GUI.Button(new Rect(0, page * 36, rail - 40, 31), names[page], selected ? UITheme.SidebarSelected : UITheme.Sidebar)) NavigateTo(page);
                if (selected) UITheme.Fill(new Rect(0, page * 36 + 7, 3, 17), UITheme.Accent);
                GUI.enabled = oldEnabled;
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(6, window.height - 107, rail - 12, 24), "v" + UmbraMenu.VERSION, UITheme.Caption);
            GUI.Label(new Rect(6, window.height - 84, rail - 12, 22), Loader.devBuild ? "Development build" : Loader.updateAvailable ? "Update available" : UmbraMenu.DLC_NAME, UITheme.Caption);

            body = new Rect(rail + 24, 105, window.width - rail - 48, window.height - 164);
            if (ActiveMenuId == 0) DrawOverview();
            else UmbraMenu.menus[ActiveMenuId].Draw();

            UITheme.Fill(new Rect(20, window.height - 25, 5, 5), CanUsePages ? UITheme.Accent : UITheme.Muted);
            GUI.Label(new Rect(22, window.height - 39, rail - 22, 36), CanUsePages ? "Run active" : "Waiting for a player", UITheme.Caption);
            string hint = resizing != ResizeEdges.None ? "Release to save size  /  Esc to cancel" : UmbraMenu.listenForKeybind ? "Press a key to bind  /  Esc to cancel" : UmbraMenu.menus[ActiveMenuId].IsSearchFocused() ? "Enter / Esc  Finish search" : UmbraMenu.AllowNavigation ? "Num 1 Back  /  2 / 8 Select  /  5 Apply  /  4 / 6 Adjust" : "Num 1 Back  /  Wheel to scroll  /  Drag edges to resize";
            GUI.Label(new Rect(rail + 10, window.height - 42, window.width - rail - 36, 20), hint, UITheme.Caption);
            GUI.Label(new Rect(rail + 10, window.height - 23, window.width - rail - 36, 18), ToggleShortcut + "  Toggle menu", UITheme.Caption);
            DrawResizeBorder();
            // Only the header is draggable. Clicking any row never hides the menu.
            if (resizing == ResizeEdges.None) GUI.DragWindow(new Rect(18, 18, window.width - 140, 68));
        }

        private static void DrawResizeBorder()
        {
            Color edgeColor = resizing != ResizeEdges.None || hoveredEdge != ResizeEdges.None ? UITheme.Accent : UITheme.GlassRim;
            ResizeEdges edges = resizing != ResizeEdges.None ? resizing : hoveredEdge;
            UITheme.Fill(new Rect(12, 1, window.width - 24, 1), UITheme.GlassRim);
            UITheme.Fill(new Rect(12, window.height - 2, window.width - 24, 1), UITheme.GlassRim);
            UITheme.Fill(new Rect(1, 12, 1, window.height - 24), UITheme.GlassRim);
            UITheme.Fill(new Rect(window.width - 2, 12, 1, window.height - 24), UITheme.GlassRim);
            if ((edges & ResizeEdges.Left) != 0) UITheme.Fill(new Rect(1, 12, 2, window.height - 24), edgeColor);
            if ((edges & ResizeEdges.Right) != 0) UITheme.Fill(new Rect(window.width - 3, 12, 2, window.height - 24), edgeColor);
            if ((edges & ResizeEdges.Top) != 0) UITheme.Fill(new Rect(12, 1, window.width - 24, 2), edgeColor);
            if ((edges & ResizeEdges.Bottom) != 0) UITheme.Fill(new Rect(12, window.height - 3, window.width - 24, 2), edgeColor);
            for (int i = 0; i < 3; i++) UITheme.Fill(new Rect(window.width - 10 - i * 4, window.height - 7 - i * 4, 2 + i * 4, 1), edgeColor);
        }

        private static void DrawOverview()
        {
            GUI.Box(new Rect(body.x, body.y, body.width, 82), "", UITheme.GroupStyle);
            GUI.Label(new Rect(body.x + 6, body.y + 8, body.width - 12, 28), CanUsePages ? "Ready to explore." : "Your next run starts here.", UITheme.Brand);
            GUI.Label(new Rect(body.x + 6, body.y + 38, body.width - 12, 35), CanUsePages ? "Choose a category. Your tools stay here as you switch pages." : "Start a run to access player, inventory and world tools.", UITheme.Hint);
            body.y += 98;
            body.height -= 98;
            UmbraMenu.mainMenu.Draw();
        }

        private static int CategoryFor(int page)
        {
            for (int depth = 0; page > 7 && depth < names.Length; depth++) page = UmbraMenu.menus[page].GetPrevMenuId();
            return Mathf.Clamp(page, 0, 7);
        }

        public static void DrawRows(IMenu menu)
        {
            if (!menu.Enabled || menu.Buttons == null) return;
            Rect viewport = body;
            if (menu is SearchableListMenu searchable)
            {
                searchable.DrawSearch(new Rect(body.x, body.y, body.width, 38));
                viewport.y += 52;
                viewport.height -= 52;
            }
            rowWidth = Mathf.Max(100, viewport.width - 16);
            float height = menu.Id == 0 ? 374 : UITheme.RowPitch * menu.NumberOfButtons;
            float maxScroll = Mathf.Max(0, height - viewport.height);
            Vector2 scroll = menu.CurrentScrollPosition;
            scroll.x = 0;
            scroll.y = Mathf.Clamp(scroll.y, 0, maxScroll);
            if (UmbraMenu.navigationToggle && Navigation.menuIndex == menu.Id && !UmbraMenu.scrolled && Navigation.buttonIndex > 0)
            {
                Rect selected = RowRect(menu.Id, Navigation.buttonIndex);
                if (selected.y < scroll.y) scroll.y = selected.y;
                else if (selected.yMax > scroll.y + viewport.height) scroll.y = selected.yMax - viewport.height;
            }
            menu.CurrentScrollPosition = GUI.BeginScrollView(viewport, scroll, new Rect(0, 0, rowWidth, height), false, false, GUIStyle.none, UITheme.ScrollTrack);
            visibleRowTop = menu.CurrentScrollPosition.y;
            visibleRowBottom = visibleRowTop + viewport.height;
            // Override only our scrollbar's thumb; never mutate GUI.skin.
            foreach (Button button in menu.Buttons) button.Draw();
            GUI.EndScrollView();
        }

        private static Rect RowRect(int menu, int position)
        {
            if (menu == 0)
            {
                if (position == 8) return new Rect(0, 332, rowWidth, 40);
                float cardWidth = (rowWidth - 10) / 2;
                return new Rect((position - 1) % 2 * (cardWidth + 10), (position - 1) / 2 * 82, cardWidth, 72);
            }
            return new Rect(0, (position - 1) * UITheme.RowPitch, rowWidth, UITheme.RowHeight);
        }

        public static void DrawButton(IButton button)
        {
            if (button.ParentMenu == null) return;
            int menu = button.ParentMenu.GetId();
            Rect rect = RowRect(menu, button.Position);
            if (button is DividerButton divider) { divider.DrawHeading(rect); return; }
            string label = DisplayText(button.Text);
            if (button is TextButton)
            {
                GUI.Box(rect, "", UITheme.GroupStyle);
                GUI.Label(rect, label, UITheme.Label);
                return;
            }
            bool selected = UmbraMenu.navigationToggle && Navigation.menuIndex == menu && Navigation.buttonIndex == button.Position;
            bool multiple = button.IncreaseAction != null || button.DecreaseAction != null;
            bool toggle = button is TogglableButton || button is TogglableMulButton;
            bool confirmation = string.Equals(button.OnText, "CONFIRM?", StringComparison.OrdinalIgnoreCase);
            int child = FindChild(menu, button.Position);
            bool card = menu == 0 && button.Position < 8;
            bool keybind = menu == 16;
            GUIStyle style = confirmation ? UITheme.DangerButton : card ? (selected ? UITheme.SelectedCard : UITheme.Card) : selected ? UITheme.SelectedButton : UITheme.Button;
            Rect actionRect = rect;
            if (multiple) actionRect.width -= 82;
            // Reserve space for state chips and chevrons without shortening the hit target.
            GUIStyle labelStyle = style;
            string badge = child > 0 || menu == 18 ? ">" : keybind ? (button.Enabled ? "Press a key..." : "Bind") : confirmation ? "" : toggle ? (button.Enabled ? "ON" : "OFF") : menu == 17 ? "-" : menu == 12 || menu == 13 || menu == 11 ? "+" : "";
            float badgeWidth = keybind ? 106 : toggle && child < 0 && !confirmation ? 42 : 30;
            bool clicked = false;
            if (button.Action == null) GUI.Box(actionRect, "", UITheme.GroupStyle);
            else clicked = GUI.Button(actionRect, GUIContent.none, style);
            Rect labelRect = new Rect(actionRect.x, actionRect.y, actionRect.width - badgeWidth - 12, actionRect.height);
            if (button is PickupButton pickup)
            {
                if (rect.yMax >= visibleRowTop && rect.y <= visibleRowBottom)
                {
                    Rect iconRect = new Rect(actionRect.x + 8, actionRect.y + 4, 32, 32);
                    if (pickup.TryGetIcon(out Texture icon, out Rect uv))
                        GUI.DrawTextureWithTexCoords(iconRect, icon, uv, true);
                    else if (!string.IsNullOrEmpty(pickup.MissingIconText))
                    {
                        GUI.Box(iconRect, "", UITheme.GroupStyle);
                        GUI.Label(iconRect, pickup.MissingIconText, UITheme.Badge);
                    }
                }
                labelRect.x += 38;
                labelRect.width -= 38;
            }
            GUI.Label(labelRect, label, labelStyle);
            if (card)
                GUI.Label(new Rect(rect.x, rect.y + 36, rect.width - 26, 28), descriptions[button.Position].Split('.')[0], UITheme.Caption);
            Rect badgeRect = new Rect(actionRect.xMax - badgeWidth - 12, actionRect.y + (actionRect.height - 20) / 2, badgeWidth, 20);
            if (toggle && child < 0 && !confirmation && !keybind) GUI.Box(badgeRect, badge, button.Enabled ? UITheme.SwitchOn : UITheme.SwitchOff);
            else GUI.Label(badgeRect, badge, UITheme.Badge);
            if (selected) UITheme.Fill(new Rect(actionRect.x, actionRect.y + 9, 3, actionRect.height - 18), UITheme.Accent);
            if (clicked)
            {
                Button.Activate(button);
                SyncPage();
            }
            if (multiple)
            {
                if (GUI.Button(new Rect(rect.xMax - 74, rect.y, 34, rect.height), "-", UITheme.SmallButton)) button.DecreaseAction?.Invoke();
                if (GUI.Button(new Rect(rect.xMax - 34, rect.y, 34, rect.height), "+", UITheme.SmallButton)) button.IncreaseAction?.Invoke();
            }
        }

        private static int FindChild(int parent, int position)
        {
            foreach (Menu menu in UmbraMenu.menus)
            {
                Button activator = menu.GetActivatingButton();
                if (activator != null && activator.GetParentMenu().GetId() == parent && activator.GetId() == position) return menu.GetId();
            }
            return -1;
        }

        internal static string DisplayText(string text)
        {
            text = text ?? "";
            if (text.EndsWith(" : ON", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 5);
            if (text.EndsWith(" : OFF", StringComparison.Ordinal)) text = text.Substring(0, text.Length - 6);
            text = text.Replace(" (ON)", "").Replace(" (OFF)", "");
            if (!text.Contains("<") && text == text.ToUpperInvariant())
                text = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant()).Replace("Esp", "ESP").Replace("Xp", "XP").Replace("Dlc", "DLC");
            return text;
        }
    }
}
