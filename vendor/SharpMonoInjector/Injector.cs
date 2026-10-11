using System;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;

namespace SharpMonoInjector
{
    public class Injector : IDisposable
    {
        private const string mono_get_root_domain = "mono_get_root_domain";

        private const string mono_thread_attach = "mono_thread_attach";

        private const string mono_image_open_from_data = "mono_image_open_from_data";

        private const string mono_assembly_load_from_full = "mono_assembly_load_from_full";

        private const string mono_assembly_get_image = "mono_assembly_get_image";

        private const string mono_class_from_name = "mono_class_from_name";

        private const string mono_class_get_method_from_name = "mono_class_get_method_from_name";

        private const string mono_runtime_invoke = "mono_runtime_invoke";

        private const string mono_assembly_close = "mono_assembly_close";

        private const string mono_image_strerror = "mono_image_strerror";

        private readonly Dictionary<string, IntPtr> Exports = new Dictionary<string, IntPtr>
        {
            { mono_get_root_domain, IntPtr.Zero },
            { mono_thread_attach, IntPtr.Zero },
            { mono_image_open_from_data, IntPtr.Zero },
            { mono_assembly_load_from_full, IntPtr.Zero },
            { mono_assembly_get_image, IntPtr.Zero },
            { mono_class_from_name, IntPtr.Zero },
            { mono_class_get_method_from_name, IntPtr.Zero },
            { mono_runtime_invoke, IntPtr.Zero },
            { mono_assembly_close, IntPtr.Zero },
            { mono_image_strerror, IntPtr.Zero }
        };

        private Memory _memory;

        private IntPtr _rootDomain;

        private bool _attach;

        private readonly IntPtr _handle;

        private IntPtr _mono;
        private bool _disposed;

        public bool Is64Bit { get; private set; }

        public Injector(int processId)
        {
            const ProcessAccessRights access = ProcessAccessRights.PROCESS_CREATE_THREAD | ProcessAccessRights.PROCESS_QUERY_INFORMATION
                | ProcessAccessRights.PROCESS_VM_OPERATION | ProcessAccessRights.PROCESS_VM_READ | ProcessAccessRights.PROCESS_VM_WRITE;
            if ((_handle = Native.OpenProcess(access, false, processId)) == IntPtr.Zero)
                throw new InjectorException("Failed to open process", new Win32Exception(Marshal.GetLastWin32Error()));
            try
            {
                Is64Bit = ProcessUtils.Is64BitProcess(_handle);
                if (!ProcessUtils.GetMonoModule(_handle, out _mono))
                    throw new InjectorException("The game's Mono runtime is not ready yet.");
                _memory = new Memory(_handle);
            }
            catch { Native.CloseHandle(_handle); throw; }
        }

        public Injector(IntPtr processHandle, IntPtr monoModule)
        {
            if ((_handle = processHandle) == IntPtr.Zero)
                throw new ArgumentException("Argument cannot be zero", nameof(processHandle));

            if ((_mono = monoModule) == IntPtr.Zero)
                throw new ArgumentException("Argument cannot be zero", nameof(monoModule));

            Is64Bit = ProcessUtils.Is64BitProcess(_handle);
            _memory = new Memory(_handle);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _memory?.Dispose();
            Native.CloseHandle(_handle);
        }

        private void ObtainMonoExports()
        {
            foreach (ExportedFunction ef in ProcessUtils.GetExportedFunctions(_handle, _mono))
                if (Exports.ContainsKey(ef.Name))
                    Exports[ef.Name] = ef.Address;

            foreach (var kvp in Exports)
                if (kvp.Value == IntPtr.Zero)
                    throw new InjectorException($"Failed to obtain the address of {kvp.Key}()");
        }

