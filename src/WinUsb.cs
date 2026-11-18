// Accesso diretto a un dispositivo USB tramite il driver WinUSB di Windows
// (quello installato con Zadig). Nessuna libreria esterna necessaria.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace XumerTool
{
    public class UsbTimeoutException : Exception
    {
        public UsbTimeoutException(string msg) : base(msg) { }
    }

    public sealed class WinUsbDevice : IDisposable
    {
        const int DIGCF_PRESENT = 0x02;
        const int DIGCF_DEVICEINTERFACE = 0x10;
        const uint PIPE_TRANSFER_TIMEOUT = 0x03;
        const int ERROR_SEM_TIMEOUT = 121;

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct WINUSB_SETUP_PACKET
        {
            public byte RequestType;
            public byte Request;
            public ushort Value;
            public ushort Index;
            public ushort Length;
        }

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr SetupDiGetClassDevsW(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, int flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInterfaces(IntPtr devInfo, IntPtr devInfoData, ref Guid interfaceGuid, int index, ref SP_DEVICE_INTERFACE_DATA data);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool SetupDiGetDeviceInterfaceDetailW(IntPtr devInfo, ref SP_DEVICE_INTERFACE_DATA data, IntPtr detail, int detailSize, out int required, IntPtr devInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiDestroyDeviceInfoList(IntPtr devInfo);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_Initialize(SafeFileHandle device, out IntPtr iface);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_Free(IntPtr iface);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_ControlTransfer(IntPtr iface, WINUSB_SETUP_PACKET setup, byte[] buffer, uint length, out uint transferred, IntPtr overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_ReadPipe(IntPtr iface, byte pipe, byte[] buffer, uint length, out uint transferred, IntPtr overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_WritePipe(IntPtr iface, byte pipe, byte[] buffer, uint length, out uint transferred, IntPtr overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_SetPipePolicy(IntPtr iface, byte pipe, uint policy, uint valueLength, ref uint value);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_ResetPipe(IntPtr iface, byte pipe);

        [DllImport("winusb.dll", SetLastError = true)]
        static extern bool WinUsb_AbortPipe(IntPtr iface, byte pipe);

        SafeFileHandle file;
        IntPtr iface;
        readonly Dictionary<byte, uint> timeouts = new Dictionary<byte, uint>();

        /// <summary>GUID d'interfaccia assegnati da Zadig al dispositivo, letti dal registro.</summary>
        static List<Guid> GuidsFromRegistry(int vid, int pid)
        {
            List<Guid> list = new List<Guid>();
            string keyName = string.Format(@"SYSTEM\CurrentControlSet\Enum\USB\VID_{0:X4}&PID_{1:X4}", vid, pid);
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(keyName))
                {
                    if (k == null) return list;
                    foreach (string sub in k.GetSubKeyNames())
                    {
                        using (RegistryKey p = k.OpenSubKey(sub + @"\Device Parameters"))
                        {
                            if (p == null) continue;
                            List<string> vals = new List<string>();
                            string[] multi = p.GetValue("DeviceInterfaceGUIDs") as string[];
                            if (multi != null) vals.AddRange(multi);
                            string single = p.GetValue("DeviceInterfaceGUID") as string;
                            if (single != null) vals.Add(single);
                            foreach (string v in vals)
                            {
                                Guid g;
                                if (Guid.TryParse(v.Trim(), out g) && !list.Contains(g)) list.Add(g);
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            return list;
        }

        public static List<string> FindDevicePaths(int vid, int pid)
        {
            List<string> paths = new List<string>();
            string match = string.Format("vid_{0:x4}&pid_{1:x4}", vid, pid);
            foreach (Guid guid in GuidsFromRegistry(vid, pid))
            {
                Guid g = guid;
                IntPtr set = SetupDiGetClassDevsW(ref g, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
                if (set == new IntPtr(-1)) continue;
                try
                {
                    for (int i = 0; ; i++)
                    {
                        SP_DEVICE_INTERFACE_DATA d = new SP_DEVICE_INTERFACE_DATA();
                        d.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
                        if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref g, i, ref d)) break;
                        int size;
                        SetupDiGetDeviceInterfaceDetailW(set, ref d, IntPtr.Zero, 0, out size, IntPtr.Zero);
                        if (size <= 0) continue;
                        IntPtr buf = Marshal.AllocHGlobal(size);
                        try
                        {
                            Marshal.WriteInt32(buf, IntPtr.Size == 8 ? 8 : 6);
                            if (SetupDiGetDeviceInterfaceDetailW(set, ref d, buf, size, out size, IntPtr.Zero))
                            {
                                string path = Marshal.PtrToStringUni(new IntPtr(buf.ToInt64() + 4));
                                if (path != null && path.ToLowerInvariant().Contains(match) && !paths.Contains(path))
                                    paths.Add(path);
                            }
                        }
                        finally { Marshal.FreeHGlobal(buf); }
                    }
                }
                finally { SetupDiDestroyDeviceInfoList(set); }
            }
            return paths;
        }

        public static WinUsbDevice Open(string path)
        {
            WinUsbDevice dev = new WinUsbDevice();
            dev.file = CreateFileW(path, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000080, IntPtr.Zero);
            if (dev.file.IsInvalid)
            {
                int err = Marshal.GetLastWin32Error();
                if (err == 5)
                    throw new Exception(Lang.T("L'adattatore è già in uso da un altro programma (chiudi la pagina web o altri programmi che lo usano)."));
                throw new Win32Exception(err);
            }
            if (!WinUsb_Initialize(dev.file, out dev.iface))
            {
                int err = Marshal.GetLastWin32Error();
                dev.file.Dispose();
                throw new Win32Exception(err, Lang.T("WinUsb_Initialize fallito"));
            }
            return dev;
        }

        static Exception LastError(string what)
        {
            int err = Marshal.GetLastWin32Error();
            if (err == ERROR_SEM_TIMEOUT) return new UsbTimeoutException(Lang.T("Timeout USB (") + what + ")");
            return new Win32Exception(err, what + ": " + new Win32Exception(err).Message);
        }

        public byte[] ControlIn(byte requestType, byte request, ushort value, ushort index, int length)
        {
            WINUSB_SETUP_PACKET s = new WINUSB_SETUP_PACKET();
            s.RequestType = requestType; s.Request = request; s.Value = value; s.Index = index; s.Length = (ushort)length;
            byte[] buf = new byte[length];
            uint got;
            if (!WinUsb_ControlTransfer(iface, s, buf, (uint)length, out got, IntPtr.Zero)) throw LastError(Lang.T("control IN"));
            byte[] r = new byte[got];
            Array.Copy(buf, r, got);
            return r;
        }

        public void ControlOut(byte requestType, byte request, ushort value, ushort index)
        {
            WINUSB_SETUP_PACKET s = new WINUSB_SETUP_PACKET();
            s.RequestType = requestType; s.Request = request; s.Value = value; s.Index = index; s.Length = 0;
            uint got;
            if (!WinUsb_ControlTransfer(iface, s, null, 0, out got, IntPtr.Zero)) throw LastError(Lang.T("control OUT"));
        }

        void SetTimeout(byte pipe, int ms)
        {
            uint v = (uint)ms;
            uint cur;
            if (timeouts.TryGetValue(pipe, out cur) && cur == v) return;
            if (!WinUsb_SetPipePolicy(iface, pipe, PIPE_TRANSFER_TIMEOUT, 4, ref v)) throw LastError("SetPipePolicy");
            timeouts[pipe] = v;
        }

        public byte[] Read(byte pipe, int length, int timeoutMs)
        {
            SetTimeout(pipe, timeoutMs);
            byte[] buf = new byte[length];
            uint got;
            if (!WinUsb_ReadPipe(iface, pipe, buf, (uint)length, out got, IntPtr.Zero)) throw LastError("lettura");
            if (got == length) return buf;
            byte[] r = new byte[got];
            Array.Copy(buf, r, got);
            return r;
        }

        public void Write(byte pipe, byte[] data, int offset, int length, int timeoutMs)
        {
            SetTimeout(pipe, timeoutMs);
            byte[] buf = data;
            if (offset != 0 || length != data.Length)
            {
                buf = new byte[length];
                Array.Copy(data, offset, buf, 0, length);
            }
            uint sent;
            if (!WinUsb_WritePipe(iface, pipe, buf, (uint)length, out sent, IntPtr.Zero)) throw LastError("scrittura");
            if (sent != length) throw new Exception(Lang.T("Scrittura USB incompleta"));
        }

        public void ResetPipe(byte pipe) { WinUsb_ResetPipe(iface, pipe); }
        public void AbortPipe(byte pipe) { WinUsb_AbortPipe(iface, pipe); }

        public void Dispose()
        {
            if (iface != IntPtr.Zero) { WinUsb_Free(iface); iface = IntPtr.Zero; }
            if (file != null) { file.Dispose(); file = null; }
        }
    }
}
