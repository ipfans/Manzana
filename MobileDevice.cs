namespace Manzana
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Win32;

    internal class MobileDevice
    {
        private static readonly DirectoryInfo ApplicationSupportDirectory = new DirectoryInfo(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Apple Inc.\Apple Application Support", "InstallDir", Environment.CurrentDirectory).ToString());
        private const string DLLName = "iTunesMobileDevice.dll";
        private static readonly FileInfo iTunesMobileDeviceFile = new FileInfo(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Apple Inc.\Apple Mobile Device Support\Shared", "iTunesMobileDeviceDLL", "iTunesMobileDevice.dll").ToString());

        static MobileDevice()
        {
            string directoryName = iTunesMobileDeviceFile.DirectoryName;
            if (!iTunesMobileDeviceFile.Exists)
            {
                directoryName = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + @"\Apple\Mobile Device Support\bin";
                if (!File.Exists(directoryName + @"\iTunesMobileDevice.dll"))
                {
                    directoryName = @"C:\Program Files\Apple\Mobile Device Support\bin";
                }
            }
            Environment.SetEnvironmentVariable("Path", string.Join(";", new string[] { Environment.GetEnvironmentVariable("Path"), directoryName, ApplicationSupportDirectory.FullName }));
        }

        [DllImport("CoreFoundation.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe void* __CFStringMakeConstantString(byte[] s);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCConnectionClose(void* conn);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCConnectionGetFSBlockSize(void* conn);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCConnectionInvalidate(void* conn);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCConnectionIsValid(void* conn);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCConnectionOpen(void* handle, uint io_timeout, ref void* conn);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCDeviceInfoOpen(void* conn, ref void* dict);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCDirectoryClose(void* conn, void* dir);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCDirectoryCreate(void* conn, string path);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCDirectoryOpen(void* conn, byte[] path, ref void* dir);
        public static unsafe int AFCDirectoryOpen(void* conn, string path, ref void* dir)
        {
            return AFCDirectoryOpen(conn, Encoding.UTF8.GetBytes(path), ref dir);
        }

        public static unsafe int AFCDirectoryRead(void* conn, void* dir, ref string buffer)
        {
            void* dirent = null;
            int num = AFCDirectoryRead(conn, dir, ref dirent);
            if ((num == 0) && (dirent != null))
            {
                buffer = Marshal.PtrToStringAnsi(new IntPtr(dirent));
                return num;
            }
            buffer = null;
            return num;
        }

        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCDirectoryRead(void* conn, void* dir, ref void* dirent);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCFileInfoOpen(void* conn, string path, ref void* dict);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCFileRefClose(void* conn, long handle);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCFileRefOpen(void* conn, string path, int mode, int unknown, out long handle);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCFileRefRead(void* conn, long handle, byte[] buffer, ref uint len);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCFileRefSeek(void* conn, long handle, uint pos, uint origin);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCFileRefSetFileSize(void* conn, long handle, uint size);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCFileRefTell(void* conn, long handle, ref uint position);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCFileRefWrite(void* conn, long handle, byte[] buffer, uint len);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCFlushData(void* conn, long handle);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCKeyValueClose(void* dict);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCKeyValueRead(void* dict, out void* key, out void* val);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCRemovePath(void* conn, string path);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AFCRenamePath(void* conn, string old_path, string new_path);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceActivate(void* device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceConnect(void* device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe IntPtr AMDeviceCopyDeviceIdentifier(void* device);
        public static unsafe string AMDeviceCopyValue(void* device, string name)
        {
            IntPtr ptr = AMDeviceCopyValue_IntPtr(device, 0, CFStringMakeConstantString(name));
            if (ptr != IntPtr.Zero)
            {
                byte len = Marshal.ReadByte(ptr, 8);
                if (len > 0)
                {
                    return Marshal.PtrToStringAnsi(new IntPtr(ptr.ToInt64() + 9L), len);
                }
            }
            return string.Empty;
        }

        [DllImport("iTunesMobileDevice.dll", EntryPoint="AMDeviceCopyValue", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe IntPtr AMDeviceCopyValue_IntPtr(void* device, uint unknown, void* cfstring);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceDeactivate(void* device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceDisconnect(void* device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceEnterRecovery(void* device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceGetConnectionID(void* device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceIsPaired(void* device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceNotificationSubscribe(DeviceNotificationCallback callback, uint unused1, uint unused2, uint unused3, out void* am_device_notification_ptr);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceRelease(void* device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceStartService(void* device, void* service_name, ref void* handle, void* unknown);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceStartSession(void* device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceStopSession(void* device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMDeviceValidatePairing(void* device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern string AMRecoveryModeDeviceCopyIMEI(AMRecoveryDevice device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern string AMRecoveryModeDeviceCopySerialNumber(AMRecoveryDevice device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern int AMRecoveryModeDeviceReboot(AMRecoveryDevice device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern int AMRecoveryModeDeviceSetAutoBoot(AMRecoveryDevice device, byte paramByte);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern string AMRestoreModeDeviceCopyIMEI(AMRecoveryDevice device);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern int AMRestoreModeDeviceCreate(uint unknown0, int connection_id, uint unknown1);
        [DllImport("iTunesMobileDevice.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern unsafe int AMRestoreRegisterForDeviceNotifications(DeviceRestoreNotificationCallback dfu_connect, DeviceRestoreNotificationCallback recovery_connect, DeviceRestoreNotificationCallback dfu_disconnect, DeviceRestoreNotificationCallback recovery_disconnect, uint unknown0, void* user_info);
        public static unsafe void* CFStringMakeConstantString(string s)
        {
            return __CFStringMakeConstantString(StringToCString(s));
        }

        public static string CFStringToString(byte[] value)
        {
            return Encoding.ASCII.GetString(value, 9, value[9]);
        }

        public static byte[] StringToCFString(string value)
        {
            byte[] bytes = new byte[value.Length + 10];
            bytes[4] = 140;
            bytes[5] = 7;
            bytes[6] = 1;
            bytes[8] = (byte) value.Length;
            Encoding.ASCII.GetBytes(value, 0, value.Length, bytes, 9);
            return bytes;
        }

        public static byte[] StringToCString(string value)
        {
            byte[] bytes = new byte[value.Length + 1];
            Encoding.ASCII.GetBytes(value, 0, value.Length, bytes, 0);
            return bytes;
        }
    }
}

