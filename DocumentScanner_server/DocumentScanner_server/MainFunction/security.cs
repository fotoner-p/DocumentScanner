using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace DocumentScanner_server
{
    class security
    {
        public static byte[] key = Encoding.UTF8.GetBytes("hanjong1");//8개 버퍼값에 딱맞게 설정해야된당

        public static bool EncryptFile(string filepath)
        {
            if (System.IO.File.Exists(filepath + ".txt"))
            {
                byte[] plainContent = File.ReadAllBytes(filepath + ".txt");//파일 경로를 읽어온다
                using (DESCryptoServiceProvider DES = new DESCryptoServiceProvider())//
                {
                    DES.IV = key;
                    DES.Key = key;
                    DES.Mode = CipherMode.CBC;
                    DES.Padding = PaddingMode.PKCS7;

                    using (MemoryStream memStream = new MemoryStream())
                    {
                        CryptoStream CryptoStream = new CryptoStream(memStream, DES.CreateEncryptor(), CryptoStreamMode.Write);
                        CryptoStream.Write(plainContent, 0, plainContent.Length);
                        CryptoStream.FlushFinalBlock();
                        File.WriteAllBytes(filepath + ".bin", memStream.ToArray());
                    }
                }
                return true;
            }
            else
                return false;
        }

        public static bool DecryptFile(string filepath)
        {
            if (System.IO.File.Exists(filepath + ".bin"))
            {
                byte[] encrypted = File.ReadAllBytes(filepath + ".bin");
                using (DESCryptoServiceProvider DES = new DESCryptoServiceProvider())
                {
                    DES.IV = key;
                    DES.Key = key;
                    DES.Mode = CipherMode.CBC;
                    DES.Padding = PaddingMode.PKCS7;

                    using (MemoryStream memStream = new MemoryStream())
                    {
                        CryptoStream CryptoStream = new CryptoStream(memStream, DES.CreateDecryptor(), CryptoStreamMode.Write);
                        CryptoStream.Write(encrypted, 0, encrypted.Length);
                        CryptoStream.FlushFinalBlock();
                        StreamWriter stream = File.CreateText(filepath + ".txt");
                        File.WriteAllBytes(filepath + ".txt", memStream.ToArray());
                    }
                }
                return true;
            }
            else
                return false;
        }
    }
}
