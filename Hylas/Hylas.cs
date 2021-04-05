using System;
using System.Linq;
using System.Reflection;
using Harmony;
using System.IO;
using MelonLoader;
using UnityEngine;
using Newtonsoft.Json;

namespace Hylas
{

#pragma warning disable 649
    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable InconsistentNaming

    /* Avoid serialize issue caused by unhollower */
    internal struct Rect
    {
        public Vector2 position;
        public Vector2 size;

        public static implicit operator UnityEngine.Rect(Rect v)
        {
            return new UnityEngine.Rect(v.position, v.size);
        }
    }

    internal struct Vector2
    {
        public float x;
        public float y;

        public static implicit operator UnityEngine.Vector2(Vector2 v)
        {
            return new UnityEngine.Vector2(v.x, v.y);
        }
    }
    internal struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static implicit operator UnityEngine.Vector4(Vector4 v)
        {
            return new UnityEngine.Vector4(v.x, v.y, v.z, v.w);
        }
    }

    internal struct SpriteParam
    {
        public Rect rect; 
        public Vector2 pivot; 
        public float pixelsPerUnit; 
        public uint extrude; 
        public SpriteMeshType meshType; 
        public Vector4 border;
        public bool generateFallbackPhysicsShape;
    }
    // ReSharper restore MemberCanBePrivate.Global
    // ReSharper restore UnassignedField.Global
    // ReSharper restore InconsistentNaming
#pragma warning restore 649


    public class Hylas: MelonMod
    {
        public override void OnApplicationStart()
        {
            var load = typeof(ResMgr).GetMethods().First(m => !m.IsGenericMethod && m.Name == "Load");
            var pre = typeof(Hylas).GetMethod(nameof(PreResLoad), BindingFlags.Public | BindingFlags.Static);
            Harmony.Patch(load, new HarmonyMethod(pre));
        }
        
        // ReSharper disable once InconsistentNaming
        public static bool PreResLoad(ref UnityEngine.Object __result, string path)
        {
            MelonLogger.Msg(path);

            // Can't use base method directly because of the missing of reverse patch.
            // FIXME: There is no guarantee `Load<T>` won't use `Load` internally
            var product = Worker.Pick(path, g.res.Load<GameObject>)?.Produce();

            if (product == null) return true;

            __result = product;

            return false;
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
            var imagePath = Path.Combine(path, "image.png");
            var paramPath = Path.Combine(path, "sprite.json");
            
            var param = JsonConvert.DeserializeObject<SpriteParam>(File.ReadAllText(paramPath));
            
            var image = File.ReadAllBytes(imagePath);

            // LoadImage will replace with with incoming image size.
            var tex = new Texture2D(100, 100);

            if (!ImageConversion.LoadImage(tex, image))
            {
                throw new InvalidOperationException();
            }

            var newSprite = Sprite.Create(tex, param.rect, param.pivot, param.pixelsPerUnit, param.extrude, param.meshType, param.border, param.generateFallbackPhysicsShape);

            renderer.sprite = newSprite;
        }
    }
}
