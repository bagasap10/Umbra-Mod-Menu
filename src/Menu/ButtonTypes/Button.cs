using System;

namespace UmbraMenu
{
    public class Button
    {
        private IButton _button;

        public Button(IButton button)
        {
            _button = button;
        }

        public void Draw()
        {
            _button.Draw();
        }

        public void Activate() => Activate(_button);

        internal static void Activate(IButton button)
        {
            bool enabled = button.Enabled;
            try { ActivateCore(button); }
            catch (Exception error) when (!(error is UnityEngine.ExitGUIException))
            {
                button.Enabled = enabled;
                UnityEngine.Debug.LogWarning("Umbra: action failed: " + button.Text + ": " + error);
                Loader.Report("degraded", "Could not complete " + button.Text + ". See the game log.");
            }
        }

        private static void ActivateCore(IButton button)
        {
            if (button.ParentMenu?.GetId() == 16)
            {
                bool wasCapturing = button.Enabled && UmbraMenu.listenForKeybind;
                MenuUI.CancelKeybindCapture();
                if (wasCapturing) return;
            }
            bool toggle = button is TogglableButton || button is TogglableMulButton;
            if (toggle && string.Equals(button.OnText, "CONFIRM?", StringComparison.OrdinalIgnoreCase))
            {
                if (!button.Enabled) { button.Enabled = true; return; }
                button.Action?.Invoke();
                button.Enabled = false;
                return;
            }
            if (toggle) button.Enabled = !button.Enabled;
            button.Action?.Invoke();
        }

        public void SetText(string newText)
        {
            _button.Text = newText;
        }

        public void SetOnText(string newText)
        {
            _button.OnText = newText;
        }

        public void SetOffText(string newText)
        {
            _button.OffText = newText;
        }

        public string GetOnText()
        {
            return _button.OnText;
        }

        public string GetOffText()
        {
            return _button.OffText;
        }

        public bool IsEnabled()
        {
            return _button.Enabled;
        }

        public void SetEnabled(bool value)
        {
            _button.Enabled = value;
        }

        public void ToggleButton()
        {
            _button.Enabled = !_button.Enabled;
        }

        public int GetId()
        {
            return _button.Position;
        }

        public Menu GetParentMenu()
        {
            return _button.ParentMenu;
        }

        public Action GetAction()
        {
            return _button.Action;
        }

        public Action GetIncreaseAction()
        {
            return _button.IncreaseAction;
        }

        public Action GetDecreaseAction()
        {
            return _button.DecreaseAction;
        }
    }
}
