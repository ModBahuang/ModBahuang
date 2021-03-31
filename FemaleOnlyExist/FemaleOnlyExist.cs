using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using MelonLoader;

namespace FemaleOnlyExist
{
    public class FemaleOnlyExist : MelonMod
    {
        public override void OnApplicationStart()
        {
            var target = typeof(ConfRoleAttributeCoefficient).GetMethod("RandomInitNPCUnit");
            var prefix = typeof(FemaleOnlyExist).GetMethod(nameof(PreRandomInitNpcUnit),
                BindingFlags.Public | BindingFlags.Static);

            Harmony.Patch(target, new HarmonyMethod(prefix));
        }

        // ReSharper disable once RedundantAssignment
        public static void PreRandomInitNpcUnit(ref int sex)
        {
            sex = (int)UnitSexType.Woman;
        }
    }
}
