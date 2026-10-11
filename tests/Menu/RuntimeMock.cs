using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static int Destroys;
        public static implicit operator bool(Object value) => value != null && !value.Destroyed;
        public static bool operator !(Object value) => !(bool)value;
        public static void Destroy(Object value) { Destroys++; GameObject.Scheduled.Add(value); }
        public static void DontDestroyOnLoad(Object value) { GameObject.AssertThread(); }
    }
    public sealed class GameObject : Object
    {
        public static int MainThread, Created;
        public static bool FailComponent;
        public static readonly List<GameObject> All = new List<GameObject>();
        public static readonly List<Object> Scheduled = new List<Object>();
        public string Name; private Object component;
        public GameObject(string name) { AssertThread(); Name = name; All.Add(this); Created++; }
        public static void AssertThread() { if (Thread.CurrentThread.ManagedThreadId != MainThread) throw new InvalidOperationException("Unity called off the main thread."); }
        public static GameObject Find(string name) { AssertThread(); return All.FirstOrDefault(g => g && g.Name == name); }
        public T GetComponent<T>() where T : Object => component as T;
        public T AddComponent<T>() where T : Object, new() { AssertThread(); if (FailComponent) return null; component = new T(); return (T)component; }
        public void SetActive(bool value) { AssertThread(); }
        public static void FlushDestroy() { foreach (var obj in Scheduled) obj.Destroyed = true; Scheduled.Clear(); }
    }
    public class Texture2D : Object { public Texture2D(int w, int h) { } public void SetPixel(int x, int y, Color32 color) { } public void Apply() { } }
    public struct Color32 { public Color32(byte r, byte g, byte b, byte a) { } }
    public enum KeyCode { None, Insert, Z, C, I, B, F1, Keypad0, Keypad1 }
    public class ExitGUIException : Exception { }
    public static class Time { public static float unscaledTime; }
    public static class Debug { public static int Warnings, Errors; public static void LogWarning(object value) { Warnings++; } public static void LogError(object value) { Errors++; } }
}
namespace RoR2
{
    public sealed class Availability { public bool available; }
    public static class ItemCatalog { public static Availability availability = new Availability(); }
    public static class EquipmentCatalog { public static Availability availability = new Availability(); }
    public static class BodyCatalog { public static Availability availability = new Availability(); }
    public static class SurvivorCatalog { public static Availability availability = new Availability(); }
    public static class BuffCatalog { public static Availability availability = new Availability(); }
    public static class RoR2Application
    {
        public static bool isLoading; public static event Action onUpdate;
        public static int Subscribers => onUpdate?.GetInvocationList().Length ?? 0;
        public static void Tick() { onUpdate?.Invoke(); }
    }
}
namespace UmbraMenu
{
    public sealed class UmbraMenu : UnityEngine.Object { public static FakeMenu mainMenu = new FakeMenu(); }
    public sealed class FakeMenu { public bool Enabled; public void SetEnabled(bool value) { Enabled = value; } }
    public static class MenuUI { public static string ToggleShortcut => "F1 / Num 0"; }
    public static class Utility { public static int Saves; public static void SaveSettings() { Saves++; } }
}
namespace UmbraSuite
{
    public static class SessionProtocol
    {
        public static string State, Message;
        public static void WriteStatus(string state, string module, string message, string shortcut) { State = state; Message = message; }
        public static void WriteAtomic(string path, string document) { System.IO.File.WriteAllText(path, document); }
    }
}
