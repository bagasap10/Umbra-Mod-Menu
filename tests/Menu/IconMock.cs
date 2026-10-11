namespace UnityEngine
{
    public class Texture : Object { }
    public struct Vector4 {public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}}
    public class Sprite : Object {public Texture2D texture;public Vector4 UV=new Vector4(0,0,1,1);}
}
namespace UnityEngine.Sprites {public static class DataUtility {public static UnityEngine.Vector4 GetOuterUV(UnityEngine.Sprite sprite)=>sprite.UV;}}
namespace UnityEngine.ResourceManagement.AsyncOperations
{
    public enum AsyncOperationStatus {None,Succeeded,Failed}
    public class Operation<T> {public AsyncOperationStatus Status;public T Result;public bool Valid=true;}
    public struct AsyncOperationHandle<T> {public Operation<T> State;public bool IsValid()=>State!=null&&State.Valid;public AsyncOperationStatus Status=>State.Status;public T Result=>State.Result;}
}
namespace UnityEngine.AddressableAssets
{
    using UnityEngine.ResourceManagement.AsyncOperations;
    public class AssetReferenceT<T> {public object RuntimeKey;public bool RuntimeKeyIsValid()=>RuntimeKey!=null;}
    public static class Addressables
    {
        public static int LoadCalls,ReleaseCalls;
        public static readonly System.Collections.Generic.Dictionary<object,Operation<UnityEngine.Sprite>> Operations=new System.Collections.Generic.Dictionary<object,Operation<UnityEngine.Sprite>>();
        public static AsyncOperationHandle<T> LoadAssetAsync<T>(object key){LoadCalls++;var state=new Operation<UnityEngine.Sprite>();Operations[key]=state;return new AsyncOperationHandle<T>{State=(Operation<T>)(object)state};}
        public static void Release<T>(AsyncOperationHandle<T> handle){ReleaseCalls++;handle.State.Valid=false;}
    }
}
