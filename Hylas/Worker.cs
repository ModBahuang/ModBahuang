using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MelonLoader;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace Hylas
{
    internal abstract class Worker
    {
        protected string resPath;

        private static readonly Dictionary<Regex, Type> _worker_type = new Dictionary<Regex, Type>();

        static Worker()
        {
            _worker_type.Add(new Regex("^Game/Portrait/.*$"), typeof(ProtraitWorker));
            _worker_type.Add(new Regex("^Battle/Human/.*$"), typeof(BattleHumanWorker));
            _worker_type.Add(new Regex("^Effect/UI/.*$"), typeof(ImageCommonWorker));
            _worker_type.Add(new Regex("^Texture/Fortuitous/.*$"), typeof(SpriteWorker));
            _worker_type.Add(new Regex("^Texture/BG/.*$"), typeof(SpriteWorker));
        }

        public virtual string TemplatePath => resPath;

        protected virtual Func<string, string> MapPath => s => s;

        protected string AbsolutelyPhysicalPath => Path.GetFullPath(Path.Combine(Utils.GetHylasHome(), MapPath(resPath)));

        public virtual Il2CppSystem.Type Type
        {
            get
            {
                return Il2CppType.Of<GameObject>();
            }
        }

        public abstract T Rework<T>(T template) where T : UnityEngine.Object;

        public static Worker Pick(string path)
        {
            Worker worker = null;
            foreach (var key in _worker_type.Keys)
            {
                if (key.IsMatch(path))
                {
                    worker = (Worker)Activator.CreateInstance(_worker_type[key]);
                    break;
                }
            }
            if (worker == null)
            {
                worker = new ImageCommonWorker();
            }
            worker.resPath = path;
            return worker;

        }
    }

    internal class GameObjectWorker : Worker
    {

        public override T Rework<T>(T t)
        {
            GameObject template = t.Cast<GameObject>();
            var renderer = template.GetComponentInChildren<SpriteRenderer>();
            renderer.LoadCustomSprite(AbsolutelyPhysicalPath);

            return template.Cast<T>();
        }
    }

    internal class ImageCommonWorker : GameObjectWorker
    {
    }

    internal class ProtraitWorker : GameObjectWorker
    {

        private readonly Regex pathPattern = new Regex("^(.+/)[0-9]{3,}(/[^/]+|$)$");

        public override string TemplatePath
        {
            get
            {
                var templateId = "101";

                var match = pathPattern.Match(resPath);

                var root = match.Groups[1].Value;
                var templateConfig = Path.Combine(Utils.GetHylasHome(), MapPath(root), ".template.txt");

                if (File.Exists(templateConfig))
                {
                    templateId = File.ReadAllText(templateConfig);
                    MelonDebug.Msg($"{templateConfig}: {templateId}");
                }

                return root + templateId + match.Groups[2].Value;
            }
        }
        protected override Func<string, string> MapPath => s => s.Replace("Game/Portrait/", "");
    }

    internal class BattleHumanWorker : GameObjectWorker
    {

        public override T Rework<T>(T t)
        {
            GameObject template = t.Cast<GameObject>();
            var (param, image) = AbsolutelyPhysicalPath.LoadSprite();

            var sprite = template.GetComponentInChildren<SpriteRenderer>().sprite;
            if (!ImageConversion.LoadImage(sprite.texture, image))
            {
                throw new InvalidOperationException();
            }
            sprite.rect.Set(param.rect.position.x, param.rect.position.y, param.rect.size.x, param.rect.size.y);
            sprite.textureRect.Set(param.rect.position.x, param.rect.position.y, param.rect.size.x, param.rect.size.y);
            sprite.pivot.Set(param.pivot.x, param.pivot.y);
            sprite.border.Set(param.border.x, param.border.y, param.border.z, param.border.w);

            return template.Cast<T>();
        }
    }

    internal class SpriteWorker : Worker
    {
        public override Il2CppSystem.Type Type
        {
            get
            {
                return Il2CppType.Of<Sprite>();
            }
        }

        public override T Rework<T>(T t)
        {
            Sprite sprite = t.Cast<Sprite>();
            MelonLogger.Msg($"sprite:({sprite.name} {sprite.texture})");

            var (param, image) = AbsolutelyPhysicalPath.LoadSprite();

            if (!ImageConversion.LoadImage(sprite.texture, image))
            {
                throw new InvalidOperationException();
            }
            sprite.rect.Set(param.rect.position.x, param.rect.position.y, param.rect.size.x, param.rect.size.y);
            sprite.textureRect.Set(param.rect.position.x, param.rect.position.y, param.rect.size.x, param.rect.size.y);
            sprite.pivot.Set(param.pivot.x, param.pivot.y);
            sprite.border.Set(param.border.x, param.border.y, param.border.z, param.border.w);

            return sprite.Cast<T>();
        }
    }
}
