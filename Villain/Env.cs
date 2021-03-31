using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using MelonLoader;

namespace Villain
{
    internal static class Env
    {
        private static readonly Lazy<string> GAME_ASSEMBLY_PATH =
            new Lazy<string>(() => Path.Combine(MelonUtils.GameDirectory, "GameAssembly.dll"));

        private static readonly Lazy<string> VILLAIN_HOME_PATH = 
            new Lazy<string>(() => Path.Combine(MelonUtils.GameDirectory, "Mods", "Villain"));

        private static readonly Lazy<string> CONFIG_INI_PATH = 
            new Lazy<string>(() => Path.Combine(VillainHomePath, "config.ini"));

        private static readonly Lazy<string> BASE_CONF_PATH =
            new Lazy<string>(() => Path.Combine(VillainHomePath, "base"));

        private static readonly Lazy<IntPtr> GAME_ASSEMBLY_BASE = new Lazy<IntPtr>(() => LoadLibrary(GameAssemblyPath));

        private static string GameAssemblyPath => GAME_ASSEMBLY_PATH.Value;

        private static string VillainHomePath => VILLAIN_HOME_PATH.Value;

        private static string ConfigIniPath => CONFIG_INI_PATH.Value;

        public static string BaseConfPath => BASE_CONF_PATH.Value;

        public static IntPtr GameAssemblyBase => GAME_ASSEMBLY_BASE.Value;
        

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpLibFileName);

        /// <summary>
        /// All patch directories exsits in <see cref="VillainHomePath"/>.
        /// </summary>
        public static IEnumerable<DirectoryInfo> Patches { get; }

        public static bool IsGameUpdated { get; }

        static Env()
        {
            var home = Directory.CreateDirectory(VillainHomePath);
            home.CreateSubdirectory(BaseConfPath);

            IsGameUpdated = CheckIsGameUpdated();

            Patches = home.EnumerateDirectories("patch_*");
        }

        private static bool CheckIsGameUpdated()
        {
            // FIXME: calc GameAssembly's hash instead file timestamp
            var timestamp = File.GetLastWriteTime(GameAssemblyPath).Ticks.ToString();
            Logger.Debug(timestamp);

            var ini = new IniFile(ConfigIniPath);

            if (ini.HasKey("Villain", "hash"))
            {
                var oldTimestamp = ini.GetString("Villain", "hash");

                ini.SetString("Villain", "hash", timestamp);

                return oldTimestamp != timestamp;
            }

            Logger.Info("Mod first run initializing");
            ini.SetString("Villain", "hash", timestamp);

            return true;
        }
    }
}
