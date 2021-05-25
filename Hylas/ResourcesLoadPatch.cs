using System;
using Harmony;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hylas
{
    public static class ResourcesLoadPatch
    {

        public static void Patch(HarmonyInstance harmony)
        {
            var original = typeof(Resources).GetMethod("Load", new[] { typeof(string), typeof(Il2CppSystem.Type) });
            harmony.Patch(original, prefix: new HarmonyMethod(typeof(ResourcesLoadPatch).GetMethod(nameof(Prefix))));
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once RedundantAssignment
        public static bool Prefix(ref Object __result, string path, Il2CppSystem.Type systemTypeInstance)
        {
            MelonDebug.Msg($"Resources::Load({path}, {systemTypeInstance.Name})");

            var exist = GoCache.TryGet(path, out __result);
            if (exist)
                MelonLogger.Msg($"Resources:({__result.name} {__result.GetInstanceID()})");

            return !exist;
        }
    }
}
