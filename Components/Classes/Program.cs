
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Diagnostics;     //Add Lokesh[14 July 2012] 
using Microsoft.Win32;


namespace Client
{
    static class Program
    {
        public static Frm_Main Frm_Main = null;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Mutex appSingleInstance = new System.Threading.Mutex(false, "SingleInstanceApp#@145");
            if (appSingleInstance.WaitOne(0, false))
            {
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Frm_Main());
                    appSingleInstance.Close();
                }
                catch (Exception ex)
                {
                    Client.Components.Classes.Log objLog = new Components.Classes.Log();
                    objLog.WriteFile(ex.Message.ToString() + "\r\n" + ex.StackTrace);
                    Application.Exit();
                    //MessageBox.Show(ex.ToString());
                }
            }
        }
    }
}
       

