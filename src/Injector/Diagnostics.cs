using System;
using System.IO;

namespace UmbraSuite.Injector
{
    internal static class Diagnostics
    {
        public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UmbraSuite", "Logs");
        private static readonly object gate = new object();
        public static void Write(string text)
        {
            try
            {
                lock (gate)
                {
                    Directory.CreateDirectory(DirectoryPath);
                    string path = Path.Combine(DirectoryPath, "launcher.log");
                    if (File.Exists(path) && new FileInfo(path).Length > 1024 * 1024)
                    {
                        string old = path + ".old";
                        if (File.Exists(old)) File.Delete(old);
                        File.Move(path, old);
                    }
                    File.AppendAllText(path, DateTime.UtcNow.ToString("O") + " " + (text.Length > 8000 ? text.Substring(0, 8000) : text) + Environment.NewLine);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
