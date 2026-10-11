using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace UmbraSuite.Injector
{
    internal sealed class InjectionSession : IDisposable
    {
        private FileStream lease;
        private readonly string requestPath;
        private static bool ValidStatus(Dictionary<string, string> status)
        {
            if (status.Count == 0) return true;
            string phase = SessionProtocol.Value(status, "state");
            return SessionProtocol.Value(status, "protocol") == "1" && Guid.TryParse(SessionProtocol.Value(status, "module"), out _)
                && (phase == "ready" || phase == "degraded" || phase == "waiting" || phase == "error" || phase == "unloaded");
        }
        public static string State(GameProcess game, Bundle bundle, out string detail, string directory = null)
        {
            var status = SessionProtocol.Read(SessionProtocol.StatusPath(game.Id, game.StartTicks, directory));
            string module = SessionProtocol.Value(status, "module");
            string phase = SessionProtocol.Value(status, "state");
            detail = SessionProtocol.Value(status, "message");
            if (!ValidStatus(status))
            { detail = "The menu status is invalid. Restart the game before another attempt."; return "blocked"; }
            if (module.Length > 0 && module != bundle.Module) { detail = "A different Umbra build is loaded. Restart the game to use this build."; return "blocked"; }
            if (phase == "ready" || phase == "degraded")
            {
                string shortcut = SessionProtocol.Value(status, "shortcut");
                if (phase == "ready") detail = "Menu active. " + (shortcut.Length > 0 ? shortcut : "Insert / Num 0") + " opens it; Num 1 goes Back.";
                return phase;
            }
            if (phase == "unloaded") return "unloaded";
            if (phase == "error") { detail += " Restart the game before trying again."; return "blocked"; }
            string request = SessionProtocol.RequestPath(game.Id, game.StartTicks, directory);
            if (File.Exists(request) && new FileInfo(request).Length > 0)
            {
                if (phase == "waiting") return "waiting";
                detail = "An injection attempt is pending or its result is uncertain. Wait for the menu, or restart the game before another attempt.";
                return "pending";
            }
            return "available";
        }
        public InjectionSession(GameProcess game, Bundle bundle, string directory = null)
        {
            requestPath = SessionProtocol.RequestPath(game.Id, game.StartTicks, directory);
            Directory.CreateDirectory(directory ?? SessionProtocol.Root);
            // A writable lease excludes every other launcher while this attempt is in progress.
            lease = new FileStream(requestPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            try
            {
                bool previousAttempt = lease.Length != 0;
                var status = SessionProtocol.Read(SessionProtocol.StatusPath(game.Id, game.StartTicks, directory));
                if (!ValidStatus(status))
                    throw new InvalidDataException("The previous session status is invalid. Restart the game.");
                bool cleanUnload = SessionProtocol.Value(status, "state") == "unloaded" && SessionProtocol.Value(status, "module") == bundle.Module;
                string phase = SessionProtocol.Value(status, "state");
                string module = SessionProtocol.Value(status, "module");
                if (module.Length > 0 && module != bundle.Module) throw new InvalidOperationException("A different menu build is loaded. Restart the game.");
                if (phase == "ready" || phase == "degraded" || phase == "error" || phase == "waiting")
                    throw new InvalidOperationException("This game session already has a menu or a pending attempt.");
                if (previousAttempt && !cleanUnload) throw new InvalidOperationException("An attempt already exists for this game session. Restart the game before trying again.");
                if (cleanUnload) File.Delete(SessionProtocol.StatusPath(game.Id, game.StartTicks, directory));
                lease.SetLength(0);
                byte[] bytes = Encoding.UTF8.GetBytes(SessionProtocol.Encode("pending", bundle.Module, "Injection requested.", ""));
                lease.Write(bytes, 0, bytes.Length); lease.Flush(true);
            }
            catch { lease.Dispose(); lease = null; throw; }
        }
        public void AbandonBeforeRemoteExecution()
        {
            Dispose();
            if (File.Exists(requestPath)) File.Delete(requestPath);
        }
        public void Dispose() { lease?.Dispose(); lease = null; }
    }
}
