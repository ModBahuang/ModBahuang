using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using System.IO;
using MelonLoader;
using UnityEngine;
using Newtonsoft.Json;
using Object = UnityEngine.Object;

namespace Hylas
{
    internal static class GoCache
    {
        public static readonly GameObject Root;
        public static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>();

        static GoCache()
        {
            Root = new GameObject
            {
                name = "hylas cache",
                active = false
            };
            Object.DontDestroyOnLoad(Root);
        }
    }

    [HarmonyPatch]
    public class ResLoadPatch
    {
        public static MethodBase TargetMethod()
        {
            return typeof(ResMgr).GetMethods().First(m => !m.IsGenericMethod && m.Name == "Load");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(ref Object __result, string path)
        {
            MelonDebug.Msg(path);

            var worker = Worker.Pick(path);

            if (worker == null) return true;

            var tp = worker.TemplatePath;



            try
            {
                GameObject product;
                if (GoCache.Cache.ContainsKey(tp))
                {
                    product = worker.Rework(GoCache.Cache[tp]);
                }
                else
                {
                    var go = Object.Instantiate(Resources.Load<GameObject>(tp), GoCache.Root.transform);
                    GoCache.Cache.Add(tp, go);

                    product = worker.Rework(go);
                }

                __result = product;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"{e}\n{tp}");
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(ResMgr), "LoadAsync")]
    public class ResLoadAsyncPatch
    {
        public static bool Prefix(ref string path, Action<Object> call)
        {
            // Use mark as a workaround to resolve the original call.
            // Reverse patch needs to update the melonloader version.
            const string mark = "//?/";

            MelonDebug.Msg(path);

            if (path.StartsWith(mark))
            {
                path = path.Substring(mark.Length);
                return true;
            }

            var worker = Worker.Pick(path);

            if (worker == null) return true;

            var tp = worker.TemplatePath;

            // It's a workaround for `call` that used in `Wrapper` got freed in Il2cpp domain
            // I have no idea why/how this could work. But, though, anyway, it just WORKS.
            // A reason might be the `native` will keep a gc handle to prevent `call` from freeing.
            Il2CppSystem.Action<Object> native = call;

            void Wrapper(Object obj)
            {
                var p = new GameObject { active = false };
                var go = Object.Instantiate(obj.Cast<GameObject>(), p.transform);
                native.Invoke(worker.Rework(go));
                Object.Destroy(p);
            }

            if (GoCache.Cache.ContainsKey(tp))
            {
                native.Invoke(worker.Rework(GoCache.Cache[tp]));
            }
            else
            {
                var a = new Action<Object>(Wrapper);
                g.res.LoadAsync(mark + tp, a);
            }

            return false;
        }
    }

    public class Hylas : MelonMod
    {
    }

    internal static class Ext
    {
        public static bool IsPortrait(this string path)
        {
            return path.StartsWith("Game/Portrait/");
        }

        public static bool IsBattleHuman(this string path)
        {
            return path.StartsWith("Battle/Human/");
        }

        public static bool Exist(this string path)
        {
            return Directory.Exists(path);
        }

        public static void LoadCustomSprite(this SpriteRenderer renderer, string path)
        {
            var (param, image) = path.LoadSprite();

            // LoadImage will replace with with incoming image size.
            var tex = new Texture2D(100, 100, TextureFormat.ARGB32, false);

            if (!ImageConversion.LoadImage(tex, image))
            {
                throw new InvalidOperationException();
            }

            var newSprite = Sprite.Create(tex, param.rect, param.pivot, param.pixelsPerUnit, param.extrude, param.meshType, param.border, param.generateFallbackPhysicsShape);

            renderer.sprite = newSprite;
        }

        public static (SpriteParam, byte[]) LoadSprite(this string path)
        {
            var imagePath = Path.Combine(path, "image.png");
            var paramPath = Path.Combine(path, "sprite.json");

            var param = JsonConvert.DeserializeObject<SpriteParam>(File.ReadAllText(paramPath));

            var image = File.ReadAllBytes(imagePath);

            return (param, image);
        }
    }
}
