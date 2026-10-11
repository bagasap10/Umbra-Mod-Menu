using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpMonoInjector
{
    public class Memory : IDisposable
    {
        private readonly IntPtr _handle;

        private readonly Dictionary<IntPtr, int> _allocations = new Dictionary<IntPtr, int>();
        private bool _disposed, _preserveAllocations;

        // A timed-out remote thread can still be executing these pages. Never free underneath it.
        internal void PreserveAllocations() => _preserveAllocations = true;

        public Memory(IntPtr processHandle)
        {
            _handle = processHandle;
        }

        public string ReadString(IntPtr address, int length, Encoding encoding)
        {
            if (length <= 0 || length > 4096) throw new ArgumentOutOfRangeException(nameof(length));
            List<byte> bytes = new List<byte>();
            while (bytes.Count < length)
            {
                IntPtr next = address + bytes.Count;
                int pageRemaining = 4096 - (int)(next.ToInt64() & 4095);
                int amount = Math.Min(64, Math.Min(pageRemaining, length - bytes.Count));
                foreach (byte value in ReadBytes(next, amount))
                {
                    if (value == 0) return encoding.GetString(bytes.ToArray());
                    bytes.Add(value);
                }
            }

            return encoding.GetString(bytes.ToArray());
        }

        public string ReadUnicodeString(IntPtr address, int length)
        {
            return Encoding.Unicode.GetString(ReadBytes(address, length));
        }

        public short ReadShort(IntPtr address)
        {
            return BitConverter.ToInt16(ReadBytes(address, 2), 0);
        }

        public int ReadInt(IntPtr address)
        {
            return BitConverter.ToInt32(ReadBytes(address, 4), 0);
        }

        public long ReadLong(IntPtr address)
        {
            return BitConverter.ToInt64(ReadBytes(address, 8), 0);
        }

        public byte[] ReadBytes(IntPtr address, int size)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Memory));
            if (size < 0 || size > 32 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(size));
            byte[] bytes = new byte[size];

            if (!Native.ReadProcessMemory(_handle, address, bytes, size))
                throw new InjectorException("Failed to read process memory", new Win32Exception(Marshal.GetLastWin32Error()));

            return bytes;
        }

        public IntPtr AllocateAndWrite(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            IntPtr addr = Allocate(data.Length);
            Write(addr, data);
            return addr;
        }

        public IntPtr AllocateAndWrite(string data) => AllocateAndWrite(Encoding.UTF8.GetBytes((data ?? "") + "\0"));

        public IntPtr AllocateAndWrite(int data) => AllocateAndWrite(BitConverter.GetBytes(data));

        public IntPtr AllocateAndWrite(long data) => AllocateAndWrite(BitConverter.GetBytes(data));

        public IntPtr Allocate(int size)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Memory));
            if (size <= 0 || size > 32 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(size));
            IntPtr addr =
                Native.VirtualAllocEx(_handle, IntPtr.Zero, size,
                    AllocationType.MEM_RESERVE | AllocationType.MEM_COMMIT, MemoryProtection.PAGE_EXECUTE_READWRITE);

            if (addr == IntPtr.Zero)
                throw new InjectorException("Failed to allocate process memory", new Win32Exception(Marshal.GetLastWin32Error()));

            _allocations.Add(addr, size);
            return addr;
        }

        public void Write(IntPtr addr, byte[] data)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Memory));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length > 32 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(data));
            if (!Native.WriteProcessMemory(_handle, addr, data, data.Length))
                throw new InjectorException("Failed to write process memory", new Win32Exception(Marshal.GetLastWin32Error()));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (!_preserveAllocations)
                foreach (var kvp in _allocations)
                    Native.VirtualFreeEx(_handle, kvp.Key, 0, MemoryFreeType.MEM_RELEASE);
            _allocations.Clear();
        }
    }
}
