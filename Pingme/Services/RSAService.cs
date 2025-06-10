using System;
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Security.Cryptography;
//using System.Text;

//namespace Pingme.Services
//{
//    class RSAService
//    {
//        private const string DllName = "RSA_DLL.dll";

//        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
//        private static extern bool GenerateKeyPair_C(string pub_file, string priv_file, int bits, bool pem_format);

//        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
//        private static extern bool RSA_Encrypt_C(string pub_file, string input_file, string output_file, string padding, string format, bool hybrid);

//        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
//        private static extern bool RSA_Decrypt_C(string priv_file, string input_file, string output_file, string padding, string format, bool hybrid);

//        public bool GenerateKeys(string pubPath, string privPath, int keySize = 2048, bool pem = true)
//        {
//            return GenerateKeyPair_C(pubPath, privPath, keySize, pem);
//        }

//        public bool EncryptFile(string pubPath, string plainFile, string encryptedFile, string padding = "oaep", string format = "binary", bool hybrid = false)
//        {
//            return RSA_Encrypt_C(pubPath, plainFile, encryptedFile, padding, format, hybrid);
//        }

//        public bool DecryptFile(string privPath, string encryptedFile, string outputFile, string padding = "oaep", string format = "binary", bool hybrid = false)
//        {
//            return RSA_Decrypt_C(privPath, encryptedFile, outputFile, padding, format, hybrid);
//        }

//        public string Encrypt(string plaintext, string pubPemContent, string padding = "oaep", string format = "base64", bool hybrid = false)
//        {
//            string tempInput = Path.GetTempFileName();
//            string tempOutput = Path.GetTempFileName();
//            string tempPubKey = Path.GetTempFileName();

//            try
//            {
//                File.WriteAllText(tempInput, plaintext, Encoding.UTF8);
//                File.WriteAllText(tempPubKey, NormalizePem(pubPemContent), Encoding.UTF8);

//                if (!EncryptFile(tempPubKey, tempInput, tempOutput, padding, format, hybrid))
//                    throw new Exception("Encryption failed");

//                return File.ReadAllText(tempOutput);
//            }
//            finally
//            {
//                TryDelete(tempInput);
//                TryDelete(tempOutput);
//                TryDelete(tempPubKey);
//            }
//        }

//        public string Decrypt(string encryptedText, string privPath, string padding = "oaep", string format = "base64", bool hybrid = false)
//        {
//            string tempInput = Path.GetTempFileName();
//            string tempOutput = Path.GetTempFileName();

//            try
//            {
//                File.WriteAllText(tempInput, encryptedText, Encoding.UTF8);

//                if (!DecryptFile(privPath, tempInput, tempOutput, padding, format, hybrid))
//                    throw new Exception("Decryption failed");

//                return File.ReadAllText(tempOutput);
//            }
//            finally
//            {
//                TryDelete(tempInput);
//                TryDelete(tempOutput);
//            }
//        }

//        private void TryDelete(string path)
//        {
//            try { if (File.Exists(path)) File.Delete(path); } catch { }
//        }

//        private string NormalizePem(string pem)
//        {
//            if (string.IsNullOrWhiteSpace(pem))
//                throw new ArgumentNullException(nameof(pem), "🔴 Public key PEM is null or empty");

//            if (pem.Contains("-----BEGIN PUBLIC KEY-----") && pem.Contains("-----END PUBLIC KEY-----"))
//                return pem;

//            var sb = new StringBuilder();
//            sb.AppendLine("-----BEGIN PUBLIC KEY-----");
//            for (int i = 0; i < pem.Length; i += 64)
//                sb.AppendLine(pem.Substring(i, Math.Min(64, pem.Length - i)));
//            sb.AppendLine("-----END PUBLIC KEY-----");
//            return sb.ToString();
//        }
//    }
//}


using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Pingme.Helpers;

