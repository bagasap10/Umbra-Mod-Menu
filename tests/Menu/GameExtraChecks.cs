using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UmbraMenu;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine
{
    public partial class Object
    {
        public bool Destroyed;
        public static implicit operator bool(Object value) { return value!=null && !value.Destroyed; }
        public static bool operator !(Object value) { return !(bool)value; }
        public static T[] FindObjectsOfType<T>() { return new T[0]; }
        public static GameObject Instantiate(GameObject prefab,Vector3 position,Quaternion rotation) { var instance=new GameObject(prefab.name+"(Clone)"); instance.Components.AddRange(prefab.Components); instance.transform.position=position; return instance; }
    }
    public class Transform : Object { public Vector3 position; }
    public class GameObject : Object
    {
        static int nextId; readonly int id=++nextId; public string name; public Transform transform=new Transform(); public readonly List<MonoBehaviour> Components=new List<MonoBehaviour>();
        public GameObject(string name="Object") { this.name=name; }
        public int GetInstanceID() { return id; } public T GetComponent<T>() where T:class { return Components.OfType<T>().FirstOrDefault(); }
    }
    public class MonoBehaviour : Object
    {
        public GameObject gameObject; public bool isActiveAndEnabled=true; public Transform transform { get { return gameObject.transform; } }
        public MonoBehaviour() { gameObject=new GameObject(); gameObject.Components.Add(this); }
        public T GetComponent<T>() where T:class { return gameObject.GetComponent<T>(); } public T[] GetComponents<T>() { return gameObject.Components.OfType<T>().ToArray(); }
    }
    public struct Vector3
    {
        public float x,y,z; public Vector3(float x,float y,float z) { this.x=x; this.y=y; this.z=z; } public static Vector3 one { get { return new Vector3(1,1,1); } }
        public static Vector3 operator +(Vector3 a,Vector3 b) { return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); }
        public static Vector3 operator -(Vector3 a,Vector3 b) { return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); }
        public static Vector3 operator *(Vector3 a,float b) { return new Vector3(a.x*b,a.y*b,a.z*b); }
    }
    public struct Quaternion { public static Quaternion identity { get { return new Quaternion(); } } }
    public struct Bounds
    {
        public Vector3 center,size; public Bounds(Vector3 center,Vector3 size) { this.center=center; this.size=size; } public Vector3 extents { get { return size*0.5f; } }
        public void Encapsulate(Bounds other) { var min=center-extents; var max=center+extents; var omin=other.center-other.extents; var omax=other.center+other.extents; min=new Vector3(Math.Min(min.x,omin.x),Math.Min(min.y,omin.y),Math.Min(min.z,omin.z)); max=new Vector3(Math.Max(max.x,omax.x),Math.Max(max.y,omax.y),Math.Max(max.z,omax.z)); center=(min+max)*0.5f; size=max-min; }
    }
    public class Collider : MonoBehaviour { public bool enabled=true; public Bounds bounds; }
    public class Camera : Object
    {
        public Transform transform=new Transform(); public float nearClipPlane=0.1f; public float FocalLength=600;
        public Vector3 WorldToScreenPoint(Vector3 world) { var p=world-transform.position; return new Vector3(Screen.width*0.5f+FocalLength*p.x/p.z,Screen.height*0.5f+FocalLength*p.y/p.z,p.z); }
    }

    public static class Debug { public static void Log(object message) {} public static void LogWarning(object message) {} }
}
namespace UnityEngine.Networking
{
    public class NetworkIdentity : MonoBehaviour {}
    public static class NetworkServer { public static bool active=true; public static List<GameObject> Spawned=new List<GameObject>(); public static void Spawn(GameObject go) { Spawned.Add(go); } }
}
namespace UnityEngine.ResourceManagement.AsyncOperations
{
    public enum AsyncOperationStatus { None,Succeeded,Failed }
    public class State<T> { public T Result; public bool Done; public AsyncOperationStatus Status; public Action<AsyncOperationHandle<T>> Callbacks; }
    public struct AsyncOperationHandle<T>
    {
        public State<T> State; public bool IsValid()=>State!=null; public bool IsDone { get { return State.Done; } } public T Result { get { return State.Result; } } public AsyncOperationStatus Status { get { return State.Status; } }
        public event Action<AsyncOperationHandle<T>> Completed { add { State.Callbacks+=value; } remove { State.Callbacks-=value; } }
    }
}
namespace UnityEngine.AddressableAssets
{
    public static partial class Addressables
    {
        public static readonly Dictionary<string,State<GameObject>> Loads=new Dictionary<string,State<GameObject>>(); public static readonly Dictionary<string,int> LoadCount=new Dictionary<string,int>(); public static int ReleaseCount;
        public static AsyncOperationHandle<T> LoadAssetAsync<T>(string address) { var state=new State<T>(); Loads[address]=(State<GameObject>)(object)state; LoadCount[address]=LoadCount.ContainsKey(address)?LoadCount[address]+1:1; return new AsyncOperationHandle<T> { State=state }; }
        public static void Release<T>(AsyncOperationHandle<T> operation) { ReleaseCount++; }
        public static void Complete(string address,GameObject result,bool success=true) { var state=Loads[address]; state.Result=result; state.Status=success?AsyncOperationStatus.Succeeded:AsyncOperationStatus.Failed; state.Done=true; var callback=state.Callbacks; state.Callbacks=null; callback?.Invoke(new AsyncOperationHandle<GameObject> { State=state }); }
    }
}
namespace RoR2
{
    public enum TeamIndex { None=-1,Neutral,Player,Monster,Lunar,Void }
    public struct TeamMask { int mask; public static TeamMask allButNeutral { get { return new TeamMask { mask=30 }; } } public void RemoveTeam(TeamIndex team) { mask&=~(1<<(int)team); } public bool HasTeam(TeamIndex team) { return team>=0 && (mask&(1<<(int)team))!=0; } }
    public class TeamComponent : MonoBehaviour { public TeamIndex teamIndex; }
    public class HealthComponent : MonoBehaviour { public bool alive=true; }
    public class CharacterBody : MonoBehaviour { public static readonly List<CharacterBody> readOnlyInstancesList=new List<CharacterBody>(); public TeamComponent teamComponent; public HealthComponent healthComponent; public HurtBoxGroup hurtBoxGroup; public HurtBox mainHurtBox; public float radius=1; public Vector3 corePosition { get { return transform.position; } } }
    public class HurtBox : MonoBehaviour { public Collider collider; public HealthComponent healthComponent; }
    public class HurtBoxGroup : MonoBehaviour { public HurtBox[] hurtBoxes; }
    public enum Interactability { Disabled,ConditionsNotMet,Available }
    public interface IInteractable { Interactability GetInteractability(Interactor interactor); string GetContextString(Interactor interactor); }
    public interface IDisplayNameProvider { string GetDisplayName(); }
    public class Interactor : MonoBehaviour {}
    public class GenericPickupController : TestInteraction {}
    public class PurchaseInteraction : TestInteraction { public bool available=true; }
    public class BarrelInteraction : TestInteraction {}
    public class ScrapperController : MonoBehaviour {}
    public class PressurePlateController : MonoBehaviour {}
    public class TestInteraction : MonoBehaviour,IInteractable { public Interactability State=Interactability.Available; public Interactability GetInteractability(Interactor i) { return State; } public string GetContextString(Interactor i) { return "Interact"; } }
    public class NewHallowedInteraction : TestInteraction,IDisplayNameProvider { public string GetDisplayName() { return "Altar of N'kuhana"; } }
    public class HoldoutZoneController : MonoBehaviour { public float baseChargeDuration=90; }
    public class TeleporterInteraction : TestInteraction
    {
        public static TeleporterInteraction instance; public HoldoutZoneController holdoutZoneController=new HoldoutZoneController(); public bool shouldAttemptToSpawnShopPortal,Network_shouldAttemptToSpawnShopPortal,shouldAttemptToSpawnGoldshoresPortal,Network_shouldAttemptToSpawnGoldshoresPortal,shouldAttemptToSpawnMSPortal,Network_shouldAttemptToSpawnMSPortal; public int shrineBonusStacks; public void AddShrineStack() { shrineBonusStacks++; }
    }
    public class Run : MonoBehaviour { public static Run instance; public object nextStageScene; public void AdvanceStage(object scene) {} }
}
partial class Checks
{
    static void PortalChecks()
    {
        var keys=new HashSet<string>(File.ReadAllLines("tests/fixtures/portal-addresses.txt"));
        Assert(PortalCatalog.Portals.Length==21,"The portal menu must contain the current base/DLC portals, including five Hallowed routes.");
        Assert(PortalCatalog.Portals.Select(p=>p.Key).Distinct().Count()==21,"Portal keys must be unique.");
        foreach(var portal in PortalCatalog.Portals)
        {
            Assert(keys.Contains(portal.Address),"Portal address must exist in the installed game: "+portal.Address);
            Assert(portal.Address.StartsWith(portal.Expansion==0?"RoR2/Base/":"RoR2/DLC"+portal.Expansion+"/"),"The portal's DLC divider must match its actual asset location: "+portal.Key);
        }
        Assert(PortalCatalog.Find("ANIMA").Expansion==4 && PortalCatalog.Find("missing")==null,"Portal lookup must tolerate key case and safely reject unknown choices.");
        Assert(PortalCatalog.Find("null").Expansion==0 && PortalCatalog.Find("infinite").Expansion==1 && PortalCatalog.Find("null").Address!=PortalCatalog.Find("infinite").Address,"Null Arena must stay in the base-game section and Infinite Tower must use its own DLC1 prefab.");
        var menu=PlainMenu(5); var calls=new List<string>(); var rows=Enumerable.Range(1,4).Select(id=>new Button(new NormalButton(menu,id,"Action",()=>{}))).ToList(); PortalCatalog.AddButtons(menu,rows,calls.Add); menu.AddButtons(rows); UmbraMenu.UmbraMenu.menus[5]=menu;
        Assert(rows.Select(b=>b.GetId()).SequenceEqual(Enumerable.Range(1,rows.Count)),"Portal actions and DLC headings must have contiguous row positions.");
        Assert(rows.Where(b=>b.GetAction()==null).Select(b=>Row(b).Text).SequenceEqual(new[] { "Normal / Not-DLC","DLC#1 / Survivors of the Void","DLC#2 / Seekers of the Storm","DLC#3 / Alloyed Collective","DLC#4 / Hallowed Concepts" }),"Portal dividers must appear in base-game then DLC1-DLC4 order.");
        foreach(var button in rows.Skip(4).Where(b=>b.GetAction()!=null)) button.GetAction()();
        Assert(calls.SequenceEqual(PortalCatalog.Portals.Select(p=>p.Key)),"Every sorted portal action must still use the matching definition.");
        UmbraMenu.UmbraMenu.navigationToggle=true; Navigation.menuIndex=5; Navigation.buttonIndex=5; UmbraMenu.UmbraMenu.scrolled=false; Navigation.UpdateIndexValues();
        Assert(Navigation.buttonIndex==6,"Portal keyboard navigation must skip the DLC divider.");
        int initial=calls.Count; Navigation.PressBtn(5,5); Assert(calls.Count==initial,"DLC dividers must remain nonclickable through keyboard navigation.");
        UmbraMenu.UmbraMenu.navigationToggle=false;
    }
    static CharacterBody Body(TeamIndex team,bool alive=true) { var body=new CharacterBody { teamComponent=new TeamComponent { teamIndex=team },healthComponent=new HealthComponent { alive=alive } }; CharacterBody.readOnlyInstancesList.Add(body); return body; }
    static void EspChecks()
    {
        CharacterBody.readOnlyInstancesList.Clear(); var player=Body(TeamIndex.Player); var friend=Body(TeamIndex.Player); var neutral=Body(TeamIndex.Neutral); var dead=Body(TeamIndex.Monster,false); var monster=Body(TeamIndex.Monster); var lunar=Body(TeamIndex.Lunar); var voidMob=Body(TeamIndex.Void); var noTeam=Body(TeamIndex.None); var destroyed=Body(TeamIndex.Monster); destroyed.Destroyed=true;
        monster.gameObject.name="BrandNewDLCEnemy(Clone)"; monster.transform.position=new Vector3(100,0,500);
        var enemies=EspTargets.GetEnemies(player);
        Assert(enemies.SequenceEqual(new[] { monster,lunar,voidMob }),"Enemy detection must include unknown DLC names/all hostile teams, beyond the old distance/cone, while excluding friends, neutrals, corpses and destroyed bodies.");
        Assert(EspTargets.GetEnemies(null).Count==0 && EspTargets.GetEnemies(new CharacterBody()).Count==0,"Missing player/team data must return an empty list safely.");
        var newEnemy=Body(TeamIndex.Monster); Assert(EspTargets.GetEnemies(player).Contains(newEnemy),"Newly spawned enemies must appear without updating a whitelist."); monster.healthComponent.alive=false; Assert(!EspTargets.GetEnemies(player).Contains(monster),"Enemies must disappear as soon as they die.");
        var body=Body(TeamIndex.Monster); body.transform.position=new Vector3(0,1,10);
        var center=new Collider { bounds=new Bounds(new Vector3(0,1,10),new Vector3(2,4,2)) }; var extremity=new Collider { bounds=new Bounds(new Vector3(2,1,10),new Vector3(1,1,1)) };
        var foreign=new Collider { bounds=new Bounds(new Vector3(100,100,100),new Vector3(100,100,100)) }; var disabled=new Collider { enabled=false,bounds=foreign.bounds };
        body.hurtBoxGroup=new HurtBoxGroup { hurtBoxes=new[] { new HurtBox { collider=center,healthComponent=body.healthComponent },new HurtBox { collider=extremity,healthComponent=body.healthComponent },new HurtBox { collider=foreign,healthComponent=new HealthComponent() },new HurtBox { collider=disabled,healthComponent=body.healthComponent },null } };
        var bounds=EspTargets.GetBodyBounds(body); Assert(bounds.size.x==3.5f && bounds.size.y==4 && bounds.size.z==2,"Enemy boxes must use only the body's active damage colliders, without foreign or disabled effects inflating them.");
        body.hurtBoxGroup=null; body.mainHurtBox=new HurtBox { collider=center }; Assert(EspTargets.GetBodyBounds(body).size.y==4,"A standalone main hurtbox must remain usable."); body.mainHurtBox=null; body.radius=2; Assert(EspTargets.GetBodyBounds(body).size.x==4,"Bodies without ready hurtboxes must fall back to their actual radius.");
        var camera=new Camera(); float previous=float.PositiveInfinity;
        foreach(float distance in new[] { 5f,10f,20f,50f,100f,250f,500f })
        {
            Assert(EspTargets.TryGetScreenRect(camera,new Bounds(new Vector3(0,0,distance),new Vector3(2,4,2)),out Rect rect),"In-front enemies must produce screen boxes at distance "+distance);
            Assert(rect.width<previous && rect.height>rect.width,"An equal-sized enemy must have a smaller box farther away, retaining its actual proportions."); previous=rect.width;
        }
        Assert(!EspTargets.TryGetScreenRect(null,bounds,out Rect ignored),"Missing cameras must not throw or retain stale boxes.");
        Assert(!EspTargets.TryGetScreenRect(camera,new Bounds(new Vector3(0,0,-10),new Vector3(2,4,2)),out ignored),"Enemies behind the camera must not draw boxes.");
        Assert(!EspTargets.TryGetScreenRect(camera,new Bounds(new Vector3(0,0,0.5f),new Vector3(2,4,2)),out ignored),"Bounds intersecting the near plane must not create huge/inverted boxes.");
        Assert(!EspTargets.TryGetScreenRect(camera,new Bounds(new Vector3(100,0,10),new Vector3(2,4,2)),out ignored),"Entirely off-screen bounds must not clutter the overlay.");
        Assert(EspTargets.TryGetScreenRect(camera,new Bounds(new Vector3(-15,0,10),new Vector3(4,4,2)),out Rect edge) && edge.x==0 && edge.width>0,"A partially visible enemy must retain a clipped positive box.");
        var altar=new NewHallowedInteraction(); var second=new TestInteraction(); second.gameObject=altar.gameObject; altar.gameObject.Components.Add(second); var ordinary=new MonoBehaviour(); var pickup=new GenericPickupController(); var tele=new TeleporterInteraction(); var purchase=new PurchaseInteraction(); var barrel=new BarrelInteraction(); var inactive=new TestInteraction { isActiveAndEnabled=false }; var gone=new TestInteraction { Destroyed=true };
        var dual=new NewHallowedInteraction(); var gatedPurchase=new PurchaseInteraction { available=false }; gatedPurchase.gameObject=dual.gameObject; dual.gameObject.Components.Add(gatedPurchase);
        var targets=EspTargets.GetAdditionalInteractables(new MonoBehaviour[] { altar,second,altar,ordinary,pickup,tele,purchase,barrel,inactive,gone,dual,gatedPurchase,null });
        Assert(targets.Count==3 && targets.Any(t=>t.Component==altar) && targets.Any(t=>t.Component==dual),"DLC interface implementations must be discovered without name/type whitelists, including objects with an unavailable purchase component.");
        Assert(targets.First(t=>t.Component==altar).NameProvider.GetDisplayName()=="Altar of N'kuhana","New interactables must use their localized display-name provider.");
        Assert(targets.Count(t=>t.Component==altar)==1 && targets.Any(t=>t.Component==second),"Duplicate scans of a component must collapse while distinct interactions on one object remain available for runtime state checks.");
        var later=new NewHallowedInteraction(); Assert(EspTargets.GetAdditionalInteractables(new[] { later }).Count==1,"Late-spawned DLC interactables must be discoverable on the next refresh.");
    }
    static GameObject Prefab(string name) { var prefab=new GameObject(name); prefab.Components.Add(new NetworkIdentity()); return prefab; }
    static void PortalSpawnChecks()
    {
        UmbraMenu.UmbraMenu.characterCollected=true; UmbraMenu.UmbraMenu.InGame=true; UmbraMenu.UmbraMenu.navigationToggle=false; UmbraMenu.UmbraMenu.LocalPlayerBody=new CharacterBody(); Run.instance=new Run(); TeleporterInteraction.instance=new TeleporterInteraction();
        var main=UmbraMenu.UmbraMenu.menus[0]; main.AddButtons(new List<Button> { new Button(new NormalButton(main,5,"TELEPORTER",()=>{})) }); var menu=new UmbraMenu.Menus.Teleporter(); UmbraMenu.UmbraMenu.menus[5]=menu;
        Assert(menu.GetButtons().Count==30 && menu.GetButtons().Count(b=>b.GetAction()!=null)==25,"The complete Teleporter menu must have four stage actions, 21 portals, and five DLC dividers.");
        menu.SetEnabled(true); Event.current=new Event { type=EventType.Repaint }; menu.Draw(); Assert(GUI.Viewport.y==0 && GUI.Viewport.y+GUI.Viewport.height<=Screen.height,"The scrollable portal viewport must leave the title fixed and fit on screen.");
        menu.SpawnPortals("blue"); menu.SpawnPortals("gold"); menu.SpawnPortals("cele"); var tele=TeleporterInteraction.instance;
        Assert(tele.Network_shouldAttemptToSpawnShopPortal && tele.Network_shouldAttemptToSpawnGoldshoresPortal && tele.Network_shouldAttemptToSpawnMSPortal,"The three standard portal shortcuts must preserve their teleporter orb/charge behavior.");
        var anima=PortalCatalog.Find("anima"); int before=NetworkServer.Spawned.Count; menu.SpawnPortals("anima"); Assert(Addressables.LoadCount[anima.Address]==1 && NetworkServer.Spawned.Count==before,"Portal prefabs must load asynchronously without spawning prematurely."); Addressables.Complete(anima.Address,Prefab("Anima")); Assert(NetworkServer.Spawned.Count==before+1,"A loaded Hallowed portal must be spawned through the network API."); Time.unscaledTime+=1.1f; menu.SpawnPortals("anima"); Assert(Addressables.LoadCount[anima.Address]==1 && NetworkServer.Spawned.Count==before+2,"Repeated portal requests must reuse the loaded asset and still create a portal each click.");
        NetworkServer.active=false; before=NetworkServer.Spawned.Count; menu.SpawnPortals("anima"); Assert(NetworkServer.Spawned.Count==before,"Non-host clients must not create unsynchronized portals."); NetworkServer.active=true;
        var ritual=PortalCatalog.Find("ritual"); menu.SpawnPortals("ritual"); tele.Destroyed=true; tele.transform.Destroyed=true; Addressables.Complete(ritual.Address,Prefab("Ritual")); Assert(NetworkServer.Spawned.Count==before,"A pending request must be canceled when its original stage anchor is destroyed.");
        var mortal=PortalCatalog.Find("mortal"); menu.SpawnPortals("mortal"); menu.SpawnPortals("mortal"); Addressables.Complete(mortal.Address,null,false); Assert(Addressables.ReleaseCount==1,"Concurrent failed requests must release a shared failed load only once."); Time.unscaledTime+=1.1f; menu.SpawnPortals("mortal"); Assert(Addressables.LoadCount[mortal.Address]==2,"A failed portal asset must be retryable."); Addressables.Complete(mortal.Address,Prefab("Mortal")); Assert(NetworkServer.Spawned.Count==before+1,"Portal spawning must work near the player when a stage has no teleporter.");
        var veil=PortalCatalog.Find("veil"); menu.SpawnPortals("veil"); Run.instance=new Run(); Addressables.Complete(veil.Address,Prefab("Veil")); Assert(NetworkServer.Spawned.Count==before+1,"A load completing in another run must not place a stale portal there.");
        before=NetworkServer.Spawned.Count; menu.SpawnPortals("missing"); Assert(NetworkServer.Spawned.Count==before,"Unknown portal choices must be safe."); menu.SetEnabled(false);
    }
}


