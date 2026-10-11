namespace UnityEngine
{
    public class Texture : Object { }
    public static class ImageConversion
    {
        public static int LoadCalls; public static bool LastMarkNonReadable,FailNext;
        public static bool LoadImage(Texture2D texture,byte[] bytes,bool markNonReadable)
        {
            LoadCalls++;LastMarkNonReadable=markNonReadable;
            if(FailNext){FailNext=false;return false;}
            try { using(var stream=new System.IO.MemoryStream(bytes))using(var image=System.Drawing.Image.FromStream(stream))texture.Image=new System.Drawing.Bitmap(image);return true; }
            catch(System.ArgumentException){return false;}
        }
    }
    public struct Vector4 {public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}}
    public enum SpriteMeshType { FullRect } public class Sprite : Object {public Texture2D texture;public Vector4 UV=new Vector4(0,0,1,1);public static Sprite Create(Texture2D texture,Rect rect,Vector2 pivot,float pixelsPerUnit,uint extrude,SpriteMeshType mesh)=>new Sprite{texture=texture};}
}
namespace UnityEngine.Sprites {public static class DataUtility {public static UnityEngine.Vector4 GetOuterUV(UnityEngine.Sprite sprite)=>sprite.UV;}}
namespace UnityEngine.AddressableAssets {
using UnityEngine.ResourceManagement.AsyncOperations;
public class AssetReferenceT<T>{public object RuntimeKey;public bool RuntimeKeyIsValid()=>RuntimeKey!=null;}
public static partial class Addressables {
public static int IconLoadCalls;
public static readonly System.Collections.Generic.Dictionary<object,State<UnityEngine.Sprite>> IconOperations=new System.Collections.Generic.Dictionary<object,State<UnityEngine.Sprite>>();
public static AsyncOperationHandle<T> LoadAssetAsync<T>(object key){IconLoadCalls++;var state=new State<UnityEngine.Sprite>();IconOperations[key]=state;return new AsyncOperationHandle<T>{State=(State<T>)(object)state};}
}}
