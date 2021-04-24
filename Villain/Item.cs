using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnhollowerBaseLib;

namespace Villain
{
    internal class Item
    {
        private class Value
        {
            private enum ValueType
            {
                Boolean,
                Byte,
                SByte,
                Int16,
                UInt16,
                Int32,
                UInt32,
                Int64,
                UInt64,
                Char,
                Double,
                Single,
                String,
                IntArray,
                Float2DArray
            }

            private readonly ValueType tag;

            private readonly bool @bool;
            private readonly byte @byte;
            private readonly sbyte @sbyte;
            private readonly short @short;
            private readonly ushort @ushort;
            private readonly int @int;
            private readonly uint @uint;
            private readonly long @long;
            private readonly ulong @ulong;
            private readonly char @char;
            private readonly double @double;
            private readonly float @float;
            private readonly IntPtr @string;
            private readonly Il2CppStructArray<int> intArray;
            private readonly Il2CppReferenceArray<Il2CppStructArray<float>> float2DArray;

            public bool IsNone { get; }

            public Value(Type type, JToken value)
            {
                IsNone = value == null;

                @bool = default;
                @byte = default;
                @sbyte = default;
                @short = default;
                @ushort = default;
                @int = default;
                @uint = default;
                @long = default;
                @ulong = default;
                @char = default;
                @double = default;
                @float = default;
                @string = IL2CPP.ManagedStringToIl2Cpp("");
                intArray = default;
                float2DArray = default;

                if (type == typeof(string))
                {
                    tag = ValueType.String;
                    if (value == null) return;
                    // FIXME:
                    //  The purpose of intern is to prevent GC from freeing our string to avoid UAF.
                    //  But intern will run into some performance problems sometimes.
                    //  Maybe making a reference circle is another solution.
                    @string = Il2CppHelper.il2cpp_string_intern(
                        IL2CPP.ManagedStringToIl2Cpp(value.Value<string>()));
                    
                }
                else if (type == typeof(bool))
                {
                    tag = ValueType.Boolean;
                    if (value == null) return;
                    @bool = value.Value<bool>();
                }
                else if (type == typeof(byte))
                {
                    tag = ValueType.Byte;
                    if (value == null) return;
                    @byte = value.Value<byte>();
                }
                else if (type == typeof(sbyte))
                {
                    tag = ValueType.SByte;
                    if (value == null) return;
                    @sbyte = value.Value<sbyte>();
                }
                else if (type == typeof(short))
                {
                    tag = ValueType.Int16;
                    if (value == null) return;
                    @short = value.Value<short>();
                }
                else if (type == typeof(ushort))
                {
                    tag = ValueType.UInt16;
                    if (value == null) return;
                    @ushort = value.Value<ushort>();
                }
                else if (type == typeof(int))
                {
                    tag = ValueType.Int32;
                    if (value == null) return;
                    @int = value.Value<int>();
                }
                else if (type == typeof(uint))
                {
                    tag = ValueType.UInt32;
                    if (value == null) return;
                    @uint = value.Value<uint>();
                }
                else if (type == typeof(long))
                {
                    tag = ValueType.Int64;
                    if (value == null) return;
                    @long = value.Value<long>();
                }
                else if (type == typeof(ulong))
                {
                    tag = ValueType.UInt64;
                    if (value == null) return;
                    @ulong = value.Value<ulong>();
                }
                else if (type == typeof(char))
                {
                    tag = ValueType.Char;
                    if (value == null) return;
                    @char = value.Value<char>();
                }
                else if (type == typeof(double))
                {
                    tag = ValueType.Double;
                    if (value == null) return;
                    @double = value.Value<double>();
                }
                else if (type == typeof(float))
                {
                    tag = ValueType.Single;
                    if (value == null) return;
                    @float = value.Value<float>();
                } 
                else if (type == typeof(Il2CppStructArray<int>))
                {
                    tag = ValueType.IntArray;
                    if (value == null) return;
                    var arr = value.Value<JArray>().Select(t => t.Value<int>()).ToArray();
                    intArray = arr;
                } 
                else if (type == typeof(Il2CppReferenceArray<Il2CppStructArray<float>>))
                {
                    tag = ValueType.Float2DArray;
                    if (value == null) return;
                    var arr = value.Value<JArray>()
                        .Select(t =>
                        {
                            Il2CppStructArray<float> arr2 = t.Value<JArray>()
                                .Select(t2 => t2.Value<float>())
                                .ToArray();
                            return arr2;
                        })
                        .ToArray();
                    float2DArray = arr;
                }
                else
                {
                    throw new NotSupportedException($"Type {type} is not supported");
                }
            }

