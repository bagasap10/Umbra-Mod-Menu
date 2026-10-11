using System.Collections.Generic;
using System.Linq;
using UmbraMenu;
using UnityEngine;
using Root = UmbraMenu.UmbraMenu;
partial class UiChecks
{
    static void BackShortcutChecks()
    {
        Setup();
        Root.menus.Add(new Menu(new SearchableListMenu(18,new Rect(),"Packs")));
        Root.menus.Add(new Menu(new ListMenu(19,new Rect(),"Pack details")));
        Root.menus[18].SetPrevMenuId(3);Root.menus[19].SetPrevMenuId(18);
        var opener=new Button(new NormalButton(Root.menus[18],7,"Pack",()=>{}));
        Root.menus[18].AddButtons(new List<Button>{opener});Root.menus[19].AddButtons(new List<Button>());
        Root.menus[19].SetActivatingButton(opener);
        Root.AllowNavigation=false;Root.navigationToggle=false;
        MenuUI.NavigateTo(19);Input.Pressed=KeyCode.Keypad1;
        Assert(MenuUI.HandleBackShortcut()&&MenuUI.ActiveMenuId==18&&Navigation.buttonIndex==7,"Numpad 1 must return to the pack list and restore its opener with keyboard navigation disabled");
        Assert(!Root.navigationToggle,"Back from mouse use must not enable keyboard selection");
        Input.Pressed=KeyCode.None;Assert(!MenuUI.HandleBackShortcut()&&MenuUI.ActiveMenuId==18,"No key must leave the current page unchanged");
        Input.Pressed=KeyCode.A;Assert(!MenuUI.HandleBackShortcut()&&MenuUI.ActiveMenuId==18,"Unrelated keys must not return to the previous menu");
        Input.Pressed=KeyCode.Keypad1;Root.listenForKeybind=true;
        Assert(!MenuUI.HandleBackShortcut()&&Root.listenForKeybind&&MenuUI.ActiveMenuId==18,"Keybind capture must consume Numpad 1 without navigating");Root.listenForKeybind=false;
        Root.chatOpen=true;Assert(!MenuUI.HandleBackShortcut()&&MenuUI.ActiveMenuId==18,"Typing in game chat must not navigate the menu");Root.chatOpen=false;
        foreach(int id in new[]{12,17,18})
        {
            MenuUI.NavigateTo(id);var search=(SearchableListMenu)Model(id);Event.current=new Event{type=EventType.Repaint};
            GUI.FocusControl("UmbraItemSearch_"+id);search.DrawSearch(new Rect(0,0,420,38));
            Assert(MenuUI.IsSearchFocused,"Every searchable menu, including packs, must report active text entry");
            Assert(!MenuUI.HandleBackShortcut()&&MenuUI.ActiveMenuId==id,"Numpad 1 typed into a search must not navigate");
            GUI.FocusControl(null);search.DrawSearch(new Rect(0,0,420,38));
            Assert(!MenuUI.IsSearchFocused,"Ending search focus must allow navigation again");
        }
        MenuUI.NavigateTo(19);Root.mainMenu.SetEnabled(false);
        Assert(!MenuUI.HandleBackShortcut()&&MenuUI.ActiveMenuId==19,"The back key must do nothing while the menu is closed");Root.mainMenu.SetEnabled(true);
        Root.AllowNavigation=true;Root.navigationToggle=true;
        Assert(MenuUI.HandleBackShortcut()&&MenuUI.ActiveMenuId==18,"Numpad 1 must also work during keyboard navigation");
        Assert(MenuUI.HandleBackShortcut()&&MenuUI.ActiveMenuId==3,"Repeating Back must return from packs to Items");
        Assert(MenuUI.HandleBackShortcut()&&MenuUI.ActiveMenuId==0,"Repeating Back must return from Items to the overview");
        Assert(MenuUI.HandleBackShortcut()&&MenuUI.ActiveMenuId==0&&Root.mainMenu.IsEnabled(),"Back on the overview must leave the menu open");
        Input.Pressed=KeyCode.None;MenuUI.NavigateTo(3);Root.WindowWidth=560;Root.WindowHeight=360;Screen.width=1000;Screen.height=800;MenuUI.ResetWindow();
        GUI.StartCanvas();MenuUI.Draw();GUI.Save("artifacts/tests/previews/back-key-minimum.png");
        Assert(GUI.Labels.Keys.Any(label=>label.StartsWith("Num 1 Back")),"The footer must display the new back key");
        Root.AllowNavigation=false;GUI.StartCanvas();MenuUI.Draw();GUI.Save("artifacts/tests/previews/back-key-mouse.png");
        Assert(GUI.Labels.Keys.Any(label=>label.StartsWith("Num 1 Back")),"Mouse mode must also display the back key");
        Root.AllowNavigation=true;Root.navigationToggle=false;
    }
}
