using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

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
            var renderer = template.GetComponent<SpriteRenderer>();
            renderer.LoadCustomSprite(AbsolutelyPhysicalPath);

            return template;
        }
    }
}