namespace Pingme.Services
{
    public class RSAService
    {
        // Tạo và lưu khóa RSA cho userId
        public bool GenerateKeysForUser(string userId, int keySize = 4096)
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider(keySize))
                {
                    string publicKeyXml = rsa.ToXmlString(false);
                    string privateKeyXml = rsa.ToXmlString(true);

                    KeyManager.EnsureKeyFolder();

                    File.WriteAllText(KeyManager.GetPublicKeyPath(userId), publicKeyXml);
                    File.WriteAllText(KeyManager.GetPrivateKeyPath(userId), privateKeyXml);

                    Console.WriteLine($"✅ Đã tạo khóa cho user: {userId}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi tạo khóa cho {userId}: {ex.Message}");
                return false;
            }
        }

        // Kiểm tra và tạo khóa nếu chưa có
        public void EnsureUserKeyExists(string userId)
        {
            if (!KeyManager.HasPrivateKey(userId) || !KeyManager.HasPublicKey(userId))
            {
                Console.WriteLine($"🔧 Chưa có khóa cho {userId}, tạo mới...");
                GenerateKeysForUser(userId);
            }
            else
            {
                Console.WriteLine($"🟢 Khóa cho {userId} đã tồn tại.");
            }
        }

        // ======== MÃ HÓA / GIẢI MÃ CHUỖI =========
        public string EncryptWithUserId(string plainText, string userId)
        {
            string pubXml = KeyManager.LoadPublicKeyContent(userId);
            return EncryptWithXml(plainText, pubXml);
        }
        public string EncryptWithXml(string plainText, string publicKeyXml)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKeyXml);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = rsa.Encrypt(data, true);
                return Convert.ToBase64String(encrypted);
            }
        }


        public string Encrypt(string plainText, string userId)
        {
            string pubXml = KeyManager.LoadPublicKeyContent(userId);

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(pubXml);
                byte[] data = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = rsa.Encrypt(data, true);
                return Convert.ToBase64String(encrypted);
            }
        }

        public string Decrypt(string encryptedText, string userId)
        {
            try
            {
                string privPath = KeyManager.GetPrivateKeyPath(userId);
                Console.WriteLine($"🔍 Giải mã với khóa tại: {privPath}");

                if (!File.Exists(privPath))
                    return "[Lỗi giải mã] (Không tìm thấy khóa)";

                string privXml = File.ReadAllText(privPath);
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(privXml);

                    byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                    byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, true); // true = OAEP
                    string aesKey = Encoding.UTF8.GetString(decryptedBytes);

                    Console.WriteLine($"✅ AES Key đã giải mã: {aesKey}");
                    return aesKey;
                }
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"❌ CryptographicException: {ex.Message} (có thể sai padding hoặc key không khớp)");
                return "[Lỗi giải mã] (Sai padding hoặc khóa không đúng)";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                return "[Lỗi giải mã]";
            }
        }
        public string DecryptWithXml(string encryptedText, string privateKeyXml)
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(privateKeyXml);
                    byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                    byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, true); // OAEP
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"❌ RSA Decrypt CryptographicException: {ex.Message}");
                return "[Lỗi giải mã]";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ RSA Decrypt Exception: {ex.Message}");
                return "[Lỗi giải mã]";
            }
        }




        // ======== MÃ HÓA / GIẢI MÃ FILE =========

        public bool EncryptFile(string plainFile, string encryptedFile, string userId)
        {
            try
            {
                string plainText = File.ReadAllText(plainFile, Encoding.UTF8);
                string encrypted = Encrypt(plainText, userId);
                File.WriteAllText(encryptedFile, encrypted, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ EncryptFile error ({userId}): {ex.Message}");
                return false;
            }
        }

        public bool DecryptFile(string encryptedFile, string outputFile, string userId)
        {
            try
            {
                string encryptedText = File.ReadAllText(encryptedFile, Encoding.UTF8);
                string decrypted = Decrypt(encryptedText, userId);
                File.WriteAllText(outputFile, decrypted, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DecryptFile error ({userId}): {ex.Message}");
                return false;
            }
        }
    }
}
