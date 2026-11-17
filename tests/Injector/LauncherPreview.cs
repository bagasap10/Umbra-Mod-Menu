using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UmbraSuite.Injector;

internal static class LauncherPreview
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static FieldInfo Field(string name) => typeof(LauncherForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static T Get<T>(LauncherForm form, string name) => (T)Field(name).GetValue(form);
    private static void UpdateSelection(LauncherForm form)
    {
        var update = (Task)typeof(LauncherForm).GetMethod("UpdateSelectedAsync", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
        var elapsed = Stopwatch.StartNew();
        while (!update.IsCompleted)
        {
            if (elapsed.Elapsed.TotalSeconds > 10) throw new TimeoutException("Launcher compatibility checks did not complete.");
            Application.DoEvents(); Thread.Sleep(1);
        }
        update.GetAwaiter().GetResult();
    }
    private static void Render(Control content, string name)
    {
        content.PerformLayout();
        using (var bitmap = new Bitmap(content.Width, content.Height))
        { content.DrawToBitmap(bitmap, new Rectangle(Point.Empty, content.Size)); bitmap.Save(Path.GetFullPath("artifacts/tests/previews/" + name + ".png")); }
    }
    private static void Compatibility(LauncherForm form, Control content)
    {
        string gameDir = Path.GetFullPath("artifacts/tests/launcher-game");
        string managed = Path.Combine(gameDir, "Risk of Rain 2_Data", "Managed"); Directory.CreateDirectory(managed);
        string assembly = Path.Combine(managed, "RoR2.dll"); File.WriteAllText(assembly, "launcher compatibility fixture");
        var bundle = new Bundle { Version = "4.1.2", Module = Guid.NewGuid().ToString("D"), GameHash = Bundle.Hash(assembly) };
        var game = new GameProcess { Id = 70002, StartTicks = DateTime.UtcNow.Ticks, Executable = Path.Combine(gameDir, "Risk of Rain 2.exe"), MonoReady = true };
        var games = Get<ComboBox>(form, "games"); var inject = Get<Button>(form, "inject"); var package = Get<Label>(form, "packageStatus");
        Field("refreshing").SetValue(form, true); Field("bundle").SetValue(form, bundle);
        games.Items.Add(game); games.SelectedIndex = 0; Field("refreshing").SetValue(form, false);
        UpdateSelection(form);
        Check(inject.Enabled && package.Text.Contains("Verified release"), "A matching game must enable injection with a verified package status.");
        Color normalColor = package.ForeColor;
        File.AppendAllText(assembly, "updated"); Field("checkedGameIdentity").SetValue(form, null); UpdateSelection(form);
        Check(inject.Enabled && inject.Text == "Inject menu", "A game-version mismatch must leave the Inject menu button enabled.");
        Check(package.Text.StartsWith("Warning:") && package.Text.Contains("still inject") && package.ForeColor != normalColor, "The package card must visibly warn while injection remains available.");
        string warning = package.Text; UpdateSelection(form);
        Check(inject.Enabled && package.Text == warning, "Routine status refreshes must retain the warning and allow injection.");
        content.Size = new Size(740, 640); Render(content, "launcher-version-warning");
        content.Size = new Size(634, 611); Render(content, "launcher-version-warning-minimum");
        Check(package.Bounds.Bottom <= package.Parent.ClientSize.Height, "The compatibility warning must fit inside its package card at the minimum window size.");
        Check(TextRenderer.MeasureText(package.Text, package.Font, new Size(package.Width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height <= package.Height,
            "The full warning must remain readable at the minimum window size.");
        bundle.GameHash = Bundle.Hash(assembly); Field("checkedGameIdentity").SetValue(form, null); UpdateSelection(form);
        Check(inject.Enabled && package.Text.Contains("Verified release") && package.ForeColor == normalColor, "A matching package must clear the previous version warning.");
        game.MonoReady = false; UpdateSelection(form);
        Check(!inject.Enabled, "A version warning must not bypass waiting for the game's Mono runtime.");
        game.MonoReady = true; File.Delete(assembly); Field("checkedGameIdentity").SetValue(form, null); UpdateSelection(form);
        Check(!inject.Enabled && Get<Label>(form, "feedback").Text.Contains("missing RoR2.dll"), "A missing game assembly must disable injection with a clear installation error.");
        Field("bundle").SetValue(form, null); Field("packageError").SetValue(form, "Release file does not match this package."); UpdateSelection(form);
        Check(!inject.Enabled && Get<Label>(form, "feedback").Text.Contains("does not match"), "Invalid package files must continue to block injection.");
    }
    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        using (var form = new LauncherForm())
        {
            // Render the real control tree without showing a window or firing Shown/refresh handlers.
            Control content = form.Controls[0]; form.Controls.Remove(content); content.Dock = DockStyle.None;
            content.BackColor = form.BackColor; content.ForeColor = form.ForeColor; content.Font = form.Font;
            content.Size = form.ClientSize; content.CreateControl(); content.PerformLayout();
            Render(content, "launcher");
            form.ClientSize = new Size(634, 611); form.PerformLayout();
            content.Size = form.ClientSize; content.PerformLayout();
            Render(content, "launcher-minimum");
            Compatibility(form, content);
            form.Controls.Add(content);
        }
        Console.WriteLine("PASS: " + checks + " launcher compatibility assertions; normal and warning layouts rendered off screen; no injection performed."); return 0;
    }
}
