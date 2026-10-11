using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UmbraMenu;
using UmbraMenu.Menus;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Root=UmbraMenu.UmbraMenu;

partial class Checks
{
    static void PortalIconChecks(List<Button> rows)
    {
        var photographed=new HashSet<string>{"blue","cele","gold","artifact","null","void","deepvoid","infinite","green","destination","virtual","encrypted","decrypted","mainline"};
        int loads=ImageConversion.LoadCalls,addressableLoads=Addressables.IconLoadCalls;
        var images=new List<Sprite>();
        Assert(PortalIcons.Get(null)==null&&PortalIcons.Get(new PortalCatalog.Definition("blue","Wrong",0,"Wrong"))==null,"Only an exact catalog portal may resolve a thumbnail");
        Assert(ImageConversion.LoadCalls==loads,"Creating portal rows must not decode images before they are drawn");
        foreach(var portal in PortalCatalog.Portals)
        {
            var row=Row(rows.Single(button=>Row(button).Text=="SPAWN "+portal.Name)) as PickupButton;
            Assert(row!=null&&row.MissingIconText=="?","Every portal row must reserve an icon and an approved neutral placeholder");
            bool found=row.TryGetIcon(out var texture,out var uv);
            Assert(found==photographed.Contains(portal.Key),"Use the exact wiki image where available, and a placeholder for missing photographs: "+portal.Key);
            if(!found){Assert(PortalIcons.Get(portal)==null,"Missing images must not be substituted with another portal's photograph");continue;}
            Sprite sprite=PortalIcons.Get(portal);images.Add(sprite);
            Assert(ReferenceEquals(sprite,PortalIcons.Get(portal))&&ReferenceEquals(texture,sprite.texture),"Repeated draws must share the cached thumbnail for that exact portal");
            Assert(uv.x==0&&uv.y==0&&uv.width==1&&uv.height==1,"Portal thumbnails must use their full, aspect-preserving image");
            Assert(sprite.texture.Image.Width==128&&sprite.texture.Image.Height==128&&sprite.texture.wrapMode==TextureWrapMode.Clamp&&sprite.texture.filterMode==FilterMode.Bilinear,"Decoded portal images must stay small and use clamped, smooth sampling");
            byte[] source=File.ReadAllBytes("src/Menu/Assets/PortalIcons/"+portal.Key+".png");
            Assert(PortalIcons.ValidPng(source),"Every bundled portal photograph must satisfy the loader's PNG bounds");
            using(var stream=new MemoryStream(source))using(var expected=System.Drawing.Image.FromStream(stream))using(var bitmap=new System.Drawing.Bitmap(expected))
                Assert(bitmap.GetPixel(64,64).ToArgb()==sprite.texture.Image.GetPixel(64,64).ToArgb(),"The embedded photograph must match the corresponding source file: "+portal.Key);
        }
        Assert(images.Count==14&&images.Distinct().Count()==14&&ImageConversion.LoadCalls==loads+14,"The fourteen distinct photos must each decode only once");
        Assert(ImageConversion.LastMarkNonReadable&&Addressables.IconLoadCalls==addressableLoads,"Portal images must free CPU image data and must not load/spawn game prefabs");
        Assert(!PortalIcons.ValidPng(null)&&!PortalIcons.ValidPng(new byte[23])&&!PortalIcons.ValidPng(new byte[PortalIcons.MaximumImageBytes+1]),"Truncated and oversized images must be rejected before decoding");
        byte[] bad=File.ReadAllBytes("src/Menu/Assets/PortalIcons/blue.png");bad[16]=255;
        Assert(!PortalIcons.ValidPng(bad),"Oversized decoded dimensions must be rejected before allocating a texture");
        PortalIcons.Release();
        Assert(images.All(sprite=>sprite.Destroyed&&sprite.texture.Destroyed),"Unload must destroy every owned portal sprite and texture");
        PortalIcons.Release();Assert(images.All(sprite=>sprite.Destroyed),"Repeated portal cleanup must be harmless");
        loads=ImageConversion.LoadCalls;ImageConversion.FailNext=true;
        Assert(PortalIcons.Get(PortalCatalog.Find("blue"))==null&&PortalIcons.Get(PortalCatalog.Find("blue"))==null&&ImageConversion.LoadCalls==loads+1,"A decode failure must fall back without retrying every repaint");
        PortalIcons.Release();
        Assert(PortalIcons.Get(PortalCatalog.Find("blue"))!=null,"A fresh menu lifetime must recover after a previous thumbnail failure");
        PortalIcons.Release();
    }

    static void PortalPreviews()
    {
        Root.characterCollected=true;Root.navigationToggle=false;Screen.width=1100;Screen.height=1000;Root.WindowWidth=680;Root.WindowHeight=720;
        var previousButtons=Root.mainMenu.GetButtons();
        var previewButtons=previousButtons.ToList();previewButtons.Add(new Button(new NormalButton(Root.mainMenu,5,"TELEPORTER",()=>{})));Root.mainMenu.AddButtons(previewButtons);
        Root.menus[5]=new Teleporter();Event.current=new Event{type=EventType.Repaint};MenuUI.ResetWindow();MenuUI.NavigateTo(5);
        foreach(var view in new[]{new{Offset=0f,Name="portals-base"},new{Offset=440f,Name="portals-dlc"},new{Offset=1000f,Name="portals-hallowed"}})
        {
            Layout(Root.menus[5]).CurrentScrollPosition=new Vector2(0,view.Offset);
            GUI.StartCanvas();MenuUI.Draw();GUI.Save("artifacts/tests/previews/"+view.Name+".png");
        }
        MenuUI.Back();PortalIcons.Release();Root.mainMenu.AddButtons(previousButtons);
    }
}