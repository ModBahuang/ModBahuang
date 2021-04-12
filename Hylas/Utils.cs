using System;
using System.IO;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hylas
{
    internal static class Utils
    {
        private delegate IntPtr Load(IntPtr path, IntPtr systemTypeInstance);

        private static readonly Load _load;

        static Utils()
        {
            _load = IL2CPP.ResolveICall<Load>("UnityEngine.Resources::Load(System.String,System.Type)");
        }

        public static string GetHylasHome() => Path.Combine(MelonUtils.GameDirectory, "Mods", nameof(Hylas));

        public static Object ResourcesLoad(string path, Il2CppSystem.Type systemTypeInstance)
        {
            var ptr = _load(IL2CPP.ManagedStringToIl2Cpp(path), IL2CPP.Il2CppObjectBaseToPtrNotNull(systemTypeInstance));
            return ptr != IntPtr.Zero ? new Object(ptr) : null;
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private static string MakeRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath)) throw new ArgumentNullException(nameof(fromPath));
            if (string.IsNullOrEmpty(toPath)) throw new ArgumentNullException(nameof(toPath));

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        public static string GetResourcePath(string physicalPath)
        {
            var path = MakeRelativePath(GetHylasHome() + "\\", Path.GetDirectoryName(physicalPath)).Replace(Path.DirectorySeparatorChar, '/');
            if (path.StartsWith("Back") || path.StartsWith("Hat") || path.StartsWith("Man") || path.StartsWith("Woman"))
            {
                path = "Game/Portrait/" + path;
            }

            return path;
        }
    }
}
