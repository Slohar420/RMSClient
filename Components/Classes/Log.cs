//#define ENCRYPT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Client.Components.Classes;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Client.Components.Classes
{
    public class Log 
    {
        //IniFile objIni;
        // Added for Logging.
        string Log_FolderPath = "C:\\Log_Data\\RMSClient\\";
        string PrevStr = "";

        public void WriteFile(string strData)
        {
            if (!Directory.Exists(Log_FolderPath))
                Directory.CreateDirectory(Log_FolderPath);

            using (StreamWriter sw = new StreamWriter(new FileStream(Log_FolderPath + @"Client_Logs_" + DateTime.Now.ToString("dd_MM_yyyy") + ".txt", FileMode.Append)))
            {
                int Success = String.Compare(PrevStr, strData.ToString());
                if (Success != 0)
                {
                    //sw.WriteLine(SSTCryptographer.Encrypt(DateTime.Now.ToString("HH:mm:ss.fff\t") + strData.ToString()));
                    sw.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff\t") + strData.ToString());
                    sw.Close();
                    PrevStr = "";
                }
                PrevStr = String.Copy(strData.ToString());
            }          
        }
    }
    
    public class SSTCryptographer
    {
        private static string _key;

        public SSTCryptographer()
        {
        }

        public static string Key
        {
            set
            {
                _key = value;
            }
        }
       
        public static string Encrypt(string strToEncrypt)
        {
            try
            {
                return Encrypt(strToEncrypt, "lipi");
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }

        public static string Decrypt(string strEncrypted)
        {
            try
            {
                return Decrypt(strEncrypted, "lipi");
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }
      
        public static string Encrypt(string strToEncrypt, string strKey)
        {
            try
            {
                TripleDESCryptoServiceProvider objDESCrypto = new TripleDESCryptoServiceProvider();
                MD5CryptoServiceProvider objHashMD5 = new MD5CryptoServiceProvider();

                byte[] byteHash, byteBuff;
                string strTempKey = strKey;

                byteHash = objHashMD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(strTempKey));
                objHashMD5 = null;
                objDESCrypto.Key = byteHash;
                objDESCrypto.Mode = CipherMode.ECB; //CBC, CFB

                byteBuff = ASCIIEncoding.ASCII.GetBytes(strToEncrypt);
                return Convert.ToBase64String(objDESCrypto.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }
           
        public static string Decrypt(string strEncrypted, string strKey)
        {
            try
            {
                TripleDESCryptoServiceProvider objDESCrypto = new TripleDESCryptoServiceProvider();
                MD5CryptoServiceProvider objHashMD5 = new MD5CryptoServiceProvider();

                byte[] byteHash, byteBuff;
                string strTempKey = strKey;

                byteHash = objHashMD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(strTempKey));
                objHashMD5 = null;
                objDESCrypto.Key = byteHash;
                objDESCrypto.Mode = CipherMode.ECB; //CBC, CFB

                byteBuff = Convert.FromBase64String(strEncrypted);
                string strDecrypted = ASCIIEncoding.ASCII.GetString(objDESCrypto.CreateDecryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
                objDESCrypto = null;

                return strDecrypted;
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }
    }             
}
