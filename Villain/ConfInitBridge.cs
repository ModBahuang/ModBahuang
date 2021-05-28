using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;

namespace Villain
{
    internal static class ConfInitBridge
    {
        private static readonly Dictionary<IntPtr, Record> BRIDGE_INDEX = new Dictionary<IntPtr, Record>();

        public static void Erect(Record record)
        {

            var trampoline = Trampoline.Generate(record.FinalInitVa);

            unsafe
            {
                var p = (IntPtr*)Marshal.AllocHGlobal(IntPtr.Size);
                *p = record.FinalInitVa;

                MelonUtils.NativeHookAttach((IntPtr)p, trampoline.Address);

                var handle = trampoline.Finish(*p, Bridge);

                BRIDGE_INDEX.Add(handle, record);

                Marshal.FreeHGlobal((IntPtr)p);
            }
        }

        private static void Bridge(IntPtr @this, IntPtr handle)
        {
            var record = BRIDGE_INDEX[handle];
            Logger.Debug(() => $"{record}");

            try
            {
                var conf = UnhollowerSupport.Il2CppObjectPtrToIl2CppObject<ConfBase>(@this)
                           ?? throw new Exception("Unable to convert ptr to object");

                if (Env.IsGameUpdated)
                {
                    BridgeHelper.DumpConf(conf, record);
                }
                else
                {
                    BridgeHelper.UpdateConf(conf, record);
                }
            }
            catch (Exception e)
            {
                var info = Trampoline.GetInfo(handle);
                MelonLogger.Warning($"Exception occurs in bridge\n{e}\n" +
                                    $"this: 0x{@this.ToInt64():X}\n" +
                                    $"trampoline info: {info}\n" +
                                    $"record: {record}");
            }
        }
    }

    internal static class BridgeHelper
    {
        public static void UpdateConf(ConfBase conf, Record record)
        {
            var items = PatchManager.ByRecord(record);

            var set = items.Keys.ToHashSet();

            foreach (var item in conf.allConfBase)
            {
                // Narrowing to subtype
                var it = (ConfBaseItem)Il2CppHelper.Il2CppObjectPtrToIl2CppObjectByType(item.Pointer, record.ItemType);

                if (!items.TryGetValue(it.id, out var v)) continue;

                set.Remove(it.id);
                v.ApplyTo(it);
            }

            foreach (var item in set.Select(id => items[id]))
            {
                conf.allConfBase.Add(item.Construct());
            }
        }

        public static void DumpConf(ConfBase conf, Record record)
        {
            // Logger.Debug($"{conf.confName}, {record}");
            var confPath = Path.Combine(Env.BaseConfPath, $"{conf.confName}.json");

            using (var writer = new JsonTextWriter(new StreamWriter(confPath))
            {
                Indentation = 2,
                Formatting = Formatting.Indented
            })
            {
                writer.WriteStartArray();

                foreach (var item in conf.allConfBase)
                {
                    if (item == null)
                        // TODO: Find out why item could be null
                        continue;

                    // Narrowing to subtype
                    var it = Il2CppHelper.Il2CppObjectPtrToIl2CppObjectByType(item.Pointer, record.ItemType);

                    writer.WriteStartObject();

                    // `id` is a special case FieldsInfo missed
                    writer.WritePropertyName("id");
                    writer.WriteValue(item.id);
                    foreach (var p in record.FieldsInfo)
                    {
                        var field = p.Item1;

                        writer.WritePropertyName(field.Name);

                        var v = field.GetValue(it);
                        // FIXME: It's a temporarily way and should be fixed later
                        if (field.PropertyType == typeof(Il2CppStructArray<int>))
                        {
                            writer.WriteStartArray();
                            foreach (var n in (Il2CppStructArray<int>)v)
                            {
                                writer.WriteValue(n);
                            }
                            writer.WriteEndArray();
                        }
                        else if (field.PropertyType == typeof(Il2CppStringArray))
                        {
                            writer.WriteStartArray();
                            foreach (var str in (Il2CppStringArray)v)
                            {
                                writer.WriteValue(str);
                            }
                            writer.WriteEndArray();
                        }
                        else if (field.PropertyType == typeof(Il2CppReferenceArray<Il2CppStructArray<float>>))
                        {
                            writer.WriteStartArray();
                            foreach (var a in (Il2CppReferenceArray<Il2CppStructArray<float>>)v)
                            {
                                writer.WriteStartArray();
                                foreach (var f in a)
                                {
                                    writer.WriteValue(f);
                                }
                                writer.WriteEndArray();
                            }
                            writer.WriteEndArray();
                        }
                        else if (field.PropertyType == typeof(Il2CppReferenceArray<Il2CppStructArray<int>>))
                        {
                            writer.WriteStartArray();
                            foreach (var ia in (Il2CppReferenceArray<Il2CppStructArray<int>>)v)
                            {
                                writer.WriteStartArray();
                                foreach (var i in ia)
                                {
                                    writer.WriteValue(i);
                                }
                                writer.WriteEndArray();
                            }
                            writer.WriteEndArray();
                        }
                        else if (field.PropertyType == typeof(Il2CppReferenceArray<Il2CppStringArray>))
                        {
                            writer.WriteStartArray();
                            foreach (var sa in (Il2CppReferenceArray<Il2CppStringArray>)v)
                            {
                                writer.WriteStartArray();
                                foreach (var s in sa)
                                {
                                    writer.WriteValue(s);
                                }
                                writer.WriteEndArray();
                            }
                            writer.WriteEndArray();
                        }
                        else
                        {
                            writer.WriteValue(v);
                        }
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }
        }

    }
}
