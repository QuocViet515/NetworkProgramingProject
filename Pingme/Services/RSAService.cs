using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace Pingme.Services
{
    class RSAService
    {
        private const string DllName = "RSA_DLL.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GenerateKeyPair_C(string pub_file, string priv_file, int bits, bool pem_format);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RSA_Encrypt_C(string pub_file, string input_file, string output_file, string padding, string format, bool hybrid);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool RSA_Decrypt_C(string priv_file, string input_file, string output_file, string padding, string format, bool hybrid);

        public bool GenerateKeys(string pubPath, string privPath, int keySize = 2048, bool pem = true)
        {
            return GenerateKeyPair_C(pubPath, privPath, keySize, pem);
        }

        public bool EncryptFile(string pubPath, string plainFile, string encryptedFile, string padding = "oaep", string format = "binary", bool hybrid = false)
        {
            return RSA_Encrypt_C(pubPath, plainFile, encryptedFile, padding, format, hybrid);
        }

        public bool DecryptFile(string privPath, string encryptedFile, string outputFile, string padding = "oaep", string format = "binary", bool hybrid = false)
        {
            return RSA_Decrypt_C(privPath, encryptedFile, outputFile, padding, format, hybrid);
        }

        public string Encrypt(string plaintext, string pubPath, string padding = "oaep", string format = "base64", bool hybrid = false)
        {
            string tempInput = Path.GetTempFileName();
            string tempOutput = Path.GetTempFileName();
            File.WriteAllText(tempInput, plaintext, Encoding.UTF8);

            if (!EncryptFile(pubPath, tempInput, tempOutput, padding, format, hybrid))
                throw new Exception("Encryption failed");

            string result = File.ReadAllText(tempOutput);
            File.Delete(tempInput);
            File.Delete(tempOutput);
            return result;
        }

        public string Decrypt(string encryptedText, string privPath, string padding = "oaep", string format = "base64", bool hybrid = false)
        {
            string tempInput = Path.GetTempFileName();
            string tempOutput = Path.GetTempFileName();
            File.WriteAllText(tempInput, encryptedText, Encoding.UTF8);

            if (!DecryptFile(privPath, tempInput, tempOutput, padding, format, hybrid))
                throw new Exception("Decryption failed");

            string result = File.ReadAllText(tempOutput);
            File.Delete(tempInput);
            File.Delete(tempOutput);
            return result;
        }
    }
}
