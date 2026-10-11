using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using UColor = UnityEngine.Color;

namespace UnityEngine
{
    public class Object { public HideFlags hideFlags; public static implicit operator bool(Object o) => o != null; public static void Destroy(Object o) {} public static T Instantiate<T>(T o) where T : Object,new() => new T(); }
    public enum HideFlags { HideAndDontSave } public enum FilterMode { Bilinear, Point } public enum TextureWrapMode { Clamp } public enum TextureFormat { RGBA32 }
    public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a) {this.r=r;this.g=g;this.b=b;this.a=a;} }
    public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a=1) {this.r=r;this.g=g;this.b=b;this.a=a;} public static Color white => new Color(1,1,1); public static Color grey => new Color(.5f,.5f,.5f); public static implicit operator Color(Color32 c)=>new Color(c.r/255f,c.g/255f,c.b/255f,c.a/255f); }
    public class Texture2D : Texture { public Color32[] Pixels; public Color value; public FilterMode filterMode;public TextureWrapMode wrapMode; public Texture2D(int w,int h) {} public Texture2D(int w,int h,TextureFormat f,bool m) {} public void SetPixel(int x,int y,Color c) {value=c;} public void SetPixels32(Color32[] p) {Pixels=p;value=p[p.Length/2+16];} public void Apply() {} public void Apply(bool m,bool r) {} }
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();}
    public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public float xMax=>x+width;public float yMax=>y+height; public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;}
    public class RectOffset {public int left,right,top,bottom; public RectOffset() {}public RectOffset(int l,int r,int t,int b){left=l;right=r;top=t;bottom=b;}}
    public enum FontStyle {Normal,Bold} public enum TextAnchor {MiddleLeft,MiddleCenter,UpperLeft}
    public class GUIStyleState {public Color textColor=Color.white;public Texture2D background;}
    public class GUIStyle {public int fontSize=14;public FontStyle fontStyle;public TextAnchor alignment;public bool wordWrap,richText;public float fixedWidth;public RectOffset padding=new RectOffset(),border=new RectOffset();public GUIStyleState normal=new GUIStyleState(),hover=new GUIStyleState(),active=new GUIStyleState(),focused=new GUIStyleState();public static readonly GUIStyle none=new GUIStyle();public GUIStyle(){} public GUIStyle(GUIStyle s){fontSize=s.fontSize;fontStyle=s.fontStyle;alignment=s.alignment;wordWrap=s.wordWrap;richText=s.richText;padding=s.padding;normal=new GUIStyleState{textColor=s.normal.textColor,background=s.normal.background};}}
    public class GUISkin:Object {public GUIStyle textField=new GUIStyle(),verticalScrollbar=new GUIStyle(),verticalScrollbarThumb=new GUIStyle(),verticalScrollbarUpButton=new GUIStyle(),verticalScrollbarDownButton=new GUIStyle();}
    public class GUIContent {public string text;public GUIContent(string t){text=t;}public static GUIContent none=>new GUIContent("");}
    public static class Input {public static KeyCode Pressed;public static bool GetKeyDown(KeyCode key)=>Pressed!=KeyCode.None&&Pressed==key;}
    public enum KeyCode {None,Insert,Escape,Return,KeypadEnter,Keypad0,Keypad1,A,B}
    public enum EventType {Layout,Repaint,MouseDown,MouseUp,MouseDrag,Ignore,KeyDown,Used}
    public class Event {public static Event current=new Event {type=EventType.Repaint};public EventType type;public KeyCode keyCode;public int button;public Vector2 mousePosition;public void Use(){type=EventType.Used;}}
    public static class Screen {public static int width=900,height=800;}
    public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));public static float Clamp01(float v)=>Clamp(v,0,1);public static float Sqrt(float v)=>(float)Math.Sqrt(v);}
    public static class GUI
    {
        public static GUISkin skin=new GUISkin();public static Color color=Color.white;public static int depth;public static bool enabled=true;
        public static Bitmap Canvas; public static Graphics Paint;public static string ClickLabel,NextText,Focus,NextControl;public static Rect LastViewport,LastContent;public static Rect? ClickRect;
        static readonly Stack<GraphicsState> states=new Stack<GraphicsState>();public static readonly Dictionary<string,Rect> Labels=new Dictionary<string,Rect>();public static int ButtonCalls;
        public delegate void WindowFunction(int id); public static Texture LastIcon;public static Rect LastIconUV;public static void DrawTextureWithTexCoords(Rect r,Texture t,Rect uv,bool alpha){LastIcon=t;LastIconUV=uv;}
        static System.Drawing.Color C(Color c)=>System.Drawing.Color.FromArgb((int)(c.a*255),(int)(c.r*255),(int)(c.g*255),(int)(c.b*255));
        public static void StartCanvas(){if(Paint!=null)Paint.Dispose();if(Canvas!=null)Canvas.Dispose();Canvas=new Bitmap(Screen.width,Screen.height);Paint=Graphics.FromImage(Canvas);Paint.SmoothingMode=SmoothingMode.AntiAlias;Paint.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;Paint.Clear(System.Drawing.Color.FromArgb(10,14,19));Labels.Clear();}
        public static void Save(string path){Canvas.Save(path,System.Drawing.Imaging.ImageFormat.Png);}
        static void Background(Rect r,GUIStyle s){if(Paint==null||s.normal.background==null)return; using(var brush=new SolidBrush(C(s.normal.background.value))) {float radius=7;using(var p=new GraphicsPath()){float d=radius*2;p.AddArc(r.x,r.y,d,d,180,90);p.AddArc(r.xMax-d,r.y,d,d,270,90);p.AddArc(r.xMax-d,r.yMax-d,d,d,0,90);p.AddArc(r.x,r.yMax-d,d,d,90,90);p.CloseFigure();Paint.FillPath(brush,p);}}}
        public static void Box(Rect r,string t,GUIStyle s){Background(r,s);if(t.Length>0)Label(r,t,s);}
        public static void Label(Rect r,string t,GUIStyle s)
        {
            Labels[t]=r;if(Paint==null)return;t=Regex.Replace(t,"<[^>]*>","");using(var font=new Font("Arial",s.fontSize,s.fontStyle==FontStyle.Bold?System.Drawing.FontStyle.Bold:System.Drawing.FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(C(s.normal.textColor)))using(var fmt=new StringFormat()){fmt.Alignment=s.alignment==TextAnchor.MiddleCenter?StringAlignment.Center:StringAlignment.Near;fmt.LineAlignment=s.alignment==TextAnchor.UpperLeft?StringAlignment.Near:StringAlignment.Center;fmt.Trimming=StringTrimming.EllipsisCharacter;Paint.DrawString(t,font,brush,new RectangleF(r.x+s.padding.left,r.y+s.padding.top,Math.Max(1,r.width-s.padding.left-s.padding.right),Math.Max(1,r.height-s.padding.top-s.padding.bottom)),fmt);}
        }
        public static bool Button(Rect r,string t,GUIStyle s)=>Button(r,new GUIContent(t),s);
        public static bool Button(Rect r,GUIContent t,GUIStyle s){ButtonCalls++;Background(r,s);if(t.text.Length>0)Label(r,t.text,s);if(!enabled)return false;bool hit=ClickLabel!=null&&ClickLabel==t.text||ClickRect.HasValue&&Math.Abs(ClickRect.Value.x-r.x)<.1f&&Math.Abs(ClickRect.Value.y-r.y)<.1f&&Math.Abs(ClickRect.Value.width-r.width)<.1f;if(hit){ClickLabel=null;ClickRect=null;return true;}return false;}
        public static Rect Window(int id,Rect r,WindowFunction f,string t,GUIStyle s){if(Paint!=null){states.Push(Paint.Save());Paint.TranslateTransform(r.x,r.y);}f(id);if(Paint!=null)Paint.Restore(states.Pop());return r;}
        public static void DragWindow(Rect r){}
        public static Vector2 BeginScrollView(Rect r,Vector2 scroll,Rect content,bool h,bool v,GUIStyle hs,GUIStyle vs){LastViewport=r;LastContent=content;if(Paint!=null){states.Push(Paint.Save());Paint.SetClip(new RectangleF(r.x,r.y,r.width,r.height),CombineMode.Intersect);Paint.TranslateTransform(r.x-scroll.x,r.y-scroll.y);}return scroll;}
        public static void EndScrollView(){if(Paint!=null)Paint.Restore(states.Pop());}
        public static void DrawTexture(Rect r,Texture2D t){if(Paint!=null)using(var b=new SolidBrush(C(color)))Paint.FillRectangle(b,r.x,r.y,r.width,r.height);}
        public static void FocusControl(string n){Focus=n;}public static string GetNameOfFocusedControl()=>Focus;public static void SetNextControlName(string n){NextControl=n;}
        public static string TextField(Rect r,string t,int n,GUIStyle s){Box(r,t,s);if(Event.current.type==EventType.MouseDown&&r.Contains(Event.current.mousePosition))Focus=NextControl;string result=NextText??t;NextText=null;return result;}
    }
}
namespace UmbraMenu
{
    public static class Styles {public static GUIStyle BtnStyle=>UITheme.Button;public static GUIStyle OffStyle=>UITheme.Button;public static GUIStyle OnStyle=>UITheme.Button;public static GUIStyle LabelStyle=>UITheme.Label;public static GUIStyle HighlightBtnStyle=>UITheme.SelectedButton;}
    public class Keybind {public UnityEngine.KeyCode KeyCode;}
    public static class Loader {public static bool devBuild,updateAvailable; public static void Report(string state,string message,string shortcut="") {}}
    public static class UmbraMenu {public const string VERSION="Hallowed Concept";public static float Width=350,WindowWidth=680,WindowHeight=560,GlassOpacity=.62f;public static bool AllowNavigation=true; public static bool characterCollected=true,forceFullModMenu,navigationToggle,lowResolutionMonitor,listenForKeybind,chatOpen,scrolled;public static int previousMenuOpen;public static Menu mainMenu;public static List<Menu> menus=new List<Menu>();public static Dictionary<string,Keybind> keybindDict=new Dictionary<string,Keybind>{{"MAIN MENU",new Keybind {KeyCode=UnityEngine.KeyCode.Insert}}};}
    public static class Utility {public static int Saves;public static void SaveSettings(){Saves++;}public static Button FindButtonById(int m,int b)=>UmbraMenu.menus[m].GetButtons()?.FirstOrDefault(x=>x.GetId()==b);}
}
namespace UmbraMenu.Menus { public class InventoryItemList {public const int MenuId=17;} }

namespace UnityEngine {
    public enum FocusType { Passive } public enum CursorMode { Auto }
    public static class GUIUtility {public static int hotControl;public static int GetControlID(int hint,FocusType focus)=>hint;}
    public static class Cursor {public static Texture2D CurrentTexture;public static void SetCursor(Texture2D texture,Vector2 hotspot,CursorMode mode){CurrentTexture=texture;}}
}



namespace UnityEngine { public class ExitGUIException : System.Exception {} }

namespace UnityEngine { public static class Debug { public static void LogWarning(object value) {} } }
