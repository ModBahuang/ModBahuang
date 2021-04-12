using System;
using System.Diagnostics;
using System.IO;
using MelonLoader;
using UnityEngine;
using Newtonsoft.Json;

namespace Hylas
{
    public class Hylas : MelonMod
    {
        public override void OnApplicationStart()
        {
            ResourcesLoadPatch.Patch(Harmony);
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex != 0) return;
            var sw = new Stopwatch();
            MelonLogger.Msg("Prefetch start...");
            sw.Start();
            GoCache.Prefetch();
            sw.Stop();
            MelonLogger.Msg($"Prefetch end, total: {sw.Elapsed}");
        }
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
