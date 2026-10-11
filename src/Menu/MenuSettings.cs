using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UmbraSuite;

namespace UmbraMenu
{
    internal static class MenuSettings
    {
        private static string DefaultKey(string name)
        {
            switch (name)
            {
                case "MAIN MENU": return "Insert";
                case "PLAYER MENU": return "Z";
                case "FLIGHT": return "C";
                case "ITEMS MENU": return "I";
                case "TELEPORTER MENU": return "B";
                default: return "None";
            }
        }
        private static Dictionary<string, string> ReadDocument(string path)
        {
            var document = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!File.Exists(path)) return document;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                if (stream.Length > 64 * 1024) throw new InvalidDataException("Settings exceed the 64 KB limit.");
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        int colon = line.IndexOf(':');
                        if (colon < 1 || line.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
                        string value = line.Substring(colon + 1).Split(':')[0].Trim();
                        document[line.Substring(0, colon).Trim()] = value;
                        if (document.Count > 256) throw new InvalidDataException("Too many settings entries.");
                    }
                }
            }
            return document;
        }
        internal static List<string> Read(string path, IList<string> names)
        {
            Dictionary<string, string> document;
            try { document = ReadDocument(path); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is InvalidDataException)
            { Debug.LogWarning("Umbra: settings could not be read; using defaults: " + error.Message); document = new Dictionary<string, string>(); }
            float width = Number(document, "Width", 350, 100, 2000);
            bool navigation = !document.TryGetValue("Allow Navigation", out string text) || !bool.TryParse(text, out bool parsed) || parsed;
            int god = document.TryGetValue("God Version", out text) && int.TryParse(text, out int value) ? Math.Max(0, Math.Min(4, value)) : 0;
            var result = new List<string> { width.ToString(CultureInfo.InvariantCulture), navigation.ToString(), god.ToString(CultureInfo.InvariantCulture) };
            foreach (string name in names)
            {
                string key = document.TryGetValue(name, out text) && Enum.TryParse(text, true, out KeyCode code) && Enum.IsDefined(typeof(KeyCode), code)
                    ? code.ToString() : DefaultKey(name);
                result.Add(key);
            }
            return result;
        }
        private static float Number(Dictionary<string, string> document, string name, float fallback, float minimum, float maximum)
        {
            return document.TryGetValue(name, out string text) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                && !float.IsNaN(value) && !float.IsInfinity(value) ? Math.Max(minimum, Math.Min(maximum, value)) : fallback;
        }
        internal static float ReadNumber(string path, string name, float fallback)
        {
            try { return Number(ReadDocument(path), name, fallback, float.MinValue, float.MaxValue); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is InvalidDataException) { return fallback; }
        }
        internal static void Save(string path, float width, bool navigation, int god, IList<string> names,
            Func<string, string> key, float windowWidth, float windowHeight, float opacity)
        {
            var text = new StringBuilder("# Umbra Suite settings\n");
            text.AppendLine("Width: " + width.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("Allow Navigation: " + navigation);
            text.AppendLine("God Version: " + god.ToString(CultureInfo.InvariantCulture));
            foreach (string name in names) text.AppendLine(name + ": " + key(name));
            text.AppendLine("Window Width: " + windowWidth.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("Window Height: " + windowHeight.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("Glass Opacity: " + opacity.ToString(CultureInfo.InvariantCulture));
            SessionProtocol.WriteAtomic(path, text.ToString());
        }
        internal static void CreateDefaults(string path, IList<string> names) => Save(path, 350, true, 0, names, DefaultKey, 680, 560, .62f);
    }
}
