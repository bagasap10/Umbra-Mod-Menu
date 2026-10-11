using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UmbraSuite;
using UmbraSuite.Injector;
using SharpMonoInjector;

internal static class InjectorChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static void Throws<T>(Action action, string message) where T : Exception
    {
        bool threw = false; try { action(); } catch (T) { threw = true; } Check(threw, message);
    }
    private static readonly string Root = Path.GetFullPath("artifacts/tests/injector");
    private static string PathFor(string name) => Path.Combine(Root, name);
    private static void Protocol()
    {
        string path = PathFor("protocol.fixture"); string module = Guid.NewGuid().ToString("D");
        SessionProtocol.WriteAtomic(path, SessionProtocol.Encode("ready", module, "line1\r\nline2\0end", "F1"));
        var data = SessionProtocol.Read(path);
        Check(data.Count == 6 && data["message"] == "line1  line2 end" && data["shortcut"] == "F1", "Status documents must sanitize control characters and preserve the active shortcut.");
        SessionProtocol.WriteAtomic(path, SessionProtocol.Encode("unloaded", module, "gone", ""));
        Check(SessionProtocol.Read(path)["state"] == "unloaded" && !Directory.GetFiles(Root, "*.tmp").Any(), "Atomic replacement must publish a complete document and remove temporary files.");
        Throws<InvalidDataException>(() => SessionProtocol.WriteAtomic(path, new string('x', SessionProtocol.MaximumDocumentBytes + 1)), "Oversized writes must be rejected.");
        Check(SessionProtocol.Read(path)["state"] == "unloaded", "Rejected writes must leave the previous status intact.");
        File.WriteAllText(path, new string('x', SessionProtocol.MaximumDocumentBytes + 1)); Throws<InvalidDataException>(() => SessionProtocol.Read(path), "Oversized reads must be rejected.");
        foreach (string invalid in new[] { "broken", "state=one\nstate=two", string.Join("\n", Enumerable.Range(0, 33).Select(i => "key" + i + "=value")) })
        { File.WriteAllText(path, invalid); Throws<InvalidDataException>(() => SessionProtocol.Read(path), "Malformed, duplicate and excessive fields must be rejected."); }
        Throws<InvalidDataException>(() => SessionProtocol.Encode("ready", "not-a-guid", "", ""), "Invalid module identities must be rejected.");
        Throws<ArgumentOutOfRangeException>(() => SessionProtocol.Identity(0, 1), "Invalid PIDs must not produce storage paths.");
        Check(SessionProtocol.StatusPath(7, 100, Root) != SessionProtocol.StatusPath(7, 101, Root), "PID reuse must not reuse a previous game session's status.");
        SessionProtocol.WriteAtomic(path, SessionProtocol.Encode("ready", module, "first", ""));
        var writer = Task.Run(() => { for (int i = 0; i < 40; i++) SessionProtocol.WriteAtomic(path, SessionProtocol.Encode("ready", module, i.ToString(), "")); });
        for (int i = 0; i < 100; i++) Check(SessionProtocol.Read(path).Count == 6, "Concurrent readers must see complete status documents.");
        writer.GetAwaiter().GetResult();
    }
    private static void Sessions()
    {
        var game = new GameProcess { Id = 70001, StartTicks = 99001 };
        var bundle = new Bundle { Module = Guid.NewGuid().ToString("D") };
        string request = SessionProtocol.RequestPath(game.Id, game.StartTicks, Root), status = SessionProtocol.StatusPath(game.Id, game.StartTicks, Root);
        if (File.Exists(request)) File.Delete(request); if (File.Exists(status)) File.Delete(status);
        Check(InjectionSession.State(game, bundle, out _, Root) == "available", "A fresh process identity must allow one attempt.");
        using (var first = new InjectionSession(game, bundle, Root))
        {
            Throws<IOException>(() => { using (var second = new InjectionSession(game, bundle, Root)) { } }, "Two launchers must not acquire the same write lease.");
            Check(InjectionSession.State(game, bundle, out _, Root) == "pending", "A missing menu marker during an active attempt must remain pending.");
        }
        Throws<InvalidOperationException>(() => { using (var retry = new InjectionSession(game, bundle, Root)) { } }, "A launcher restart must not bypass an uncertain attempt.");
        SessionProtocol.WriteAtomic(status, SessionProtocol.Encode("waiting", bundle.Module, "Catalogs loading", ""));
        Check(InjectionSession.State(game, bundle, out string loading, Root) == "waiting" && loading == "Catalogs loading", "The launcher must display catalog loading.");
        SessionProtocol.WriteAtomic(status, SessionProtocol.Encode("ready", bundle.Module, "", "F1 / Num 0"));
        Check(InjectionSession.State(game, bundle, out string ready, Root) == "ready" && ready.Contains("F1 / Num 0"), "Startup confirmation must show the actual configured keybind.");
        Throws<InvalidOperationException>(() => { using (var retry = new InjectionSession(game, bundle, Root)) { } }, "An active menu must block reinjection.");
        var different = new Bundle { Module = Guid.NewGuid().ToString("D") };
        Check(InjectionSession.State(game, different, out _, Root) == "blocked", "A different menu build must require a fresh game process.");
        SessionProtocol.WriteAtomic(status, SessionProtocol.Encode("unloaded", bundle.Module, "", ""));
        using (var reloaded = new InjectionSession(game, bundle, Root))
        { Check(!File.Exists(status), "Reload must remove the stale unloaded marker before the new attempt."); reloaded.AbandonBeforeRemoteExecution(); }
        Check(!File.Exists(request) && InjectionSession.State(game, bundle, out _, Root) == "available", "Only a proven preflight failure may remove the retry gate.");
        SessionProtocol.WriteAtomic(status, SessionProtocol.Encode("error", bundle.Module, "Startup failed", ""));
        Check(InjectionSession.State(game, bundle, out _, Root) == "blocked", "A startup error must block retries even without a request file.");
        File.WriteAllText(status, "protocol=2\nstate=ready\nmodule=" + bundle.Module);
        Check(InjectionSession.State(game, bundle, out _, Root) == "blocked", "An unsupported status protocol must fail closed.");
        Throws<InvalidDataException>(() => { using (var invalid = new InjectionSession(game, bundle, Root)) { } }, "The attempt constructor must also reject unsupported protocols.");
        File.WriteAllText(status, "protocol=1\nstate=unexpected\nmodule=" + bundle.Module);
        Check(InjectionSession.State(game, bundle, out _, Root) == "blocked", "Unknown states must fail closed.");
        File.Delete(status); File.Delete(request);
        using (var current = Process.GetCurrentProcess()) Throws<InvalidOperationException>(() => GameProcess.Inspect(current.Id), "Game discovery must not select unrelated processes.");
    }
    private static void Bundles()
    {
        string bundleDir = PathFor("bundle"); Directory.CreateDirectory(bundleDir);
        File.Copy("src/Menu/bin/Release/UmbraMenu.dll", Path.Combine(bundleDir, "UmbraMenu.dll"), true);
        File.Copy("vendor/SharpMonoInjector/bin/Release/SharpMonoInjector.dll", Path.Combine(bundleDir, "SharpMonoInjector.dll"), true);
        string gameDir = PathFor("fake-game"), managed = Path.Combine(gameDir, "Risk of Rain 2_Data", "Managed"); Directory.CreateDirectory(managed);
        string gameDll = Path.Combine(managed, "RoR2.dll"); File.WriteAllText(gameDll, "compatibility fingerprint fixture");
        var document = new XmlDocument(); document.LoadXml("<umbra protocol='1' module='" + Guid.NewGuid().ToString("D") + "' version='3.0.0' gameSha256='" + Bundle.Hash(gameDll) + "'/>");
        foreach (string name in new[] { "UmbraMenu.dll", "SharpMonoInjector.dll" })
        { var file = document.CreateElement("file"); file.SetAttribute("name", name); file.SetAttribute("sha256", Bundle.Hash(Path.Combine(bundleDir, name))); document.DocumentElement.AppendChild(file); }
        string manifest = Path.Combine(bundleDir, "bundle.xml"); document.Save(manifest);
        var bundle = Bundle.Load(bundleDir); bundle.VerifyGame(Path.Combine(gameDir, "Risk of Rain 2.exe"));
        Check(bundle.Version == "3.0.0" && File.Exists(bundle.MenuPath), "A complete matching bundle must pass metadata and checksum validation.");
        File.AppendAllText(gameDll, "updated"); Throws<InvalidDataException>(() => bundle.VerifyGame(Path.Combine(gameDir, "Risk of Rain 2.exe")), "A game update must block the mismatched release.");
        string backend = Path.Combine(bundleDir, "SharpMonoInjector.dll"); File.AppendAllText(backend, "changed");
        Throws<InvalidDataException>(() => Bundle.Load(bundleDir), "Modified or mixed dependencies must be rejected before injection.");
        File.Copy("vendor/SharpMonoInjector/bin/Release/SharpMonoInjector.dll", backend, true);
        document.DocumentElement.FirstChild.Attributes["name"].Value = "../outside.dll"; document.Save(manifest);
        Throws<InvalidDataException>(() => Bundle.Load(bundleDir), "The manifest may not select arbitrary paths.");
        File.WriteAllText(manifest, "<!DOCTYPE umbra [<!ENTITY secret SYSTEM 'file:///unreadable'>]><umbra>&secret;</umbra>");
        Throws<XmlException>(() => Bundle.Load(bundleDir), "The manifest must prohibit external entities.");
        File.Delete(manifest); Throws<FileNotFoundException>(() => Bundle.Load(bundleDir), "An incomplete extraction must fail with a clear file error.");
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryInfo { public IntPtr BaseAddress, AllocationBase; public uint AllocationProtect; public UIntPtr RegionSize; public uint State, Protect, Type; }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern UIntPtr VirtualQueryEx(IntPtr process, IntPtr address, out MemoryInfo info, UIntPtr size);
    private static uint State(IntPtr process, IntPtr address)
    { Check(VirtualQueryEx(process, address, out var info, (UIntPtr)Marshal.SizeOf(typeof(MemoryInfo))) != UIntPtr.Zero, "VirtualQueryEx must succeed for the test process."); return info.State; }
    private static void NativeMemory()
    {
        // All native memory tests target THIS console process. No remote thread or game code is executed.
        using (var process = Process.GetCurrentProcess())
        {
            var memory = new Memory(process.Handle);
            IntPtr pointer = memory.AllocateAndWrite("hello λ"); Check(memory.ReadString(pointer, 100, Encoding.UTF8) == "hello λ", "Native strings must be UTF-8 and null terminated.");
            IntPtr page = memory.Allocate(4096); memory.Write(page + 4094, Encoding.UTF8.GetBytes("A\0"));
            Check(memory.ReadString(page + 4094, 100, Encoding.UTF8) == "A", "String reads must stop at a terminator without crossing an inaccessible page.");
            Throws<ArgumentOutOfRangeException>(() => memory.Allocate(0), "Zero-sized allocations must be rejected.");
            Throws<ArgumentOutOfRangeException>(() => memory.Allocate(32 * 1024 * 1024 + 1), "Oversized allocations must be rejected.");
            Throws<ArgumentOutOfRangeException>(() => memory.ReadBytes(pointer, -1), "Negative reads must be rejected.");
            memory.Dispose(); Check(State(process.Handle, pointer) == 0x10000 && State(process.Handle, page) == 0x10000, "Dispose must release reservations, not merely decommit pages.");
            memory.Dispose(); Throws<ObjectDisposedException>(() => memory.Allocate(1), "Disposed memory owners must reject new allocations.");
            Throws<ObjectDisposedException>(() => memory.ReadBytes(pointer, 1), "Disposed owners must not dereference freed memory.");
            var uncertain = new Memory(process.Handle); IntPtr executing = uncertain.Allocate(4096);
            typeof(Memory).GetMethod("PreserveAllocations", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(uncertain, null); uncertain.Dispose();
            Check(State(process.Handle, executing) == 0x1000, "Timeout cleanup must preserve pages that may still be executing.");
            Check(Native.VirtualFreeEx(process.Handle, executing, 0, MemoryFreeType.MEM_RELEASE), "The simulated uncertain allocation must be cleaned up by the test itself.");
            for (int i = 0; i < 32; i++)
            { var owner = new Memory(process.Handle); IntPtr address = owner.AllocateAndWrite(123456789L); Check(owner.ReadLong(address) == 123456789L, "Repeated allocations must retain 64-bit values."); owner.Dispose(); Check(State(process.Handle, address) == 0x10000, "Repeated cleanup must not retain reservations."); }
            try { using (var backend = new SharpMonoInjector.Injector(process.Id)) { } } catch (InjectorException) { }
            process.Refresh(); int before = process.HandleCount;
            for (int i = 0; i < 32; i++) Throws<InjectorException>(() => { using (var backend = new SharpMonoInjector.Injector(process.Id)) { } }, "A process without Mono must fail before executing any remote code.");
            process.Refresh(); Check(process.HandleCount <= before + 4, "Failed backend construction must close its owned process handle.");
        }
    }
    private static int Main()
    {
        try { Directory.CreateDirectory(Root); Protocol(); Sessions(); Bundles(); NativeMemory(); Console.WriteLine("PASS: " + checks + " protocol, bundle, concurrency, retry safety and native memory assertions."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
