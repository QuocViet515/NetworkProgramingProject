using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Services
{
    class AESService
    {
        private const string DLLpath = "aes_key.dll";

        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
        private static extern void GenerateAESKeyIV(byte[] key, byte[] iv);

        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SaveKeyToFile(string filename, byte[] key, byte[] iv);

        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
        private static extern void LoadKeyFromFile(string filename, byte[] key, byte[] iv);

        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AESEncrypt(byte[] key, byte[] iv, string plainFilePath, string cipherFilePath, int outSize);

        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AESDecrypt(byte[] key, byte[] iv, string cipherFilePath, string plainFilePath, int outSize);
        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AESEncryptString(string plaintext,byte[] key, byte[] iv,[Out] byte[] outBuffer,int bufferSize);

        [DllImport(DLLpath, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AESDecryptString( string ciphertext, byte[] key, byte[] iv, [Out] byte[] outBuffer,int bufferSize);

        // --- Dữ liệu key và iv ---
        private byte[] _key;
        private byte[] _iv;

        public AESService()
        {
            _key = new byte[16]; // AES::DEFAULT_KEYLENGTH
            _iv = new byte[16];  // AES::BLOCKSIZE
        }

        public (byte[] key, byte[] iv) GenerateKeyAndIV()
        {
            GenerateAESKeyIV(_key, _iv);
            return (_key, _iv);
        }
        public string GenerateAesKey()
        {
            byte[] key = new byte[16];
            GenerateAESKeyIV(key, new byte[16]); // chỉ cần key, không cần iv ở đây
            return Convert.ToBase64String(key);
        }

        public void SaveKeyToFile(string filePath)
        {
            if (_key == null || _iv == null)
                throw new InvalidOperationException("Key/IV chưa được khởi tạo.");
            SaveKeyToFile(filePath, _key, _iv);
        }

        public void LoadKeyFromFile(string filePath)
        {
            _key = new byte[16];
            _iv = new byte[16];
            LoadKeyFromFile(filePath, _key, _iv);
        }

        public void EncryptFile(string inputFilePath, string encryptedFilePath)
        {
            if (_key == null || _iv == null)
                throw new InvalidOperationException("Key/IV chưa được khởi tạo.");
            AESEncrypt(_key, _iv, inputFilePath, encryptedFilePath, 0);
        }

        public void DecryptFile(string encryptedFilePath, string outputFilePath)
        {
            if (_key == null || _iv == null)
                throw new InvalidOperationException("Key/IV chưa được khởi tạo.");
            AESDecrypt(_key, _iv, encryptedFilePath, outputFilePath, 0);
        }
        public string EncryptMessage(string plainText, string base64Key)
        {
            byte[] key = Convert.FromBase64String(base64Key);
            byte[] iv = new byte[16];
            GenerateAESKeyIV(new byte[16], iv); // chỉ cần iv

            byte[] outBuffer = new byte[4096];
            AESEncryptString(plainText, key, iv, outBuffer, outBuffer.Length);

            string cipherBase64 = Convert.ToBase64String(outBuffer.TakeWhile(b => b != 0).ToArray());
            string ivBase64 = Convert.ToBase64String(iv);

            return $"{ivBase64}:{cipherBase64}";
        }
        public string DecryptMessage(string encryptedText, string base64Key)
        {
            var parts = encryptedText.Split(':');
            if (parts.Length != 2)
                throw new FormatException("Định dạng tin nhắn sai");

            byte[] iv = Convert.FromBase64String(parts[0]);
            byte[] cipherBytes = Convert.FromBase64String(parts[1]);
            string cipherText = Encoding.UTF8.GetString(cipherBytes);

            byte[] key = Convert.FromBase64String(base64Key);
            byte[] outBuffer = new byte[4096];

            AESDecryptString(cipherText, key, iv, outBuffer, outBuffer.Length);
            return Encoding.UTF8.GetString(outBuffer.TakeWhile(b => b != 0).ToArray());
        }
        public void LoadKeyFromBytes(byte[] key, byte[] iv)
        {
            if (key.Length != 16 || iv.Length != 16)
                throw new ArgumentException("Key hoặc IV không hợp lệ.");
            _key = key;
            _iv = iv;
        }

        public byte[] GetKey() => _key;
        public byte[] GetIV() => _iv;
    }
}
