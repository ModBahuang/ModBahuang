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
                String
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

            public bool IsNone { get; }

            public Value(Type type, JValue value)
            {
                IsNone = value == null;

                if (type == typeof(string))
                {
                    tag = ValueType.String;
                    
                    if (value == null)
                    {
                        // An empty string will never be clean up.
                        @string = IL2CPP.ManagedStringToIl2Cpp("");
                    }
                    else
                    {
                        // FIXME:
                        //  The purpose of intern is to prevent GC from freeing our string to avoid UAF.
                        //  But intern will run into some performance problems sometimes.
                        //  Maybe making a reference circle is another solution.
                        @string = Il2CppHelper.il2cpp_string_intern(
                            IL2CPP.ManagedStringToIl2Cpp(value.Value<string>()));
                    }
                }
                else if (type == typeof(bool))
                {
                    tag = ValueType.Boolean;
                    @bool = value?.Value<bool>() ?? default;
                }
                else if (type == typeof(byte))
                {
                    tag = ValueType.Byte;
                    @byte = value?.Value<byte>() ?? default;
                }
                else if (type == typeof(sbyte))
                {
                    tag = ValueType.SByte;
                    @sbyte = value?.Value<sbyte>() ?? default;
                }
                else if (type == typeof(short))
                {
                    tag = ValueType.Int16;
                    @short = value?.Value<short>() ?? default;
                }
                else if (type == typeof(ushort))
                {
                    tag = ValueType.UInt16;
                    @ushort = value?.Value<ushort>() ?? default;
                }
                else if (type == typeof(int))
                {
                    tag = ValueType.Int32;
                    @int = value?.Value<int>() ?? default;
                }
                else if (type == typeof(uint))
                {
                    tag = ValueType.UInt32;
                    @uint = value?.Value<uint>() ?? default;
                }
                else if (type == typeof(long))
                {
                    tag = ValueType.Int64;
                    @long = value?.Value<long>() ?? default;
                }
                else if (type == typeof(ulong))
                {
                    tag = ValueType.UInt64;
                    @ulong = value?.Value<ulong>() ?? default;
                }
                else if (type == typeof(char))
                {
                    tag = ValueType.Char;
                    @char = value?.Value<char>() ?? default;
                }
                else if (type == typeof(double))
                {
                    tag = ValueType.Double;
                    @double = value?.Value<double>() ?? default;
                }
                else if (type == typeof(float))
                {
                    tag = ValueType.Single;
                    @float = value?.Value<float>() ?? default;
                }
                else
                {
                    throw new NotSupportedException($"Type {type} is not supported");
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
                Action<float> f13)
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
        private readonly (PropertyInfo, int, Value)[] values;

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
                var (p, ofs) = a;
                var v = new Value(p.PropertyType, obj.GetValue(p.Name)?.Value<JValue>());
                return (p, ofs, v);
            }).ToArray();

            var item = new Item(id, new Spans(span), values, record);

            return item;
        }

        private Item(int id, Spans source, (PropertyInfo, int, Value)[] values, Record record)
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

            foreach (var (_, ofs, value) in values)
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
                        float1 => *(float*)ptr = float1
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

                    var (p, _, v) = values[i - 1];

                    if (v.IsNone)
                    {
                        Logger.Warn($"Required field {p} does not exsit in item(id: {Id}, @ {Source}), set to default.");
                    }

                    var j = i;

                    v.Switch(
                        string1 => args[j] = string1,
                        bool1 => args[j] = (IntPtr) (&bool1),
                        byte1 => args[j] = (IntPtr) (&byte1),
                        sbyte1 => args[j] = (IntPtr) (&sbyte1),
                        short1 => args[j] = (IntPtr) (&short1),
                        ushort1 => args[j] = (IntPtr) (&ushort1),
                        int1 => args[j] = (IntPtr) (&int1),
                        uint1 => args[j] = (IntPtr) (&uint1),
                        long1 => args[j] = (IntPtr) (&long1),
                        ulong1 => args[j] = (IntPtr) (&ulong1),
                        char1 => args[j] = (IntPtr) (&char1),
                        double1 => args[j] = (IntPtr) (&double1),
                        float1 => args[j] = (IntPtr) (&float1)
                    );
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
                var (p1, ofs1, _) = values[i];
                var (p2, ofs2, v) = other.values[i];

                Logger.Assert(p1 == p2 && ofs1 == ofs2, "Must be same");
                if (!v.IsNone)
                {
                    values[i].Item3 = v;
                }
            }
        }
    }
}
