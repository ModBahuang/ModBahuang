using System;
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
            MelonLogger.Msg(path);

            var worker = Worker.Pick(path);

            if (worker == null) return true;

            var product = worker.Rework(Object.Instantiate(Resources.Load<GameObject>(worker.TemplatePath)));

            __result = product;

            return false;
        }
    }

    [HarmonyPatch(typeof(ResMgr), "LoadAsync")]
    public class ResLoadAsyncPatch
    {
        public static void Prefix(ref string path, ref Action<Object> call)
        {
            MelonLogger.Msg(path);

            var worker = Worker.Pick(path);

            if (worker == null) return;

            call = obj => worker.Rework(Object.Instantiate(obj.Cast<GameObject>()));
            path = worker.TemplatePath;

        }
    }

    public class Hylas: MelonMod
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