            public unsafe IntPtr Ptr()
            {
                switch (tag)
                {
                    case ValueType.String:
                        return @string;
                    case ValueType.Boolean:
                        fixed (bool* p = &@bool) { return (IntPtr)p; }
                    case ValueType.Byte:
                        fixed (byte* p = &@byte) { return (IntPtr)p; }
                    case ValueType.SByte:
                        fixed (sbyte* p = &@sbyte) { return (IntPtr)p; }
                    case ValueType.Int16:
                        fixed (short* p = &@short) { return (IntPtr)p; }
                    case ValueType.UInt16:
                        fixed (ushort* p = &@ushort) { return (IntPtr)p; }
                    case ValueType.Int32:
                        fixed (int* p = &@int) { return (IntPtr)p; }
                    case ValueType.UInt32:
                        fixed (uint* p = &@uint) { return (IntPtr)p; }
                    case ValueType.Int64:
                        fixed (long* p = &@long) { return (IntPtr)p; }
                    case ValueType.UInt64:
                        fixed (ulong* p = &@ulong) { return (IntPtr)p; }
                    case ValueType.Char:
                        fixed (char* p = &@char) { return (IntPtr)p; }
                    case ValueType.Double:
                        fixed (double* p = &@double) { return (IntPtr)p; }
                    case ValueType.Single:
                        fixed (float* p = &@float) { return (IntPtr)p; }
                    case ValueType.IntArray:
                        return intArray.Pointer;
                    case ValueType.Float2DArray:
                        return float2DArray.Pointer;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public void Switch(
                Action<IntPtr> f1,
                Action<bool> f2,
                Action<byte> f3,
                Action<sbyte> f4,
                Action<short> f5,
                Action<ushort> f6,
                Action<int> f7,
                Action<uint> f8,
                Action<long> f9,
                Action<ulong> f10,
                Action<char> f11,
                Action<double> f12,
                Action<float> f13,
                Action<Il2CppStructArray<int>> f14,
                Action<Il2CppReferenceArray<Il2CppStructArray<float>>> f15)
            {
                switch (tag)
                {
                    case ValueType.String:
                        f1(@string);
                        break;
                    case ValueType.Boolean:
                        f2(@bool);
                        break;
                    case ValueType.Byte:
                        f3(@byte);
                        break;
                    case ValueType.SByte:
                        f4(@sbyte);
                        break;
                    case ValueType.Int16:
                        f5(@short);
                        break;
                    case ValueType.UInt16:
                        f6(@ushort);
                        break;
                    case ValueType.Int32:
                        f7(@int);
                        break;
                    case ValueType.UInt32:
                        f8(@uint);
                        break;
                    case ValueType.Int64:
                        f9(@long);
                        break;
                    case ValueType.UInt64:
                        f10(@ulong);
                        break;
                    case ValueType.Char:
                        f11(@char);
                        break;
                    case ValueType.Double:
                        f12(@double);
                        break;
                    case ValueType.Single:
                        f13(@float);
                        break;
                    case ValueType.IntArray:
                        f14(intArray);
                        break;
                    case ValueType.Float2DArray:
                        f15(float2DArray);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal struct Span
        {
            public FileInfo File;
            public int LineNumber;
            public int LinePosition;
        }

        public class Spans
        {
            private readonly List<Span> inner;

            internal Spans(Span span)
            {
                inner = new List<Span> { span };
            }

            internal void Merge(Spans other) => inner.AddRange(other.inner);

            public override string ToString()
            {
                var b = new StringBuilder();
                foreach (var span in inner)
                {
                    b.AppendLine($"File: \"{span.File}\", Line: {span.LineNumber}, Position: {span.LinePosition}");
                }
                return b.ToString().TrimEnd();
            }
        }

        public Spans Source { get; }

        public int Id { get; }

        /// <summary>
        /// The fields of the <see cref="Record.ItemType"/>, in parameter order.
        /// </summary>
        private readonly Value[] values;

        private readonly Record record;

        public static Item From(Record record, JObject obj, FileInfo source)
        {
            var lineInfo = (IJsonLineInfo)obj;
            Logger.Assert(lineInfo.HasLineInfo(), () => $"{obj} must have line info");
            var span = new Span
            {
                File = source,
                LineNumber = lineInfo.LineNumber,
                LinePosition = lineInfo.LinePosition
            };

            var id = obj.GetValue("id")?.Value<int>() ?? throw new ArgumentException("`id` not found", nameof(obj));

            var values = record.FieldsInfo.Select(a =>
            {
                var p = a.Item1;
                var v = new Value(p.PropertyType, obj.GetValue(p.Name));
                return v;
            }).ToArray();

            var item = new Item(id, new Spans(span), values, record);

            return item;
        }

        private Item(int id, Spans source, Value[] values, Record record)
        {
            Id = id;
            Source = source;
            this.values = values;
            this.record = record;
        }

        public void ApplyTo(ConfBaseItem item)
        {
            if (!record.ItemType.IsInstanceOfType(item))
            {
                throw new ArgumentException("Unexcepted instance", nameof(item));
            }

            foreach (var (ofs, value) in values.Zip(record.FieldsInfo, (v, info) => (info.Item2, v)))
            {
                if (value.IsNone) continue;

                var ptr = IL2CPP.Il2CppObjectBaseToPtrNotNull(item) + ofs;

                unsafe
                {
                    value.Switch(
                        string1 => *(IntPtr*)ptr = string1,
                        bool1 => *(bool*)ptr = bool1,
                        byte1 => *(byte*)ptr = byte1,
                        sbyte1 => *(sbyte*)ptr = sbyte1,
                        short1 => *(short*)ptr = short1,
                        ushort1 => *(ushort*)ptr = ushort1,
                        int1 => *(int*)ptr = int1,
                        uint1 => *(uint*)ptr = uint1,
                        long1 => *(long*)ptr = long1,
                        ulong1 => *(ulong*)ptr = ulong1,
                        char1 => *(char*)ptr = char1,
                        double1 => *(double*)ptr = double1,
                        float1 => *(float*)ptr = float1,
                        intArray => *(IntPtr*)ptr = intArray.Pointer,
                        float2D => *(IntPtr*)ptr = float2D.Pointer
                    );
                }
            }
        }

        public ConfBaseItem Construct()
        {
            var o = IL2CPP.il2cpp_object_new(record.NativeItemClassPtr);
            var obj = (ConfBaseItem)record.ItemType
                .GetConstructor(new[] { typeof(IntPtr) })?
                .Invoke(new object[] { o });

            var paramLen = values.Length + 1;

            unsafe
            {
                var args = stackalloc IntPtr[paramLen];

                var id = Id;
                args[0] = (IntPtr) (&id);
                
                for (var i = 1; i < paramLen; i++)
                {
                    var j = i - 1;
                    var v = values[j];

                    if (v.IsNone)
                    {
                        Logger.Warn($"Required field({record.FieldsInfo[j].Item1}) does not exsit in item(id: {Id}, @ {Source}), set to default.");
                    }

                    // FIXME: values could be moved by JIT
                    args[i] = v.Ptr();
                }
                var exc = IntPtr.Zero;
                IL2CPP.il2cpp_runtime_invoke(record.ItemCtor.Item2, IL2CPP.Il2CppObjectBaseToPtrNotNull(obj), (void**)args, ref exc);
                Il2CppException.RaiseExceptionIfNecessary(exc);
                
                
            }

            return obj;
        }

        public void Merge(Item other)
        {
            if (record != other.record || Id != other.Id)
            {
                throw new ArgumentException("Two items have different type", nameof(other));
            }

            Source.Merge(other.Source);
            for (var i = 0; i < values.Length; i++)
            {
                var (p1, ofs1) = record.FieldsInfo[i];
                var (p2, ofs2) = other.record.FieldsInfo[i];
                
                Logger.Assert(p1 == p2 && ofs1 == ofs2, "Must be same");

                var v = other.values[i];
                if (!v.IsNone)
                {
                    values[i] = v;
                }
            }
        }
    }
}
