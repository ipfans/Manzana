namespace Manzana
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public unsafe class iPhone
    {
        private bool connected;
        private string current_directory;
        internal AMRecoveryDevice DFUHandle;
        private DeviceNotificationCallback dnc;
        private DeviceRestoreNotificationCallback drn1;
        private DeviceRestoreNotificationCallback drn2;
        private DeviceRestoreNotificationCallback drn3;
        private DeviceRestoreNotificationCallback drn4;
        internal unsafe void* hAFC;
        internal unsafe void* hService;
        internal unsafe void* iPhoneHandle;
        private static char[] path_separators = new char[] { '/' };
        private bool wasAFC2;

        public event ConnectEventHandler Connect;

        public event DeviceNotificationEventHandler DfuConnect;

        public event DeviceNotificationEventHandler DfuDisconnect;

        public event ConnectEventHandler Disconnect;

        public event DeviceNotificationEventHandler RecoveryModeEnter;

        public event DeviceNotificationEventHandler RecoveryModeLeave;

        public iPhone()
        {
            this.wasAFC2 = false;
            this.doConstruction();
        }

        public iPhone(ConnectEventHandler myConnectHandler, ConnectEventHandler myDisconnectHandler)
        {
            this.wasAFC2 = false;
            this.Connect = (ConnectEventHandler) Delegate.Combine(this.Connect, myConnectHandler);
            this.Disconnect = (ConnectEventHandler) Delegate.Combine(this.Disconnect, myDisconnectHandler);
            this.doConstruction();
        }

        private unsafe bool ConnectToPhone()
        {
            if (MobileDevice.AMDeviceConnect(this.iPhoneHandle) == 1)
            {
                throw new Exception("Phone in recovery mode, support not yet implemented");
            }
            if (MobileDevice.AMDeviceIsPaired(this.iPhoneHandle) == 0)
            {
                return false;
            }
            if (MobileDevice.AMDeviceValidatePairing(this.iPhoneHandle) != 0)
            {
                return false;
            }
            if (MobileDevice.AMDeviceStartSession(this.iPhoneHandle) == 1)
            {
                return false;
            }
            if (MobileDevice.AMDeviceStartService(this.iPhoneHandle, MobileDevice.CFStringMakeConstantString("com.apple.afc2"), ref this.hService, null) != 0)
            {
                if (MobileDevice.AMDeviceStartService(this.iPhoneHandle, MobileDevice.CFStringMakeConstantString("com.apple.afc"), ref this.hService, null) != 0)
                {
                    return false;
                }
            }
            else
            {
                this.wasAFC2 = true;
            }
            if (MobileDevice.AFCConnectionOpen(this.hService, 0, ref this.hAFC) != 0)
            {
                return false;
            }
            this.connected = true;
            return true;
        }

        public void Copy(string sourceName, string destName)
        {
        }

        public unsafe bool CreateDirectory(string path)
        {
            return (MobileDevice.AFCDirectoryCreate(this.hAFC, this.FullPath(this.CurrentDirectory, path)) == 0);
        }

        public unsafe void DeleteDirectory(string path)
        {
            string str = this.FullPath(this.CurrentDirectory, path);
            if (this.IsDirectory(str))
            {
                MobileDevice.AFCRemovePath(this.hAFC, str);
            }
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            if (!recursive)
            {
                this.DeleteDirectory(path);
            }
            else
            {
                string str = this.FullPath(this.CurrentDirectory, path);
                if (this.IsDirectory(str))
                {
                    this.InternalDeleteDirectory(path);
                }
            }
        }

        public unsafe void DeleteFile(string path)
        {
            string str = this.FullPath(this.CurrentDirectory, path);
            if (this.Exists(str))
            {
                MobileDevice.AFCRemovePath(this.hAFC, str);
            }
        }

        private void DfuConnectCallback(ref AMRecoveryDevice callback)
        {
            this.DFUHandle = callback;
            this.OnDfuConnect(new DeviceNotificationEventArgs(callback));
        }

        private void DfuDisconnectCallback(ref AMRecoveryDevice callback)
        {
            this.DFUHandle = callback;
            this.OnDfuDisconnect(new DeviceNotificationEventArgs(callback));
        }

        private unsafe void doConstruction()
        {
            void* voidPtr;
            this.dnc = new DeviceNotificationCallback(this.NotifyCallback);
            this.drn1 = new DeviceRestoreNotificationCallback(this.DfuConnectCallback);
            this.drn2 = new DeviceRestoreNotificationCallback(this.RecoveryConnectCallback);
            this.drn3 = new DeviceRestoreNotificationCallback(this.DfuDisconnectCallback);
            this.drn4 = new DeviceRestoreNotificationCallback(this.RecoveryDisconnectCallback);
            int num = MobileDevice.AMDeviceNotificationSubscribe(this.dnc, 0, 0, 0, out voidPtr);
            if (num != 0)
            {
                throw new Exception("AMDeviceNotificationSubscribe failed with error " + num);
            }
            num = MobileDevice.AMRestoreRegisterForDeviceNotifications(this.drn1, this.drn2, this.drn3, this.drn4, 0, null);
            if (num != 0)
            {
                throw new Exception("AMRestoreRegisterForDeviceNotifications failed with error " + num);
            }
            this.current_directory = "/";
        }

        public unsafe bool Exists(string path)
        {
            void* dict = null;
            int num = MobileDevice.AFCFileInfoOpen(this.hAFC, path, ref dict);
            if (num == 0)
            {
                MobileDevice.AFCKeyValueClose(dict);
            }
            return (num == 0);
        }

        public ulong FileSize(string path)
        {
            bool flag;
            ulong num;
            this.GetFileInfo(path, out num, out flag);
            return num;
        }

        public unsafe int FSBlockSize()
        {
            return MobileDevice.AFCConnectionGetFSBlockSize(this.hAFC);
        }

        internal string FullPath(string path1, string path2)
        {
            string[] strArray;
            if ((path1 == null) || (path1 == string.Empty))
            {
                path1 = "/";
            }
            if ((path2 == null) || (path2 == string.Empty))
            {
                path2 = "/";
            }
            if (path2[0] == '/')
            {
                strArray = path2.Split(path_separators);
            }
            else if (path1[0] == '/')
            {
                strArray = (path1 + "/" + path2).Split(path_separators);
            }
            else
            {
                strArray = ("/" + path1 + "/" + path2).Split(path_separators);
            }
            string[] strArray2 = new string[strArray.Length];
            int count = 0;
            for (int i = 0; i < strArray.Length; i++)
            {
                if (strArray[i] == "..")
                {
                    if (count > 0)
                    {
                        count--;
                    }
                }
                else if ((strArray[i] != ".") && !(strArray[i] == ""))
                {
                    strArray2[count++] = strArray[i];
                }
            }
            return ("/" + string.Join("/", strArray2, 0, count));
        }

        private string Get_st_ifmt(string path)
        {
            return this.GetFileInfo(path)["st_ifmt"];
        }

        public unsafe string GetCopyDeviceIdentifier()
        {
            return Marshal.PtrToStringAnsi(MobileDevice.AMDeviceCopyDeviceIdentifier(this.iPhoneHandle));
        }

        public unsafe Dictionary<string, string> GetDeviceInfo()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            void* dict = null;
            if ((MobileDevice.AFCDeviceInfoOpen(this.hAFC, ref dict) == 0) && (dict != null))
            {
                void* voidPtr2;
                void* voidPtr3;
                while (((MobileDevice.AFCKeyValueRead(dict, out voidPtr2, out voidPtr3) == 0) && (voidPtr2 != null)) && (voidPtr3 != null))
                {
                    string key = Marshal.PtrToStringAnsi(new IntPtr(voidPtr2));
                    string str2 = Marshal.PtrToStringAnsi(new IntPtr(voidPtr3));
                    dictionary.Add(key, str2);
                }
                MobileDevice.AFCKeyValueClose(dict);
            }
            return dictionary;
        }

        public string GetDFUImei()
        {
            try
            {
                return MobileDevice.AMRecoveryModeDeviceCopySerialNumber(this.DFUHandle);
            }
            catch
            {
                return "NO";
            }
        }

        public unsafe string[] GetDirectories(string path)
        {
            if (!this.IsConnected)
            {
                throw new Exception("Not connected to phone");
            }
            void* dir = null;
            string str = this.FullPath(this.CurrentDirectory, path);
            int num = MobileDevice.AFCDirectoryOpen(this.hAFC, str, ref dir);
            if (num != 0)
            {
                throw new Exception("Path does not exist: " + num.ToString());
            }
            string buffer = null;
            ArrayList list = new ArrayList();
            MobileDevice.AFCDirectoryRead(this.hAFC, dir, ref buffer);
            while (buffer != null)
            {
                if (((buffer != ".") && (buffer != "..")) && this.IsDirectory(this.FullPath(str, buffer)))
                {
                    list.Add(buffer);
                }
                MobileDevice.AFCDirectoryRead(this.hAFC, dir, ref buffer);
            }
            MobileDevice.AFCDirectoryClose(this.hAFC, dir);
            return (string[]) list.ToArray(typeof(string));
        }

        public string GetDirectoryRoot(string path)
        {
            return "/";
        }

        public unsafe Dictionary<string, string> GetFileInfo(string path)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            void* dict = null;
            if ((MobileDevice.AFCFileInfoOpen(this.hAFC, path, ref dict) == 0) && (dict != null))
            {
                void* voidPtr2;
                void* voidPtr3;
                while (((MobileDevice.AFCKeyValueRead(dict, out voidPtr2, out voidPtr3) == 0) && (voidPtr2 != null)) && (voidPtr3 != null))
                {
                    string key = Marshal.PtrToStringAnsi(new IntPtr(voidPtr2));
                    string str2 = Marshal.PtrToStringAnsi(new IntPtr(voidPtr3));
                    dictionary.Add(key, str2);
                }
                MobileDevice.AFCKeyValueClose(dict);
            }
            return dictionary;
        }

        public unsafe void GetFileInfo(string path, out ulong size, out bool directory)
        {
            Dictionary<string, string> fileInfo = this.GetFileInfo(path);
            size = fileInfo.ContainsKey("st_size") ? ulong.Parse(fileInfo["st_size"]) : ((ulong) 0L);
            bool flag = false;
            directory = false;
            if (fileInfo.ContainsKey("st_ifmt"))
            {
                string str = fileInfo["st_ifmt"];
                if (str != null)
                {
                    if (!(str == "S_IFDIR"))
                    {
                        if (str == "S_IFLNK")
                        {
                            flag = true;
                        }
                    }
                    else
                    {
                        directory = true;
                    }
                }
            }
            if (flag)
            {
                bool flag3;
                void* dir = null;
                directory = flag3 = MobileDevice.AFCDirectoryOpen(this.hAFC, path, ref dir) == 0;
                if (flag3)
                {
                    MobileDevice.AFCDirectoryClose(this.hAFC, dir);
                }
            }
        }

        public unsafe string[] GetFiles(string path)
        {
            if (!this.IsConnected)
            {
                throw new Exception("Not connected to phone");
            }
            string str = this.FullPath(this.CurrentDirectory, path);
            void* dir = null;
            if (MobileDevice.AFCDirectoryOpen(this.hAFC, str, ref dir) != 0)
            {
                throw new Exception("Path does not exist");
            }
            string buffer = null;
            ArrayList list = new ArrayList();
            MobileDevice.AFCDirectoryRead(this.hAFC, dir, ref buffer);
            while (buffer != null)
            {
                if (!this.IsDirectory(this.FullPath(str, buffer)))
                {
                    list.Add(buffer);
                }
                MobileDevice.AFCDirectoryRead(this.hAFC, dir, ref buffer);
            }
            MobileDevice.AFCDirectoryClose(this.hAFC, dir);
            return (string[]) list.ToArray(typeof(string));
        }

        public unsafe int GetiphoneSize()
        {
            void* dict = null;
            int num = 0;
            if ((MobileDevice.AFCDeviceInfoOpen(this.hAFC, ref dict) == 0) && (dict != null))
            {
                num = *((int*) dict);
            }
            return num;
        }

        public unsafe string GetiPhoneStr(string str)
        {
            return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, str);
        }

        public int GoOutDFU()
        {
            int num = 0xff;
            try
            {
                num = MobileDevice.AMRecoveryModeDeviceSetAutoBoot(this.DFUHandle, 1);
                return MobileDevice.AMRecoveryModeDeviceReboot(this.DFUHandle);
            }
            catch
            {
                return num;
            }
        }

        public unsafe int GotoDFU()
        {
            return MobileDevice.AMDeviceEnterRecovery(this.iPhoneHandle);
        }

        private void InternalDeleteDirectory(string path)
        {
            int num;
            string str = this.FullPath(this.CurrentDirectory, path);
            string[] files = this.GetFiles(path);
            for (num = 0; num < files.Length; num++)
            {
                this.DeleteFile(str + "/" + files[num]);
            }
            files = this.GetDirectories(path);
            for (num = 0; num < files.Length; num++)
            {
                this.InternalDeleteDirectory(str + "/" + files[num]);
            }
            this.DeleteDirectory(path);
        }

        public bool IsDirectory(string path)
        {
            bool flag;
            ulong num;
            this.GetFileInfo(path, out num, out flag);
            return flag;
        }

        public bool IsFile(string path)
        {
            return (this.Get_st_ifmt(path) == "S_IFREG");
        }

        public bool IsLink(string path)
        {
            return (this.Get_st_ifmt(path) == "S_IFLNK");
        }

        private unsafe void NotifyCallback(ref AMDeviceNotificationCallbackInfo callback)
        {
            if (callback.msg == NotificationMessage.Connected)
            {
                this.iPhoneHandle = callback.dev;
                if (this.ConnectToPhone())
                {
                    this.OnConnect(new ConnectEventArgs(callback));
                }
            }
            else if (callback.msg == NotificationMessage.Disconnected)
            {
                this.connected = false;
                this.OnDisconnect(new ConnectEventArgs(callback));
            }
        }

        protected void OnConnect(ConnectEventArgs args)
        {
            ConnectEventHandler connect = this.Connect;
            if (connect != null)
            {
                connect(this, args);
            }
        }

        protected void OnDfuConnect(DeviceNotificationEventArgs args)
        {
            DeviceNotificationEventHandler dfuConnect = this.DfuConnect;
            if (dfuConnect != null)
            {
                dfuConnect(this, args);
            }
        }

        protected void OnDfuDisconnect(DeviceNotificationEventArgs args)
        {
            DeviceNotificationEventHandler dfuDisconnect = this.DfuDisconnect;
            if (dfuDisconnect != null)
            {
                dfuDisconnect(this, args);
            }
        }

        protected void OnDisconnect(ConnectEventArgs args)
        {
            ConnectEventHandler disconnect = this.Disconnect;
            if (disconnect != null)
            {
                disconnect(this, args);
            }
        }

        protected void OnRecoveryModeEnter(DeviceNotificationEventArgs args)
        {
            DeviceNotificationEventHandler recoveryModeEnter = this.RecoveryModeEnter;
            if (recoveryModeEnter != null)
            {
                recoveryModeEnter(this, args);
            }
        }

        protected void OnRecoveryModeLeave(DeviceNotificationEventArgs args)
        {
            DeviceNotificationEventHandler recoveryModeLeave = this.RecoveryModeLeave;
            if (recoveryModeLeave != null)
            {
                recoveryModeLeave(this, args);
            }
        }

        public unsafe void ReConnect()
        {
            int num = MobileDevice.AFCConnectionClose(this.hAFC);
            num = MobileDevice.AMDeviceStopSession(this.iPhoneHandle);
            num = MobileDevice.AMDeviceDisconnect(this.iPhoneHandle);
            this.ConnectToPhone();
        }

        private void RecoveryConnectCallback(ref AMRecoveryDevice callback)
        {
            this.DFUHandle = callback;
            this.OnRecoveryModeEnter(new DeviceNotificationEventArgs(callback));
        }

        private void RecoveryDisconnectCallback(ref AMRecoveryDevice callback)
        {
            this.DFUHandle = callback;
            this.OnRecoveryModeLeave(new DeviceNotificationEventArgs(callback));
        }

        public unsafe bool Rename(string sourceName, string destName)
        {
            return (MobileDevice.AFCRenamePath(this.hAFC, this.FullPath(this.CurrentDirectory, sourceName), this.FullPath(this.CurrentDirectory, destName)) == 0);
        }

        public void* AFCHandle
        {
            get
            {
                return this.hAFC;
            }
        }

        public string CurrentDirectory
        {
            get
            {
                return this.current_directory;
            }
            set
            {
                string path = this.FullPath(this.current_directory, value);
                if (!this.IsDirectory(path))
                {
                    throw new Exception("Invalid directory specified");
                }
                this.current_directory = path;
            }
        }

        public void* Device
        {
            get
            {
                return this.iPhoneHandle;
            }
        }

        public string GetActivationState
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "ActivationState");
            }
        }

        public string GetBasebandBootloaderVersion
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "BasebandBootloaderVersion");
            }
        }

        public string GetBasebandSerialNumber
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "BasebandSerialNumber");
            }
        }

        public string GetBasebandVersion
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "BasebandVersion");
            }
        }

        public string GetBluetoothAddress
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "BluetoothAddress");
            }
        }

        public string GetBuildVersion
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "BuildVersion");
            }
        }

        public string GetCPUArchitecture
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "CPUArchitecture");
            }
        }

        public string GetDeviceCertificate
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "DeviceCertificate");
            }
        }

        public string GetDeviceClass
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "DeviceClass");
            }
        }

        public string GetDeviceColor
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "DeviceColor");
            }
        }

        public string GetDeviceName
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "DeviceName");
            }
        }

        public string GetDevicePublicKey
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "DevicePublicKey");
            }
        }

        public string GetDeviceVersion
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "ProductVersion");
            }
        }

        public string GetDieID
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "DieID");
            }
        }

        public string GetFirmwareVersion
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "FirmwareVersion");
            }
        }

        public string GetHardwareModel
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "HardwareModel");
            }
        }

        public string GetHostAttached
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "HostAttached");
            }
        }

        public string GetIntegratedCircuitCardIdentity
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "IntegratedCircuitCardIdentity");
            }
        }

        public string GetInternationalMobileEquipmentIdentity
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "InternationalMobileEquipmentIdentity");
            }
        }

        public string GetInternationalMobileSubscriberIdentity
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "InternationalMobileSubscriberIdentity");
            }
        }

        public string GetiTunesHasConnected
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "iTunesHasConnected");
            }
        }

        public string GetMLBSerialNumber
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "MLBSerialNumber");
            }
        }

        public string GetMobileSubscriberCountryCode
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "MobileSubscriberCountryCode");
            }
        }

        public string GetMobileSubscriberNetworkCode
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "MobileSubscriberNetworkCode");
            }
        }

        public string GetModelNumber
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "ModelNumber");
            }
        }

        public string GetPasswordProtected
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "PasswordProtected");
            }
        }

        public string GetPhoneNumber
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "PhoneNumber");
            }
        }

        public string GetProductionSOC
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "ProductionSOC");
            }
        }

        public string GetProductType
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "ProductType");
            }
        }

        public string GetProtocolVersion
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "ProtocolVersion");
            }
        }

        public string GetRegionInfo
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "RegionInfo");
            }
        }

        public string GetSBLockdownEverRegisteredKey
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "SBLockdownEverRegisteredKey");
            }
        }

        public string GetSerialNumber
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "SerialNumber");
            }
        }

        public string GetSIMStatus
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "SIMStatus");
            }
        }

        public string GetUniqueChipID
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "UniqueChipID");
            }
        }

        public string GetUniqueDeviceID
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "UniqueDeviceID");
            }
        }

        public string GetWiFiAddress
        {
            get
            {
                return MobileDevice.AMDeviceCopyValue(this.iPhoneHandle, "WiFiAddress");
            }
        }

        public bool IsConnected
        {
            get
            {
                return this.connected;
            }
        }

        public bool IsJailbreak
        {
            get
            {
                return (this.wasAFC2 || (this.connected ? this.Exists("/Applications") : false));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct strFileInfo
        {
            public byte[] name;
            public ulong size;
            public bool isDir;
            public bool isSLink;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct strStatVfs
        {
            public int Namemax;
            public int Bsize;
            public float Btotal;
            public float Bfree;
        }
    }
}

