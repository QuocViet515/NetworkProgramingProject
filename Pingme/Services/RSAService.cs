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

        public string Encrypt(string plaintext, string pubPemContent, string padding = "oaep", string format = "base64", bool hybrid = false)
        {
            string tempInput = Path.GetTempFileName();
            string tempOutput = Path.GetTempFileName();
            string tempPubKey = Path.GetTempFileName();

            File.WriteAllText(tempInput, plaintext, Encoding.UTF8);

            // ✅ Ghi đúng PEM định dạng bằng ASCII và đảm bảo newline chuẩn
            File.WriteAllText(tempPubKey, NormalizePem(pubPemContent), Encoding.ASCII);

            if (!EncryptFile(tempPubKey, tempInput, tempOutput, padding, format, hybrid))
                throw new Exception("Encryption failed");

            string result = File.ReadAllText(tempOutput);
            File.Delete(tempInput);
            File.Delete(tempOutput);
            File.Delete(tempPubKey);
            return result;
        }

        // 👇 Đảm bảo PEM có newline chuẩn sau mỗi 64 ký tự nếu Firebase trả về chuỗi dính
        private string NormalizePem(string pem)
        {
            if (string.IsNullOrWhiteSpace(pem))
                throw new ArgumentNullException(nameof(pem), "🔴 Public key PEM is null or empty");

            if (pem.Contains("-----BEGIN PUBLIC KEY-----") && pem.Contains("-----END PUBLIC KEY-----"))
                return pem;

            // Nếu chỉ là base64 dính → wrap lại thành PEM
            var sb = new StringBuilder();
            sb.AppendLine("-----BEGIN PUBLIC KEY-----");
            for (int i = 0; i < pem.Length; i += 64)
                sb.AppendLine(pem.Substring(i, Math.Min(64, pem.Length - i)));
            sb.AppendLine("-----END PUBLIC KEY-----");
            return sb.ToString();
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
