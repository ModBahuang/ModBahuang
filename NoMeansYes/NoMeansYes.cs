using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using MelonLoader;

namespace NoMeansYes
{
    public class NoMeansYes : MelonMod
    {
        public override void OnApplicationStart()
        {
            var target = typeof(UnitActionFeedback1031).GetMethod("OnCreate");
            var postfix = typeof(NoMeansYes).GetMethod(nameof(ModifyNpcFeedback),
                BindingFlags.Public | BindingFlags.Static);

            Harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        // ReSharper disable once InconsistentNaming
        public static void ModifyNpcFeedback(UnitActionFeedback1031 __instance)
        {
            // Only apply on player
            var isPlayer = __instance.trainsUnit.GetHashCode() == g.world.playerUnit.GetHashCode();
            if (!isPlayer) return;
            
            __instance.state = 1;
        }
    }
}
