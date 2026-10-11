using System;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;

namespace UmbraSuite.Injector
{
    internal static class InjectionService
    {
        public static async Task<string> InjectAsync(GameProcess game, Bundle bundle, Action<string> progress, string executable)
        {
            await Task.Run(() => { game.VerifyIdentity(); bundle.VerifyGame(game.Executable); }).ConfigureAwait(false);
            using (var session = new InjectionSession(game, bundle))
            {
                progress("Connecting to the game…");
                var start = new ProcessStartInfo(executable, "--worker " + game.Id.ToString(CultureInfo.InvariantCulture) + " " + game.StartTicks.ToString(CultureInfo.InvariantCulture))
                {
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Path.GetDirectoryName(executable)
                };
                Process worker;
                try { worker = Process.Start(start); }
                catch { session.AbandonBeforeRemoteExecution(); throw; }
                using (worker)
                {
                    var outputTask = worker.StandardOutput.ReadToEndAsync();
                    var errorTask = worker.StandardError.ReadToEndAsync();
                    // No process is killed on timeout. A native operation may still be running in the game.
                    var finished = await Task.Run(() => worker.WaitForExit(45000)).ConfigureAwait(false);
                    if (!finished) return "The helper is taking longer than expected. Do not inject again. Wait for the menu, or restart the game.";
                    string output = await outputTask.ConfigureAwait(false);
                    string error = await errorTask.ConfigureAwait(false);
                    if (worker.ExitCode == 10)
                    {
                        session.AbandonBeforeRemoteExecution();
                        throw new InvalidOperationException(Limit(error.Length > 0 ? error : output));
                    }
                    if (worker.ExitCode != 0) throw new InvalidOperationException(Limit(error.Length > 0 ? error : output) + " Restart the game before another attempt.");
                }
                progress("Injection submitted. Waiting for the menu to confirm startup…");
                var deadline = DateTime.UtcNow.AddSeconds(25);
                do
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    game.VerifyIdentity();
                    string state = InjectionSession.State(game, bundle, out string detail);
                    if (state == "ready" || state == "degraded" || state == "blocked") return detail;
                } while (DateTime.UtcNow < deadline);
                return "The menu is waiting for the game to finish loading. Keep the launcher open; its status will update automatically.";
            }
        }
        private static string Limit(string value) => value.Length > 4000 ? value.Substring(0, 4000) : value;
    }
}
