using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using MonoInjector = SharpMonoInjector.Injector;

namespace UmbraSuite.Injector
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--worker") return Worker(args);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, error) => MessageBox.Show(error.Exception.Message, "Umbra Injector", MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppDomain.CurrentDomain.UnhandledException += (sender, error) => Diagnostics.Write(error.ExceptionObject.ToString());
            Application.Run(new LauncherForm());
            return 0;
        }
        private static int Worker(string[] args)
        {
            MonoInjector backend = null;
            bool started = false;
            try
            {
                if (args.Length != 3 || !int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out int pid)
                    || !long.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks)) throw new ArgumentException("Invalid helper arguments.");
                var game = GameProcess.Inspect(pid);
                if (game.StartTicks != ticks) throw new InvalidOperationException("The game restarted during injection.");
                game.VerifyIdentity();
                var bundle = Bundle.Load(AppDomain.CurrentDomain.BaseDirectory);
                bundle.VerifyGame(game.Executable);
                backend = new MonoInjector(pid);
                if (!backend.Is64Bit) throw new InvalidOperationException("This release requires the 64-bit game.");
                byte[] assembly = File.ReadAllBytes(bundle.MenuPath);
                started = true;
                backend.Inject(assembly, "UmbraMenu", "Loader", "Load");
                Console.WriteLine("Menu startup queued.");
                return 0;
            }
            catch (Exception error)
            {
                string message = error.GetBaseException().Message;
                Console.Error.WriteLine(message.Length > 2000 ? message.Substring(0, 2000) : message);
                return started ? 20 : 10;
            }
            finally { backend?.Dispose(); }
        }
    }
}
