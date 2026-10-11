using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RoR2;
using UmbraMenu;
using UnityEngine;
using UmbraSuite;
using Object = UnityEngine.Object;

internal static class RuntimeChecks
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static void Ready(bool value)
    {
        ItemCatalog.availability.available = EquipmentCatalog.availability.available = BodyCatalog.availability.available = SurvivorCatalog.availability.available = BuffCatalog.availability.available = value;
    }
    private static void Lifecycle()
    {
        GameObject.MainThread = Thread.CurrentThread.ManagedThreadId;
        Ready(false);
        Task.Run(() => { Loader.Load(); Loader.Load(); }).GetAwaiter().GetResult();
        Check(GameObject.Created == 0 && RoR2Application.Subscribers == 1 && SessionProtocol.State == "waiting", "Load must only enqueue one main thread bootstrap.");
        RoR2Application.Tick(); Check(GameObject.Created == 0, "The bootstrap must wait for game catalogs.");
        Ready(true); BuffCatalog.availability.available = false; RoR2Application.Tick(); Check(GameObject.Created == 0, "Every dependent catalog must be ready.");
        Ready(true); RoR2Application.isLoading = true; RoR2Application.Tick(); Check(GameObject.Created == 0, "Loading scenes must not initialize the menu prematurely.");
        RoR2Application.isLoading = false; RoR2Application.Tick();
        Check(GameObject.Created == 1 && Loader.gameObject && RoR2Application.Subscribers == 0, "Ready catalogs must create one menu and remove the bootstrap hook.");
        Task.Run(() => Loader.Load()).GetAwaiter().GetResult(); RoR2Application.Tick();
        Check(GameObject.Created == 1 && SessionProtocol.State == "ready" && UmbraMenu.UmbraMenu.mainMenu.Enabled, "Duplicate Load must reuse the existing component.");
        Task.Run(() => { Loader.Unload(); Loader.Unload(); }).GetAwaiter().GetResult();
        Check(GameObject.Scheduled.Count == 0 && RoR2Application.Subscribers == 1, "Unload must also defer Unity operations and deduplicate requests.");
        int destroyed = Object.Destroys; RoR2Application.Tick();
        Check(Object.Destroys == destroyed + 1 && Loader.gameObject == null && RoR2Application.Subscribers == 0 && Utility.Saves == 1, "Unload must schedule exactly one Destroy without waiting for deferred destruction.");
        GameObject.FlushDestroy();
        GameObject.FailComponent = true; Loader.Load(); RoR2Application.Tick();
        Check(SessionProtocol.State == "error" && Loader.gameObject == null && RoR2Application.Subscribers == 0, "A failed AddComponent must clean up and report failure.");
        GameObject.FlushDestroy(); GameObject.FailComponent = false;
        var foreign = new GameObject("Umbra Menu"); Loader.Load(); RoR2Application.Tick();
        Check(SessionProtocol.State == "error" && !GameObject.Scheduled.Contains(foreign), "A different menu object must be rejected without destroying someone else's object.");
        Object.Destroy(foreign); GameObject.FlushDestroy(); Loader.gameObject = null;
        Ready(false); Loader.Load(); typeof(Loader).GetField("requestedAt", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, DateTime.UtcNow.AddMinutes(-4)); RoR2Application.Tick();
        Check(SessionProtocol.State == "error" && RoR2Application.Subscribers == 0, "Catalog timeout must release the bootstrap subscription.");
    }
    private static void Routines()
    {
        Time.unscaledTime = 0; int calls = 0;
        var failing = new SafeRoutine("Failure", () => { calls++; throw new InvalidOperationException(); });
        failing.Run(); failing.Run(); Check(calls == 1, "Faulting routines must back off instead of throwing every frame.");
        Time.unscaledTime = 2; failing.Run(); Time.unscaledTime = 4; failing.Run(); Time.unscaledTime = 100; failing.Run();
        Check(calls == 3 && SessionProtocol.State == "degraded", "Three repeated failures must pause only the failing feature.");
        failing.Reset(); failing.Run(); Check(calls == 4, "Scene reset must allow the routine to recover.");
        bool shouldFail = true; int recoveryCalls = 0;
        var recovery = new SafeRoutine("Recovery", () => { recoveryCalls++; if (shouldFail) throw new Exception(); });
        recovery.Run(); Time.unscaledTime += 2; shouldFail = false; recovery.Run(); shouldFail = true; recovery.Run(); Time.unscaledTime += 2; recovery.Run();
        Check(recoveryCalls == 4, "Successful execution must reset consecutive failure counting.");
        var exit = new SafeRoutine("GUI", () => { throw new ExitGUIException(); }); bool propagated = false;
        try { exit.Run(); } catch (ExitGUIException) { propagated = true; } Check(propagated, "ExitGUI must propagate through the routine guard.");
    }
    private sealed class ReflectTarget { public int Value = 1; public int ReadOnly => 2; }
    private static void Resources()
    {
        var first = Textures.BtnTextureLabel;
        Check(first != null && ReferenceEquals(first, Textures.BtnTextureLabel), "Label texture lookup must return the cached field without recursive calls.");
        var properties = typeof(Textures).GetProperties().Where(p => p.PropertyType == typeof(Texture2D)).ToArray();
        foreach (var property in properties) property.GetValue(null);
        int before = Object.Destroys; Textures.Release(); Check(Object.Destroys == before + 11, "Cleanup must release every owned legacy texture exactly once.");
        Textures.Release(); Check(Object.Destroys == before + 11, "Texture cleanup must be idempotent.");
        Check(!ReferenceEquals(first, Textures.BtnTextureLabel), "Reload must create a fresh owned texture."); Textures.Release(); GameObject.FlushDestroy();
        var target = new ReflectTarget(); target.SetField("Value", 7); Check(target.GetField<int>("Value") == 7, "Reflection helpers must retain valid field access.");
        Check(target.GetField<int>("Missing") == 0 && typeof(ReflectTarget).GetFieldFast("Missing", "", "") == null, "Missing fields must return safely without null dereference.");
        bool failed = false; try { target.SetField("Missing", 1); } catch (MissingMemberException) { failed = true; } Check(failed, "Invalid writes must fail with a useful exception.");
        Check(InventorySafety.BulkQuantity(int.MinValue) == 1 && InventorySafety.RollCount(int.MinValue) == 5, "Negative bulk counts must recover to valid defaults.");
    }
    private static void Settings()
    {
        string path = Path.GetFullPath("artifacts/tests/settings.fixture");
        string[] names = { "MAIN MENU", "PLAYER MENU", "FLIGHT", "ITEMS MENU", "TELEPORTER MENU", "OTHER" };
        if (File.Exists(path)) File.Delete(path);
        var values = MenuSettings.Read(path, names); Check(values.Count == 9 && values[3] == "Insert" && values[8] == "None", "Missing settings must use complete defaults.");
        File.WriteAllText(path, "OTHER: F1\nFLIGHT: garbage\nWidth: NaN\nMAIN MENU: 9999\nAllow Navigation: false\nGod Version: 9999\nPLAYER MENU: z : old comment\nBroken line\n");
        values = MenuSettings.Read(path, names);
        Check(values[0] == "350" && values[1] == "False" && values[2] == "4" && values[3] == "Insert" && values[4] == "Z" && values[5] == "C" && values[8] == "F1", "Malformed, reordered, missing and old commented fields must recover independently.");
        foreach (string number in new[] { "Infinity", "-Infinity", "NaN", "bad" }) { File.WriteAllText(path, "Width: " + number); Check(MenuSettings.Read(path, names)[0] == "350", "Non-finite and malformed dimensions must use a finite default."); }
        File.WriteAllText(path, "Width: -123\nGod Version: -5\nAllow Navigation: bad"); values = MenuSettings.Read(path, names); Check(values[0] == "100" && values[1] == "True" && values[2] == "0", "Out of range fields must clamp safely.");
        File.WriteAllText(path, new string('x', 65537)); Check(MenuSettings.Read(path, names)[3] == "Insert", "Oversized settings must recover instead of exhausting parsing memory.");
        var previous = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR"); MenuSettings.CreateDefaults(path, names); Check(MenuSettings.ReadNumber(path, "Glass Opacity", 0) == .62f && MenuSettings.Read(path, names)[0] == "350", "Settings must round trip with invariant numeric formatting."); }
        finally { CultureInfo.CurrentCulture = previous; }
        File.Delete(path);
    }
    private static int Main()
    {
        try { Lifecycle(); Routines(); Resources(); Settings(); Console.WriteLine("PASS: " + checks + " lifecycle, failure isolation, texture, reflection, count and settings assertions."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
