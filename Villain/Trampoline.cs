using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Iced.Intel;

namespace Villain
{
    

    internal class Trampoline
    {
        private class MemCodeWriter : CodeWriter
        {
            private readonly IntPtr address;
            private int cursor;

            public MemCodeWriter(IntPtr address)
            {
                this.address = address;
            }

            public override void WriteByte(byte value)
            {
                Marshal.WriteByte(address, cursor++, value);
            }
        }

        public struct Info
        {
            public long Header;
            public long Target;
            public long Origin;
            public long Proxy;

            public override string ToString()
            {
                return $"{nameof(Header)}: {Header}, {nameof(Target)}: {Target}, {nameof(Origin)}: {Origin}, {nameof(Proxy)}: {Proxy}";
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void PostfixDelegate(IntPtr @this, IntPtr handle);


        private const int MEM_COMMIT = 0x00001000;
        private const int MEM_RESERVE = 0x00002000;
        private const int PAGE_EXECUTE_READWRITE = 0x40;

        private const int TRAMPOLINE_IN_ONE_PAGE = 100;
        private const int TRAMPOLINE_HEADER_SIZE = sizeof(ulong) * 4;

        private static readonly int TRAMPOLINE_SIZE;
        private static readonly int PAGE_SIZE;

        private static readonly Assembler TRAMPOLINE_TEMPLATE;
        private static readonly List<IntPtr> PAGES = new List<IntPtr>();
        private static int _cursor;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAlloc(IntPtr lpAddress, int dwSize, int flAllocationType, int flProtect);

        static Trampoline()
        {
            /*
             * ;Trampoline
             * ; +0x00 this
             * ; +0x08 fpTarget
             * ; +0x10 fpOrigin
             * ; +0x18 fpProxy
             * ; +0x20 Code
             */
            var asm = new Assembler(64);

            // The pointer to the block start
            var @this = asm.CreateLabel();
            var originAddr = asm.CreateLabel();
            var proxyAddr = asm.CreateLabel();
            var targetAddr = asm.CreateLabel();

            asm.Label(ref @this);
            asm.dq(0xCC_CC_CC_CC_CC_CC_CC_CC);
            asm.Label(ref targetAddr);
            asm.dq(0xCC_CC_CC_CC_CC_CC_CC_CC);
            asm.Label(ref originAddr);
            asm.dq(0xCC_CC_CC_CC_CC_CC_CC_CC);
            asm.Label(ref proxyAddr);
            asm.dq(0xCC_CC_CC_CC_CC_CC_CC_CC);

            asm.int3();
            asm.push(AssemblerRegisters.rdi);
            asm.sub(AssemblerRegisters.rsp, 32);
            asm.mov(AssemblerRegisters.rdi, AssemblerRegisters.rcx);
            asm.mov(AssemblerRegisters.rax, AssemblerRegisters.__[originAddr]);
            asm.call(AssemblerRegisters.rax);
            asm.mov(AssemblerRegisters.rcx, AssemblerRegisters.rdi);
            asm.mov(AssemblerRegisters.rdx, AssemblerRegisters.__[@this]);
            asm.mov(AssemblerRegisters.rax, AssemblerRegisters.__[proxyAddr]);
            asm.call(AssemblerRegisters.rax);
            asm.add(AssemblerRegisters.rsp, 32);
            asm.pop(AssemblerRegisters.rdi);
            asm.ret();

            TRAMPOLINE_TEMPLATE = asm;

            TRAMPOLINE_SIZE = CalcAsmLength(asm);
            PAGE_SIZE = TRAMPOLINE_SIZE * TRAMPOLINE_IN_ONE_PAGE;

            Logger.Debug($"Block Size = {TRAMPOLINE_SIZE}");
            PushNewPage();
        }

        private static int CalcAsmLength(Assembler asm)
        {
            var stream = new MemoryStream();
            asm.Assemble(new StreamCodeWriter(stream), 0);

            // Disassemble the result
            stream.Position = 0;
            var reader = new StreamCodeReader(stream);
            var decoder = Decoder.Create(64, reader);
            return decoder.Select(i => i.Length).Sum();
        }

        private static void PushNewPage()
        {
            var ptr = VirtualAlloc(IntPtr.Zero, PAGE_SIZE, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
            Logger.Assert(ptr != IntPtr.Zero, () => $"Allocation failed, Last error: {Marshal.GetLastWin32Error()}");

            PAGES.Add(ptr);
            _cursor = 0;
        }

        private static IntPtr NewBlock()
        {
            if (_cursor > TRAMPOLINE_IN_ONE_PAGE)
                // Alloc new page
                PushNewPage();

            return PAGES.Last() + TRAMPOLINE_SIZE * _cursor++;
        }

        public static Trampoline Generate(IntPtr target)
        {
            var blockAddr = NewBlock();

            var writer = new MemCodeWriter(blockAddr);

            var success = TRAMPOLINE_TEMPLATE.TryAssemble(writer, (ulong)blockAddr, out var errorMessage, out _);
            Logger.Assert(success, $"Assemble must success, error: {errorMessage}");

            // Save some info for debugging
            Marshal.WriteIntPtr(blockAddr, blockAddr);
            Marshal.WriteIntPtr(blockAddr, sizeof(ulong), target);

            return new Trampoline(blockAddr);
        }

        public static Info GetInfo(IntPtr handle)
        {
            var header = handle;

            var @this = Marshal.ReadInt64(header);
            var target = Marshal.ReadInt64(header, sizeof(ulong));
            var origin = Marshal.ReadInt64(header, sizeof(ulong) * 2);
            var proxy = Marshal.ReadInt64(header, sizeof(ulong) * 3);

            return new Info()
            {
                Header = @this,
                Origin = origin,
                Target = target,
                Proxy = proxy
            };
        }

        private readonly IntPtr header;

        public IntPtr Address => header + TRAMPOLINE_HEADER_SIZE;

        private Trampoline(IntPtr header)
        {
            this.header = header;
        }

        /// <summary>
        /// Complete the tampoline
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="postfix"></param>
        /// <returns>The handle of completed tampoline</returns>
        public IntPtr Finish(IntPtr origin, PostfixDelegate postfix)
        {
            // Update function pointer
            Marshal.WriteIntPtr(header, sizeof(long) * 2, origin);
            Marshal.WriteIntPtr(header, sizeof(long) * 3, Marshal.GetFunctionPointerForDelegate(postfix));

            // Remove int3 instruction
            Marshal.WriteByte(header + TRAMPOLINE_HEADER_SIZE, 0x90);

            return header;
        }
    }
}
