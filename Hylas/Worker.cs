using System;
using System.IO;
using System.Text.RegularExpressions;
using MelonLoader;
using UnityEngine;

namespace Hylas
{
    internal abstract class Worker
    {
        private string resPath;

        private readonly Regex pathPattern = new Regex("^(.+/)[0-9]{3,}(/[^/]+|$)$");

        public string TemplatePath
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

        protected virtual Func<string, string> MapPath => s => s;

        protected string AbsolutelyPhysicalPath => Path.GetFullPath(Path.Combine(Utils.GetHylasHome(), MapPath(resPath)));

        public abstract GameObject Rework(GameObject template);

        public static Worker Pick(string path)
        {
            Worker worker = null;
            if (path.IsPortrait())
            {
                worker = new ProtraitWorker
                {
                    resPath = path
                };
            }
            else if (path.IsBattleHuman())
            {
                worker = new BattleHumanWorker
                {
                    resPath = path
                };
            }

            return worker;
            
        }
    }

    internal class ProtraitWorker : Worker
    {
        protected override Func<string, string> MapPath => s => s.Replace("Game/Portrait/", "");

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

            var sprite = template.GetComponentInChildren<SpriteRenderer>().sprite;
            ImageConversion.LoadImage(sprite.texture, image);
            sprite.rect.Set(param.rect.position.x, param.rect.position.y, param.rect.size.x, param.rect.size.y);
            sprite.textureRect.Set(param.rect.position.x, param.rect.position.y, param.rect.size.x, param.rect.size.y);
            sprite.pivot.Set(param.pivot.x, param.pivot.y);
            sprite.border.Set(param.border.x, param.border.y, param.border.z, param.border.w);

            return template;
        }
    }
}
