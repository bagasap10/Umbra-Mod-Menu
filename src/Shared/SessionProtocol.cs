using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace UmbraSuite
{
    // Small, atomic files bridge the launcher and Mono without a socket, background game thread or dependency.
    public static class SessionProtocol
    {
        public const int MaximumDocumentBytes = 16 * 1024;
        public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UmbraSuite", "Sessions");
        public static string Identity(int processId, long startTicks)
        {
            if (processId <= 0 || startTicks <= 0) throw new ArgumentOutOfRangeException(nameof(processId));
            return processId + "-" + startTicks;
        }
        public static string StatusPath(int pid, long ticks, string directory = null) => Path.Combine(directory ?? Root, Identity(pid, ticks) + ".status");
        public static string RequestPath(int pid, long ticks, string directory = null) => Path.Combine(directory ?? Root, Identity(pid, ticks) + ".request");
        public static Dictionary<string, string> Read(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!File.Exists(path)) return result;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                if (stream.Length > MaximumDocumentBytes) throw new InvalidDataException("Session document is too large.");
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        int equals = line.IndexOf('=');
                        if (equals <= 0 || result.Count >= 32) throw new InvalidDataException("Invalid session document.");
                        string key = line.Substring(0, equals);
                        if (result.ContainsKey(key)) throw new InvalidDataException("Duplicate session field.");
                        result.Add(key, line.Substring(equals + 1));
                    }
                }
            }
            return result;
        }
        public static string Value(Dictionary<string, string> values, string name) => values.TryGetValue(name, out string value) ? value : "";
        public static string Encode(string state, string module, string message, string shortcut)
        {
            if (!Guid.TryParse(module, out _)) throw new InvalidDataException("Invalid menu module identity.");
            return "protocol=1\nstate=" + Clean(state, 32) + "\nmodule=" + module + "\nmessage=" + Clean(message, 1000)
                + "\nshortcut=" + Clean(shortcut, 100) + "\nutc=" + DateTime.UtcNow.ToString("O") + "\n";
        }
        public static void WriteStatus(string state, string module, string message = "", string shortcut = "")
        {
            using (Process process = Process.GetCurrentProcess())
                WriteAtomic(StatusPath(process.Id, process.StartTime.ToUniversalTime().Ticks), Encode(state, module, message, shortcut));
        }
        public static void WriteAtomic(string path, string document)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(document);
            if (bytes.Length > MaximumDocumentBytes) throw new InvalidDataException("Session document is too large.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private static string Clean(string value, int limit)
        {
            value = (value ?? "").Replace('\r', ' ').Replace('\n', ' ').Replace('\0', ' ');
            return value.Length > limit ? value.Substring(0, limit) : value;
        }
    }
}
