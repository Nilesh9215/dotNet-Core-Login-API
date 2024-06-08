using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;


namespace LoginApi.Helper
{
    public static class CryptoHelper
    {
        private static readonly string EncryptionKey;

        static CryptoHelper()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            EncryptionKey = configuration["EncryptionKey"];
        }

        public static string EncryptString(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                aes.GenerateIV(); // Generate a new IV for each encryption
                byte[] iv = aes.IV;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    memoryStream.Write(iv, 0, iv.Length); // Prepend the IV
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        public static string DecryptString(string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                byte[] iv = new byte[16];
                memoryStream.Read(iv, 0, iv.Length); // Extract the IV
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                    aes.IV = iv;
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}    
