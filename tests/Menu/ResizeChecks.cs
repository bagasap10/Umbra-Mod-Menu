using System;
using System.Reflection;
using UmbraMenu;
using UnityEngine;
using Root = UmbraMenu.UmbraMenu;
partial class UiChecks
{
    static Rect Window => (Rect)typeof(MenuUI).GetField("window",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
    static void Mouse(EventType type,float x,float y){Event.current=new Event{type=type,button=0,mousePosition=new Vector2(x,y)};MenuUI.Draw();}
    static void ResizeChecks()
    {
        var r=new Rect(100,100,700,500);
        var edges=new[]{MenuUI.ResizeEdges.Left,MenuUI.ResizeEdges.Right,MenuUI.ResizeEdges.Top,MenuUI.ResizeEdges.Bottom,MenuUI.ResizeEdges.Left|MenuUI.ResizeEdges.Top,MenuUI.ResizeEdges.Right|MenuUI.ResizeEdges.Top,MenuUI.ResizeEdges.Left|MenuUI.ResizeEdges.Bottom,MenuUI.ResizeEdges.Right|MenuUI.ResizeEdges.Bottom};
        var points=new[]{new Vector2(101,300),new Vector2(799,300),new Vector2(300,101),new Vector2(300,599),new Vector2(112,112),new Vector2(788,112),new Vector2(112,588),new Vector2(788,588)};
        for(int i=0;i<edges.Length;i++)Assert(MenuUI.HitResizeEdges(r,points[i])==edges[i],"All four edges and all four corners must be usable");
        Assert(MenuUI.HitResizeEdges(r,new Vector2(140,140))==MenuUI.ResizeEdges.None,"Header interior must not capture resizing");
        Assert(MenuUI.HitResizeEdges(r,new Vector2(80,100))==MenuUI.ResizeEdges.None,"Outside clicks must not resize");
        foreach(var edge in edges)
        {
            var resized=MenuUI.ResizeWindow(r,new Vector2(20,20),edge,1920,1080);
            if((edge&MenuUI.ResizeEdges.Left)!=0)Assert(resized.x==120&&resized.xMax==800,"Left resizing must hold the right edge fixed");
            if((edge&MenuUI.ResizeEdges.Right)!=0)Assert(resized.x==100&&resized.xMax==820,"Right resizing must hold the left edge fixed");
            if((edge&MenuUI.ResizeEdges.Top)!=0)Assert(resized.y==120&&resized.yMax==600,"Top resizing must hold the bottom edge fixed");
            if((edge&MenuUI.ResizeEdges.Bottom)!=0)Assert(resized.y==100&&resized.yMax==620,"Bottom resizing must hold the top edge fixed");
        }
        var minimum=MenuUI.ResizeWindow(r,new Vector2(900,900),MenuUI.ResizeEdges.Left|MenuUI.ResizeEdges.Top,1920,1080);
        Assert(minimum.width==560&&minimum.height==360&&minimum.xMax==800&&minimum.yMax==600,"Shrinking must stop at readable limits while keeping opposite edges fixed");
        var maximum=MenuUI.ResizeWindow(r,new Vector2(9000,9000),MenuUI.ResizeEdges.Right|MenuUI.ResizeEdges.Bottom,1280,720);
        Assert(maximum.xMax<=1280&&maximum.yMax<=720,"Growing must stop at the screen edges");
        var tiny=MenuUI.FitWindow(new Rect(0,0,0,0),680,560,500,340);
        tiny=MenuUI.ResizeWindow(tiny,new Vector2(100,100),MenuUI.ResizeEdges.Left|MenuUI.ResizeEdges.Top,500,340);
        Assert(tiny.x>=0&&tiny.y>=0&&tiny.xMax<=500&&tiny.yMax<=340,"Screens below the usual minimum must not push the window outside the screen");
        Setup();Root.WindowWidth=680;Root.WindowHeight=560;Screen.width=1280;Screen.height=720;Root.navigationToggle=false;GUI.ClickLabel=null;GUI.ClickRect=null;MenuUI.ResetWindow();Event.current=new Event{type=EventType.Repaint};MenuUI.Draw();
        var start=Window;Assert(start.width==680&&start.height==560,"Default menu must be smaller");int saves=Utility.Saves;
        Mouse(EventType.MouseDown,start.xMax-2,start.yMax-2);Assert(GUIUtility.hotControl!=0&&Event.current.type==EventType.Used,"Dragging a border must capture and consume the click");
        Mouse(EventType.MouseDrag,start.xMax+88,start.yMax+48);Assert(Window.width==770&&Window.height==610&&Utility.Saves==saves,"Resize must follow the pointer without writing settings on each move");
        Mouse(EventType.MouseUp,start.xMax+88,start.yMax+48);Assert(Root.WindowWidth==770&&Root.WindowHeight==610&&GUIUtility.hotControl==0&&Utility.Saves==saves+1,"Release must save both dimensions once and release capture");
        Root.mainMenu.SetEnabled(false);MenuUI.Draw();Root.mainMenu.SetEnabled(true);Event.current=new Event{type=EventType.Repaint};MenuUI.Draw();Assert(Window.width==770&&Window.height==610,"Opening the menu again must preserve the resized dimensions");
        start=Window;Mouse(EventType.MouseDown,start.x+2,start.y+2);Mouse(EventType.MouseDrag,start.x+32,start.y+32);Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape};MenuUI.Draw();Assert(Window.x==start.x&&Window.y==start.y&&Window.width==start.width&&Window.height==start.height&&GUIUtility.hotControl==0&&Utility.Saves==saves+1,"Escape must cancel the whole drag without saving");
        Mouse(EventType.MouseDown,start.x+40,start.y+40);Assert(GUIUtility.hotControl==0&&Event.current.type==EventType.MouseDown,"Interior header clicks must stay available to window dragging");
        Event.current=new Event{type=EventType.Repaint,mousePosition=new Vector2(start.x+2,start.y+40)};MenuUI.Draw();Assert(Cursor.CurrentTexture!=null,"Hovering an edge must display a resize cursor");Root.mainMenu.SetEnabled(false);MenuUI.Draw();Assert(Cursor.CurrentTexture==null,"Closing the menu must restore the cursor");Root.mainMenu.SetEnabled(true);
        Root.GlassOpacity=.35f;UITheme.Prepare();var low=UITheme.BackgroundStyle.normal.background;Root.GlassOpacity=.9f;UITheme.Prepare();var high=UITheme.BackgroundStyle.normal.background;Assert(high.Pixels[528].a>low.Pixels[528].a,"The opacity setting must change the panel alpha");Assert(UITheme.Label.normal.textColor.a==1,"Glass opacity must never fade text");
        Root.menus[12].AddButtons(new System.Collections.Generic.List<Button>{new Button(new DividerButton(Root.menus[12],1,"White / Common",new Color32(230,237,242,255),true)),new Button(new DividerButton(Root.menus[12],2,"Normal / Not-DLC",new Color32(185,203,215,255),false)),new Button(new NormalButton(Root.menus[12],3,"Soldier's Syringe",()=>{}))});
        Root.GlassOpacity=.62f;Root.WindowWidth=560;Root.WindowHeight=360;Screen.width=900;Screen.height=600;MenuUI.ResetWindow();Event.current=new Event{type=EventType.Repaint};MenuUI.NavigateTo(12);GUI.StartCanvas();MenuUI.Draw();Assert(GUI.LastViewport.height>0&&GUI.LastViewport.width>250,"Search and rows must retain usable viewports at minimum size");GUI.Save("artifacts/tests/previews/minimum.png");
        Root.WindowWidth=680;Root.WindowHeight=560;Screen.width=900;Screen.height=800;MenuUI.ResetWindow();MenuUI.NavigateTo(0);SceneCanvas();MenuUI.Draw();GUI.Save("artifacts/tests/previews/glass.png");
        var settings=Root.menus[7];settings.AddButtons(new System.Collections.Generic.List<Button>{new Button(new MulButton(settings,1,"WINDOW WIDTH : 680",()=>{},()=>{},()=>{})),new Button(new TogglableButton(settings,2,"KEYBOARD NAVIGATION : OFF","KEYBOARD NAVIGATION : ON",()=>{},()=>{},true)),new Button(new NormalButton(settings,3,"GOD TYPE : NORMAL",()=>{})),new Button(new TogglableButton(settings,4,"KEYBIND MENU : OFF","KEYBIND MENU : ON",()=>{},()=>{})),new Button(new NormalButton(settings,5,"RELOAD MENU",()=>{})),new Button(new NormalButton(settings,6,"RESET SETTINGS",()=>{})),new Button(new MulButton(settings,7,"WINDOW HEIGHT : 560",()=>{},()=>{},()=>{})),new Button(new MulButton(settings,8,"GLASS OPACITY : 62%",()=>{},()=>{},()=>{}))});Root.menus[16].SetActivatingButton(settings.GetButtons()[3]);
        MenuUI.NavigateTo(7);SceneCanvas();MenuUI.Draw();GUI.Save("artifacts/tests/previews/glass-settings.png");
    }
    static void SceneCanvas()
    {
        GUI.StartCanvas();
        using(var sky=new System.Drawing.Drawing2D.LinearGradientBrush(new System.Drawing.Rectangle(0,0,Screen.width,Screen.height),System.Drawing.Color.FromArgb(105,156,173),System.Drawing.Color.FromArgb(15,48,61),System.Drawing.Drawing2D.LinearGradientMode.Vertical))GUI.Paint.FillRectangle(sky,0,0,Screen.width,Screen.height);
        using(var back=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(45,95,106)))GUI.Paint.FillPolygon(back,new[]{new System.Drawing.Point(0,450),new System.Drawing.Point(100,250),new System.Drawing.Point(210,390),new System.Drawing.Point(460,160),new System.Drawing.Point(650,420),new System.Drawing.Point(900,230),new System.Drawing.Point(900,800),new System.Drawing.Point(0,800)});
        using(var front=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(20,65,77)))GUI.Paint.FillPolygon(front,new[]{new System.Drawing.Point(0,600),new System.Drawing.Point(320,450),new System.Drawing.Point(620,580),new System.Drawing.Point(900,430),new System.Drawing.Point(900,800),new System.Drawing.Point(0,800)});
    }
}
