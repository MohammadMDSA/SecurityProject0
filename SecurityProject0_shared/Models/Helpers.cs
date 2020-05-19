using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SecurityProject0_shared.Models
{
    public static class Helper
    {
        private static string Chars => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static Random random = new Random();
        public static string SocketMessageAttributeSeperator => "@@@@@@@@@@";
        public static string SocketMessageSplitter => "||||||||||";
        public static string AESChunkSeperator => "%%%%%%%%%%";
        public static string SessionKeySeperator => "$$$$$$$$$$";
        public static string MacSeperator => "##########";
        public static string RandomString64 => new string(Enumerable.Repeat(Chars, 64)
              .Select(s => s[random.Next(s.Length)]).ToArray());

        public static IEnumerable<string> Split(this string value, int desiredLength)
        {
            var characters = StringInfo.GetTextElementEnumerator(value);
            while (characters.MoveNext())
                yield return String.Concat(Take(characters, desiredLength));
        }

        private static IEnumerable<string> Take(TextElementEnumerator enumerator, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                yield return (string)enumerator.Current;

                if (!enumerator.MoveNext())
                    yield break;
            }
        }


        public static string RSADecrypt(string input, RSAParameters param)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(param);
                var byteConverter = new UnicodeEncoding();
                var dwKeySize = rsa.KeySize;
                int base64BlockSize = ((dwKeySize / 8) % 3 != 0) ? (((dwKeySize / 8) / 3) * 4) + 4 : ((dwKeySize / 8) / 3) * 4;
                int iterations = input.Length / base64BlockSize;
                var buffer = new StringBuilder();
                for (int i = 0; i < iterations; i++)
                {
                    byte[] encryptedBytes = Convert.FromBase64String(
                         input.Substring(base64BlockSize * i, base64BlockSize));
                    Array.Reverse(encryptedBytes);
                    buffer.Append(byteConverter.GetString(rsa.Decrypt(encryptedBytes, false)));
                }
                return buffer.ToString();
            }
        }

        public static string RSAEncrypt(string input, RSAParameters param)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(param);
                var byteConverter = new UnicodeEncoding();
                var bytes = byteConverter.GetBytes(input);
                int maxLength = (128 - 44) / 2;
                int iterations = bytes.Length / maxLength;
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i <= iterations; i++)
                {
                    byte[] tempBytes = new byte[
                            (bytes.Length - maxLength * i > maxLength) ? maxLength :
                                          bytes.Length - maxLength * i];
                    Buffer.BlockCopy(bytes, maxLength * i, tempBytes, 0,
                                      tempBytes.Length);
                    byte[] encryptedBytes = rsa.Encrypt(tempBytes, false);
                    Array.Reverse(encryptedBytes);
                    stringBuilder.Append(Convert.ToBase64String(encryptedBytes));
                }
                return stringBuilder.ToString(); ;
            }
        }
        public static string RSASign(string input, RSAParameters param)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(param);
                var byteConverter = new UnicodeEncoding();
                var bytes = byteConverter.GetBytes(input);
                int maxLength = (128 - 44) / 2;
                int iterations = bytes.Length / maxLength;
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i <= iterations; i++)
                {
                    byte[] tempBytes = new byte[
                            (bytes.Length - maxLength * i > maxLength) ? maxLength :
                                          bytes.Length - maxLength * i];
                    Buffer.BlockCopy(bytes, maxLength * i, tempBytes, 0,
                                      tempBytes.Length);
                    byte[] encryptedBytes = rsa.SignData(tempBytes, new SHA256CryptoServiceProvider());
                    Array.Reverse(encryptedBytes);
                    stringBuilder.Append(Convert.ToBase64String(encryptedBytes));
                }
                return stringBuilder.ToString(); ;
            }
        }

        public static bool RSAVerify(string hash, string data, RSAParameters param)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(param);
                var byteConverter = new UnicodeEncoding();
                var bytes = byteConverter.GetBytes(data);
                var dwKeySize = rsa.KeySize;
                int base64BlockSize = ((dwKeySize / 8) % 3 != 0) ? (((dwKeySize / 8) / 3) * 4) + 4 : ((dwKeySize / 8) / 3) * 4;
                int iterations = hash.Length / base64BlockSize;
                int maxLength = (128 - 44) / 2;
                for (int i = 0; i < iterations; i++)
                {
                    byte[] tempBytes = new byte[
                               (bytes.Length - maxLength * i > maxLength) ? maxLength :
                                             bytes.Length - maxLength * i];
                    Buffer.BlockCopy(bytes, maxLength * i, tempBytes, 0,
                                      tempBytes.Length);
                    byte[] encryptedBytes = Convert.FromBase64String(
                         hash.Substring(base64BlockSize * i, base64BlockSize));
                    Array.Reverse(encryptedBytes);
                    if (!rsa.VerifyData(tempBytes, new SHA256CryptoServiceProvider(), encryptedBytes))
                        return false;
                }
                return true;
            }
        }

        public static string AESDecrypt(string input, AESKey key)
        {
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = key.Key;
                aesAlg.IV = key.IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(input)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        public static string AESEncrypt(string input, AESKey key)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.IV = key.IV;
                aes.Key = key.Key;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(input);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }

            }
        }

        public static SessionKey GenerateSessionKey()
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                return new SessionKey
                {
                    ExpirationDate = DateTime.Now + TimeSpan.FromMinutes(1),
                    AESKey = new AESKey
                    {
                        IV = aes.IV,
                        Key = aes.Key
                    }
                };
            }
        }

        public static string Sha256Hash(string input)
        {
            using (var sha = new SHA256CryptoServiceProvider())
            {
                return Convert.ToBase64String(sha.ComputeHash(Encoding.Unicode.GetBytes(input)));
            }
        }
    }
}
