using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using UmbraSuite.Injector;

internal static class LauncherPreview
{
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
            using (var bitmap = new Bitmap(content.Width, content.Height))
            { content.DrawToBitmap(bitmap, new Rectangle(Point.Empty, content.Size)); bitmap.Save(Path.GetFullPath("artifacts/tests/previews/launcher.png")); }
            form.ClientSize = new Size(634, 611); form.PerformLayout();
            content.Size = form.ClientSize; content.PerformLayout();
            using (var bitmap = new Bitmap(content.Width, content.Height))
            { content.DrawToBitmap(bitmap, new Rectangle(Point.Empty, content.Size)); bitmap.Save(Path.GetFullPath("artifacts/tests/previews/launcher-minimum.png")); }
            form.Controls.Add(content);
        }
        Console.WriteLine("PASS: launcher layouts rendered off screen; no injection performed."); return 0;
    }
}
