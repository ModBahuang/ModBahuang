using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using System.IO;
using System.Threading;
using MelonLoader;
using UnityEngine;
using Newtonsoft.Json;
using UnhollowerBaseLib;
using Object = UnityEngine.Object;

namespace Hylas
{
    internal static class GoCache
    {
        private class Cached
        {
            private const int TRUE = 1;
            private const int FALSE = 0;

            private int expired = FALSE;
            private readonly GameObject go;
            private readonly Worker worker;
            private readonly ReaderWriterLockSlim goLock = new ReaderWriterLockSlim();

            public Cached(GameObject go, Worker worker)
            {
                this.go = go;
                this.worker = worker;
            }

            public GameObject Get()
            {
                if (Interlocked.CompareExchange(ref expired, FALSE, TRUE) == TRUE)
                {
                    MelonLogger.Msg($"renew {worker.AbsolutelyPhysicalPath}");
                    goLock.EnterWriteLock();
                    try
                    {
                        return worker.Rework(go);
                    }
                    finally
                    {
                        goLock.ExitWriteLock();
                    }
                }

                goLock.EnterReadLock();
                try
                {
                    return go;
                }
                finally
                {
                    goLock.ExitReadLock();
                }
            }

            public void Expire()
            {
                Interlocked.Exchange(ref expired, TRUE);
            }
        }

        private static readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        private static readonly GameObject _root;

        private static readonly Dictionary<string, Cached> _cache = new Dictionary<string, Cached>();

        private static readonly FileSystemWatcher _watcher = new FileSystemWatcher(Helper.GetHome());

        static GoCache()
        {
            _root = new GameObject
            {
                name = "hylas cache",
                active = false
            };
            Object.DontDestroyOnLoad(_root);

            _watcher.NotifyFilter = NotifyFilters.Attributes
                                    | NotifyFilters.DirectoryName
                                    | NotifyFilters.FileName
                                    | NotifyFilters.LastWrite
                                    | NotifyFilters.Security
                                    | NotifyFilters.Size;

            _watcher.Changed += Handler;
            _watcher.Created += Handler;
            _watcher.Renamed += Handler;
            _watcher.Deleted += Handler;
            _watcher.Error += OnError;

            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
            _watcher.Filter = "*.*";

        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            MelonLogger.Error($"{e.GetException()}");
        }

        private static void Handler(object sender, FileSystemEventArgs e)
        {
            MelonLogger.Msg($"{e.Name} {e.ChangeType} {e.FullPath}");

            _cacheLock.EnterReadLock();
            try
            {
                var dir = Path.GetDirectoryName(e.Name) ?? throw new InvalidOperationException();

                if (!_cache.ContainsKey(dir)) return;

                _cache[dir].Expire();
                throw new NotImplementedException();
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public static GameObject Get(string path)
        {
            _cacheLock.EnterReadLock();

            try
            {
                return _cache.TryGetValue(path, out var cached) ? cached.Get() : null;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public static GameObject Add(string path, GameObject go, Worker worker)
        {
            var go2 = Get(path);
            if (go2 != null)
            {
                return go2;
            }

            _cacheLock.EnterWriteLock();
            try
            {
                var tmp = worker.Rework(Object.Instantiate(go, _root.transform));
                _cache.Add(path, new Cached(tmp, worker));
                return tmp;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
    }

    public static class ResourcesLoadPatch
    {
        private delegate IntPtr Load(IntPtr path, IntPtr systemTypeInstance);

        private static readonly Load _load;

        static ResourcesLoadPatch()
        {
            _load = IL2CPP.ResolveICall<Load>("UnityEngine.Resources::Load(System.String,System.Type)");
        }

        public static void Patch(HarmonyInstance harmony)
        {
            var original = typeof(Resources).GetMethod("Load", new[] { typeof(string), typeof(Il2CppSystem.Type) });
            harmony.Patch(original, prefix: new HarmonyMethod(typeof(ResourcesLoadPatch).GetMethod(nameof(Prefix))));
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once RedundantAssignment
        public static bool Prefix(ref Object __result, string path, Il2CppSystem.Type systemTypeInstance)
        {
            MelonLogger.Msg($"{path}, {systemTypeInstance.Name}");
            MelonDebug.Msg(path);

            var worker = Worker.Pick(path);

            if (worker == null) return true;

            var pp = worker.AbsolutelyPhysicalPath;
            var tp = worker.TemplatePath;
            try
            {
                __result = GoCache.Get(pp);

                if (__result == null)
                {
                    var obj = _load(IL2CPP.ManagedStringToIl2Cpp(tp), IL2CPP.Il2CppObjectBaseToPtrNotNull(systemTypeInstance));

                    // Will throw an exception when obj is null
                    var go = new Object(obj).Cast<GameObject>();
                    __result = GoCache.Add(pp, go, worker);
                }

            }
            catch (Exception e)
            {
                MelonLogger.Error($"{e}\n{tp}");
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(ResMgr), "LoadAsync")]
    public class ResLoadAsyncPatch
    {
        public static bool Prefix(ref string path, Action<Object> call)
        {
            // Use mark as a workaround to resolve the original call.
            // Reverse patch needs to update the melonloader version.
            const string mark = "//?/";

            MelonDebug.Msg(path);

            if (path.StartsWith(mark))
            {
                path = path.Substring(mark.Length);
                return true;
            }

            var worker = Worker.Pick(path);

            if (worker == null) return true;

            var pp = worker.AbsolutelyPhysicalPath;
            var tp = worker.TemplatePath;

            // It's a workaround for `call` that used in `Wrapper` got freed in Il2cpp domain
            // I have no idea why/how this could work. But, though, anyway, it just WORKS.
            // A reason might be the `native` will keep a gc handle to prevent `call` from freeing.
            Il2CppSystem.Action<Object> native = call;

            void Wrapper(Object obj)
            {
                native.Invoke(GoCache.Add(pp, obj.Cast<GameObject>(), worker));
            }

            var product = GoCache.Get(pp);
            if (product != null)
            {
                native.Invoke(product);
            }
            else
            {
                var a = new Action<Object>(Wrapper);
                g.res.LoadAsync(mark + tp, a);
            }

            return false;
        }
    }

    public class Hylas : MelonMod
    {
        public override void OnApplicationStart()
        {
            ResourcesLoadPatch.Patch(Harmony);
        }
    }

    internal static class Ext
    {
        public static bool IsPortrait(this string path)
        {
            return path.StartsWith("Game/Portrait/");
        }

        public static bool IsBattleHuman(this string path)
        {
            return path.StartsWith("Battle/Human/");
        }

        public static bool Exist(this string path)
        {
            return Directory.Exists(path);
        }

        public static void LoadCustomSprite(this SpriteRenderer renderer, string path)
        {
            var (param, image) = path.LoadSprite();

            // LoadImage will replace with with incoming image size.
            var tex = new Texture2D(100, 100, TextureFormat.ARGB32, false);

            if (!ImageConversion.LoadImage(tex, image))
            {
                throw new InvalidOperationException();
            }

            var newSprite = Sprite.Create(tex, param.rect, param.pivot, param.pixelsPerUnit, param.extrude, param.meshType, param.border, param.generateFallbackPhysicsShape);

            renderer.sprite = newSprite;
        }

        public static (SpriteParam, byte[]) LoadSprite(this string path)
        {
            var imagePath = Path.Combine(path, "image.png");
            var paramPath = Path.Combine(path, "sprite.json");

            var param = JsonConvert.DeserializeObject<SpriteParam>(File.ReadAllText(paramPath));

            var image = File.ReadAllBytes(imagePath);

            return (param, image);
        }
    }
}
