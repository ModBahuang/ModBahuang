using System;
using Harmony;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hylas
{
    [HarmonyPatch(typeof(ResMgr), "LoadAsync")]
    public class ResLoadAsyncPatch
    {
        public static bool Prefix(ref string path, Action<Object> call)
        {
            MelonDebug.Msg($"ResMgr::LoadAsync({path})");
            
            var exist = GoCache.TryGet(path, out var go);

            if (!exist) return true;

            // It's a workaround for `call` that used in `Wrapper` got freed in Il2cpp domain
            // I have no idea why/how this could work. But, though, anyway, it just WORKS.
            // A reason might be the `native` will keep a gc handle to prevent `call` from freeing.
            Il2CppSystem.Action<Object> native = call;
            
            native.Invoke(go);

            return false;
        }
    }
}
