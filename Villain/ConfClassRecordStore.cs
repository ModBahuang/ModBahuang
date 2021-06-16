using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MelonLoader;
using dnlib.DotNet;
using UnhollowerBaseLib;

namespace Villain
{
    public class Record
    {
        /// <summary>
        /// The <c>AnimaWeapon</c> part in <see cref="ConfAnimaWeaponBase"/> and <see cref="ConfAnimaWeaponItem"/>
        /// </summary>
        public string Name;

        /// <summary>
        /// The <see cref="Type"/> for <c>ConfXxxBase</c> like <see cref="ConfAnimaWeaponBase"/>
        /// </summary>
        public Type BaseType;

        /// <summary>
        /// The <see cref="Type"/> of <c>ConfXxxItem</c> like <see cref="ConfAnimaWeaponItem"/>
        /// </summary>
        public Type ItemType;

        /// <summary>
        /// The native class pointer for <see cref="ItemType"/> used by <see cref="IL2CPP.il2cpp_object_new"/>
        /// </summary>
        public IntPtr NativeItemClassPtr;

        /// <summary>
        /// The proper constructor for the <c>ConfXxxItem</c> like <see cref="ConfAnimaWeaponItem(int, string, string, string, string)"/>
        /// </summary>
        public (ConstructorInfo, IntPtr) ItemCtor;

        /// <summary>
        /// The fields' type and offset for the <c>ConfXxxItem</c> like <see cref="ConfAnimaWeaponItem.name"/>
        /// </summary>
        public (PropertyInfo, int)[] FieldsInfo;