        public IntPtr Inject(byte[] rawAssembly, string @namespace, string className, string methodName)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Injector));
            if (rawAssembly == null)
                throw new ArgumentNullException(nameof(rawAssembly));

            if (rawAssembly.Length == 0)
                throw new ArgumentException($"{nameof(rawAssembly)} cannot be empty", nameof(rawAssembly));
            if (rawAssembly.Length > 32 * 1024 * 1024) throw new ArgumentException("Assembly exceeds the 32 MB limit.", nameof(rawAssembly));

            if (className == null)
                throw new ArgumentNullException(nameof(className));

            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName));

            IntPtr rawImage, assembly, image, @class, method;

            ObtainMonoExports();
            _rootDomain = GetRootDomain();
            _attach = true;
            rawImage = OpenImageFromData(rawAssembly);
            assembly = OpenAssemblyFromImage(rawImage);
            image = GetImageFromAssembly(assembly);
            @class = GetClassFromName(image, @namespace, className);
            method = GetMethodFromName(@class, methodName);
            RuntimeInvoke(method);
            return assembly;
        }

        public void Eject(IntPtr assembly, string @namespace, string className, string methodName)
        {
            if (assembly == IntPtr.Zero)
                throw new ArgumentException($"{nameof(assembly)} cannot be zero", nameof(assembly));

            if (className == null)
                throw new ArgumentNullException(nameof(className));

            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName));

            IntPtr image, @class, method;

            ObtainMonoExports();
            _rootDomain = GetRootDomain();
            _attach = true;
            image = GetImageFromAssembly(assembly);
            @class = GetClassFromName(image, @namespace, className);
            method = GetMethodFromName(@class, methodName);
            RuntimeInvoke(method);
            CloseAssembly(assembly);
        }

        private static void ThrowIfNull(IntPtr ptr, string methodName)
        {
            if (ptr == IntPtr.Zero)
                throw new InjectorException($"{methodName}() returned NULL");
        }

        private IntPtr GetRootDomain()
        {
            IntPtr rootDomain = Execute(Exports[mono_get_root_domain]);
            ThrowIfNull(rootDomain, mono_get_root_domain);
            return rootDomain;
        }

        private IntPtr OpenImageFromData(byte[] assembly)
        {
            IntPtr statusPtr = _memory.Allocate(4);
            IntPtr rawImage = Execute(Exports[mono_image_open_from_data],
                _memory.AllocateAndWrite(assembly), (IntPtr)assembly.Length, (IntPtr)1, statusPtr);

            MonoImageOpenStatus status = (MonoImageOpenStatus)_memory.ReadInt(statusPtr);
            
            if (status != MonoImageOpenStatus.MONO_IMAGE_OK) {
                IntPtr messagePtr = Execute(Exports[mono_image_strerror], (IntPtr)status);
                string message = _memory.ReadString(messagePtr, 256, Encoding.UTF8);
                throw new InjectorException($"{mono_image_open_from_data}() failed: {message}");
            }

            ThrowIfNull(rawImage, mono_image_open_from_data);
            return rawImage;
        }

        private IntPtr OpenAssemblyFromImage(IntPtr image)
        {
            IntPtr statusPtr = _memory.Allocate(4);
            IntPtr assembly = Execute(Exports[mono_assembly_load_from_full],
                image, _memory.AllocateAndWrite(new byte[1]), statusPtr, IntPtr.Zero);

            MonoImageOpenStatus status = (MonoImageOpenStatus)_memory.ReadInt(statusPtr);

            if (status != MonoImageOpenStatus.MONO_IMAGE_OK) {
                IntPtr messagePtr = Execute(Exports[mono_image_strerror], (IntPtr)status);
                string message = _memory.ReadString(messagePtr, 256, Encoding.UTF8);
                throw new InjectorException($"{mono_assembly_load_from_full}() failed: {message}");
            }

            ThrowIfNull(assembly, mono_assembly_load_from_full);
            return assembly;
        }

        private IntPtr GetImageFromAssembly(IntPtr assembly)
        {
            IntPtr image = Execute(Exports[mono_assembly_get_image], assembly);
            ThrowIfNull(image, mono_assembly_get_image);
            return image;
        }

        private IntPtr GetClassFromName(IntPtr image, string @namespace, string className)
        {
            IntPtr @class = Execute(Exports[mono_class_from_name],
                image, _memory.AllocateAndWrite(@namespace), _memory.AllocateAndWrite(className));
            ThrowIfNull(@class, mono_class_from_name);
            return @class;
        }

        private IntPtr GetMethodFromName(IntPtr @class, string methodName)
        {
            IntPtr method = Execute(Exports[mono_class_get_method_from_name],
                @class, _memory.AllocateAndWrite(methodName), IntPtr.Zero);
            ThrowIfNull(method, mono_class_get_method_from_name);
            return method;
        }

        private void RuntimeInvoke(IntPtr method)
        {
            IntPtr excPtr = Is64Bit ? _memory.AllocateAndWrite((long)0) : _memory.AllocateAndWrite(0);

            IntPtr result = Execute(Exports[mono_runtime_invoke],
                method, IntPtr.Zero, IntPtr.Zero, excPtr);

            IntPtr exc = Is64Bit ? (IntPtr)_memory.ReadLong(excPtr) : (IntPtr)_memory.ReadInt(excPtr);

            if (exc != IntPtr.Zero) {
                // The object is not a retained GC root after the remote thread exits. Never dereference it.
                throw new InjectorException("The menu entry point threw a managed exception. See the game's Player.log and verify the matching menu build.");
            }
        }

        private void CloseAssembly(IntPtr assembly)
        {
            IntPtr result = Execute(Exports[mono_assembly_close], assembly);
            ThrowIfNull(result, mono_assembly_close);
        }

        private IntPtr Execute(IntPtr address, params IntPtr[] args)
        {
            IntPtr retValPtr = Is64Bit
                ? _memory.AllocateAndWrite((long)0)
                : _memory.AllocateAndWrite(0);

            byte[] code = Assemble(address, retValPtr, args);
            IntPtr alloc = _memory.AllocateAndWrite(code);
            if (!Native.FlushInstructionCache(_handle, alloc, (UIntPtr)code.Length))
                throw new InjectorException("Failed to flush the instruction cache.", new Win32Exception(Marshal.GetLastWin32Error()));

            IntPtr thread = Native.CreateRemoteThread(
                _handle, IntPtr.Zero, 0, alloc, IntPtr.Zero, 0, out _);

            if (thread == IntPtr.Zero)
                throw new InjectorException("Failed to create a remote thread", new Win32Exception(Marshal.GetLastWin32Error()));

            try
            {
                WaitResult result = Native.WaitForSingleObject(thread, 15000);
                if (result != WaitResult.WAIT_OBJECT_0)
                {
                    _memory.PreserveAllocations();
                    throw new InjectorException("The game's Mono call did not complete safely. Restart the game before another attempt.");
                }
                if (!Native.GetExitCodeThread(thread, out uint exitCode) || exitCode == 0xC0000005)
                    throw new InjectorException("The remote Mono call failed. Restart the game before another attempt.");
            }
            finally { Native.CloseHandle(thread); }

            IntPtr ret = Is64Bit
                ? (IntPtr)_memory.ReadLong(retValPtr)
                : (IntPtr)_memory.ReadInt(retValPtr);

            if ((long)ret == 0x00000000C0000005)
                throw new InjectorException($"An access violation occurred while executing {Exports.First(e => e.Value == address).Key}()");

            return ret;
        }

        private byte[] Assemble(IntPtr functionPtr, IntPtr retValPtr, IntPtr[] args)
        {
            return Is64Bit
                ? Assemble64(functionPtr, retValPtr, args)
                : Assemble86(functionPtr, retValPtr, args);
        }

        private byte[] Assemble86(IntPtr functionPtr, IntPtr retValPtr, IntPtr[] args)
        {
            Assembler asm = new Assembler();

            if (_attach) {
                asm.Push(_rootDomain);
                asm.MovEax(Exports[mono_thread_attach]);
                asm.CallEax();
                asm.AddEsp(4);
            }

            for (int i = args.Length - 1; i >= 0; i--)
                asm.Push(args[i]);

            asm.MovEax(functionPtr);
            asm.CallEax();
            asm.AddEsp((byte)(args.Length * 4));
            asm.MovEaxTo(retValPtr);
            asm.Return();

            return asm.ToByteArray();
        }

        private byte[] Assemble64(IntPtr functionPtr, IntPtr retValPtr, IntPtr[] args)
        {
            Assembler asm = new Assembler();

            asm.SubRsp(40);

            if (_attach) {
                asm.MovRax(Exports[mono_thread_attach]);
                asm.MovRcx(_rootDomain);
                asm.CallRax();
            }

            asm.MovRax(functionPtr);

            for (int i = 0; i < args.Length; i++) {
                switch (i) {
                    case 0:
                        asm.MovRcx(args[i]);
                        break;
                    case 1:
                        asm.MovRdx(args[i]);
                        break;
                    case 2:
                        asm.MovR8(args[i]);
                        break;
                    case 3:
                        asm.MovR9(args[i]);
                        break;
                }
            }

            asm.CallRax();
            asm.AddRsp(40);
            asm.MovRaxTo(retValPtr);
            asm.Return();

            return asm.ToByteArray();
        }
    }
}
