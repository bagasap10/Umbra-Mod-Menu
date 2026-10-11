using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace UmbraSuite.Injector
{
    internal sealed class GameProcess
    {
        public int Id;
        public long StartTicks;
        public string Executable, Error;
        public bool MonoReady;
        public string Identity => SessionProtocol.Identity(Id, StartTicks);
        public override string ToString() => "Risk of Rain 2  /  PID " + Id;
        public static GameProcess Inspect(int id)
        {
            using (var process = Process.GetProcessById(id))
            {
                if (!string.Equals(process.ProcessName, "Risk of Rain 2", StringComparison.OrdinalIgnoreCase) || process.HasExited)
                    throw new InvalidOperationException("The selected game process is no longer running.");
                var result = new GameProcess { Id = id, StartTicks = process.StartTime.ToUniversalTime().Ticks };
                try
                {
                    result.Executable = process.MainModule.FileName;
                    if (!string.Equals(Path.GetFileName(result.Executable), "Risk of Rain 2.exe", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("The game executable could not be verified.");
                    foreach (ProcessModule module in process.Modules)
                        if (module.ModuleName.StartsWith("mono", StringComparison.OrdinalIgnoreCase)) { result.MonoReady = true; break; }
                }
                catch (Exception error) { result.Error = "Cannot inspect the game: " + error.Message + " If the game is running as administrator, run the launcher at the same privilege level."; }
                return result;
            }
        }
        public static List<GameProcess> Discover()
        {
            var games = new List<GameProcess>();
            foreach (var process in Process.GetProcessesByName("Risk of Rain 2"))
            {
                using (process)
                    try { games.Add(Inspect(process.Id)); }
                    catch (ArgumentException) { }
                    catch (InvalidOperationException) { }
                    catch (System.ComponentModel.Win32Exception) { }
            }
            return games;
        }
        public void VerifyIdentity()
        {
            var current = Inspect(Id);
            if (current.StartTicks != StartTicks || current.Identity != Identity) throw new InvalidOperationException("The game restarted. Select the new process.");
            if (current.Error != null) throw new InvalidOperationException(current.Error);
            if (!current.MonoReady) throw new InvalidOperationException("Wait for the game to finish starting, then try again.");
        }
    }
}
