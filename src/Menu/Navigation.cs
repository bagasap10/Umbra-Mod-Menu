using System.Linq;
using UnityEngine;

namespace UmbraMenu
{
    static class Navigation
    {
        public static int menuIndex = 0;
        public static int buttonIndex = 0;
        public static int prevButtonIndex;

        public static void PressBtn(int menuId, int btnId)
        {
            if (btnId < 1)
                return;

            Button button = Utility.FindButtonById(menuId, btnId);
            if (button?.GetAction() == null)
                return;

            prevButtonIndex = btnId;
            button.Activate();
            MenuUI.SyncPage();
        }

        public static void IncreaseValue(int menuId, int btnId)
        {
            Utility.FindButtonById(menuId, btnId)?.GetIncreaseAction()?.Invoke();
        }

        public static void DecreaseValue(int menuId, int btnId)
        {
            Utility.FindButtonById(menuId, btnId)?.GetDecreaseAction()?.Invoke();
        }

        public static void GoBackAMenu()
        {
            Button selected = UmbraMenu.menus[menuIndex].GetButtons()?.FirstOrDefault(row => row.GetId() == buttonIndex);
            if (selected?.GetOnText() == "CONFIRM?" && selected.IsEnabled())
            {
                selected.SetEnabled(false);
                return;
            }
            if (menuIndex == 0)
            {
                UmbraMenu.navigationToggle = false;
                buttonIndex = 0;
                return;
            }
            MenuUI.Back();
        }

        public static void MoveSelection(int direction)
        {
            Menu menu = UmbraMenu.menus[menuIndex];
            int menuLength = menu.GetNumberOfButtons();
            for (int attempt = 0; attempt < menuLength; attempt++)
            {
                buttonIndex += direction;
                if (buttonIndex > menuLength)
                    buttonIndex = 1;
                if (buttonIndex < 1)
                    buttonIndex = menuLength;

                Button button = menu.GetButtons().FirstOrDefault(row => row.GetId() == buttonIndex);
                if (button?.GetAction() != null)
                    return;
            }

            buttonIndex = 0;
        }

        public static void UpdateIndexValues()
        {
            Menu menu = UmbraMenu.menus[menuIndex];
            int menuLength = menu.GetNumberOfButtons();
            if (menuLength == 0)
            {
                buttonIndex = 0;
                return;
            }

            if (buttonIndex > menuLength)
                buttonIndex = 1;
            if (buttonIndex < 1)
                buttonIndex = menuLength;

            Button selectedButton = menu.GetButtons().FirstOrDefault(row => row.GetId() == buttonIndex);
            if (selectedButton?.GetAction() == null)
                MoveSelection(1);

            // The shared window reveals the selected row using its actual viewport.
        }
    }
}
