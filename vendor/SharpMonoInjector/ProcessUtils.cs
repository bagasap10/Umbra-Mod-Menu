using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpMonoInjector
{
    public static class ProcessUtils
    {
        public static IEnumerable<ExportedFunction> GetExportedFunctions(IntPtr handle, IntPtr mod)
        {
            using (Memory memory = new Memory(handle)) {
                int e_lfanew = memory.ReadInt(mod + 0x3C);
                IntPtr ntHeaders = mod + e_lfanew;
                IntPtr optionalHeader = ntHeaders + 0x18;
                IntPtr dataDirectory = optionalHeader + (Is64BitProcess(handle) ? 0x70 : 0x60);
                IntPtr exportDirectory = mod + memory.ReadInt(dataDirectory);
                IntPtr names = mod + memory.ReadInt(exportDirectory + 0x20);
                IntPtr ordinals = mod + memory.ReadInt(exportDirectory + 0x24);
                IntPtr functions = mod + memory.ReadInt(exportDirectory + 0x1C);
                int count = memory.ReadInt(exportDirectory + 0x18);
                if (count < 0 || count > 65536) throw new InjectorException("Invalid Mono export table size.");
                int functionCount = memory.ReadInt(exportDirectory + 0x14);
                if (functionCount < 0 || functionCount > 65536) throw new InjectorException("Invalid Mono function table size.");
                byte[] nameTable = memory.ReadBytes(names, checked(count * 4));
                byte[] ordinalTable = memory.ReadBytes(ordinals, checked(count * 2));
                byte[] functionTable = memory.ReadBytes(functions, checked(functionCount * 4));

                for (int i = 0; i < count; i++) {
                    int offset = BitConverter.ToInt32(nameTable, i * 4);
                    string name = memory.ReadString(mod + offset, 32, Encoding.ASCII);
                    int ordinal = BitConverter.ToUInt16(ordinalTable, i * 2);
                    if (ordinal >= functionCount) throw new InjectorException("Invalid Mono export ordinal.");
                    IntPtr address = mod + BitConverter.ToInt32(functionTable, ordinal * 4);

                    if (address != IntPtr.Zero)
                        yield return new ExportedFunction(name, address);
                }
            }
        }

        public static bool GetMonoModule(IntPtr handle, out IntPtr monoModule)
        {
            int size = Is64BitProcess(handle) ? 8 : 4;

            IntPtr[] ptrs = new IntPtr[0];

            if (!Native.EnumProcessModulesEx(
                handle, ptrs, 0, out int bytesNeeded, ModuleFilter.LIST_MODULES_ALL)) {
                throw new InjectorException("Failed to enumerate process modules", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            int count = bytesNeeded / IntPtr.Size;
            if (count <= 0 || count > 16384) throw new InjectorException("Invalid process module count.");
            ptrs = new IntPtr[count];

            if (!Native.EnumProcessModulesEx(
                handle, ptrs, bytesNeeded, out bytesNeeded, ModuleFilter.LIST_MODULES_ALL)) {
                throw new InjectorException("Failed to enumerate process modules", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            for (int i = 0; i < count; i++) {
                StringBuilder path = new StringBuilder(260);
                Native.GetModuleFileNameEx(handle, ptrs[i], path, 260);

                if (path.ToString().IndexOf("mono", StringComparison.OrdinalIgnoreCase) > -1) {
                    if (!Native.GetModuleInformation(handle, ptrs[i], out MODULEINFO info, (uint)Marshal.SizeOf(typeof(MODULEINFO))))
                        throw new InjectorException("Failed to get module information", new Win32Exception(Marshal.GetLastWin32Error()));

                    var funcs = GetExportedFunctions(handle, info.lpBaseOfDll);

                    if (funcs.Any(f => f.Name == "mono_get_root_domain")) {
                        monoModule = info.lpBaseOfDll;
                        return true;
                    }
                }
            }

            monoModule = IntPtr.Zero;
            return false;
        }

        public static bool Is64BitProcess(IntPtr handle)
        {
            if (!Environment.Is64BitOperatingSystem)
                return false;

            if (!Native.IsWow64Process(handle, out bool isWow64))
                throw new InjectorException("Unable to verify process architecture.", new Win32Exception(Marshal.GetLastWin32Error()));

            return !isWow64;
        }
    }
}
