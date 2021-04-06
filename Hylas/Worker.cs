using System;
using System.IO;
using System.Text.RegularExpressions;
using MelonLoader;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hylas
{
    internal abstract class Worker
    {
        protected string Path;

        private Func<string, GameObject> load;

        private string TemplatePath => Regex.Replace(Path, "^(.+/)[0-9]+(/?)", "${1}101$2");

        protected virtual string RelativePhysicalPath => Path;

        protected string AbsolutelyPhysicalPath => System.IO.Path.Combine(MelonUtils.GameDirectory, "Mods", nameof(Hylas), RelativePhysicalPath);

        protected abstract GameObject Produce(GameObject template);

        public GameObject Produce()
        {
            var template = load(Path) ?? load(TemplatePath);
            
            return Produce(Object.Instantiate(template).Cast<GameObject>());
        }

        public static Worker Pick(string path, Func<string, GameObject> load)
        {
            Worker worker = null;
            if (path.IsPortrait())
            {
                worker = new ProtraitWorker
                {
                    Path = path,
                    load = load
                };
            }
            else if (path.IsBattleHuman())
            {
                worker = new BattleHumanWorker
                {
                    Path = path,
                    load = load
                };
            }

            return worker?.AbsolutelyPhysicalPath.Exist() == true ? worker : null;
            
        }
    }

    internal class ProtraitWorker : Worker
    {
        protected override string RelativePhysicalPath => Path.Replace("Game/Portrait/", "");

        protected override GameObject Produce(GameObject template)
        {
            var renderer = template.GetComponentInChildren<SpriteRenderer>();
            renderer.LoadCustomSprite(AbsolutelyPhysicalPath);

            return template;
        }
    }

    internal class BattleHumanWorker : Worker
    {
        protected override GameObject Produce(GameObject template)
        {
            var (param, image) = AbsolutelyPhysicalPath.LoadSprite();

            var sprite = template.GetComponent<SpriteRenderer>().sprite;
            ImageConversion.LoadImage(sprite.texture, image);
            sprite.rect.Set(param.rect.position.x, param.rect.position.y, param.rect.size.x, param.rect.size.y);
            sprite.textureRect.Set(param.rect.position.x, param.rect.position.y, param.rect.size.x, param.rect.size.y);
            sprite.pivot.Set(param.pivot.x, param.pivot.y);
            sprite.border.Set(param.border.x, param.border.y, param.border.z, param.border.w);

            return template;
        }
    }
}