        /// <summary>
        /// The virtual address of the conf class's final init method like <see cref="ConfAnimaWeaponBase.Init1"/>.
        /// </summary>
        public IntPtr FinalInitVa;

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(BaseType)}: {BaseType}, {nameof(ItemType)}: {ItemType}, {nameof(NativeItemClassPtr)}: {NativeItemClassPtr}, {nameof(ItemCtor)}: {ItemCtor}, {nameof(FieldsInfo)}: {FieldsInfo}, {nameof(FinalInitVa)}: {FinalInitVa}";
        }
    }

    internal static class ConfClassRecordStore
    {
        public static string[] specialClass = new[]
        {
            "ConfNpcSpecialBase",
            "ConfNpcFeedback1031Base"
        };
        
        private static class Validator
        {
            public static bool IsValidConfMgr(TypeDef confMgr)
            {
                // TODO
                return true;
            }

            public static bool IsValidConfBase(TypeDef confBase)
            {
                // TODO
                return true;
            }
        }

        private class Collector
        {
            private readonly ModuleDefMD module;

            public Collector(string assemblyPath)
            {
                module = ModuleDefMD.Load(assemblyPath);
            }

            public IEnumerable<Record> Collect()
            {
                var asm = typeof(ConfMgr).Assembly;

                foreach (var baseClass in CollectConfBaseClasses())
                {
                    var baseTypeName = baseClass.Name.ToString();
                    var itemTypeName = baseTypeName.Substring(0, baseTypeName.Length - 4) + "Item";
                    var name = baseTypeName.Substring(4, baseTypeName.Length - 8);

                    var managedBaseType = asm.GetType(baseTypeName);
                    Logger.Assert(managedBaseType != null, $"Type {baseTypeName} must exist in unhollowed assembly");

                    var managedItemType = asm.GetType(itemTypeName);
                    Logger.Assert(managedItemType != null, $"Type {itemTypeName} must exist in unhollowed assembly");


                    var itemType = module.Find(itemTypeName, false);
                    Logger.Assert(itemType != null, $"Type {itemTypeName} must exist in dumped assembly");

                    var ctorInfo = GetProperConstructorInfo(managedItemType);
                    var ctorToken = GetProperConstructorToken(itemType, ctorInfo);

                    var classPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", itemTypeName);
                    IL2CPP.il2cpp_runtime_class_init(classPtr);
                    var ctorVa = IL2CPP.GetIl2CppMethodByToken(classPtr, ctorToken);

                    var fields = GetItemFields(ctorInfo, itemType, managedItemType).ToArray();

                    // GetIl2CppMethodByToken sometimes points to dynamic generated function, so use RVA in dummy dll directly.
                    var finalInitRva = GetFinalInitRva(baseClass);

                    yield return new Record
                    {
                        Name = name,
                        BaseType = managedBaseType,
                        ItemType = managedItemType,
                        NativeItemClassPtr = classPtr,
                        ItemCtor = (ctorInfo, ctorVa),
                        FieldsInfo = fields,
                        FinalInitVa = RvaToVa(finalInitRva),
                    };
                }
            }

            private static IEnumerable<(PropertyInfo, int)> GetItemFields(ConstructorInfo ctor, TypeDef native, Type managed)
            {
                var list = new List<(PropertyInfo, int)>();

                foreach (var parameter in ctor.GetParameters().Skip(1))
                {
                    var name = parameter.Name;

                    var fd = native.FindField(name);
                    Logger.Assert(fd != null, () => $"Field({name}) must exist");

                    var property = managed.GetProperty(name);
                    Logger.Assert(property != null, () => $"Property({name}) must exist");

                    list.Add((property, GetFieldOffset(fd)));
                }

                return list;
            }

            private static int GetFieldOffset(IHasCustomAttribute field)
            {
                var attr = field.CustomAttributes.Find("Il2CppDummyDll.FieldOffsetAttribute");
                Logger.Assert(attr != null, $"Field({field}) in dummy dll must have a FieldOffsetAttribute");

                var rva = attr?.GetField("Offset")?.Argument.Value.ToString();
                Logger.Assert(rva != null, "AddressAttribute must have a Offset argument");

                // ReSharper disable once PossibleNullReferenceException
                Logger.Assert(rva.StartsWith("0x"), "The value of Offset must be a hex");

                // Remove hex prefix(`0x`)
                rva = rva.Substring(2);

                return int.Parse(rva, NumberStyles.HexNumber);
            }

            private static IntPtr RvaToVa(int rva)
            {
                return Env.GameAssemblyBase + rva;
            }

            private static ConstructorInfo GetProperConstructorInfo(Type itemType)
            {
                var ctors = itemType.GetConstructors().OrderBy(c => c.GetParameters().Length).ToArray();

                // Those assertions are in truth unnecessary, just add it as a sign of update
                Logger.Assert(ctors.Length == 3, () => $"Unhollowed type {itemType} must have only 3 constructors");

                Logger.Assert(ctors[0].GetParameters().Length == 0, () => $"The 1st constructor({ctors[0]}) must have 0 parameter");

                var c1 = ctors[1];
                var p1 = c1.GetParameters();
                Logger.Assert(p1.Length == 1, () => $"The 2nd constructor({c1}) must have only one parameter");
                if (p1[0].ParameterType == typeof(int))
                {
                    Logger.Assert(p1[0].Name == "id", () => $"The first parameter({c1}) must be `int id`");
                    // Has no field
                    return ctors[1];
                }

                var c2 = ctors[2];
                var p2 = c2.GetParameters();
                Logger.Assert(p2.Length > 1, () => $"The 3rd constructor{c2} must have 1 parameters at least");
                Logger.Assert(p2[0].ParameterType == typeof(int) && p2[0].Name == "id", () => $"The first parameter({p2[0]}) must be `int id`");

                return c2;


            }

            private static int GetProperConstructorToken(TypeDef itemType, ConstructorInfo info)
            {
                // TODO: Check fields are exhaustive with ctor

                // The first parameter of constructor is `this`
                var cs = itemType.FindInstanceConstructors()
                    .Where(c => c.Parameters.Count > 1)
                    .Where(c => c.Parameters[1].Name == "id" && c.Parameters[1].Type.TypeName == "Int32")
                    .Where(c =>
                    {
                        return c.Parameters.Skip(1)
                            .Zip(info.GetParameters(), (a, b) => (a, b))
                            .All(p => p.a.Name == p.b.Name);
                    })
                    .ToArray();

                Logger.Assert(cs.Length == 1, () => $"Must have only 1 proper constructor({cs.Length}) @ {itemType}");

                var ctor = cs[0];

                var addressAttribute = ctor.CustomAttributes.Find("Il2CppDummyDll.TokenAttribute");
                Logger.Assert(addressAttribute != null, $"Method({ctor}) in dummy dll must have a TokenAttribute");

                var token = addressAttribute?.GetField("Token")?.Argument.Value.ToString();
                Logger.Assert(token != null, "TokenAttribute must have a Token argument");

                // ReSharper disable once PossibleNullReferenceException
                Logger.Assert(token.StartsWith("0x"), "The value of Token must be a hex");

                // Remove hex prefix(`0x`)
                token = token.Substring(2);

                return int.Parse(token, NumberStyles.HexNumber);
            }

            private static int GetFinalInitRva(TypeDef @class)
            {
                var init = @class.FindMethod("Init");
                Logger.Assert(init != null, () => $"Method `Init` must exsit in `{@class.FullName}`");
                MethodDef finalInit;
                if (specialClass.Contains<string>(@class.Name))
                {
                    finalInit = init;
                }
                else
                {
                    var inits = @class.Methods
                        .Where(m => m.IsPrivate && !m.IsStatic && m.Name.StartsWith("Init"))
                        .OrderBy(m => int.Parse(m.Name.Substring(4)))
                        .ToArray();

                    Logger.Assert(inits.Length > 0, $"There must be at least one `Init1` in `{@class.FullName}`");
                    Logger.Assert(inits[0].Name == "Init1", "The first method must be `Init1`");

                    finalInit = inits[inits.Length - 1];
                }
                

                return GetMethodRva(finalInit);

            }

            private static int GetMethodRva(IHasCustomAttribute method)
            {
                var addressAttribute = method.CustomAttributes.Find("Il2CppDummyDll.AddressAttribute");
                Logger.Assert(addressAttribute != null, $"Non-virtual method({method}) in dummy dll must have a AddressAttribute");

                var rva = addressAttribute?.GetField("RVA")?.Argument.Value.ToString();
                Logger.Assert(rva != null, "AddressAttribute must have a RVA argument");

                // ReSharper disable once PossibleNullReferenceException
                Logger.Assert(rva.StartsWith("0x"), "The value of RVA must be a hex");

                // Remove hex prefix(`0x`)
                rva = rva.Substring(2);

                return int.Parse(rva, NumberStyles.HexNumber);
            }

            private IEnumerable<TypeDef> CollectConfBaseClasses()
            {
                var confMgr = module.Find("ConfMgr", false);

                Logger.Assert(confMgr != null && Validator.IsValidConfMgr(confMgr),
                    "A proper class `ConfMgr` must be found in game assmebly");


                var confBase = module.Find("ConfBase", false);

                Logger.Assert(confBase != null && Validator.IsValidConfBase(confBase),
                    "A proper class `ConfBase` must be found in game assmebly");


                // Checked by Logger.Assert
                // ReSharper disable once PossibleNullReferenceException
                foreach (var property in confMgr.Properties)
                {
                    // e.g. ConfAnimaWeapon
                    var propertyType = property?.GetMethod?.ReturnType?.ScopeType?.ResolveTypeDefThrow();
                    Logger.Assert(propertyType != null, "propertyType must not be null");

                    // e.g. ConfAnimaWeaponBase
                    var baseType = propertyType?.BaseType?.ResolveTypeDefThrow();
                    Logger.Assert(baseType != null, "baseType must not be null");

                    // ConfBase
                    var root = baseType?.BaseType?.ResolveTypeDefThrow();

                    Logger.Assert(root == confBase, "The root class of a property in `ConfMgr` must be `ConfBase`");

                    yield return baseType;
                }
            }
        }

        private static readonly Dictionary<string, Record> RECORDS;

        static ConfClassRecordStore()
        {
            // FIXME: Is this path reliable?
            var dummyPath = Path.Combine(MelonUtils.GameDirectory,
                "MelonLoader", "Dependencies", "AssemblyGenerator",
                "Il2CppDumper", "DummyDll");

            var mainAssmeblyPath = Path.Combine(dummyPath, "Assembly-CSharp.dll");

            var collector = new Collector(mainAssmeblyPath);
            var records = collector.Collect().ToLookup(r => r.FinalInitVa);
            
            // dedup
            var lookup = records.ToLookup(g => g.Count() == 1);

            var b = new StringBuilder("Remove all small classes below" +
                                      "(Because these classes usually use commom function with an empty body which makes hook more hard):\n");
            foreach (var group in lookup[false])
            {
                foreach (var record in group)
                {
                    b.AppendLine($"{record.Name}");
                }
            }

            Logger.Info(b.ToString().TrimEnd());

            RECORDS = lookup[true].Select(g => g.First()).ToDictionary(r => r.Name);

        }

        public static Record ByName(string name)
        {
            return RECORDS[name];
        }

        public static IEnumerable<Record> Records()
        {
            return RECORDS.Values;
        }
    }
}
