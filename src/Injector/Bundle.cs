using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;

namespace UmbraSuite.Injector
{
    internal sealed class Bundle
    {
        public string MenuPath, Module, Version, GameHash;
        public static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }
        public static Bundle Load(string directory)
        {
            var document = new XmlDocument { XmlResolver = null };
            using (var reader = XmlReader.Create(Path.Combine(directory, "bundle.xml"), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 16384 })) document.Load(reader);
            var root = document.DocumentElement;
            if (root == null || root.Name != "umbra" || root.GetAttribute("protocol") != "1") throw new InvalidDataException("Unrecognized bundle manifest.");
            string module = root.GetAttribute("module");
            if (!Guid.TryParse(module, out _)) throw new InvalidDataException("The bundle has an invalid menu identity.");
            var found = new HashSet<string>(StringComparer.Ordinal);
            foreach (XmlNode node in root.SelectNodes("file"))
            {
                string name = node.Attributes["name"]?.Value;
                if (name != "UmbraMenu.dll" && name != "SharpMonoInjector.dll") throw new InvalidDataException("Unrecognized bundle dependency.");
                if (!found.Add(name)) throw new InvalidDataException("Duplicate bundle dependency.");
                string path = Path.Combine(directory, name);
                var file = new FileInfo(path);
                if (!file.Exists || file.Length <= 0 || file.Length > 32 * 1024 * 1024) throw new InvalidDataException("Missing or oversized " + name + ". Extract the complete release ZIP.");
                if (!string.Equals(Hash(path), node.Attributes["sha256"]?.Value, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(name + " does not match this release. Extract a fresh complete ZIP.");
            }
            if (found.Count != 2) throw new InvalidDataException("The bundle does not list all dependencies.");
            string menuPath = Path.Combine(directory, "UmbraMenu.dll");
            if (AssemblyName.GetAssemblyName(menuPath).Name != "UmbraMenu")
                throw new InvalidDataException("The menu assembly identity is incorrect.");
            string gameHash = root.GetAttribute("gameSha256");
            if (gameHash.Length != 64 || !IsHex(gameHash)) throw new InvalidDataException("The bundle lacks a valid game compatibility fingerprint.");
            return new Bundle { MenuPath = menuPath, Module = module, Version = root.GetAttribute("version"), GameHash = gameHash };
        }
        private static bool IsHex(string value)
        {
            foreach(char character in value) if (!Uri.IsHexDigit(character)) return false;
            return true;
        }
        public void VerifyGame(string gameExecutable)
        {
            string managed = Path.Combine(Path.GetDirectoryName(gameExecutable), "Risk of Rain 2_Data", "Managed");
            string gameAssembly = Path.Combine(managed, "RoR2.dll");
            if (!File.Exists(gameAssembly) || !string.Equals(Hash(gameAssembly), GameHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("This menu was built for a different game update. Rebuild the suite for your installed game, or use a matching release.");
        }
    }
}
