using System;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using UnhollowerBaseLib;

namespace Villain
{
    internal static class Il2CppHelper
    {
        public static object Il2CppObjectPtrToIl2CppObjectByType(IntPtr ptr, Type type)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentException("The ptr cannot be IntPtr.Zero.");
            if (!UnhollowerSupport.IsGeneratedAssemblyType(type))
                throw new ArgumentException("The type must be a Generated Assembly Type.");

            var ctor = type?.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new[]
            {
                typeof(IntPtr)
            }, new ParameterModifier[0]) ?? throw new MissingMethodException($"{type?.FullName}.ctor(IntPtr) not found");

            try
            {
                var obj = ctor.Invoke(new object[] { ptr });
                return obj;
            }
            catch (Exception e)
            {
                Logger.Warn($"Exception while constructing {type.FullName}: {e}");
                return null;
            }
        }

        /// <summary>
        /// Dot *NOT* use <see cref="IL2CPP.il2cpp_string_intern"/> whose definition is not well-formed.
        /// The parameter is <c>Il2CppString*</c>, not a <c>const char* str</c>, so it requires a custom <c>Il2CppString</c> object instead a <see cref="string"/>.
        /// See <see href="https://github.com/4ch12dy/il2cpp/blob/93f63348743a017cfb7267f64b2b7a2cdae8af51/unity_2019_x/libil2cpp/il2cpp-api.cpp#L1130">the source of Il2Cpp</see>.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern IntPtr il2cpp_string_intern(IntPtr str);
    }
}
