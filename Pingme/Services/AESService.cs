//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace Pingme.Services
//{
//    class AESService
//    {
//        private const string DLLpath = "aes_key.dll";

//        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
//        private static extern void GenerateAESKeyIV(byte[] key, byte[] iv);

//        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
//        private static extern void SaveKeyToFile(string filename, byte[] key, byte[] iv);

//        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
//        private static extern void LoadKeyFromFile(string filename, byte[] key, byte[] iv);

//        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
//        private static extern void AESEncrypt(byte[] key, byte[] iv, string plainFilePath, string cipherFilePath, int outSize);

//        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
//        private static extern void AESDecrypt(byte[] key, byte[] iv, string cipherFilePath, string plainFilePath, int outSize);

//        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
//        private static extern void AESEncryptString(string plaintext, byte[] key, byte[] iv, [Out] byte[] outBuffer, int bufferSize);

//        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
//        private static extern void AESDecryptString(string ciphertext, byte[] key, byte[] iv, [Out] byte[] outBuffer, int bufferSize);

//        private byte[] _key;
//        private byte[] _iv;

//        public AESService()
//        {
//            _key = new byte[16];
//            _iv = new byte[16];
//        }

//        public (byte[] key, byte[] iv) GenerateKeyAndIV()
//        {
//            GenerateAESKeyIV(_key, _iv);
//            return (_key, _iv);
//        }

//        public string GenerateAesKey()
//        {
//            byte[] key = new byte[16];
//            GenerateAESKeyIV(key, new byte[16]); // chỉ lấy key
//            return Convert.ToBase64String(key);
//        }

//        public void SaveKeyToFile(string filePath)
//        {
//            if (_key == null || _iv == null)
//                throw new InvalidOperationException("Key/IV chưa được khởi tạo.");
//            SaveKeyToFile(filePath, _key, _iv);
//        }

//        public void LoadKeyFromFile(string filePath)
//        {
//            _key = new byte[16];
//            _iv = new byte[16];
//            LoadKeyFromFile(filePath, _key, _iv);
//        }

//        public void EncryptFile(string inputFilePath, string encryptedFilePath)
//        {
//            if (_key == null || _iv == null)
//                throw new InvalidOperationException("Key/IV chưa được khởi tạo.");
//            AESEncrypt(_key, _iv, inputFilePath, encryptedFilePath, 0);
//        }

//        public void DecryptFile(string encryptedFilePath, string outputFilePath)
//        {
//            if (_key == null || _iv == null)
//                throw new InvalidOperationException("Key/IV chưa được khởi tạo.");
//            AESDecrypt(_key, _iv, encryptedFilePath, outputFilePath, 0);
//        }

//        public string EncryptMessage(string plainText, string base64Key)
//        {
//            byte[] key = Convert.FromBase64String(base64Key);
//            byte[] iv = new byte[16];
//            GenerateAESKeyIV(new byte[16], iv); // sinh iv

//            byte[] outBuffer = new byte[4096];
//            AESEncryptString(plainText, key, iv, outBuffer, outBuffer.Length);

//            int actualLength = Array.FindLastIndex(outBuffer, b => b != 0) + 1;
//            if (actualLength <= 0)
//                throw new Exception("Encryption buffer is empty. Kiểm tra lại DLL hoặc key/iv.");

//            string cipherBase64 = Convert.ToBase64String(outBuffer, 0, actualLength);
//            string ivBase64 = Convert.ToBase64String(iv);
//            return $"{ivBase64}:{cipherBase64}";
//        }

//        public string DecryptMessage(string encryptedText, string base64Key)
//        {
//            try
//            {
//                var parts = encryptedText.Split(':');
//                if (parts.Length != 2)
//                    return "[Không thể giải mã] (Sai định dạng)";

//                byte[] iv = Convert.FromBase64String(parts[0]);
//                string cipherTextBase64 = parts[1]; // ⚠️ Giữ nguyên chuỗi base64

//                byte[] key = Convert.FromBase64String(base64Key);
//                byte[] outBuffer = new byte[4096];

//                AESDecryptString(cipherTextBase64, key, iv, outBuffer, outBuffer.Length);

//                int actualLength = Array.FindLastIndex(outBuffer, b => b != 0) + 1;
//                if (actualLength <= 0)
//                    return "[Không thể giải mã]";

//                return Encoding.UTF8.GetString(outBuffer, 0, actualLength);
//            }
//            catch (Exception ex)
//            {
//                return $"[Không thể giải mã] ({ex.Message})";
//            }
//        }

//        public void LoadKeyFromBytes(byte[] key, byte[] iv)
//        {
//            if (key.Length != 16 || iv.Length != 16)
//                throw new ArgumentException("Key hoặc IV không hợp lệ.");
//            _key = key;
//            _iv = iv;
//        }

//        public byte[] GetKey() => _key;
//        public byte[] GetIV() => _iv;
//    }
//}
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Pingme.Services
{
    public class AESService 
    {
        public string GenerateAesKey()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                return Convert.ToBase64String(aes.Key);
            }
        }

        // ==== MÃ HÓA / GIẢI MÃ CHUỖI ====
        public string EncryptMessage(string plainText, string base64Key)
        {
            byte[] key = Convert.FromBase64String(base64Key);
            using (var aes = Aes.Create())
            {
                aes.KeySize = 128;
                aes.Key = key;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    string ivBase64 = Convert.ToBase64String(aes.IV);
                    string cipherBase64 = Convert.ToBase64String(cipherBytes);
                    return $"{ivBase64}:{cipherBase64}";
                }
            }
        }

        public string DecryptMessage(string encryptedText, string base64Key)
        {
            try
            {
                string[] parts = encryptedText.Split(':');
                if (parts.Length != 2)
                    return "[Không thể giải mã] (Sai định dạng)";

                byte[] iv = Convert.FromBase64String(parts[0]);
                byte[] cipherBytes = Convert.FromBase64String(parts[1]);
                byte[] key = Convert.FromBase64String(base64Key);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = 128;
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                        return Encoding.UTF8.GetString(plainBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                return $"[Không thể giải mã] ({ex.Message})";
            }
        }

        // ==== MÃ HÓA / GIẢI MÃ FILE ====
        public void EncryptFile(string inputPath, string outputPath, string base64Key)
        {
            byte[] key = Convert.FromBase64String(base64Key);
            using (var aes = Aes.Create())
            {
                aes.KeySize = 128;
                aes.Key = key;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                 var fsOutput = new FileStream(outputPath, FileMode.Create);
                // Ghi IV trước
                fsOutput.Write(aes.IV, 0, aes.IV.Length);

                var cryptoStream = new CryptoStream(fsOutput, aes.CreateEncryptor(), CryptoStreamMode.Write);
                var fsInput = new FileStream(inputPath, FileMode.Open);

                fsInput.CopyTo(cryptoStream);
            }
        }

        public void DecryptFile(string inputPath, string outputPath, string base64Key)
        {
            byte[] key = Convert.FromBase64String(base64Key);

            using (var fsInput = new FileStream(inputPath, FileMode.Open))
            {
                byte[] iv = new byte[16];
                fsInput.Read(iv, 0, iv.Length); // đọc IV đầu file

                var aes = Aes.Create();
                aes.KeySize = 128;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var cryptoStream = new CryptoStream(fsInput, aes.CreateDecryptor(), CryptoStreamMode.Read);
                var fsOutput = new FileStream(outputPath, FileMode.Create);

                cryptoStream.CopyTo(fsOutput);
            }
        }
    }
}
