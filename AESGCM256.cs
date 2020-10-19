﻿using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Text;

public class AesGcm256
{
    private static readonly SecureRandom securerandom = new SecureRandom();

    // Pre-configured Encryption Parameters
    public static readonly int NonceBitSize = 128;
    public static readonly int MacBitSize = 128;
    public static readonly int KeyBitSize = 256;

    private AesGcm256() { }

    public static byte[] NewKey()
    {
        var key = new byte[KeyBitSize / 8];
        securerandom.NextBytes(key);
        return key;
    }

    public static byte[] NewIv()
    {
        var iv = new byte[NonceBitSize / 8];
        securerandom.NextBytes(iv);
        return iv;
    }

    public static Byte[] HexToByte(string hexStr)
    {
        byte[] bArray = new byte[hexStr.Length / 2];
        for (int i = 0; i < (hexStr.Length / 2); i++)
        {
            byte firstNibble = Byte.Parse(hexStr.Substring((2 * i), 1),
                               System.Globalization.NumberStyles.HexNumber); // [x,y)
            byte secondNibble = Byte.Parse(hexStr.Substring((2 * i) + 1, 1),
                                System.Globalization.NumberStyles.HexNumber);
            int finalByte = (secondNibble) | (firstNibble << 4); // bit-operations 
                                                                 // only with numbers, not bytes.
            bArray[i] = (byte)finalByte;
        }
        return bArray;
    }

    public static string toHex(byte[] data)
    {
        string hex = string.Empty;
        foreach (byte c in data)
        {
            hex += c.ToString("X2");
        }
        return hex;
    }

    public static string toHex(string asciiString)
    {
        string hex = string.Empty;
        foreach (char c in asciiString)
        {
            int tmp = c;
            hex += string.Format("{0:x2}", System.Convert.ToUInt32(tmp.ToString()));
        }
        return hex;
    }

    public static string Encrypt(byte[] PlainText)
    {

        string sR = string.Empty;
        try
        {
            byte[] key = HexToByte("2143B39425BBD08B6E8E61C5D1E1BC9F428FC569FBC6F78C0BC48FCCDB0F89AE");
            byte[] iv = HexToByte("E1E592E89225847C19D948684F3B070D");

            GcmBlockCipher cipher = new GcmBlockCipher(new AesFastEngine());
            AeadParameters parameters =
                         new AeadParameters(new KeyParameter(key), 128, iv, null);

            cipher.Init(true, parameters);

            byte[] encryptedBytes = new byte[cipher.GetOutputSize(PlainText.Length)];
            Int32 retLen = cipher.ProcessBytes
                           (PlainText, 0, PlainText.Length, encryptedBytes, 0);
            cipher.DoFinal(encryptedBytes, retLen);
            sR = Convert.ToBase64String(encryptedBytes, Base64FormattingOptions.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        return sR;
    }
    public static string Encrypt(string PlainText)
    {
        string sR = string.Empty;
        try
        {
            byte[] key = HexToByte("2143B39425BBD08B6E8E61C5D1E1BC9F428FC569FBC6F78C0BC48FCCDB0F89AE");
            byte[] iv = HexToByte("E1E592E89225847C19D948684F3B070D");

            byte[] plainBytes = Encoding.UTF8.GetBytes(PlainText);

            GcmBlockCipher cipher = new GcmBlockCipher(new AesFastEngine());
            AeadParameters parameters =
                         new AeadParameters(new KeyParameter(key), 128, iv, null);

            cipher.Init(true, parameters);

            byte[] encryptedBytes = new byte[cipher.GetOutputSize(plainBytes.Length)];
            Int32 retLen = cipher.ProcessBytes
                           (plainBytes, 0, plainBytes.Length, encryptedBytes, 0);
            cipher.DoFinal(encryptedBytes, retLen);
            sR = Convert.ToBase64String(encryptedBytes, Base64FormattingOptions.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        return sR;
    }

    public static string Decrypt(string EncryptedText)
    {
        string sR = string.Empty;
        try
        {
            byte[] key = HexToByte("2143B39425BBD08B6E8E61C5D1E1BC9F428FC569FBC6F78C0BC48FCCDB0F89AE");
            byte[] iv = HexToByte("E1E592E89225847C19D948684F3B070D");

            byte[] encryptedBytes = Convert.FromBase64String(EncryptedText);

            GcmBlockCipher cipher = new GcmBlockCipher(new AesFastEngine());
            AeadParameters parameters =
                      new AeadParameters(new KeyParameter(key), 128, iv, null);
            //ParametersWithIV parameters = new ParametersWithIV(new KeyParameter(key), iv);

            cipher.Init(false, parameters);
            byte[] plainBytes = new byte[cipher.GetOutputSize(encryptedBytes.Length)];
            Int32 retLen = cipher.ProcessBytes
                           (encryptedBytes, 0, encryptedBytes.Length, plainBytes, 0);
            cipher.DoFinal(plainBytes, retLen);

            sR = Encoding.UTF8.GetString(plainBytes).TrimEnd("\r\n\0".ToCharArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        return sR;
    }
}

