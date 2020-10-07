#region > Imported Libraries <
    using System;
    using System.IO;
    using System.Net;
    using System.Data;
    using System.Text;
    using System.Drawing;
    using Microsoft.Win32;
    using System.Collections;
    using System.Windows.Forms;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using Client.Components.Classes;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Threading;
    using Ionic.Zip;
    using System.Net.Security;
    using System.Linq;
    using System.Reflection;
    using System.Net.Sockets;
    using Lipi.Communication.Scs.Client;
    using Lipi.Communication.Scs.Communication.EndPoints.Tcp;
    using Lipi.Communication.Scs.Communication.Messages;
    using System.Net.NetworkInformation;
    using System.Xml;
    using System.Security.Cryptography;
    using Microsoft.VisualBasic.Devices;
    using System.Management;
    using System.ServiceProcess;
    using System.Runtime.Serialization.Json;
    using System.Security.AccessControl;
using Newtonsoft.Json;
#endregion

#region > Client Form <
namespace Client
{
    #region > Client Form Partial Class <
    public partial class Frm_Main : Form
    {
        #region > Declaration Area <

        private IScsClient client;
        private WebClient objWC = null;

        private bool bIsServiceConnectivityPresent = false;
        private bool bIsTcpConnectivityPresent = false;
        string strClientDetails = "", strPrevClientDetails = "";
        Log objLog;

        private const int SC_CLOSE = 0xf060;
        private const int WM_SYSCOMMAND = 0x0112;

        FileSystemWatcher _fileWatcherHealth = null;     

        //string AssemblyPath = "";
        string ClientConfigINIPath = "";
        string ClientHealthINIPath = "";      

        //bool bFormMinimized = false;

        //Msg Variable
        string KioskDetailsSent = "";
        string KioskMachineSrNo = "";
        string strPrevHealth = "";
        string strCurrentHealth = "";

        //AV seeetings V11.0.19.0
        private int[] iWeekDays = new int[0];

        System.Windows.Forms.Timer TxnSchedulerTick = new System.Windows.Forms.Timer();  //TxnSchedulerTimer

        //Server Details Variable Define
        string RMS_ServerInfo = "";
        string WebHostIP = "";
        //string WebHostIP_Secondary = "";
        string Port_No = "";

        string KioskIP = "";
        string LastPatchUpdated = "";
        string KioskID = "";       
        string DownloadPath = "";
        string VendorName = "";        
        string strTV_IP = "";
        int nConnectInter = 0;     
        string strMacAddress = "";
       
        
        //Mass Storage
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, string lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult);

        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        private const int WM_SETTINGCHANGE = 0x1a;
        private const int SMTO_ABORTIFHUNG = 0x0002;
        private const int WM_WININICHANGE = 0x001A;
        private const int SHCNE_ALLEVENTS = 0x7FFFFFFF;
        private const int SHCNE_ASSOCCHANGED = 0x08000000;

        //To verify file with checksum
        private static MD5 md5 = MD5.Create();
        //private UInt16 UIntChecksum = 0;

        INIFile objClientConfigINI = null;
        INIFile objClientHealthINI = null; 
        object objlock = new object();
        int iSleepMode = 0;
      

        //Dll and Variables used for Patch Installation
        public static List<string> ZipEntryList;
        public static List<string> ZipEntryListDirPath;

        //private static int g_count = 0;
        //static string IntermediatePath = null;
        static string Patchfolderpath = null;
        //string patchName = "";

