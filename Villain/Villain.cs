using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;

namespace Villain
{
    public class Villain : MelonMod
    {
        public override void OnApplicationStart()
        {
            if (Env.IsGameUpdated)
            {
                Logger.Info("NOTE: Game update detected.");
                Logger.Info("Mod loadded in dump mode, no patch applied.");
                PatchManager.ApplyAll();
            }
            else
            {
                PatchManager.LoadAndApplyOnlyPatch(Env.Patches);
            }
        }
    }
}
