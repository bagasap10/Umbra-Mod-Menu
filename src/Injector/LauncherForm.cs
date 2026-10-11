using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UmbraSuite.Injector
{
    internal sealed class LauncherForm : Form
    {
        private static readonly Color Background = Color.FromArgb(14, 22, 30), Surface = Color.FromArgb(23, 35, 46),
            Foreground = Color.FromArgb(230, 239, 243), Muted = Color.FromArgb(153, 174, 186), Accent = Color.FromArgb(100, 218, 186);
        private readonly ComboBox games = new ComboBox();
        private readonly Label gameStatus = new Label(), packageStatus = new Label(), feedback = new Label(), heading = new Label();
        private readonly Button inject = new Button(), launch = new Button(), refresh = new Button(), logs = new Button();
        private readonly ProgressBar progressBar = new ProgressBar();
        private readonly Timer timer = new Timer { Interval = 2000 };
        private readonly List<Font> fonts = new List<Font>();
        private Bundle bundle;
        private bool refreshing, busy;
        private string packageError, compatibleIdentity;
        private string lastMessage;
        private string lastAttemptIdentity, lastAttemptError;
        private readonly string directory = AppDomain.CurrentDomain.BaseDirectory;

        public LauncherForm()
        {
            Text = "Umbra Injector"; StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(740, 640); MinimumSize = new Size(650, 650);
            BackColor = Background; ForeColor = Foreground; Font = NewFont(10);
            AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
            inject.Enabled = false;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 7 };
            foreach (float height in new[] { 76f, 112, 86, 62, 128, 60 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(layout);

            var header = new Panel { Dock = DockStyle.Fill };
            var title = Label("UMBRA", 26, Accent); title.SetBounds(0, 0, 400, 40);
            var subtitle = Label("Risk of Rain 2  /  Hallowed Concept", 10, Muted); subtitle.SetBounds(2, 45, 650, 25);
            header.Controls.Add(title); header.Controls.Add(subtitle); layout.Controls.Add(header, 0, 0);

            var gameCard = Card(); heading.Text = "1  Connect to your game"; heading.Font = NewFont(12); heading.SetBounds(18, 12, 610, 28); gameCard.Controls.Add(heading);
            games.DropDownStyle = ComboBoxStyle.DropDownList; games.FlatStyle = FlatStyle.Flat; games.BackColor = Surface; games.ForeColor = Foreground;
            games.SetBounds(18, 44, 400, 30);
            gameStatus.Text = "Waiting for Risk of Rain 2…";
            gameStatus.ForeColor = Muted; gameStatus.SetBounds(18, 78, 610, 25);
            gameCard.Resize += (sender, args) => { games.Width = gameStatus.Width = heading.Width = Math.Max(100, gameCard.ClientSize.Width - 36); };
            gameCard.Controls.Add(games); gameCard.Controls.Add(gameStatus); layout.Controls.Add(gameCard, 0, 1);

            var packageCard = Card(); var packageTitle = Label("2  Menu package", 12, Foreground); packageTitle.SetBounds(18, 12, 600, 25);
            packageStatus.Text = "Checking release files…";
            packageStatus.ForeColor = Muted; packageStatus.SetBounds(18, 43, 630, 36);
            packageCard.Resize += (sender, args) => { packageStatus.Width = packageTitle.Width = Math.Max(100, packageCard.ClientSize.Width - 36); };
            packageCard.Controls.Add(packageTitle); packageCard.Controls.Add(packageStatus); layout.Controls.Add(packageCard, 0, 2);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0), WrapContents = false };
            StyleButton(inject, "Inject menu", true, 210); StyleButton(launch, "Start game", false, 145); StyleButton(refresh, "Refresh", false, 110);
            actions.Controls.AddRange(new Control[] { inject, launch, refresh }); layout.Controls.Add(actions, 0, 3);

            var feedbackCard = Card(); feedback.SetBounds(18, 15, 640, 78);
            feedback.Text = "Start Risk of Rain 2. When it is ready, click Inject menu."; feedback.ForeColor = Foreground;
            progressBar.SetBounds(18, 102, 630, 5);
            feedbackCard.Resize += (sender, args) => { feedback.Width = progressBar.Width = Math.Max(100, feedbackCard.ClientSize.Width - 36); };
            progressBar.Style = ProgressBarStyle.Marquee; progressBar.Visible = false;
            feedbackCard.Controls.Add(feedback); feedbackCard.Controls.Add(progressBar); layout.Controls.Add(feedbackCard, 0, 4);

            var help = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
            StyleButton(logs, "Open logs", false, 120); help.Controls.Add(logs);
            var shortcuts = Label("Insert / Num 0  Open menu\nNum 1  Back", 10, Muted); shortcuts.AutoSize = true; shortcuts.Padding = new Padding(18, 0, 0, 0); help.Controls.Add(shortcuts);
            layout.Controls.Add(help, 0, 5);
            var footer = Label("Keep this release's files together. Restart the game when changing menu builds.", 9, Muted); footer.Dock = DockStyle.Fill; layout.Controls.Add(footer, 0, 6);

            games.SelectedIndexChanged += async (sender, args) => { if (!refreshing && !busy) await UpdateSelectedAsync(); };
            inject.Click += async (sender, args) => await InjectAsync();
            refresh.Click += async (sender, args) => { bundle = null; compatibleIdentity = null; lastAttemptError = null; await RefreshAsync(); };
            launch.Click += (sender, args) => Open("steam://rungameid/632360");
            logs.Click += (sender, args) => { Directory.CreateDirectory(Diagnostics.DirectoryPath); Open(Diagnostics.DirectoryPath); };
            timer.Tick += async (sender, args) => await RefreshAsync();
            Shown += async (sender, args) => { await RefreshAsync(); timer.Start(); };
        }

        private Font NewFont(float size) { var font = new Font("Segoe UI", size); fonts.Add(font); return font; }
        private Label Label(string text, float size, Color color) => new Label { Text = text, Font = NewFont(size), ForeColor = color, BackColor = Color.Transparent };
        private static Panel Card() => new Panel { Dock = DockStyle.Fill, BackColor = Surface, Margin = new Padding(0, 0, 0, 12) };
        private void StyleButton(Button button, string text, bool primary, int width)
        {
            button.Text = text; button.Size = new Size(width, 40); button.Margin = new Padding(0, 0, 10, 0); button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0; button.BackColor = primary ? Accent : Surface; button.ForeColor = primary ? Background : Foreground;
            button.Cursor = Cursors.Hand; button.Font = NewFont(10);
        }
        private void Open(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception error) { ShowMessage(error.Message); }
        }
        private void ShowMessage(string message)
        {
            if (IsDisposed || Disposing) return;
            message = message ?? "Package unavailable. Extract the complete release ZIP.";
            feedback.Text = message.Length > 800 ? message.Substring(0, 800) : message;
            if (message != lastMessage) { Diagnostics.Write(message); lastMessage = message; }
        }
        private void PostMessage(string message)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke((Action)(() => ShowMessage(message))); }
            catch (InvalidOperationException) { }
        }
        private async Task RefreshAsync()
        {
            if (refreshing || busy || IsDisposed) return;
            refreshing = true;
            string previous = (games.SelectedItem as GameProcess)?.Identity;
            try
            {
                if (bundle == null)
                {
                    try { bundle = await Task.Run(() => Bundle.Load(directory)); packageError = null; }
                    catch (Exception error) { packageError = error.Message; }
                }
                var detected = await Task.Run(GameProcess.Discover);
                if (IsDisposed) return;
                games.BeginUpdate(); games.Items.Clear();
                foreach (var game in detected) games.Items.Add(game);
                int selected = detected.FindIndex(game => game.Identity == previous);
                games.SelectedIndex = selected >= 0 ? selected : detected.Count == 1 ? 0 : -1;
                games.EndUpdate();
                await UpdateSelectedAsync();
            }
            catch (Exception error) { ShowMessage(error.Message); inject.Enabled = false; }
            finally { refreshing = false; }
        }
        private async Task UpdateSelectedAsync()
        {
            inject.Enabled = false; inject.Text = "Inject menu";
            packageStatus.Text = bundle == null ? packageError : "Hallowed Concepts  /  Verified release " + bundle.Version;
            var game = games.SelectedItem as GameProcess;
            launch.Enabled = !busy && games.Items.Count == 0;
            if (game == null)
            {
                gameStatus.Text = games.Items.Count == 0 ? "Waiting for Risk of Rain 2…" : "Choose the game process above.";
                ShowMessage(bundle == null ? packageError : games.Items.Count == 0 ? "Start the game, then click Inject menu when it is ready." : "More than one game is running. Choose the process to use."); return;
            }
            if (game.Error != null) { gameStatus.Text = "Game found; access unavailable."; ShowMessage(game.Error); return; }
            gameStatus.Text = game.MonoReady ? "Game detected. Mono runtime is ready." : "Game is starting…";
            if (bundle == null) { ShowMessage(packageError); return; }
            if (!game.MonoReady) { ShowMessage("Wait for the game's main menu to appear."); return; }
            if (compatibleIdentity != game.Identity + bundle.Module)
            {
                try
                {
                    var selectedBundle = bundle;
                    await Task.Run(() => selectedBundle.VerifyGame(game.Executable));
                    if (IsDisposed || bundle != selectedBundle || (games.SelectedItem as GameProcess)?.Identity != game.Identity || busy) return;
                    compatibleIdentity = game.Identity + bundle.Module;
                }
                catch (Exception error) { ShowMessage(error.Message); return; }
            }
            if (IsDisposed || (games.SelectedItem as GameProcess)?.Identity != game.Identity || busy) return;
            string state = InjectionSession.State(game, bundle, out string detail);
            inject.Enabled = !busy && (state == "available" || state == "unloaded");
            inject.Text = state == "ready" || state == "degraded" ? "Menu active" : state == "waiting" ? "Waiting for menu…" : "Inject menu";
            ShowMessage(state == "available" ? lastAttemptIdentity == game.Identity && lastAttemptError != null
                ? lastAttemptError + "\nThe attempt stopped before remote execution. You can try again."
                : "Ready. Click Inject menu; the launcher will confirm when the menu has started."
                : state == "unloaded" ? "Menu unloaded. You can inject this same build again." : detail);
        }
        private async Task InjectAsync()
        {
            if (busy || !inject.Enabled || bundle == null || !(games.SelectedItem is GameProcess game)) return;
            busy = true; inject.Enabled = launch.Enabled = refresh.Enabled = games.Enabled = false; progressBar.Visible = true; inject.Text = "Injecting…";
            lastAttemptError = null;
            string resultMessage = null;
            try
            {
                ShowMessage("Checking the game and menu package…");
                resultMessage = await InjectionService.InjectAsync(game, bundle, PostMessage, Application.ExecutablePath);
                ShowMessage(resultMessage);
            }
            catch (Exception error)
            {
                resultMessage = error.GetBaseException().Message; lastAttemptIdentity = game.Identity; lastAttemptError = resultMessage;
                ShowMessage(resultMessage);
            }
            finally
            {
                busy = false;
                if (!IsDisposed)
                {
                    refresh.Enabled = games.Enabled = true; progressBar.Visible = false;
                    await RefreshAsync();
                    if (resultMessage != null) ShowMessage(resultMessage);
                }
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
            if (disposing) foreach (var font in fonts) font.Dispose();
        }
    }
}
