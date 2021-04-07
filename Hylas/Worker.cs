using System.Text.RegularExpressions;
using MelonLoader;
using UnityEngine;

namespace Hylas
{
    internal abstract class Worker
    {
        protected string Path;
        
        public string TemplatePath => Regex.Replace(Path, "^(.+/)[0-9]{3,}(/|$)", "${1}101$2");

        protected virtual string RelativePhysicalPath => Path;

        protected string AbsolutelyPhysicalPath => System.IO.Path.Combine(MelonUtils.GameDirectory, "Mods", nameof(Hylas), RelativePhysicalPath);

        public abstract GameObject Rework(GameObject template);

        public static Worker Pick(string path)
        {
            Worker worker = null;
            if (path.IsPortrait())
            {
                worker = new ProtraitWorker
                {
                    Path = path
                };
            }
            else if (path.IsBattleHuman())
            {
                worker = new BattleHumanWorker
                {
                    Path = path
                };
            }

            return worker?.AbsolutelyPhysicalPath.Exist() == true ? worker : null;
            
        }
    }

    internal class ProtraitWorker : Worker
    {
        protected override string RelativePhysicalPath => Path.Replace("Game/Portrait/", "");

        public override GameObject Rework(GameObject template)
        {
            var renderer = template.GetComponentInChildren<SpriteRenderer>();
            renderer.LoadCustomSprite(AbsolutelyPhysicalPath);

            return template;
        }
    }

    internal class BattleHumanWorker : Worker
    {
        public override GameObject Rework(GameObject template)
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
