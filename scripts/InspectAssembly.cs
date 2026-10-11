using System;
using System.IO;
using System.Reflection;

internal static class InspectAssembly
{
    private static int Main(string[] args)
    {
        if (args.Length != 1) return 1;
        Console.WriteLine(Assembly.ReflectionOnlyLoad(File.ReadAllBytes(args[0])).ManifestModule.ModuleVersionId.ToString("D"));
        return 0;
    }
}