        //For change system Date Time
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }

        public enum TaskManager
        {
            Enabled,
            Disabled
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int SetLocalTime(ref SYSTEMTIME lpSystemTime);

        [DllImport("user32")]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);  //For Kiosk LogOff

        [DllImport("user32")]
        public static extern void LockWorkStation();  //For Kiosk Lock

        Thread thTxnHealth = null;
        Thread thTCPConnection = null;
        Thread thUDPConnection = null;
        Thread thEstablishedNetwork = null;



        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public Keys key;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr extra;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wp, IntPtr lp);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short GetAsyncKeyState(Keys key);
        //Declaring Global objects     
        private IntPtr ptrHook;
        private LowLevelKeyboardProc objKeyboardProcess;
        [DllImport("user32.dll")]
        private static extern int FindWindow(string className, string windowText);
        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hwnd, int command);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 1;
        bool bKeyHooking = false;
        #endregion

        public Frm_Main()
        {
            InitializeComponent();
            Top = 0;
            Left = Screen.PrimaryScreen.WorkingArea.Width - Width;

            //Memory Initialize Object
            if (objLog == null)
                objLog = new Log();
            
            ClientConfigINIPath = Application.StartupPath  + "\\KioskClientConfig.ini";
            ClientHealthINIPath = Application.StartupPath + "\\KioskClientHealth.ini";

            if (objClientConfigINI == null)
                objClientConfigINI = new INIFile(ClientConfigINIPath);
            
            if (objClientHealthINI == null)
                objClientHealthINI = new INIFile(ClientHealthINIPath);           

        }       
        

        private void Frm_Main_Load(object sender, EventArgs e)
        {
            try
            {
                this.Text = "Lipi Remote Client V" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
                notifyIcon1.Text = this.Text;
                
                objLog.WriteFile("****************" + this.Text + "*****************");
             
                strMacAddress = GetMACAddress();
                objLog.WriteFile("Mac Address:" + strMacAddress);


                if (objClientConfigINI.Read("Kiosk_Details", "PreviousDate", "") == "")
                    objClientConfigINI.Write("Kiosk_Details", "PreviousDate", DateTime.Now.ToString("dd_MM_yyyy"));

                GetLogicalDrives();
                
                ReadKioskDetails();

                //Connect();   //Client Connect Directly No wait 10 Sec...

                string strCommunicationThroughTCP = objClientConfigINI.Read("Server_Details", "IsCommunicationThroughTCP", "");

                if (strCommunicationThroughTCP == "true")
                {
                    if (thTCPConnection == null)
                        thTCPConnection = new Thread(new ThreadStart(TCPConnectivityThread));

                    thTCPConnection.Start();
                    objLog.WriteFile("TCP Thread Started For Listening");
                }
                else
                {
                    if (thUDPConnection == null)
                        thUDPConnection = new Thread(new ThreadStart(UDPlistener));

                    thUDPConnection.Start();
                    objLog.WriteFile("Udp Thread Started For Listening");
                }

                if (thTxnHealth == null)
                    thTxnHealth = new Thread(new ThreadStart(ServiceThreadTxnHealth));

               thTxnHealth.Start();

                if (_fileWatcherHealth == null)
                    _fileWatcherHealth = new FileSystemWatcher();

                _fileWatcherHealth.InternalBufferSize = 4096 * 2;
                _fileWatcherHealth.Path = "C:\\RMS\\";
                _fileWatcherHealth.NotifyFilter = NotifyFilters.LastWrite;
                _fileWatcherHealth.Filter = "KioskClientHealth.ini";
                _fileWatcherHealth.Changed += new FileSystemEventHandler(OnChangedHealth);
                _fileWatcherHealth.EnableRaisingEvents = true;

                TxnSchedulerTick.Interval = 1 * 60 * 1000;
                TxnSchedulerTick.Tick += new EventHandler(TxnSchedulerTimer_Tick);
                TxnSchedulerTick.Enabled = true;

                objLog.WriteFile("Timer Started");

                ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;
                objKeyboardProcess = new LowLevelKeyboardProc(captureKey);
                ptrHook = SetWindowsHookEx(13, objKeyboardProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);
                bKeyHooking = false;
                int hwnd = FindWindow("Shell_TrayWnd", "");
                ShowWindow(hwnd, SW_HIDE);
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in Frm_Main_Load : " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void SetKey(TaskManager command)
        {
            RegistryKey mKey;
            string subKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            mKey = Registry.CurrentUser.CreateSubKey(subKey);
            switch (command)
            {
                case TaskManager.Enabled:
                    mKey.SetValue("DisableTaskMgr", 0);
                    break;
                case TaskManager.Disabled:
                    mKey.SetValue("DisableTaskMgr", 1);
                    break;
            }
            mKey.Close();
        }

        private void Frm_Main_Activated(object sender, EventArgs e)
        {
            this.Hide();
            this.Visible = false;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            notifyIcon1.Visible = true;
        }

        private void ReadKioskDetails()
        {
            try
            {
                KioskIP = LocalIPAddress();
                KioskID = GetMACAddress(); //objClientConfigINI.Read("Kiosk_Details", "Kiosk_ID", "");
                KioskMachineSrNo = "Lipi" + KioskID; //objClientConfigINI.Read("Kiosk_Details", "KioskMachine_SrNo", "");
                RMS_ServerInfo = objClientConfigINI.Read("Kiosk_Details", "ServiceUrl", ""); 
                LastPatchUpdated = objClientConfigINI.Read("Kiosk_Details", "Last_PatchReceived", "");
                WebHostIP = objClientConfigINI.Read("Server_Details", "RMSServer_IP", "");
                Port_No = objClientConfigINI.Read("Server_Details", "Port_No", "");
                KioskDetailsSent = objClientConfigINI.Read("Kiosk_Details", "Kiosk_DetailSent", "");
                VendorName = objClientConfigINI.Read("Vendor_Details", "Vendor_Name", "");
                DownloadPath = objClientConfigINI.Read("Server_Details", "Download_Path", "");

                if (!Directory.Exists(DownloadPath))
                    Directory.CreateDirectory(DownloadPath);

                objLog.WriteFile("Web Service URL : " + RMS_ServerInfo);
                objLog.WriteFile("RMS Server IP : " + WebHostIP);
                objLog.WriteFile("RMS Port : " + Port_No);
                objLog.WriteFile("DownloadPath:" + DownloadPath);                
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in ReadKioskDetails : " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }       
        
        void UpdateCommandsFromFile(string strCommandFilePath)
        {
            try
            {
                using (StreamReader objSR = new StreamReader(strCommandFilePath))
                {
                    string[] strCommands = objSR.ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                    //loop through each command
                    foreach (var strCommand in strCommands)
                    {
                        //get command parameters
                        string[] strCommandParams = strCommand.Split(new string[] { "#" }, StringSplitOptions.None);

                        if (strCommandParams.Count() >= 4)
                        {
                            File.SetAttributes(strCommandParams[1], FileAttributes.Normal);
                            XmlDocument objXML = new XmlDocument();
                            if (objXML != null)
                            {
                                objXML.Load(strCommandParams[1]);
                                objXML.SelectSingleNode(strCommandParams[2]).InnerText = strCommandParams[3];
                                objXML.Save(strCommandParams[1]);

                                objLog.WriteFile("Success set_xml command.");
                            }
                        }
                    }
                }

                if (File.Exists(strCommandFilePath))
                    File.Delete(strCommandFilePath);
            }
            catch (Exception ex)
            {
                objLog.WriteFile("UpdateCommandsFromFile() exception- " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
      
        public string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            //Add[Lokesh 21 May 2013] V1.0.0.2
            if (localIP == "127.0.0.1")
                localIP = objClientConfigINI.Read("Kiosk_Details", "Kiosk_IP", "");
            else if (localIP != objClientConfigINI.Read("Kiosk_Details", "Kiosk_IP", ""))
                objClientConfigINI.Write("Kiosk_Details", "Kiosk_IP", localIP);

            return localIP;
        }

        private void TCPConnectivityThread()
        {          
            int nTimeInt = 1000 * 60;
            int iWaitTime = 0;

            while (true)
            {
                try
                {
                    if (!bIsTcpConnectivityPresent)
                    {
                        Connect();
                        iWaitTime = 0;
                    }
                    else
                    {
                        if (iWaitTime > 15)     //means 15 minutes V35.1.0.8
                        {
                            objLog.WriteFile("Send keep alive byte");
                            iWaitTime = 0;
                            client.SendMessage(new ScsRawDataMessage(Encoding.Default.GetBytes("33")));
                        }
                    }
                    iWaitTime++;
                }
                catch (Exception excp)
                {
                    objLog.WriteFile("Excp in TCPConnectivityThread()- " + excp.Message + "\n" + excp.StackTrace);
                }
                finally
                {
                    Thread.Sleep(nTimeInt);
                }
            }
        }

        public void ConnectivityThread()
        {
            int nTimeInt = 1000 * 60;
            while (true)
            {
                try
                {
                    BuildClientDetail();

                    if (!bIsServiceConnectivityPresent ||
                        strPrevClientDetails != strClientDetails)
                    {
                        if (objWC == null)
                            objWC = new WebClient();

                        objWC.Headers[HttpRequestHeader.ContentType] = "text/json";
                        //string strKD = "03#SWDelL06490A#006490#Passbook#74D435DD3CE9#5461DAC6#BK40SOPWU3279#05:00:00#23:30:00#1110#V12.0.9.4/ SBI G8.0 V12.5.3.3_R/12.0.0.1_R#C:\\     D:\\     R:\\     #0";

                        //strKD += "#10.1.1.66";
                        byte[] barr = Encoding.ASCII.GetBytes(strClientDetails);
                        string strbase64KD = Convert.ToBase64String(barr);
                        string result = objWC.UploadString(RMS_ServerInfo + "/KioskDetails", "POST", "\"" + strbase64KD + "\"");
                        result = result.Replace("\"", "");
                        if (result.ToLower().Contains("success"))
                        {
                            bIsServiceConnectivityPresent = true;
                            objLog.WriteFile("KDAck rcvd- " + result);
                            objClientConfigINI.Write("Kiosk_Details", "Kiosk_DetailSent", "1");
                            objClientConfigINI.Write("Kiosk_Details", "Docket_No", result.Substring(result.IndexOf(":") + 1));
                            KioskDetailsSent = "1";

                            nTimeInt = nConnectInter;//once docket receive then ...
                        }
                        else
                            bIsServiceConnectivityPresent = false;

                        strPrevClientDetails = strClientDetails;
                    }
                }
                catch (Exception ex)
                {
                    objLog.WriteFile("Error: " + "ConnectivityThread()- " + ex.Message + "\n" + ex.StackTrace);
                }
                finally
                {
                    Thread.Sleep(nTimeInt);
                }
            }
        }

        public string GetDigitalSignageStatus()
        {
            string strRet = "";

            try
            {
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(strTV_IP);

                if (reply.Status == IPStatus.Success)
                {
                    strRet = "Connected";
                    objLog.WriteFile("TV ping connected");
                }
                else
                {
                    strRet = "Disconnected";
                    objLog.WriteFile("TV ping error: " + reply.Status);
                }                
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Error: " + "GetDigitalSignageStatus()- " + ex.Message + "\n" + ex.StackTrace);
                strRet = "Disconnected";
            }
            return strRet;
        }
      
        private void ServiceThreadTxnHealth()
        {
            lock (objlock)
            {
                objLog.WriteFile("In ServiceThreadTxnHealth Function");
                while (true)
                {
                    try
                    {
                        if (bIsServiceConnectivityPresent)     //when connectivity is present
                        {
                            try
                            {
                                bool bTouchScreenFound = false;

                                //ManagementObjectSearcher deviceList = new ManagementObjectSearcher("Select Name, Status, DeviceID from Win32_PnPEntity");

                                //string DevName_Touch = objClientHealthINI.Read("Device_DriverName", "TouchScreen", "");                               

                                // Any results? There should be!
                                //if (deviceList != null)
                                //{
                                //    // Enumerate the devices
                                //    foreach (ManagementObject device in deviceList.Get())    //To Get Status of USB Device 
                                //    {

                                //        if (device.GetPropertyValue("Name") != null && device.GetPropertyValue("Name").ToString().ToLower().Contains(DevName_Touch.ToLower()))
                                //        {
                                //            bTouchScreenFound = true;
                                //        }
                                //    }

                                //    if (bTouchScreenFound)
                                //        objClientHealthINI.Write("Health_Details", "D1", "Connected");
                                //    else
                                //        objClientHealthINI.Write("Health_Details", "D1", "Disconnected");
                                //}
                            }
                            catch (Exception ex)
                            {
                                objLog.WriteFile("Error: " + "CheckAndUpdateDeviceHealth()- " + ex.Message + "\n" + ex.StackTrace);
                            }


                            string strDeviceData = "";
                            int iDeviceCount = objClientHealthINI.ReadIntValue("Health_Details", "DeviceCount", "0");
                            for (int iCount = 0; iCount < iDeviceCount; iCount++)
                            {
                                string strTemp = objClientHealthINI.Read("Health_Details", "D" + (iCount + 1), "");
                                strDeviceData += "#" + (strTemp != "" ? strTemp : "Unknown");
                            }
                                                      

                           

                            //Now Sending The Health Data
                            string HealthData = "02"
                                                   + "$" + KioskIP
                                                   + "$" + KioskMachineSrNo
                                                   + "$" + DateTime.Now.ToString("yyyyMMdd HH:mm:ss")
                                                   + "$" + objClientHealthINI.Read("Health_Details", "DeviceCount","0");

                            HealthData += strDeviceData;
                            objLog.WriteFile("HealthData  Message For Sending:- " + HealthData);

                            try
                            {
                                if (objWC == null)
                                    objWC = new WebClient();

                                objWC.Headers[HttpRequestHeader.ContentType] = "text/json";

                                byte[] barr = Encoding.ASCII.GetBytes(HealthData);
                                string strbase64 = Convert.ToBase64String(barr);
                                string strURL = "";
                                strURL = RMS_ServerInfo + "/HealthData";
                                string result = objWC.UploadString(strURL, "POST", "\"" + strbase64 + "\"");

                                objLog.WriteFile("HealthData Response Recieved : " + result);

                                MemoryStream memstrToReceive = new MemoryStream(Encoding.UTF8.GetBytes(result));
                                DataContractJsonSerializer objJsonSerRecv = new DataContractJsonSerializer(typeof(ResLipiHealth));
                                ResLipiHealth objPatchName = (ResLipiHealth)objJsonSerRecv.ReadObject(memstrToReceive);                                

                                if (objPatchName.Result)
                                {   
                                    if (objPatchName.PatchName != null && objPatchName.PatchName != "")
                                    {
                                        objLog.WriteFile("Patch name received - " + objPatchName.PatchName);
                                        
                                        if (!Directory.Exists(DownloadPath))
                                            Directory.CreateDirectory(DownloadPath);

                                        File.WriteAllBytes(DownloadPath + objPatchName.PatchName, Convert.FromBase64String(objPatchName.PatchData));
                                        objLog.WriteFile("File saved:" + objPatchName.PatchName);

                                        LipiPatchUpdateAck(LocalIPAddress(), KioskMachineSrNo, "Client");

                                        objClientConfigINI.Write("Kiosk_Details", "PatchReceived", objPatchName.PatchName);
                                        objClientConfigINI.Write("Kiosk_Details", "Last_PatchReceived", objPatchName.PatchName);
                                    }
                                    else
                                    {
                                        objLog.WriteFile("No client patch recevied");

                                        objLog.WriteFile("Now Checking for the Lipi Monitor");

                                        string strbase64Message = Convert.ToBase64String(Encoding.ASCII.GetBytes(KioskMachineSrNo + "#" + LocalIPAddress()));

                                        if (objWC == null)
                                            objWC = new WebClient();

                                        objWC.Headers[HttpRequestHeader.ContentType] = "text/json";

                                        EncRequest req = new EncRequest() { RequestData =JsonConvert.SerializeObject(KioskMachineSrNo + "#" + LocalIPAddress()) };
                                        string jsondata = JsonConvert.SerializeObject(req);

                                        //check if monitor update available 
                                        result = objWC.UploadString(RMS_ServerInfo + "/CheckLipiMonitor", "POST", jsondata);

                                        memstrToReceive = new MemoryStream(Encoding.UTF8.GetBytes(result));
                                        objJsonSerRecv = new DataContractJsonSerializer(typeof(EncResponse));
                                        EncResponse encResponse = (EncResponse)objJsonSerRecv.ReadObject(memstrToReceive);

                                        var decData = AesGcm256.Decrypt(encResponse.ResponseData);

                                        ResMonitorPatchUpdate objNewMonitorPatchName = JsonConvert.DeserializeObject<ResMonitorPatchUpdate>(decData);



                                        objLog.WriteFile("Reply CheckLipiMonitor - " + objNewMonitorPatchName.Result);

                                        if (objNewMonitorPatchName.Result &&
                                            objNewMonitorPatchName.PatchName != null &&
                                            objNewMonitorPatchName.PatchName != "")
                                        {   
                                            objLog.WriteFile("Monitor zip file rcvd- " + objNewMonitorPatchName.PatchName);

                                            if (!Directory.Exists(DownloadPath))
                                                Directory.CreateDirectory(DownloadPath);

                                            //save zip file received
                                            File.WriteAllBytes(DownloadPath + objNewMonitorPatchName.PatchName, Convert.FromBase64String(objNewMonitorPatchName.PatchData));
                                            objClientConfigINI.Write("Kiosk_Details", "Last_PatchReceived", objNewMonitorPatchName.PatchName);
                                            objLog.WriteFile("File saved:" + objNewMonitorPatchName.PatchName);

                                            //call function to update the client monitor
                                            if (UpdateRemoteClientMonitor(objNewMonitorPatchName.PatchName))
                                            {
                                                objLog.WriteFile("Client monitor updated");
                                            }
                                            else
                                                objLog.WriteFile("Client monitor update fail");

                                            LipiPatchUpdateAck(LocalIPAddress(), KioskMachineSrNo, "Monitor");
                                        }
                                        else
                                        { 
                                            objLog.WriteFile("No monitor patch recevied");

                                            objLog.WriteFile("Now Checking for the Lipi RD Service");

                                            strbase64Message = Convert.ToBase64String(Encoding.ASCII.GetBytes(KioskMachineSrNo + "#" + LocalIPAddress()));

                                            if (objWC == null)
                                                objWC = new WebClient();

                                            objWC.Headers[HttpRequestHeader.ContentType] = "text/json";

                                            EncRequest encRequest = new EncRequest() {RequestData= AesGcm256.Encrypt(JsonConvert.SerializeObject( KioskMachineSrNo + "#" + LocalIPAddress())) };
                                            jsondata = JsonConvert.SerializeObject(encRequest);
                                            //check if monitor update available 
                                            result = objWC.UploadString(RMS_ServerInfo + "/CheckLipiRDService", "POST", jsondata);

                                            memstrToReceive = new MemoryStream(Encoding.UTF8.GetBytes(result));
                                            objJsonSerRecv = new DataContractJsonSerializer(typeof(EncResponse));
                                             encResponse = (EncResponse)objJsonSerRecv.ReadObject(memstrToReceive);

                                             decData = AesGcm256.Decrypt(encResponse.ResponseData);

                                            ResLipiRDServiceUpdate objLipiRDServicePatchName = JsonConvert.DeserializeObject<ResLipiRDServiceUpdate>(decData);


                                            if (objLipiRDServicePatchName.Result &&
                                                objLipiRDServicePatchName.PatchName != null &&
                                                objLipiRDServicePatchName.PatchName != "")
                                            {
                                                objLog.WriteFile("Lipi RD Service zip file rcvd- " + objLipiRDServicePatchName.PatchName);

                                                if (!Directory.Exists(DownloadPath))
                                                    Directory.CreateDirectory(DownloadPath);

                                                //save zip file received
                                                File.WriteAllBytes(DownloadPath + objLipiRDServicePatchName.PatchName, Convert.FromBase64String(objLipiRDServicePatchName.PatchData));
                                                objClientConfigINI.Write("Kiosk_Details", "Last_PatchReceived", objLipiRDServicePatchName.PatchName);
                                                objLog.WriteFile("File saved:" + objLipiRDServicePatchName.PatchName);

                                                LipiPatchUpdateAck(LocalIPAddress(), KioskMachineSrNo, "LipiRDService");
                                            }
                                            else
                                            {
                                                objLog.WriteFile("Now Checking for the Command File");

                                                if (objWC == null)
                                                    objWC = new WebClient();

                                                objWC.Headers[HttpRequestHeader.ContentType] = "text/json";

                                                encRequest = new EncRequest() { RequestData= AesGcm256.Encrypt( Newtonsoft.Json.JsonConvert.SerializeObject( LocalIPAddress()))};
                                                string json = Newtonsoft.Json.JsonConvert.SerializeObject(encRequest);

                                                result = objWC.UploadString(RMS_ServerInfo + "/GetCommand", "POST", json);

                                             

                                                memstrToReceive = new MemoryStream(Encoding.UTF8.GetBytes(result));
                                                objJsonSerRecv = new DataContractJsonSerializer(typeof(EncResponse));
                                                encResponse = (EncResponse)objJsonSerRecv.ReadObject(memstrToReceive);

                                                decData = AesGcm256.Decrypt(encResponse.ResponseData);
                                                objLog.WriteFile("GetCommand Response : " + decData);

                                                 result= JsonConvert.DeserializeObject<string>(decData);

                                                objLog.WriteFile("Call  ExecuteCommands ");
                                                ExecuteCommands(result);
                                            }
                                        }
                                    }

                                    iSleepMode = 30 * 60 * 1000;  //30 min to send health
                                }
                                else
                                {
                                    objLog.WriteFile("LipiTxnHealthMessage Response Failure : " + objPatchName.Result);
                                    bIsServiceConnectivityPresent = false;
                                    objLog.WriteFile("Result Comes With Error Of HealthData");
                                    iSleepMode = 1 * 60 * 1000;  //1 min to check connectivity
                                }
                            }
                            catch (Exception ex)
                            {
                                objLog.WriteFile("Exception Occurs While Sending HealthData:- " + ex.Message + "," + ex.StackTrace);
                                bIsServiceConnectivityPresent = false;
                                iSleepMode = 1 * 60 * 1000;  //1 min to check connectivity
                            }
                        }
                        else
                        {
                            if (thEstablishedNetwork == null)
                            {
                                thEstablishedNetwork = new Thread(new ThreadStart(CheckNetwotkConnectivity));
                                thEstablishedNetwork.Start();
                            }

                            //iSleepMode = 1 * 60 * 1000;  
                            iSleepMode =  3000;  
                        }
                    }
                    catch (Exception ex)
                    {
                        objLog.WriteFile("Exception Occurs ServiceThreadTxnHealth:- " + ex.Message + "," + ex.StackTrace);
                    }    
                    finally
                    {
                        Thread.Sleep(iSleepMode);
                    }
                }
            }
        }

        private void CheckNetwotkConnectivity()
        {
            try
            {
                bool IsKeepAlive = true;
                byte[] bArr = new byte[0];
                string strResult = "";

                while (IsKeepAlive)
                {
                    try
                    {
                        if (objWC == null)
                            objWC = new WebClient();

                        objWC.Headers[HttpRequestHeader.ContentType] = "text/json";

                        strResult = objWC.UploadString(RMS_ServerInfo + "/CheckConnectivity", "POST", "");
                        objLog.WriteFile("CheckConnectivity reply- " + strResult);
                        strResult = strResult.Replace("\"", "");
                        if (strResult.ToLower().Contains("success"))
                        {
                            BuildClientDetail();

                            if (KioskDetailsSent == "0" ||          //if first time checking when docket not present/client not registered/or ClientDetails has Been Changed So Send It Again
                                strPrevClientDetails != strClientDetails)
                            {
                                if (objWC == null)
                                    objWC = new WebClient();

                                objWC.Headers[HttpRequestHeader.ContentType] = "text/json";
                                strPrevClientDetails = strClientDetails;

                                objLog.WriteFile("Kiosk Details Message Composed - " + strClientDetails);

                                Array.Clear(bArr, 0, bArr.Length);
                                bArr = Encoding.ASCII.GetBytes(strClientDetails);
                                string strbase64KD = Convert.ToBase64String(bArr);

                                try
                                {
                                    strResult = objWC.UploadString(RMS_ServerInfo + "/KioskDetails", "POST", "\"" + strbase64KD + "\"");
                                    objLog.WriteFile("KioskDetails reply Comes From Service- " + strResult);
                                    strResult = strResult.Replace("\"", "");
                                    if (strResult.ToLower().Contains("success"))
                                    {
                                        bIsServiceConnectivityPresent = true;
                                        objLog.WriteFile("KDAck rcvd- " + strResult);
                                        objClientConfigINI.Write("Kiosk_Details", "Kiosk_DetailSent", "1");
                                        objClientConfigINI.Write("Kiosk_Details", "Docket_No", strResult.Substring(strResult.IndexOf(":") + 1));
                                        KioskDetailsSent = "1";
                                        IsKeepAlive = false;
                                    }
                                    else
                                    {
                                        bIsServiceConnectivityPresent = false;
                                    }


                                }
                                catch (Exception excp)
                                {
                                    objLog.WriteFile("Service KioskDetails fail- " + excp.Message);
                                    bIsServiceConnectivityPresent = false;
                                }
                            }
                            else
                            {
                                bIsServiceConnectivityPresent = true;
                                IsKeepAlive = false;
                            }
                        }                      
                    }
                    catch (Exception excp)
                    {
                        objLog.WriteFile("Service CheckConnectivity fail- " + excp.Message + ",," + excp.StackTrace);
                        bIsServiceConnectivityPresent = false;
                    }
                    finally
                    {
                        Thread.Sleep(1 * 60 * 1000);
                    }
                }
            }
            catch(Exception ex)
            {
                objLog.WriteFile("CheckNetwotkConnectivity - " + ex.Message + ",," + ex.StackTrace);
            }
            finally
            {
                try
                {
                    thEstablishedNetwork.Abort();
                }
                catch { }
                thEstablishedNetwork = null;
            }
        }
        private void UDPlistener()
        {
            try
            {
                bool done = false;

                string strUDP_Port = objClientConfigINI.Read("Server_Details", "UDP_Port", "");
                if (strUDP_Port == "")
                    strUDP_Port = "11001";

                objLog.WriteFile("UDP Port : " + strUDP_Port);

                UdpClient listener = new UdpClient(Convert.ToInt32(strUDP_Port));
                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, Convert.ToInt32(strUDP_Port));
                string received_data;
                byte[] receive_byte_array;
                while (!done)
                {
                    try
                    {
                        receive_byte_array = listener.Receive(ref groupEP);
                        received_data = Encoding.ASCII.GetString(receive_byte_array, 0, receive_byte_array.Length);
                        if (received_data != null && received_data != "")
                        {
                            objLog.WriteFile("UDP message received - " + received_data);
                            ExecuteCommands(received_data);
                        }
                    }
                    catch (Exception ex)
                    {
                        objLog.WriteFile("Exception in UDP message received - " + ex.Message);
                    }
                }

                listener.Close();
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in UDPlistener - " + ex.Message);
            }
        }

        private void ExecuteCommands(string strMsg)
        {
            objLog.WriteFile("ExecuteCommands msg receive  : " + strMsg);

            string[] strSplitData = strMsg.Split('#');
            switch (strSplitData[0])
            {
                case "clientupdate":
                    {
                        try
                        {
                            WebClient objWC = new WebClient();

                            objWC.Headers[HttpRequestHeader.ContentType] = "text/json";

                            objLog.WriteFile("Try to Client Download : " + strSplitData[1]);

                            string result = objWC.UploadString(RMS_ServerInfo + "/GetClientPatch", "POST", "\"" + strSplitData[1] + "\"");

                            MemoryStream memstrToReceive = new MemoryStream(Encoding.UTF8.GetBytes(result));
                            DataContractJsonSerializer objJsonSerRecv = new DataContractJsonSerializer(typeof(ResPatchUpdate));
                            ResPatchUpdate objNewPatchName = (ResPatchUpdate)objJsonSerRecv.ReadObject(memstrToReceive);

                            objLog.WriteFile("Client Download Result: " + objNewPatchName.PatchName + "\n" + objNewPatchName.Result.ToString());

                            if (objNewPatchName.Result)
                            {
                                if (!Directory.Exists(DownloadPath + "PATCH"))
                                    Directory.CreateDirectory(DownloadPath + "PATCH");

                                File.WriteAllBytes(DownloadPath + "PATCH\\" + objNewPatchName.PatchName, Convert.FromBase64String(objNewPatchName.PatchData));
                                objLog.WriteFile("File saved:" + objNewPatchName.PatchName);

                                LipiPatchUpdateAck(LocalIPAddress(), KioskMachineSrNo, "Client");

                                objClientConfigINI.Write("Kiosk_Details", "PatchReceived", objNewPatchName.PatchName);
                                objLog.WriteFile("Patch name updated in ini file : " + objNewPatchName.PatchName);
                            }
                            else
                            {
                                objLog.WriteFile("Patch download Failed: " + objNewPatchName.PatchName + "\n" + objNewPatchName.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            objLog.WriteFile("Exception in client update download  " + ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                    }
                    break;

                case "monitorupdate":
                    {
                        try
                        {
                            WebClient objWC = new WebClient();

                            objWC.Headers[HttpRequestHeader.ContentType] = "text/json";

                            objLog.WriteFile("Try to Monitor Download : " + strSplitData[1]);

                            string result = objWC.UploadString(RMS_ServerInfo + "/GetClientMonitor", "POST", "\"" + strSplitData[1] + "\"");

                            MemoryStream memstrToReceive = new MemoryStream(Encoding.UTF8.GetBytes(result));
                            DataContractJsonSerializer objJsonSerRecv = new DataContractJsonSerializer(typeof(ResPatchUpdate));
                            ResPatchUpdate objNewPatchName = (ResPatchUpdate)objJsonSerRecv.ReadObject(memstrToReceive);

                            objLog.WriteFile("Monitor Download Result: " + objNewPatchName.PatchName + "\n" + objNewPatchName.Result.ToString());

                            if (objNewPatchName.Result)
                            {
                                if (!Directory.Exists(DownloadPath + "PATCH"))
                                    Directory.CreateDirectory(DownloadPath + "PATCH");

                                File.WriteAllBytes(DownloadPath + "PATCH\\" + objNewPatchName.PatchName, Convert.FromBase64String(objNewPatchName.PatchData));
                                objLog.WriteFile("File saved:" + objNewPatchName.PatchName);

                                //call function to update the client monitor
                                if (UpdateRemoteClientMonitor(objNewPatchName.PatchName))
                                {
                                    objLog.WriteFile("Client monitor updated");
                                }
                                else
                                    objLog.WriteFile("Client monitor update fail");

                                LipiPatchUpdateAck(LocalIPAddress(), KioskMachineSrNo, "Monitor");
                            }
                            else
                            {
                                objLog.WriteFile("Patch download Failed: " + objNewPatchName.PatchName + "\n" + objNewPatchName.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            objLog.WriteFile("Exception in monitor update download  " + ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                    }
                    break;

                case "rdserviceupdate":
                    {
                        try
                        {
                            WebClient objWC = new WebClient();

                            objWC.Headers[HttpRequestHeader.ContentType] = "text/json";

                            objLog.WriteFile("Try to RD Service Download : " + strSplitData[1]);

                            string result = objWC.UploadString(RMS_ServerInfo + "/GetRDService", "POST", "\"" + strSplitData[1] + "\"");

                            MemoryStream memstrToReceive = new MemoryStream(Encoding.UTF8.GetBytes(result));
                            DataContractJsonSerializer objJsonSerRecv = new DataContractJsonSerializer(typeof(ResPatchUpdate));
                            ResPatchUpdate objNewPatchName = (ResPatchUpdate)objJsonSerRecv.ReadObject(memstrToReceive);

                            objLog.WriteFile("RD Service Download Result: " + objNewPatchName.PatchName + "\n" + objNewPatchName.Result.ToString());

                            if (objNewPatchName.Result)
                            {
                                if (!Directory.Exists(DownloadPath + "PATCH"))
                                    Directory.CreateDirectory(DownloadPath + "PATCH");

                                File.WriteAllBytes(DownloadPath + "PATCH\\" + objNewPatchName.PatchName, Convert.FromBase64String(objNewPatchName.PatchData));
                                objLog.WriteFile("File saved:" + objNewPatchName.PatchName);

                                LipiPatchUpdateAck(LocalIPAddress(), KioskMachineSrNo, "LipiRDService");

                                objClientConfigINI.Write("Kiosk_Details", "PatchReceived", objNewPatchName.PatchName);
                                objLog.WriteFile("Patch name updated in ini file : " + objNewPatchName.PatchName);
                            }
                            else
                            {
                                objLog.WriteFile("Patch download Failed: " + objNewPatchName.PatchName + "\n" + objNewPatchName.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            objLog.WriteFile("Exception in RD Service download  " + ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                    }
                    break;
                case "customcommand":
                    {
                        //customcommand#upload#Dir#C:\\Kiosk\\XML$#upload#File#C:\\Kiosk\\XML\\Camera.xml";
                        //customcommand#upload#Dir#C:\\Kiosk\\XML";

                        //customcommand#download#D:\Shared\Lipi\Patch\KPROC.zip#C:\Kiosk\#execute#C:\Kiosk\KPROC.exe";

                        objLog.WriteFile("In Custom Command Case");

                        try
                        {
                            if (strSplitData[1].ToLower() == "upload")
                            {
                                //Skip customcommand string
                                string strData = strMsg.Substring(14);

                                string[] strCommands = strData.Split(new string[] { "$#" }, StringSplitOptions.None);

                                string szFileZipPath = DownloadPath + "UploadData_" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".zip";
                                using (ZipFile zip = new ZipFile(szFileZipPath))
                                {
                                    foreach (string strCommand in strCommands)
                                    {
                                        string[] strCommandParams = strCommand.Split(new string[] { "#" }, StringSplitOptions.None);

                                        if (strCommandParams[1].ToLower() == "dir")     //For Directory Upload
                                        {
                                            objLog.WriteFile("Custom Zip Folder:" + szFileZipPath);
                                            if (Directory.Exists(strCommandParams[2]))
                                            {
                                                zip.AddDirectory(strCommandParams[2], strCommandParams[2].Substring(strCommandParams[2].LastIndexOf("\\") + 1));   //For Directory Upload
                                                objLog.WriteFile("Add Custom Directory:" + strCommandParams[2]);
                                            }
                                            else
                                                objLog.WriteFile("Custom Directory Not Exits." + strCommandParams[2]);
                                        }
                                        else if (strCommandParams[1].ToLower() == "file") //For File Upload
                                        {
                                            objLog.WriteFile("Custom Zip Folder:" + szFileZipPath);

                                            if (File.Exists(strCommandParams[2]))
                                            {
                                                zip.AddFile(strCommandParams[2], "\\");   //For File Upload
                                                objLog.WriteFile("Add Custom File:" + strCommandParams[2]);
                                            }
                                            else
                                                objLog.WriteFile("Custom File Not Exits." + strCommandParams[2]);
                                        }
                                    }

                                    zip.Save();
                                    objLog.WriteFile("Zip File Saved Successfully - " + szFileZipPath);
                                    objLog.WriteFile("Called- LipiUploadData()");
                                    LipiUploadData(LocalIPAddress(), szFileZipPath);
                                }
                            }
                            else if (strSplitData[1].ToLower() == "download")
                            {
                                //customcommand#download#KPROC.zip#C:\Kiosk\#execute#C:\Kiosk\KPROC.exe
                                //customcommand#download#KPROC.zip#C:\Kiosk\#saved#C:\Kiosk\KPROC.exe
                                //customcommand#download#KPROC.zip#C:\Kiosk\#extract#C:\Kiosk\KPROC.exe

                                //Skip customcommand string
                                string strData = strMsg.Substring(14);

                                string[] strCommandParams = strData.Split(new string[] { "#" }, StringSplitOptions.None);

                                //Get File Download
                                try
                                {
                                    WebClient objWC = new WebClient();
                                    objWC.Headers[HttpRequestHeader.ContentType] = "text/json";
                                    string strURL = RMS_ServerInfo + "/LipiDownloadData";
                                    string result = objWC.UploadString(strURL, "POST", "\"" + strCommandParams[1] + "\"");
                                    objLog.WriteFile("LipiDownloadData Response : " + result);

                                    MemoryStream memstrToReceive = new MemoryStream(Encoding.UTF8.GetBytes(result));
                                    DataContractJsonSerializer objJsonSerRecv = new DataContractJsonSerializer(typeof(ResDownloadData));
                                    ResDownloadData objDownloadData = (ResDownloadData)objJsonSerRecv.ReadObject(memstrToReceive);

                                    if (objDownloadData.Result)
                                    {
                                        objLog.WriteFile("Download Data Successfully : " + strCommandParams[1]);

                                        //create directory if not exist
                                        if (!Directory.Exists(strCommandParams[2]))
                                            Directory.CreateDirectory(strCommandParams[2]);

                                        //save the file
                                        File.WriteAllBytes(strCommandParams[2] + strCommandParams[1], Convert.FromBase64String(objDownloadData.DownalodData));
                                        objLog.WriteFile("Zip file saved : " + strCommandParams[1]);

                                        if ((strCommandParams.Count() >= 4 && strCommandParams[3].ToLower() == "extract") ||
                                        (strCommandParams.Count() >= 5 && strCommandParams[3].ToLower() == "execute"))
                                        {
                                            //Extract the zip file saved
                                            using (ZipFile zip = ZipFile.Read(strCommandParams[2] + strCommandParams[1]))
                                            {
                                                foreach (ZipEntry zipentry in zip)
                                                {
                                                    try
                                                    {
                                                        zipentry.Extract(strCommandParams[2], ExtractExistingFileAction.OverwriteSilently);  // overwrite == true  
                                                        objLog.WriteFile("Extract File Name: " + zipentry.ToString());
                                                    }
                                                    catch (Exception ex)  //added catch region to extract file when .tmp and .PendingOverwritefile exists 
                                                    {
                                                        objLog.WriteFile("Excp in extracting file:- " + ex.Message);
                                                        foreach (var postFix in new[] { ".tmp", ".PendingOverwrite" })
                                                        {
                                                            var errorPath = Path.Combine(strCommandParams[2], zipentry.FileName) + postFix;
                                                            if (File.Exists(errorPath))
                                                            {
                                                                try
                                                                {
                                                                    File.Delete(errorPath);
                                                                    objLog.WriteFile("Deleted File Name: " + errorPath.ToString());
                                                                }
                                                                catch (Exception Excp)
                                                                {
                                                                    objLog.WriteFile("Exception in deleting .tmp files :-" + Excp.Message);
                                                                    throw (Excp);
                                                                }
                                                            }
                                                        }

                                                        objLog.WriteFile("extracting after deleting .tmp file");
                                                        zipentry.Extract(strCommandParams[2], ExtractExistingFileAction.OverwriteSilently);
                                                        objLog.WriteFile("Extract File Name: " + ex.Message);
                                                    }
                                                }
                                            }

                                            //delete the zip file after extract
                                            if (File.Exists(strCommandParams[2] + strCommandParams[1]))
                                                File.Delete(strCommandParams[2] + strCommandParams[1]);
                                        }

                                        if (strCommandParams.Count() >= 5 && strCommandParams[5].ToLower() == "execute")
                                        {
                                            if (File.Exists(strCommandParams[4]))
                                            {
                                                Process.Start(strCommandParams[4]);
                                                objLog.WriteFile("Execute Process : " + strCommandParams[4]);
                                            }
                                            else
                                            {
                                                objLog.WriteFile("Execute Process not found : " + strCommandParams[4]);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        objLog.WriteFile("Download Data Failed : " + strCommandParams[1]);
                                    }

                                    LipiCommandUpdateAck(LocalIPAddress());
                                }
                                catch (Exception ex)
                                {
                                    objLog.WriteFile("Exception in LipiCommandUpdateAck Response : " + ex.Message);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            objLog.WriteFile("Exception in customcommand - " + ex.Message);
                        }
                    }
                    break;
                case "collectlogs":
                    {
                        objLog.WriteFile("In Machine Logs Upload Case");

                        try
                        {
                            string strZipFilePath = UploadLog(strSplitData[1], strSplitData[2], strSplitData[3]);
                            if (strZipFilePath != "")
                                LipiUploadData(LocalIPAddress(), strZipFilePath);
                        }
                        catch (Exception ex)
                        {
                            objLog.WriteFile("Exception in Machine Logs Uploaded failed - " + ex.StackTrace + " " + ex.Message);
                        }
                    }
                    break;
                case "collectimages":
                    {
                        objLog.WriteFile("In Camera Image Upload Case");

                        try
                        {
                            string strZipFilePath = UploadCameraImages(strSplitData[1], strSplitData[2]);
                            if (strZipFilePath != "")
                                LipiUploadData(LocalIPAddress(), strZipFilePath);
                        }
                        catch (Exception ex)
                        {
                            objLog.WriteFile("Exception in Camera Image Uploaded failed - " + ex.StackTrace + " " + ex.Message);
                        }
                    }
                    break;
                case "securityupdate":
                    {
                        switch (strSplitData[1].ToLower())
                        {
                            case "shutdown":
                                {
                                    try
                                    {
                                        LipiCommandUpdateAck(LocalIPAddress());
                                        Process.Start("shutdown.exe", "-s -f -t 0");
                                    }
                                    catch (Exception ex) { objLog.WriteFile("Exception in shutdown - " + ex.StackTrace + " " + ex.Message); }
                                }
                                break;
                            case "restart":
                                {
                                    try
                                    {
                                        LipiCommandUpdateAck(LocalIPAddress());
                                        Process.Start("shutdown.exe", "-r -f -t 0");
                                    }
                                    catch (Exception ex) { objLog.WriteFile("Exception in restart - " + ex.StackTrace + " " + ex.Message); }
                                }
                                break;
                            case "logoff":
                                {
                                    try
                                    {
                                        LipiCommandUpdateAck(LocalIPAddress());
                                        ExitWindowsEx(0, 0);
                                    }
                                    catch (Exception ex) { objLog.WriteFile("Exception in logoff - " + ex.StackTrace + " " + ex.Message); }
                                }
                                break;
                            case "lock":
                                {
                                    try
                                    {
                                        LipiCommandUpdateAck(LocalIPAddress());
                                        LockWorkStation();
                                    }
                                    catch (Exception ex) { objLog.WriteFile("Exception in system lock - " + ex.StackTrace + " " + ex.Message); }
                                }
                                break;
                            case "hibernate":
                                {
                                    try
                                    {
                                        LipiCommandUpdateAck(LocalIPAddress());
                                        Application.SetSuspendState(PowerState.Hibernate, true, true);
                                    }
                                    catch (Exception ex) { objLog.WriteFile("Exception in hibernate - " + ex.StackTrace + " " + ex.Message); }
                                }
                                break;
                            case "sleep":
                                {
                                    try
                                    {
                                        LipiCommandUpdateAck(LocalIPAddress());
                                        Application.SetSuspendState(PowerState.Suspend, true, true);
                                    }
                                    catch (Exception ex) { objLog.WriteFile("Exception in system sleep - " + ex.StackTrace + " " + ex.Message); }
                                }
                                break;
                            case "usb":
                                {
                                    if (strSplitData[2].ToLower() == "enable")
                                        USBEnable();
                                    else
                                        USBDisable();

                                    LipiCommandUpdateAck(LocalIPAddress());
                                }
                                break;
                            case "remote":
                                {
                                    if (strSplitData[2].ToLower() == "enable")
                                        RemoteEnable();
                                    else
                                        RemoteDisable();

                                    LipiCommandUpdateAck(LocalIPAddress());
                                }
                                break;

                            case "mouse":
                                {
                                    if (strSplitData[2].ToLower() == "enable")
                                    {
                                        //g_Hook_Obj.UnHook();
                                        objLog.WriteFile("Mouse enable");
                                    }
                                    else
                                    {
                                        //g_Hook_Obj.Hook();
                                        objLog.WriteFile("Mouse disable");
                                    }
                                }
                                break;
                            case "keyboard":
                                {
                                    if (strSplitData[2].ToLower() == "enable")
                                    {
                                        UnhookWindowsHookEx(ptrHook);
                                        bKeyHooking = false;
                                        objLog.WriteFile("Keyboard enable");
                                        SetKey(TaskManager.Enabled);                                        
                                    }
                                    else
                                    {
                                        //ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;
                                        //objKeyboardProcess = new LowLevelKeyboardProc(captureKey);
                                        //ptrHook = SetWindowsHookEx(13, objKeyboardProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);
                                        bKeyHooking = true;
                                        objLog.WriteFile("Keyboard disable");
                                        SetKey(TaskManager.Disabled);                                       
                                    }
                                }
                                break;
                            case "systempassword":
                                {
                                    try
                                    {
                                        string NewPass = strSplitData[2];

                                        //Create New Process
                                        Process QProc = new Process();

                                        // Do Something To hide Command(cmd) Window
                                        QProc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                        QProc.StartInfo.CreateNoWindow = true;

                                        // Call Net.exe
                                        QProc.StartInfo.WorkingDirectory = "C:\\windows\\SYSTEM32";
                                        QProc.StartInfo.FileName = "net.exe";
                                        QProc.StartInfo.UseShellExecute = false;
                                        QProc.StartInfo.RedirectStandardError = true;
                                        QProc.StartInfo.RedirectStandardInput = true;
                                        QProc.StartInfo.RedirectStandardOutput = true;

                                        // Prepare Command for Exec
                                        QProc.StartInfo.Arguments = @" user administrator " + NewPass;
                                        QProc.Start();

                                        // MyProc.WaitForExit();
                                        QProc.Close();

                                        objLog.WriteFile("System administrator password changed succesfully - " + NewPass);
                                    }
                                    catch (Exception ex)
                                    {
                                        objLog.WriteFile("Exception in system administrator password changed - " + ex.StackTrace + " " + ex.Message);
                                    }
                                }
                                break;

                            case "drive":
                                {
                                    if (strSplitData[2].ToLower() == "enable")
                                        DriveEnable();
                                    else
                                        DriveDisable();

                                    LipiCommandUpdateAck(LocalIPAddress());
                                }
                                break;
                        }

                    }
                    break;
                case "datapurging":
                    {
                        try
                        {
                            int Days = Convert.ToInt32(strSplitData[2]);

                            // Put all bin files in root directory into array.
                            // ... This is case-insensitive.
                            string[] strGetFiles = Directory.GetFiles(strSplitData[1].ToLower(), "*.txt");

                            // Display all files.
                            foreach (string strFileName in strGetFiles)
                            {
                                FileInfo objFile = new FileInfo(strFileName);

                                // Check Logic for File is 3 month older and Need to purge
                                if (objFile.CreationTime <= DateTime.Now.AddDays(-Days))
                                {
                                    //Delete the file.
                                    objFile.Delete();

                                    objLog.WriteFile("Delete File - " + strFileName);
                                }
                            }

                            LipiCommandUpdateAck(LocalIPAddress());
                        }
                        catch (Exception ex)
                        {
                            objLog.WriteFile("Exception in datapurging - " + ex.StackTrace + " " + ex.Message);
                        }
                    }
                    break;

                case "process":
                    {
                        if (strSplitData[1].ToLower() == "application")
                        {
                            if (strSplitData[2].ToLower() == "start")
                            {
                                Process.Start(strSplitData[3]);
                            }
                            else if (strSplitData[2].ToLower() == "kill")
                            {
                                // Kill Process.
                                Process[] procs = Process.GetProcessesByName(strSplitData[3]);
                                foreach (Process proc in procs)
                                {
                                    proc.Kill();
                                    objLog.WriteFile(strSplitData[3] + " application successfully killed.");
                                }
                            }
                        }
                        else if (strSplitData[1].ToLower() == "service")
                        {
                            if (strSplitData[2].ToLower() == "start")
                            {
                                ServiceController sc = null;
                                if (sc == null)
                                    sc = new ServiceController(strSplitData[3]);

                                if (sc.Status != ServiceControllerStatus.Running)
                                {
                                    sc.Start();
                                    objLog.WriteFile(strSplitData[3] + " service started successfully");
                                }
                            }
                            else if (strSplitData[2].ToLower() == "kill")
                            {
                                ServiceController sc = null;
                                if (sc == null)
                                    sc = new ServiceController(strSplitData[3]);

                                if (sc.Status != ServiceControllerStatus.Stopped)
                                {
                                    sc.Stop();
                                    objLog.WriteFile(strSplitData[3] + " service stopped successfully");
                                }
                            }
                        }

                        LipiCommandUpdateAck(LocalIPAddress());
                    }
                    break;               
            }

        }

        private IntPtr captureKey(int nCode, IntPtr wp, IntPtr lp)
        {

            if (nCode >= 0 && bKeyHooking == true)
            {
                KBDLLHOOKSTRUCT objKeyInfo = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));
                if (objKeyInfo.key == Keys.LButton || objKeyInfo.key == Keys.Escape)
                {
                    int LCtrlKey = GetAsyncKeyState(Keys.LControlKey);
                    if (LCtrlKey != 0 && (objKeyInfo.key == Keys.LButton || objKeyInfo.key == Keys.Escape))
                    {
                        return (IntPtr)1;
                    }

                    int RCtrlKey = GetAsyncKeyState(Keys.RControlKey);
                    if (RCtrlKey != 0 && (objKeyInfo.key == Keys.LButton || objKeyInfo.key == Keys.Escape))
                    {
                        return (IntPtr)1;
                    }

                    int LAltKey = GetAsyncKeyState(Keys.MButton | Keys.Space | Keys.F17);
                    if (LAltKey != 0 && (objKeyInfo.key == Keys.LButton || objKeyInfo.key == Keys.Escape))
                    {
                        return (IntPtr)1;
                    }

                    int RAltKey = GetAsyncKeyState(Keys.LButton | Keys.MButton | Keys.Space | Keys.F17);
                    if (RAltKey != 0 && (objKeyInfo.key == Keys.LButton || objKeyInfo.key == Keys.Escape))
                    {
                        return (IntPtr)1;
                    }
                }

                // If the property is enabled, not hooking delete button
                //if (!DeleteButtonEnabled)
                {
                    if (objKeyInfo.vkCode == 46)
                    {
                        return (IntPtr)1;
                    }
                }

                //TAB button
                if (objKeyInfo.vkCode == 9)
                {
                    return (IntPtr)1;
                }

                // If the property is enabled, not hooking Insert button
                //if (!InsertButtonEnabled)
                {
                    if (objKeyInfo.vkCode == 45)
                    {
                        return (IntPtr)1;
                    }
                }

                // If the property is enabled, not hooking backspace button
                //if (!BackspaceEnabled)
                //{
                //    if (objKeyInfo.vkCode == 8)
                //    {
                //        return (IntPtr)1;
                //    }
                //}

                // If the property is enabled, not hooking Properties(Right Click) button
                //if (!RightClickEnabled)
                {
                    if (objKeyInfo.vkCode == 93)
                    {
                        return (IntPtr)1;
                    }
                }

                // hooking ALT button
                if (objKeyInfo.vkCode == 164)
                {
                    return (IntPtr)1;
                }

                if (objKeyInfo.vkCode == 9 || objKeyInfo.key == Keys.LButton || objKeyInfo.key == Keys.Back || objKeyInfo.key == Keys.Tab)
                {
                    int LCtrlKey = GetAsyncKeyState(Keys.LControlKey);
                    int RCtrlKey = GetAsyncKeyState(Keys.RControlKey);
                    int LAltKey = GetAsyncKeyState(Keys.MButton | Keys.Space | Keys.F17);
                    int RAltKey = GetAsyncKeyState(Keys.LButton | Keys.MButton | Keys.Space | Keys.F17);

                    if ((LCtrlKey != 0 && (objKeyInfo.vkCode == 9 || objKeyInfo.key == Keys.LButton || objKeyInfo.key == Keys.Back || objKeyInfo.key == Keys.Tab))
                        || ((RCtrlKey != 0 && (objKeyInfo.vkCode == 9 || objKeyInfo.key == Keys.LButton || objKeyInfo.key == Keys.Back || objKeyInfo.key == Keys.Tab))))
                    {
                        return (IntPtr)1;
                    }

                    // Alt+Tab
                    if ((RAltKey != 0 || LAltKey != 0) && (objKeyInfo.vkCode == 9 || objKeyInfo.key == Keys.LButton || objKeyInfo.key == Keys.Back || objKeyInfo.key == Keys.Tab))
                    {
                        return (IntPtr)1;
                    }
                }

                //Alt + F4
                if ((objKeyInfo.key == Keys.F4) || (objKeyInfo.vkCode == 115))
                {
                    int LAltKey = GetAsyncKeyState(Keys.MButton | Keys.Space | Keys.F17);
                    if (LAltKey != 0 && (objKeyInfo.vkCode == 115 || objKeyInfo.key == Keys.F4))
                    {
                        return (IntPtr)1;
                    }

                    int RAltKey = GetAsyncKeyState(Keys.LButton | Keys.MButton | Keys.Space | Keys.F17);
                    if (RAltKey != 0 && (objKeyInfo.vkCode == 115 || objKeyInfo.key == Keys.F4))
                    {
                        return (IntPtr)1;
                    }
                }

                //Window key
                if (objKeyInfo.key == Keys.RWin || objKeyInfo.key == Keys.LWin)
                {
                    {
                        return (IntPtr)1;
                    }
                }

                //CTRL + X
                //if (!CutEnable)
                {
                    if ((objKeyInfo.key == Keys.X) || (objKeyInfo.vkCode == 88))
                    {
                        int LCtrlKey = GetAsyncKeyState(Keys.RButton | Keys.Space | Keys.F17);
                        int RCtrlKey = GetAsyncKeyState(Keys.LButton | Keys.RButton | Keys.Space | Keys.F17);

                        if ((LCtrlKey != 0 && (objKeyInfo.key == Keys.X || objKeyInfo.vkCode == 88)) || (RCtrlKey != 0 && (objKeyInfo.key == Keys.X || objKeyInfo.vkCode == 88)))
                        {
                            return (IntPtr)1;
                        }
                    }
                }

                if ((objKeyInfo.vkCode == 81))
                {
                    int LCtrlKey = GetAsyncKeyState(Keys.RButton | Keys.Space | Keys.F17);
                    int RCtrlKey = GetAsyncKeyState(Keys.LButton | Keys.RButton | Keys.Space | Keys.F17);

                    if ((LCtrlKey != 0 && (objKeyInfo.key == Keys.Q || objKeyInfo.vkCode == 81)) || (RCtrlKey != 0 && (objKeyInfo.key == Keys.Q || objKeyInfo.vkCode == 81)))
                    {
                        //this.WindowState = FormWindowState.Normal;
                        //this.Focus();
                    }
                }

                //CTRL + C,V ,X
                //if ((objKeyInfo.key == Keys.C || objKeyInfo.key == Keys.V || objKeyInfo.key == Keys.X) ||
                //(objKeyInfo.vkCode == 67) || (objKeyInfo.vkCode == 86) || (objKeyInfo.vkCode == 88))
                //{
                //    int LCtrlKey = GetAsyncKeyState(Keys.RButton | Keys.Space | Keys.F17);
                //    int RCtrlKey = GetAsyncKeyState(Keys.LButton | Keys.RButton | Keys.Space | Keys.F17);

                //    if ((LCtrlKey != 0 && (objKeyInfo.key == Keys.C || objKeyInfo.vkCode == 67))
                //        || (RCtrlKey != 0 && (objKeyInfo.key == Keys.C || objKeyInfo.vkCode == 67)))
                //    {
                //        return (IntPtr)1;
                //    }

                //    if ((LCtrlKey != 0 && (objKeyInfo.key == Keys.V || objKeyInfo.vkCode == 86))
                //        || (RCtrlKey != 0 && (objKeyInfo.key == Keys.V || objKeyInfo.vkCode == 86)))
                //    {
                //        return (IntPtr)1;
                //    }

                //    if ((LCtrlKey != 0 && (objKeyInfo.key == Keys.X || objKeyInfo.vkCode == 88))
                //        || (RCtrlKey != 0 && (objKeyInfo.key == Keys.X || objKeyInfo.vkCode == 88)))
                //    {
                //        return (IntPtr)1;
                //    }
                //}

                //}
            }

            return CallNextHookEx(ptrHook, nCode, wp, lp);
        }
        bool HasAltModifier(int flags)
        {
            return (flags & 0x20) == 0x20;
        }

        public bool LipiDownloadData(string strFileName)
        {
            try
            {
                //RMS_ServerInfo = "http://localhost:50462/Service1.svc";
                WebClient objWC = new WebClient();
                objWC.Headers[HttpRequestHeader.ContentType] = "text/json";
                string strURL = RMS_ServerInfo + "/LipiDownloadData";
                string result = objWC.UploadString(strURL, "POST", "\"" + strFileName + "\"");
                objLog.WriteFile("LipiDownloadData Response : " + result);

                MemoryStream memstrToReceive = new MemoryStream(Encoding.UTF8.GetBytes(result));
                DataContractJsonSerializer objJsonSerRecv = new DataContractJsonSerializer(typeof(ResDownloadData));
                ResDownloadData objDownloadData = (ResDownloadData)objJsonSerRecv.ReadObject(memstrToReceive);

                if (objDownloadData.Result)
                {
                    objLog.WriteFile("Download Data Successfully : " + strFileName);
                    return true;
                }
                else
                {
                    objLog.WriteFile("Download Data Failed : " + strFileName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in LipiCommandUpdateAck Response : " + ex.Message);
                return false;
            }
        }

        public void LipiUploadData(string strClientIP, string strFileName)
        {
            try
            {
                //RMS_ServerInfo = "http://localhost:50462/Service1.svc";
                LogDetails objLogDetails = new LogDetails();
                objLogDetails.ClientIP = strClientIP;
                objLogDetails.Filename = strFileName.Substring(strFileName.LastIndexOf("\\") + 1);
                objLogDetails.Logdata = Convert.ToBase64String(File.ReadAllBytes(strFileName));
                
                WebClient objWC = new WebClient();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(LogDetails));
                MemoryStream stream1 = new MemoryStream();
                ser.WriteObject(stream1, objLogDetails);
                string strdata = Encoding.Default.GetString(stream1.ToArray());
                objWC.Headers[HttpRequestHeader.ContentType] = "text/json";
                string strURL = RMS_ServerInfo + "/LipiUploadData";
                string result = objWC.UploadString(strURL, "POST", strdata);
                objLog.WriteFile("LipiUploadData Response : " + result);
                if (result.ToLower().Contains("true"))
                {
                    objLog.WriteFile("Lipi Upload Data SuccessFully");
                    objClientConfigINI.Read("Kiosk_Details", "PreviousDate", DateTime.Now.ToString("ddMMyyyy"));
                }
                else
                    objLog.WriteFile("Lipi Upload Data Uploaded failed");
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in LipiUploadData Response : " + ex.Message);
            }
        }
        public void LipiPatchUpdateAck(string strClientIP, string strMachineSerialNo, string strUpdateType)
        {
            try
            {
                string strSendData = strClientIP + "#" + strMachineSerialNo + "#" + strUpdateType;

                EncRequest encRequest = new EncRequest() {RequestData=AesGcm256.Encrypt(JsonConvert.SerializeObject(strSendData)) };
                string jsonData = JsonConvert.SerializeObject(encRequest);

                WebClient objWC = new WebClient();
                objWC.Headers[HttpRequestHeader.ContentType] = "text/json";
                string strURL = RMS_ServerInfo + "/LipiPatchAck";
                string result = objWC.UploadString(strURL, "POST", jsonData);
                objLog.WriteFile("LipiPatchUpdateAck Response : " + result);
            }
            catch(Exception ex)
            {
                objLog.WriteFile("Exception in LipiPatchUpdateAck Response : " + ex.Message);
            }
        }
        public void LipiCommandUpdateAck(string strClientIP)
        {
            try
            {   
                WebClient objWC = new WebClient();
                objWC.Headers[HttpRequestHeader.ContentType] = "text/json";
                string strURL = RMS_ServerInfo + "/LipiCommandAck";
                string result = objWC.UploadString(strURL, "POST", "\"" + strClientIP + "\"");
                objLog.WriteFile("LipiCommandUpdateAck Response : " + result);
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in LipiCommandUpdateAck Response : " + ex.Message);
            }
        }       

        public void OnChangedHealth(object source, FileSystemEventArgs e)
        {
            string strChanges_Done = "";

            try
            {
                _fileWatcherHealth.EnableRaisingEvents = false;

                //b_ChangedHealth = true;
                if (KioskDetailsSent == "1")
                {
                    strChanges_Done = objClientHealthINI.Read("Health_Details", "Changes_DoneHealth", "");
                    objLog.WriteFile("Event Raised OnChangedHealthIni: Changes_Done Value = " + strChanges_Done);

                    if (strChanges_Done == "1")    //Changes Done Health
                    {
                        objClientHealthINI.Write("Health_Details", "Changes_DoneHealth", "0");

                        string strDeviceData = "";
                        int iDeviceCount = objClientHealthINI.ReadIntValue("Health_Details", "DeviceCount", "0");
                        for (int iCount = 0; iCount < iDeviceCount; iCount++)
                        {
                            string strTemp = objClientHealthINI.Read("Health_Details", "D" + (iCount + 1), "");
                            strDeviceData += "#" + (strTemp != "" ? strTemp : "Unknown");
                        }

                        strCurrentHealth = strDeviceData;

                        //Now Sending The Health Data
                        string HealthData = "02"
                                               + "$" + KioskIP
                                               + "$" + KioskMachineSrNo
                                               + "$" + DateTime.Now.ToString("yyyyMMdd HH:mm:ss")
                                               + "$" + objClientHealthINI.Read("Health_Details", "DeviceCount", "0");

                        HealthData += strDeviceData;

                        if (strPrevHealth != strCurrentHealth)
                        {
                            strPrevHealth = strCurrentHealth;

                            objLog.WriteFile("HealthData send on OnChangedHealth() :- " + HealthData);

                            try
                            {
                                if (objWC == null)
                                    objWC = new WebClient();

                                objWC.Headers[HttpRequestHeader.ContentType] = "text/json";

                                byte[] barr = Encoding.ASCII.GetBytes(HealthData);
                                string strbase64 = Convert.ToBase64String(barr);
                                string strURL = "";
                                strURL = RMS_ServerInfo + "/HealthData";
                                string result = objWC.UploadString(strURL, "POST", "\"" + strbase64 + "\"");

                                objLog.WriteFile("HealthData Response Recieved : " + result);
                            }
                            catch (Exception ex)
                            {
                                objLog.WriteFile("HealthData Message Sending : " + ex.Message);
                            }
                        }
                        else
                        {
                            objLog.WriteFile("Previous and Current Health matched");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Error: " + "OnChanged_Health()\n" + ex.Message.ToString());
            }
            finally
            {
                _fileWatcherHealth.EnableRaisingEvents = true;               
            }
        }


        void TxnSchedulerTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                //this.Invoke(new MethodInvoker(delegate { TxnSchedulerTick.Enabled = false; }));

                //string strPreviousDate = objClientConfigINI.Read("Kiosk_Details", "PreviousDate", "");
                //if (strPreviousDate != DateTime.Now.ToString("dd_MM_yyyy"))
                //{
                //    objLog.WriteFile("Date changed : upload ej data " + strPreviousDate);

                //    //To upload EJ Data on date changed
                //    string strZipFilePath = UploadLogEJ(strPreviousDate, strPreviousDate);
                //    if (strZipFilePath != "")
                //        LipiUploadData(LocalIPAddress(), strZipFilePath);
                //}
            }
            catch (Exception ex)
            {
                objLog.WriteFile("TxnSchedulerTimer_Tick: " + ex.Message.ToString());
            }
            finally
            {
                this.Invoke(new MethodInvoker(delegate { TxnSchedulerTick.Enabled = true; }));
            }
        }


        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Q))
            {
                objLog.WriteFile("*******************Application Close********************");
                Process.GetCurrentProcess().Kill();                
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void WndProc(ref System.Windows.Forms.Message message)
        {
            switch (message.Msg)
            {
                case WM_SYSCOMMAND:

                    int command = message.WParam.ToInt32() & 0xfff0;
                    if (command == SC_CLOSE)
                    {
                        this.Hide();
                        return;
                    }

                    break;
            }
            base.WndProc(ref message);
        }
        
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //this.UseWaitCursor = false; Application.DoEvents();
            //this.Visible = true;
            //this.ShowInTaskbar = false;
            //this.WindowState = FormWindowState.Normal;
        }
        


        public static bool IsValidIP(string ipAddress)
        {
            bool bReturnValue = false;
            IPAddress address;

            if (IPAddress.TryParse(ipAddress, out address))
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    bReturnValue = true;
                }
            }
            else
                bReturnValue = false;

            if (bReturnValue)
            {
                string inIP = ipAddress;
                int iPartIP = 0;
                if (inIP.IndexOf(".") >= 0 && int.TryParse(inIP.Substring(0, inIP.IndexOf(".")), out iPartIP) && (iPartIP > 0 && iPartIP <= 255))
                {
                    inIP = inIP.Substring(inIP.IndexOf(".") + 1);
                    iPartIP = 0;
                    if (inIP.IndexOf(".") >= 0 && int.TryParse(inIP.Substring(0, inIP.IndexOf(".")), out iPartIP) && (iPartIP >= 0 && iPartIP <= 255))
                    {
                        inIP = inIP.Substring(inIP.IndexOf(".") + 1);
                        iPartIP = 0;
                        if (inIP.IndexOf(".") >= 0 && int.TryParse(inIP.Substring(0, inIP.IndexOf(".")), out iPartIP) && (iPartIP >= 0 && iPartIP <= 255))
                        {
                            inIP = inIP.Substring(inIP.IndexOf(".") + 1);
                            iPartIP = 0;
                            if (int.TryParse(inIP, out iPartIP) && (iPartIP >= 0 && iPartIP <= 255))
                            {
                                bReturnValue = true;
                            }
                            else
                                bReturnValue = false;
                        }
                        else
                            bReturnValue = false;
                    }
                    else
                        bReturnValue = false;
                }
                else
                    bReturnValue = false;
            }

            return bReturnValue;
        }

        public string GetLipiRDService_Version()
        {   
            try
            {
                //Get Lipi RD Service version
                FileVersionInfo objFileVersion;
                objFileVersion = FileVersionInfo.GetVersionInfo("C:\\LipiRDService\\LipiRDService.exe");
                return "RD" + objFileVersion.FileVersion;                
            }
            catch { return "NA" + "/"; }
        }

        public string GetLipiRMSMonitor_Version()
        {   
            try
            {
                //Get Client monitor version
                FileVersionInfo objFileVersion;
                objFileVersion = FileVersionInfo.GetVersionInfo(Application.StartupPath + @"\RMSClientMonitor.exe");
                return "M" + objFileVersion.FileVersion;                
            }
            catch { return "NA" + "/"; }
        }
       
        //Build Client Details
        void BuildClientDetail()
        {   
            ReadKioskDetails();

            strClientDetails = "";

            //01 For Kiosk Information
            strClientDetails += "01" + "#";

            //Kiosk IP
            strClientDetails += KioskIP + "#";

            //Kiosk ID
            strClientDetails += KioskID + "#";

            //Kiosk MAC address
            strClientDetails += strMacAddress + "#";

            // Machine Serial number
            strClientDetails += KioskMachineSrNo + "#"; 

            //RMS Client Version
            strClientDetails += "V" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "/";

            //Lipi RD Service Version
            strClientDetails += GetLipiRDService_Version() + "/";

            //RMS Client Monitor Version
            strClientDetails += GetLipiRMSMonitor_Version() + "#";            

            //loop through each drive V1.0.0.6
            foreach (var drive in DriveInfo.GetDrives())
            {
                //fetch drive's type
                DriveType driveType = drive.DriveType;

                //check the type then
                switch (driveType)
                {
                    case DriveType.Fixed:   //if fixed drive
                        strClientDetails += drive.Name + " ";
                        break;
                }
            }

            strClientDetails += "#" + LastPatchUpdated;   // Last Patch Updated
        }
              
        void Connect()
        {
            //Create a client object to connect a server on 127.0.0.1 (local) IP and listens 10085 TCP port
            if (client == null)
            {
                try
                {                    
                    client = ScsClientFactory.CreateClient(new ScsTcpEndPoint(WebHostIP, Convert.ToInt32(Port_No)));                  
                }
                catch (Exception ex)
                {
                    objLog.WriteFile("Exception in Connect(): " + ex.Message + Environment.NewLine + ex.StackTrace);
                    return;
                }
                
                //Register to MessageReceived event to receive messages from server.
                client.MessageReceived += Client_MessageReceived;
                client.Connected += client_Connected;
                client.Disconnected += client_Disconnected;               
            }

            try
            {
                client.Connect(); //Connect to the server.
                bIsTcpConnectivityPresent = true;
            }
            catch (Exception ex)
            {
                bIsTcpConnectivityPresent = false;
                objLog.WriteFile("Connection failed, could not contact to server. Trying to connect...." + ex.Message + Environment.NewLine + ex.StackTrace);
                client = null;
            }
        }

        void client_Connected(object sender, EventArgs e)
        {
            try
            {
                objLog.WriteFile("Client connected");

                //BuildClientDetail();
                if (client != null && client.CommunicationState == Lipi.Communication.Scs.Communication.CommunicationStates.Connected)
                {
                    client.SendMessage(new ScsRawDataMessage(Encoding.Default.GetBytes("01")));   //Success
                }
            }
            catch(Exception ex)
            {
                objLog.WriteFile("Exception in client_Connected() : " + ex.Message);
            }
        }

        void Disconnect()
        {
            client.Disconnect();
            objLog.WriteFile("Client disconnected");
            bIsServiceConnectivityPresent = false;
            GC.Collect();           
            client = null;  //Lokesh Add[10 May 2013]
        }

        void client_Disconnected(object sender, EventArgs e)
        {
            //if (InvokeRequired)
            //    this.Invoke(new MethodInvoker(delegate { ErrorMsg.Visible = false; }));
            bIsTcpConnectivityPresent = false;
            objLog.WriteFile("Command Socket Disconnected.(Shutdown Method)");            
        }
        
        public void Client_MessageReceived(object sender, MessageEventArgs e)
        {
            
            //Client only accepts text messages
            var message = e.Message as ScsRawDataMessage;
            if (message == null)
            {
                return;
            }

            string strMessageParams = Encoding.Default.GetString(message.MessageData);
            objLog.WriteFile("TCP message received - " + strMessageParams);

            //SendFirstACK();
            ExecuteCommands(strMessageParams);
            //SendLastACK();

        }

        private void Client_PatchDownload(string patchName)
        {
            try
            {
                if (objWC == null)
                    objWC = new WebClient();

                objLog.WriteFile("Try to Patch Download in Client_PatchDownload : " + patchName);

                string result = objWC.UploadString(RMS_ServerInfo + "/PatchUpdate", "POST", "");             

                MemoryStream memstrToReceive = new MemoryStream(Encoding.UTF8.GetBytes(result));
                DataContractJsonSerializer objJsonSerRecv = new DataContractJsonSerializer(typeof(ResPatchUpdate));
                ResPatchUpdate objNewPatchName = (ResPatchUpdate)objJsonSerRecv.ReadObject(memstrToReceive);

                objLog.WriteFile("Client Patch Download Result: " + objNewPatchName.PatchName + "\n" + objNewPatchName.Result.ToString());

                if (objNewPatchName.Result)
                {
                    if (!Directory.Exists(DownloadPath + "PATCH"))
                        Directory.CreateDirectory(DownloadPath + "PATCH");

                    File.WriteAllBytes(DownloadPath + "PATCH\\" + objNewPatchName.PatchName, Convert.FromBase64String(objNewPatchName.PatchData));
                    objClientConfigINI.Write("Kiosk_Details", "PatchReceived", objNewPatchName.PatchName);
                    objLog.WriteFile("File saved:" + objNewPatchName.PatchName);
                    
                    objLog.WriteFile("Patch name updated in ini file : " + objNewPatchName.PatchName);

                    client.SendMessage(new ScsRawDataMessage(Encoding.Default.GetBytes("50#S")));  //OK
                }
                else
                {
                    client.SendMessage(new ScsRawDataMessage(Encoding.Default.GetBytes("50#F")));  //Failed
                    objLog.WriteFile("Patch download Failed: " + objNewPatchName.PatchName + "\n" + objNewPatchName.Error);
                }
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception: Client_PatchDownload " + ex.Message + Environment.NewLine + ex.StackTrace);
                client.SendMessage(new ScsRawDataMessage(Encoding.Default.GetBytes("50#F")));  //Failed
            }
        }

        public void KillApplication()
        {
            try
            {
                Process p = Process.Start("C:\\Program Files\\Internet Explorer\\iexplore.exe", "-k \"C:\\Kiosk\\lipi\\OOOS\\MaintenanceMode.html\"");
                Thread.Sleep(1000);
            }
            catch (Exception excp)
            {
                objLog.WriteFile("Exception in C:\\Kiosk\\lipi\\OOOS\\MaintenanceMode.html " + excp.Message + Environment.NewLine + excp.StackTrace);
            }

            try
            {
                // Kill KPROC First.
                Process[] procs = Process.GetProcessesByName("KPROC");
                foreach (Process proc in procs)
                {
                    proc.Kill();
                    objLog.WriteFile("KPROC Application Successfully Killed.");
                }
                Thread.Sleep(1000);

                // Kill  Lipi_Browser 
                Process[] procs1 = Process.GetProcessesByName("Lipi_Browser");
                foreach (Process proc in procs1)
                {
                    proc.Kill();
                    objLog.WriteFile("Lipi_Browser Application Successfully Killed.");
                }
                System.Threading.Thread.Sleep(1000);

                //Kill Mirror
                Process[] procs2 = Process.GetProcessesByName("Mirror");
                foreach (Process proc in procs2)
                {
                    proc.Kill();
                    objLog.WriteFile("Mirror Application Successfully Killed.");
                }
                Thread.Sleep(1000);

                //Kill ProgDlg
                Process[] procs3 = Process.GetProcessesByName("progdlg");
                foreach (Process proc in procs3)
                {
                    proc.Kill();
                    objLog.WriteFile("Progdlg Application Successfully Killed.");
                }
                Thread.Sleep(1000);

                //Kill Ftp Upload
                Process[] procs4 = Process.GetProcessesByName("FTP Upload");
                foreach (Process proc in procs4)
                {
                    proc.Kill();
                    objLog.WriteFile("FTP Upload Application Successfully Killed.");
                }
                Thread.Sleep(1000);
            }
            catch (Exception e)
            {
                objLog.WriteFile("Exception in KillApplication(): " + e.Message + Environment.NewLine + e.StackTrace);
                return;
            }
        }

        public bool RemoteEnable()
        {
            bool bIsRemoteEnable = false;
            try
            {
                RegistryKey key = Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server");
                key.SetValue("fDenyTSConnections", 0, RegistryValueKind.DWord);
                key.Close();
                objLog.WriteFile("RemoteEnable : Success");
                bIsRemoteEnable = true;
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in RemoteEnable - " + ex.Message);
                bIsRemoteEnable = false;
            }
            finally
            {
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE | WM_WININICHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 200, IntPtr.Zero);
            }
            return bIsRemoteEnable;
        }

        public bool RemoteDisable()
        {
            bool bIsRemoteDisable = false;
            try
            {
                RegistryKey key = Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server");
                key.SetValue("fDenyTSConnections", 1, RegistryValueKind.DWord);
                key.Close();
                objLog.WriteFile("RemoteDisable : Success");
                bIsRemoteDisable = true;
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in RemoteDisable - " + ex.Message);
                bIsRemoteDisable = false;
            }
            finally
            {
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE | WM_WININICHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 200, IntPtr.Zero);
            }
            return bIsRemoteDisable;
        }

        public bool DriveEnable()  //C and D Drive
        {
            bool bIsDriveEnable = false;
            try
            {   
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoViewOnDrive", 0, RegistryValueKind.DWord);
                objLog.WriteFile("DriveEnable C and D Success");
                bIsDriveEnable = true;
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in DriveEnable - " + ex.Message);
                bIsDriveEnable = false;
            }
            finally
            {
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE | WM_WININICHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 200, IntPtr.Zero);
            }
            return bIsDriveEnable;
        }

        public bool DriveDisable()  //C and D Drive
        {
            bool bIsDriveDisable = false;
            try
            {
                //A: 1, B: 2, C: 4, D: 8, E: 16, F: 32, G: 64, H: 128, 
                //I: 256, J: 512, K: 1024, L: 2048, M: 4096, N: 8192, 
                //O: 16384, P: 32768, Q: 65536, R: 131072, S: 262144, 
                //T: 524288, U: 1048576, V: 2097152, W: 4194304, X: 8388608, Y: 16777216, Z: 33554432, ALL: 67108863

                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoViewOnDrive", 12, RegistryValueKind.DWord);
                objLog.WriteFile("DriveEnable C and D Success");
                bIsDriveDisable = true;
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in DriveDisable - " + ex.Message);
                bIsDriveDisable = false;
            }
            finally
            {
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE | WM_WININICHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 200, IntPtr.Zero);
            }
            return bIsDriveDisable;
        }


        public bool USBEnable()
        {
            bool bIsUSBEnable = false;
            try
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", 3, RegistryValueKind.DWord);
                objLog.WriteFile("USBEnable Success");
                bIsUSBEnable = true;
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in USBEnable - " + ex.Message);
                bIsUSBEnable = false;
            }
            finally
            {
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE | WM_WININICHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 200, IntPtr.Zero);
            }
            return bIsUSBEnable;
        }

        public bool USBDisable()
        {
            bool bIsUSBDisable = false;
            try
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\USBSTOR", "Start", 4, RegistryValueKind.DWord);
                objLog.WriteFile("USBDisable Success");
                bIsUSBDisable = true;
            }
            catch (Exception ex)
            {   
                objLog.WriteFile("Exception in USBDisable - " + ex.Message);
                bIsUSBDisable = false;
            }
            finally
            {
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE | WM_WININICHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 200, IntPtr.Zero);
            }
            return bIsUSBDisable;
        }

        public bool WindowsKeyEnable()
        {
            bool bIsUSBDisable = false;
            try
            {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoWinKeys", 0, RegistryValueKind.DWord);
                objLog.WriteFile("WindowsKeyEnable Success");
                bIsUSBDisable = true;
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in WindowsKeyEnable - " + ex.Message);
                bIsUSBDisable = false;
            }
            finally
            {
                SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE | WM_WININICHANGE, IntPtr.Zero, null, SMTO_ABORTIFHUNG, 200, IntPtr.Zero);
            }
            return bIsUSBDisable;
        }

        public string UploadLog(string LogsType, string fDate, string Tdate)
        {
            try
            {
                int dateDifference = 0;              
                DateTime dtIterator;
                string szFileZipPath = "";
                DateTime fromdate;
                DateTime todate1;
                string strLogDatapath = "C:\\Log_Data";

                fromdate = DateTime.ParseExact(fDate.Trim(), "dd_MM_yyyy", null);
                todate1 = DateTime.ParseExact(Tdate.Trim(), "dd_MM_yyyy", null);

                //count total days difference
                dateDifference = todate1.Subtract(fromdate).Days + 1;

                if (LogsType.ToLower() == "alllogs")
                    szFileZipPath = DownloadPath + "Log\\AllLog" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".zip";
                else if (LogsType.ToLower() == "kioskmachinelogs")
                    szFileZipPath = DownloadPath + "Log\\MachineLog" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".zip";
                else if (LogsType.ToLower() == "clientlogs")
                    szFileZipPath = DownloadPath + "Log\\ClientLog" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".zip";
                else if (LogsType.ToLower() == "ej")
                    szFileZipPath = DownloadPath + "Log\\EJLog" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".zip";

                objLog.WriteFile("Logs Copy Information:" + szFileZipPath);

                dtIterator = fromdate;

                using (ZipFile zip = new ZipFile(szFileZipPath))
                {
                    for (int i = 0; i < dateDifference; i++)
                    {
                        if (Directory.Exists(strLogDatapath))
                        {
                            //Loop through all directories to seaarch for files
                            foreach (string file in Directory.GetFiles(strLogDatapath, "*" + dtIterator.ToString("dd_MM_yyyy") + ".txt", SearchOption.AllDirectories))
                            {
                                bool bIsRequired = false;

                                if (LogsType.ToLower() == "alllogs")
                                    bIsRequired = true;
                                else if (LogsType.ToLower() == "kioskmachinelogs")
                                {
                                    if (!file.Contains("RMSClient") && !file.Contains("MonitorClient"))
                                        bIsRequired = true;
                                }
                                else if (LogsType.ToLower() == "clientlogs")
                                {
                                    if (file.Contains("RMSClient") || file.Contains("MonitorClient"))
                                        bIsRequired = true;
                                }
                                else if (LogsType.ToLower() == "ej")
                                {
                                    if (file.Contains("EJDATA"))
                                        bIsRequired = true;
                                }


                                if (bIsRequired)
                                {
                                    try
                                    {
                                        //give permission to file first V35.1.0.4
                                        DirectoryInfo dInfo = new DirectoryInfo(file);
                                        DirectorySecurity dSecurity = dInfo.GetAccessControl();
                                        dSecurity.AddAccessRule(new FileSystemAccessRule("everyone", FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.InheritOnly, AccessControlType.Allow));
                                        dInfo.SetAccessControl(dSecurity);

                                        if (file.LastIndexOf("Backup") > 0)
                                        {
                                            string strParentDir = Path.GetDirectoryName(Path.GetDirectoryName(file));

                                            //add the file in zip
                                            zip.AddFile(file, "Logs\\" + strParentDir.Substring(strParentDir.LastIndexOf("\\") + 1) + "\\");

                                            objLog.WriteFile("Zip Add File:" + file);
                                        }
                                        else
                                        {
                                            //add the file in zip
                                            zip.AddFile(file, "Logs\\" + Path.GetDirectoryName(file).Substring(Path.GetDirectoryName(file).LastIndexOf("\\") + 1) + "\\");

                                            objLog.WriteFile("Zip Add File:" + file);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        objLog.WriteFile("Excp in UploadLog() adding file- " + ex.Message + Environment.NewLine + ex.StackTrace);
                                    }
                                }
                            }
                        }

                        //increment the iterator date
                        dtIterator = dtIterator.AddDays(1);
                    }

                    objLog.WriteFile("Zip file is saving....");
                    zip.Save();
                    objLog.WriteFile("Zip Saved:");
                }

                GC.Collect();
                return szFileZipPath;
            }
            catch (Exception excp)
            {
                objLog.WriteFile("Excp in UploadLog()- " + excp.Message + Environment.NewLine + excp.StackTrace);
                return "";
            }
        }

        public string UploadCameraImages(string strTXNDate, string strTxnID)
        {
            string szFileZipPath = DownloadPath + "Log\\CameraImages_" + strTxnID.ToLower() + "_" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".zip";
            string strCameraPath = "D:\\CameraImages\\" + strTXNDate + "\\" + strTxnID;           
            if (Directory.Exists(strCameraPath))
            {
                using (ZipFile zip = new ZipFile(szFileZipPath))
                {
                    //Loop through all directories to seaarch for files
                    foreach (string file in Directory.GetFiles(strCameraPath, "*.jpg"))
                    {
                        zip.AddFile(file, strTxnID + "\\") ;
                        objLog.WriteFile("Zip add image file- " + file);
                    }
                    zip.Save();
                    objLog.WriteFile("Zip saved");
                }
                return szFileZipPath;
            }
            else
            {
                return "";
            }
        }

        private void PatchInstaller(string strPatchPath,string strPatchName)
        {
            KillApplication();

            // Extract patch
            Extract(strPatchPath, strPatchName);

            //clear patch received from INI
            objClientConfigINI.Write("Kiosk_Details", "PatchReceived", "");
            objLog.WriteFile("PatchReceived clear from INI");

            Process[] procs = Process.GetProcessesByName("iexplore");
            foreach (Process proc in procs)
            {
                proc.Kill();
                objLog.WriteFile("iexplore Application Successfully Killed.");
            }
            Thread.Sleep(500);

            System.Diagnostics.Process.Start("C:\\Kiosk\\KPROC.exe");
            objLog.WriteFile("KPROC application started successfully.");
            Thread.Sleep(2000);
        }

        private void PatchInstallerClient(string strPatchPath, string strPatchName)
        {
            
           // Thread.Sleep(1000);
            Extract(strPatchPath, strPatchName);
            Process[] procs = Process.GetProcessesByName("Remote Client");
            foreach (Process proc in procs)
            {
                proc.Kill();
                objLog.WriteFile("Client Application Successfully Killed.");
            }
        }
     
        public void DateTimeChange(string DateTime)
        {
            try
            {
                SYSTEMTIME st = new SYSTEMTIME();
                st.wYear = (ushort)Convert.ToInt16(DateTime.Substring(0, 4)); ; // must be short
                st.wMonth = (ushort)Convert.ToInt16(DateTime.Substring(4, 2));
                st.wDay = (ushort)Convert.ToInt16(DateTime.Substring(6, 2));
                st.wHour = (ushort)Convert.ToInt16(DateTime.Substring(8, 2));
                st.wMinute = (ushort)Convert.ToInt16(DateTime.Substring(10, 2));
                st.wSecond = 0;
                SetLocalTime(ref st);
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in  DateTimeChange():" + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public void Extract1()
        {
            try
            {
                if (Patchfolderpath != null)
                {
                    using (ZipFile zip = ZipFile.Read(Patchfolderpath))
                    {
                        foreach (ZipEntry e in zip)
                        {
                            e.Extract("C:\\Kiosk\\XML\\", ExtractExistingFileAction.OverwriteSilently);  // overwrite == true  
                            objLog.WriteFile("Extract File Name: " + e.ToString());
                        }
                    }
                  
                    Application.DoEvents();
                    //zp.Dispose();
                    objLog.WriteFile("ISO8583CFG.xml Installed Successfully");
                }
            }
            catch (Exception e)
            {
                objLog.WriteFile("Exception in  Extract() function" + e.Message + Environment.NewLine + e.StackTrace);
            }
        }

        public void Extract(string strPatchPath, string strPatchName)
        {
            try
            {
                if (strPatchName.ToLower().Contains("kproc"))
                {
                    if (strPatchPath != null)
                    {
                        using (ZipFile zip = ZipFile.Read(strPatchPath + "\\" + strPatchName))
                        {
                            foreach (ZipEntry zipEntry in zip)
                            {
                                try
                                {
                                    zipEntry.Extract("C:\\Kiosk\\", ExtractExistingFileAction.OverwriteSilently);  // overwrite == true  
                                    objLog.WriteFile("Extract File Name: " + zipEntry.ToString());
                                }
                                catch (Exception ex)  //added catch region to extract file when .tmp and .PendingOverwritefile exists
                                {
                                    objLog.WriteFile("Excp in extracting file:- " + ex.Message);
                                    foreach (var postFix in new[] { ".tmp", ".PendingOverwrite" })
                                    {
                                        var errorPath = Path.Combine(strPatchPath + "\\", zipEntry.FileName) + postFix;
                                        if (File.Exists(errorPath))
                                        {
                                            try
                                            {
                                                File.Delete(errorPath);
                                                objLog.WriteFile("Deleted File Name: " + errorPath.ToString());
                                            }
                                            catch (Exception Excp)
                                            {
                                                objLog.WriteFile("Exception in deleting .tmp files :-" + Excp.Message);
                                                throw Excp;
                                            }
                                        }
                                    }

                                    objLog.WriteFile("extracting after deleting .tmp file");
                                    zipEntry.Extract("C:\\Kiosk\\", ExtractExistingFileAction.OverwriteSilently);
                                    objLog.WriteFile("Extract File Name: " + zipEntry.ToString());
                                }
                            }

                            if (File.Exists(strPatchPath + "\\StartBatFile.bat"))
                            {
                                objLog.WriteFile("StartBatFile found : " + strPatchPath + "\\StartBatFile.bat");
                                Process.Start(strPatchPath + "\\StartBatFile.bat");
                                objLog.WriteFile(strPatchPath + "\\StartBatFile.bat" + " application started");
                            }
                        }

                        objLog.WriteFile("KPROC Patch Installed Successfully");
                    }
                }
                else if (strPatchName.ToLower().Contains("s4pict"))
                {
                    if (strPatchPath != null)
                    {
                        using (ZipFile zip = ZipFile.Read(strPatchPath + "\\" + strPatchName))
                        {
                            foreach (ZipEntry zipEntry in zip)
                            {
                                try
                                {
                                    zipEntry.Extract("C:\\S4PICT\\", ExtractExistingFileAction.OverwriteSilently);  // overwrite == true  
                                    objLog.WriteFile("Extract File Name: " + zipEntry.ToString());
                                }
                                catch (Exception ex)  //added catch region to extract file when .tmp and .PendingOverwritefile exists
                                {
                                    objLog.WriteFile("Excp in extracting file:- " + ex.Message);
                                    foreach (var postFix in new[] { ".tmp", ".PendingOverwrite" })
                                    {
                                        var errorPath = Path.Combine(strPatchPath + "\\", zipEntry.FileName) + postFix;
                                        if (File.Exists(errorPath))
                                        {
                                            try
                                            {
                                                File.Delete(errorPath);
                                                objLog.WriteFile("Deleted File Name: " + errorPath.ToString());
                                            }
                                            catch (Exception Excp)
                                            {
                                                objLog.WriteFile("Exception in deleting .tmp files :-" + Excp.Message);
                                                throw Excp;
                                            }
                                        }
                                    }

                                    objLog.WriteFile("extracting after deleting .tmp file");
                                    zipEntry.Extract("C:\\S4PICT\\", ExtractExistingFileAction.OverwriteSilently);
                                    objLog.WriteFile("Extract File Name: " + zipEntry.ToString());
                                }
                            }


                        }

                        objLog.WriteFile("S4PICT Patch Installed Successfully");
                    }

                }
                else if (strPatchName.ToLower().Contains("remote client"))
                    {
                    if (strPatchPath != null)
                    {
                        using (ZipFile zip = ZipFile.Read(strPatchPath + "\\" + strPatchName))
                        {
                            foreach (ZipEntry zipEntry in zip)
                            {
                                try
                                {
                                    zipEntry.Extract("C:\\RMS\\", ExtractExistingFileAction.OverwriteSilently);  // overwrite == true  
                                    objLog.WriteFile("Extract File Name: " + zipEntry.ToString());
                                }
                                catch (Exception ex)  //added catch region to extract file when .tmp and .PendingOverwritefile exists
                                {
                                    objLog.WriteFile("Excp in extracting file:- " + ex.Message);
                                    foreach (var postFix in new[] { ".tmp", ".PendingOverwrite" })
                                    {
                                        var errorPath = Path.Combine(strPatchPath + "\\", zipEntry.FileName) + postFix;
                                        if (File.Exists(errorPath))
                                        {
                                            try
                                            {
                                                File.Delete(errorPath);
                                                objLog.WriteFile("Deleted File Name: " + errorPath.ToString());
                                            }
                                            catch (Exception Excp)
                                            {
                                                objLog.WriteFile("Exception in deleting .tmp files :-" + Excp.Message);
                                                throw Excp;
                                            }
                                        }
                                    }

                                    objLog.WriteFile("extracting after deleting .tmp file");
                                    zipEntry.Extract("C:\\RMS\\", ExtractExistingFileAction.OverwriteSilently);
                                    objLog.WriteFile("Extract File Name: " + zipEntry.ToString());
                                }
                            }


                        }

                        objLog.WriteFile("Client  Patch Installed Successfully");
                    }


                 }

                }
            catch (Exception e)
            {
                objLog.WriteFile("Exception in  Extract() function" + e.Message + Environment.NewLine + e.StackTrace);
            }
        }

        private void SendAntiVirusDate(string strBatchfilePath)
        {
#region  added by Rajesh V11.0.0.15
            try
            {
                objLog.WriteFile("SendAntiVirusDate start");

                if (File.Exists(strBatchfilePath))
                {
                    objLog.WriteFile("Antivirus path exist." + strBatchfilePath);

                    if (!Directory.Exists(@"D:\BatchFile\output"))
                        Directory.CreateDirectory(@"D:\BatchFile\output");

                    //   Process.Start("D:\\BatchFile\\QHReport.bat");
                    objLog.WriteFile("QHReport.bat application started");

                    Process antivrusBatchFile = null;

                    //Check if initialized
                    if (antivrusBatchFile == null)
                        antivrusBatchFile = new Process();

                    // Set process initial properties
                    antivrusBatchFile.StartInfo.FileName = "D:\\BatchFile\\QHReport.bat"; // AssemblyPath + "\\WinSCP.com";
                    antivrusBatchFile.StartInfo.UseShellExecute = false;
                    antivrusBatchFile.StartInfo.RedirectStandardInput = true;
                    antivrusBatchFile.StartInfo.RedirectStandardOutput = true;
                    antivrusBatchFile.StartInfo.CreateNoWindow = true;

                    // Start the connection
                    antivrusBatchFile.Start();
                    antivrusBatchFile.StandardInput.Close();
                    string output = antivrusBatchFile.StandardOutput.ReadToEnd();
                    antivrusBatchFile.WaitForExit(1500000); // Wait until WinSCP finishes


                    string filename = "";
                    filename = "D:\\BatchFile\\output" + "\\Result" + DateTime.Now.ToString("MM-dd-yy") + ".txt";  // Result02-09-16.txt

                    objLog.WriteFile("FileName read- " + filename);

                    using (StreamReader sr = new StreamReader(filename))
                    {
                        try
                        {
                            string[] strAntiVirusDate = sr.ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.None);
                            //string date = strAntiVirusDate[2].ToString();
                            //date = date.Substring(date.IndexOf("-") + 1);
                            //string VirusDate = "56" + "#" + date;
                            string VirusDate = "";
                            foreach (string strLine in strAntiVirusDate)
                            {
                                if (strLine.IndexOf("Virus Database -") >= 0)
                                    VirusDate = "56" + "#" + strLine.Substring(strLine.IndexOf("-") + 2);
                            }

                            if (VirusDate != "")
                            {
                                objLog.WriteFile("Antivirus date read- " + VirusDate);
                                client.SendMessage(new ScsRawDataMessage(Encoding.Default.GetBytes(VirusDate)));
                            }

                            sr.Close();
                        }
                        catch (Exception ex)
                        {
                            objLog.WriteFile("Exception in send Date in SendAntiVirusDate():" + ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                    }
                }
                else
                {
                    objLog.WriteFile("antiviruspath path not exist." + strBatchfilePath);
                }
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in antivirus Date send SendAntiVirusDate():" + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        #endregion
        }

        private bool AntiVirusBatchFile(string strFileName)
        {
            bool bResult = false;
            try
            {
                objLog.WriteFile("AntiVirusbatchFile Application patch received");

                string strAVBatchPath = DownloadPath + "PATCH\\" + strFileName;

                if (strAVBatchPath.ToLower().Contains("qhreport") && strAVBatchPath.ToLower().Contains(".zip"))
                {
                    using (ZipFile zip = ZipFile.Read(strAVBatchPath))
                    {
                        foreach (ZipEntry eZip in zip)
                        {
                            try
                            {
                                eZip.Extract("D:\\BatchFile\\", ExtractExistingFileAction.OverwriteSilently);  // overwrite == true  
                                objLog.WriteFile("Extract File Name: " + eZip.ToString());
                            }
                            catch (Exception ex)  //added catch region to extract file when .tmp and .PendingOverwritefile exists 
                            {
                                objLog.WriteFile("Excp in extracting file:- " + ex.ToString());
                                foreach (var postFix in new[] { ".tmp", ".PendingOverwrite" })
                                {
                                    var errorPath = Path.Combine("D:\\BatchFile\\", eZip.FileName) + postFix;
                                    if (File.Exists(errorPath))
                                    {
                                        try
                                        {
                                            File.Delete(errorPath);
                                            objLog.WriteFile("Deleted File Name: " + errorPath.ToString());
                                        }
                                        catch (Exception Excp)
                                        {
                                            objLog.WriteFile("Exception in deleting .tmp files :-" + Excp.Message);
                                            throw (Excp);
                                        }
                                    }
                                }

                                objLog.WriteFile("Extracting after deleting .tmp file");
                                eZip.Extract("D:\\BatchFile\\", ExtractExistingFileAction.OverwriteSilently);
                                objLog.WriteFile("Extract File Name: " + ex.Message);
                            }
                        }
                    }

                    objLog.WriteFile("AntiVirusbatchFile patch extracted successfully");
                }
                else
                {
                    objLog.WriteFile("Antivirus path not found, path is : " + strAVBatchPath);
                }

                bResult = true;
            }
            catch (Exception ex)
            {
                objLog.WriteFile("exception in AntiVirusbatchFile()  : " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            return bResult;
        }

        private bool UpdateRemoteClientMonitor(string strFileName)
        {
            bool bResult = false;
            try
            {
                objLog.WriteFile("RMSClientMonitor Application patch received");

                //first stop if already running
                Process[] procs = Process.GetProcessesByName("RMSClientMonitor");
                foreach (Process proc in procs)
                {
                    proc.Kill();
                    objLog.WriteFile("RMSClientMonitor Application Killed");
                }
                Thread.Sleep(1000);

                string strClientMonitorPath = DownloadPath + strFileName;

                if (strClientMonitorPath.ToLower().Contains("rmsclientmonitor") && strClientMonitorPath.ToLower().Contains(".zip"))
                {
                    using (ZipFile zip = ZipFile.Read(strClientMonitorPath))
                    {
                        //extract monitor application exe dll's
                        foreach (ZipEntry eZip in zip)
                        {
                            try
                            {
                                eZip.Extract("C:\\RMS\\", ExtractExistingFileAction.OverwriteSilently);  // overwrite == true  
                                objLog.WriteFile("Extract File Name: " + eZip.ToString());
                            }
                            catch (Exception ex)  //added catch region to extract file when .tmp and .PendingOverwritefile exists 
                            {
                                objLog.WriteFile("Excp in extracting file:- " + ex.ToString());
                                foreach (var postFix in new[] { ".tmp", ".PendingOverwrite" })
                                {
                                    var errorPath = Path.Combine("C:\\RMS\\", eZip.FileName) + postFix;
                                    if (File.Exists(errorPath))
                                    {
                                        try
                                        {
                                            File.Delete(errorPath);
                                            objLog.WriteFile("Deleted File Name: " + errorPath.ToString());
                                        }
                                        catch (Exception Excp)
                                        {
                                            objLog.WriteFile("Exception in deleting .tmp files :-" + Excp.Message);
                                            throw (Excp);
                                        }
                                    }
                                }

                                objLog.WriteFile("extracting after deleting .tmp file");
                                eZip.Extract("C:\\RMS\\", ExtractExistingFileAction.OverwriteSilently);
                                objLog.WriteFile("Extract File Name: " + ex.Message);
                            }
                        }
                    }

                    objLog.WriteFile("RMSClientMonitor changed successfully");
                }
                else
                {
                    objLog.WriteFile("path not found, path is : " + strClientMonitorPath);
                }

                Thread.Sleep(1000);

                //Start updated monitor application again
                Process.Start("C:\\RMS\\RMSClientMonitor.exe");
                objLog.WriteFile("RMSClientMonitor Application Started:");

                bResult = true;
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception:" + ex.Message + Environment.NewLine + ex.StackTrace);

                bool IsRunning = false;
                foreach (var P in Process.GetProcesses())
                {
                    if (P.ProcessName.Contains("RMSClientMonitor"))
                    {
                        IsRunning = true;
                        break;
                    }
                }

                if (IsRunning == false)
                {
                    Process.Start("C:\\RMS\\RMSClientMonitor.exe");
                    objLog.WriteFile("RMSClientMonitor Application Started:");
                }
            }

            return bResult;
        }

        private bool UpdateAVClient(string strFileName)
        {
            bool bResult = false;
            try
            {
                objLog.WriteFile("AVUpdateClient application patch received");

                //first stop if already running
                Process[] procs = Process.GetProcessesByName("AVUpdateClient");
                foreach (Process proc in procs)
                {
                    proc.Kill();
                    objLog.WriteFile("AVUpdateClient application killed");
                }
                Thread.Sleep(1000);

                string strAVClientPath = DownloadPath + "PATCH\\" + strFileName;

                if (strAVClientPath.ToLower().Contains("avupdateclient") && strAVClientPath.ToLower().Contains(".zip"))
                {
                    using (ZipFile zip = ZipFile.Read(strAVClientPath))
                    {
                        //extract monitor application exe dll's
                        foreach (ZipEntry eZip in zip)
                        {
                            try
                            {
                                eZip.Extract("C:\\RMS\\", ExtractExistingFileAction.OverwriteSilently);  // overwrite == true  
                                objLog.WriteFile("Extract File Name: " + eZip.ToString());
                            }
                            catch (Exception ex)  //added catch region to extract file when .tmp and .PendingOverwritefile exists 
                            {
                                objLog.WriteFile("Excp in extracting file:- " + ex.Message);
                                foreach (var postFix in new[] { ".tmp", ".PendingOverwrite" })
                                {
                                    var errorPath = Path.Combine("C:\\RMS\\", eZip.FileName) + postFix;
                                    if (File.Exists(errorPath))
                                    {
                                        try
                                        {
                                            File.Delete(errorPath);
                                            objLog.WriteFile("Deleted File Name: " + errorPath.ToString());
                                        }
                                        catch (Exception Excp)
                                        {
                                            objLog.WriteFile("Exception in deleting .tmp files :-" + Excp.Message);
                                            throw (Excp);
                                        }
                                    }
                                }

                                objLog.WriteFile("extracting after deleting .tmp file");
                                eZip.Extract("C:\\RMS\\", ExtractExistingFileAction.OverwriteSilently);
                                objLog.WriteFile("Extract File Name: " + ex.Message);
                            }
                        }
                    }

                    objLog.WriteFile("AVUpdateClient changed successfully");
                }
                else
                {
                    objLog.WriteFile("path not found, path is : " + strAVClientPath);
                }

                Thread.Sleep(1000);

                //Start updated monitor application again
                Process.Start("C:\\RMS\\AVUpdateClient.exe");
                objLog.WriteFile("AVUpdateClient application started");

                bResult = true;
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception:" + ex.Message + Environment.NewLine + ex.StackTrace);

                bool IsRunning = false;
                foreach (var P in Process.GetProcesses())
                {
                    if (P.ProcessName.Contains("AVUpdateClient"))
                    {
                        IsRunning = true;
                        break;
                    }
                }

                if (IsRunning == false)
                {
                    Process.Start("C:\\RMS\\AVUpdateClient.exe");
                    objLog.WriteFile("AVUpdateClient application started");
                }
            }

            return bResult;
        }

        public string GetMACAddress()
        {
            try
            {
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                string sMacAddress = string.Empty;
                foreach (NetworkInterface adapter in nics)
                {
                    if (sMacAddress == string.Empty)// only return MAC Address from first card
                    {
                        sMacAddress = adapter.GetPhysicalAddress().ToString();
                    }
                }
                return sMacAddress;
            }
            catch (Exception ex)
            {
                objLog.WriteFile("Exception in GetMACAddress(): " + ex.Message + Environment.NewLine + ex.StackTrace);
                return "";
            }
        }

        private void GetLogicalDrives()
        {
            string[] LogicalDrives = Environment.GetLogicalDrives();
            if (LogicalDrives.Contains("D:\\"))
            {
                DownloadPath = "D:\\DownloadPath\\";
               // DownTimeRead = "D:\\RMS_TXN\\";
                try
                {
                    if (!Directory.Exists(DownloadPath))
                    {
                        Directory.CreateDirectory(DownloadPath);

                        if (!Directory.Exists(DownloadPath + "LOG"))
                            Directory.CreateDirectory(DownloadPath + "LOG");
                       // Directory.CreateDirectory("D:\\RMS_TXN");
                       
                        objLog.WriteFile("DownloadPath:" + DownloadPath);
                    }
                    else
                    {
                        objLog.WriteFile("DownloadPath:" + DownloadPath);
                        //if (!Directory.Exists("D:\\RMS_TXN"))
                        //    Directory.CreateDirectory("D:\\RMS_TXN");
                        if (!Directory.Exists(DownloadPath + "LOG"))
                            Directory.CreateDirectory(DownloadPath + "LOG");
                    }
                }
                catch
                {
                    objLog.WriteFile("DownloadPath:" + DownloadPath + "Directory not created");
                }
            }
            else if (LogicalDrives.Contains("E:\\"))
            {
                DownloadPath = "E:\\DownloadPath\\";
                //DownTimeRead = "E:\\RMS_TXN\\";

                try
                {
                    if (!Directory.Exists(DownloadPath))
                    {
                        Directory.CreateDirectory(DownloadPath + "LOG");
                     //   Directory.CreateDirectory("E:\\RMS_TXN");
                        Directory.CreateDirectory(DownloadPath);
                        objLog.WriteFile("DownloadPath:" + DownloadPath);
                    }
                    else
                    {
                        objLog.WriteFile("DownloadPath:" + DownloadPath);
                     //   if (!Directory.Exists("E:\\RMS_TXN"))
                    //        Directory.CreateDirectory("E:\\RMS_TXN");
                        if (!Directory.Exists(DownloadPath + "LOG"))
                            Directory.CreateDirectory(DownloadPath + "LOG");
                    }
                }
                catch
                {
                    objLog.WriteFile("DownloadPath:" + DownloadPath + "Directory not created");
                }
            }
            else if (LogicalDrives.Contains("F:\\"))
            {
                DownloadPath = "F:\\DownloadPath\\";
                //     DownTimeRead = "F:\\RMS_TXN\\";

                try
                {
                    if (!Directory.Exists(DownloadPath))
                    {
                        Directory.CreateDirectory(DownloadPath + "LOG");
                      //  Directory.CreateDirectory("F:\\RMS_TXN");
                        Directory.CreateDirectory(DownloadPath);
                        objLog.WriteFile("DownloadPath:" + DownloadPath);
                    }
                    else
                    {
                        objLog.WriteFile("DownloadPath:" + DownloadPath);
                        //if (!Directory.Exists("F:\\RMS_TXN"))
                       //     Directory.CreateDirectory("F:\\RMS_TXN");
                        if (!Directory.Exists(DownloadPath + "LOG"))
                            Directory.CreateDirectory(DownloadPath + "LOG");
                    }
                }
                catch
                {
                    objLog.WriteFile("DownloadPath:" + DownloadPath + "Directory not created");
                }
            }
            else
            {
                DownloadPath = "C:\\DownloadPath\\";
              //  DownTimeRead = "C:\\RMS_TXN\\";

                try
                {
                    if (!Directory.Exists(DownloadPath))
                    {
                        Directory.CreateDirectory(DownloadPath + "LOG");
                     //   Directory.CreateDirectory("C:\\RMS_TXN");
                        Directory.CreateDirectory(DownloadPath);
                        objLog.WriteFile("DownloadPath:" + DownloadPath);
                    }
                    else
                    {
                        objLog.WriteFile("DownloadPath:" + DownloadPath);
                        //if (!Directory.Exists("C:\\RMS_TXN"))
                       //     Directory.CreateDirectory("C:\\RMS_TXN");
                        if (!Directory.Exists(DownloadPath + "LOG"))
                            Directory.CreateDirectory(DownloadPath + "LOG");
                    }
                }
                catch
                {
                    objLog.WriteFile("DownloadPath:" + DownloadPath + "Directory not created");
                }
            }
        }

        public long GetDirSize(DirectoryInfo d)
        {
            try
            {
                long Size = 0;
                // Add file sizes.
                FileInfo[] fis = d.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    Size += fi.Length;
                }
                // Add subdirectory sizes.
                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    Size += GetDirSize(di);
                }

                return (Size / (1024 * 1024));
            }
            catch (Exception ex)
            {
                objLog.WriteFile(" Exception in GetDirSize() " + ex.Message + Environment.NewLine + ex.StackTrace);
                return 0;
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {          
        }
    }

    public class ResLipiHealth
    {
        public bool Result;
        public string PatchName;
        public string PatchData;
        public string Error;
    }

    public class ResDownloadData
    {
        public bool Result;
        public string DownalodData;        
        public string Error;
    }
    public class LogDetails
    {
        public string Logdata;
        public string Filename;
        public string ClientIP;

    }
    public class ResPatchUpdate
    {
        public bool Result;
        public string PatchName;
        public string PatchData;
        public string Error;
    }

    public class ResMonitorPatchUpdate
    {
        public bool Result;
        public string PatchName;
        public string PatchData;
        public string Error;
    }
    public class ResLipiRDServiceUpdate
    {
        public bool Result;
        public string PatchName;
        public string PatchData;
        public string Error;
    }
    public class EncResponse
    {
        public string ResponseData { get; set; }
    }
    public class EncRequest
    {
        public string RequestData { get; set; }
    }
    #endregion
    #region > Helper Classes <
    #region > INI Class <
    class INIFile
    {
        private string filePath;
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern UInt32 GetPrivateProfileSection
            (
                [In] [MarshalAs(UnmanagedType.LPStr)] string strSectionName,
                // Note that because the key/value pars are returned as null-terminated
                // strings with the last string followed by 2 null-characters, we cannot
                // use StringBuilder.
                [In] IntPtr pReturnedString,
                [In] UInt32 nSize,
                [In] [MarshalAs(UnmanagedType.LPStr)] string strFileName
            );

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public INIFile(string filePath)
        {
            this.filePath = filePath;
        }

        public void Write(string section, string key, string value)
        {
            long a = WritePrivateProfileString(section, key, value, this.filePath);
            Thread.Sleep(60);
        }

        public string Read(string section, string key, string def)
        {
            string strReturnVal = "";
            try
            {
                StringBuilder SB = new StringBuilder(255);
                int i = GetPrivateProfileString(section, key, def, SB, 255, this.filePath);
                strReturnVal = SB.ToString();
            }
            catch (Exception)
            {
                strReturnVal = "";
            }
            return strReturnVal;
        }

        public int ReadIntValue(string section, string key, string def)
        {
            int iReturnVal = 0;
            try
            {
                StringBuilder SB = new StringBuilder(255);
                int i = GetPrivateProfileString(section, key, def, SB, 255, this.filePath);
                if (SB.ToString() != "")
                    iReturnVal = Convert.ToInt32(SB.ToString());
            }
            catch (Exception)
            {
                iReturnVal = 0;
            }
            return iReturnVal;
        }

        public bool IniReadDateValue(string Section, string Key, out DateTime objDT, out string strExcp)
        {
            try
            {
                StringBuilder temp = new StringBuilder(25);
                int i = GetPrivateProfileString(Section, Key, "", temp, 255, this.filePath);

                objDT = new DateTime(Convert.ToInt32(temp.ToString().Substring(0, 4)), Convert.ToInt32(temp.ToString().Substring(5, 2)), Convert.ToInt32(temp.ToString().Substring(8, 2)), Convert.ToInt32(temp.ToString().Substring(11, 2)), Convert.ToInt32(temp.ToString().Substring(14, 2)), Convert.ToInt32(temp.ToString().Substring(17, 2)));
                strExcp = "";   //Added [Shubhit 03May13]
                return true;
            }
            catch (Exception excp)
            {
                objDT = DateTime.Now;
                strExcp = excp.Message.ToString();  //Added [Shubhit 03May13]
                return false;
            }
        }

        public double IniReadDoubleValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, this.filePath);
            double dRes;
            Double.TryParse(temp.ToString(), out dRes);
            return dRes;
        }

        public string[] GetAllKeysInIniFileSection(string strSectionName)
        {
            string[] strArray = null;
            try
            {

                // Allocate in unmanaged memory a buffer of suitable size.
                // I have specified here the max size of 32767 as documentated 
                // in MSDN.
                IntPtr pBuffer = Marshal.AllocHGlobal(32767);
                // Start with an array of 1 string only. 
                // Will embellish as we go along.

                strArray = new string[0];
                UInt32 uiNumCharCopied = 0;

                uiNumCharCopied = GetPrivateProfileSection(strSectionName, pBuffer, 32767, this.filePath);

                // iStartAddress will point to the first character of the buffer,
                int iStartAddress = pBuffer.ToInt32();
                // iEndAddress will point to the last null char in the buffer.
                int iEndAddress = iStartAddress + (int)uiNumCharCopied;

                // Navigate through pBuffer.
                while (iStartAddress < iEndAddress)
                {
                    // Determine the current size of the array.
                    int iArrayCurrentSize = strArray.Length;
                    // Increment the size of the string array by 1.
                    Array.Resize<string>(ref strArray, iArrayCurrentSize + 1);
                    // Get the current string which starts at "iStartAddress".
                    string strCurrent = Marshal.PtrToStringAnsi(new IntPtr(iStartAddress));
                    // Insert "strCurrent" into the string array.
                    strArray[iArrayCurrentSize] = strCurrent;
                    // Make "iStartAddress" point to the next string.
                    iStartAddress += (strCurrent.Length + 1);
                }

                Marshal.FreeHGlobal(pBuffer);
                pBuffer = IntPtr.Zero;
                for (int i = 0; i < strArray.Length; i++)
                {
                    strArray[i] = strArray[i].Substring(strArray[i].LastIndexOf('=') + 1).ToLower();
                }
            }
            catch (Exception ) { }//EventLog.WriteEntry("LipiWhiteListing", "Exception Message: " + ex.Message, //EventLogEntryType.Error); }

            return strArray;
        }
        public string FilePath
        {
            get { return this.filePath; }
            set { this.filePath = value; }
        }
    }
#endregion
#region > Thread Safe Helper Class <
    class ThreadSafe
    {
#region > Thread Safe Delegates <
        delegate void Delegate_String(Control TargetControl, string Text);
        delegate void Delegate_Boolean(Control TargetControl, bool Boolean, ControlBooleanType CType);
        delegate string Delegate_GetString(Control TargetControl, bool GetBoolean);
#endregion
#region > Declaration Area <
#region > Static Vartiables <
        static Delegate_String Ptr_String = new Delegate_String(SetString);
        static Delegate_Boolean Ptr_Boolean = new Delegate_Boolean(SetBoolean);
        static Delegate_GetString Ptr_GetString = new Delegate_GetString(GetString);
#endregion
#region > Public Enums <
        public enum ControlBooleanType { Control_Enabled, TextBox_ReadOnly };
#endregion
#endregion
#region > Public Functions <
        public static void SetString(Control TargetControl, string String)
        {
            try
            {
                if (TargetControl.InvokeRequired)
                {
                    TargetControl.BeginInvoke(Ptr_String, new object[] { TargetControl, String });
                    return;
                }
                TargetControl.Text = String;
            }
            catch { }
        }
        public static void SetBoolean(Control TargetControl, bool Boolean, ControlBooleanType CType)
        {
            try
            {
                if (TargetControl.InvokeRequired)
                {
                    TargetControl.BeginInvoke(Ptr_Boolean, new object[] { TargetControl, Boolean, CType });
                    return;
                }
                switch ((int)CType)
                {
                    case (0):   // Enabled
                        TargetControl.Enabled = Boolean;
                        break;
                    case (1):   // Read Only
                        (TargetControl as TextBox).ReadOnly = Boolean;
                        break;
                }
            }
            catch { }
        }
        public static string GetString(Control TargetControl, bool GetBoolean)
        {
            if (TargetControl.InvokeRequired)
            {
                TargetControl.BeginInvoke(Ptr_GetString, new object[] { TargetControl, GetBoolean });
                //return ("");
            }
            if (GetBoolean) return ((TargetControl as RadioButton).Checked.ToString());
            else return ((TargetControl as TextBox).Text);
        }
#endregion
    }
#endregion
#endregion
}
#endregion