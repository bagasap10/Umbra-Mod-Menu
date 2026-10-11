namespace UnityEngine
{
    public class Texture : Object { }
    public struct Vector4 {public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}}
    public class Sprite : Object {public Texture2D texture;public Vector4 UV=new Vector4(0,0,1,1);}
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
