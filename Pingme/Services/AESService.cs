using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Pingme.Services
{
    public class AESService
    {
        public string GenerateAesKey()
        {
            byte[] key = new byte[32]; // 256 bit
            new SecureRandom().NextBytes(key);
            return Convert.ToBase64String(key);
        }

        public string ComputeSHA256(string text)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(text);
                byte[] hash = sha.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }

        public (string EncryptedText, string IV, string Tag, string Hash) EncryptMessageWithHash(string plainText, string base64Key)
        {
            byte[] key = Convert.FromBase64String(base64Key);
            byte[] iv = new byte[12]; // 96-bit IV
            new SecureRandom().NextBytes(iv);

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            var gcm = new GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), 128, iv);
            gcm.Init(true, parameters);

            byte[] output = new byte[gcm.GetOutputSize(plainBytes.Length)];
            int len = gcm.ProcessBytes(plainBytes, 0, plainBytes.Length, output, 0);
            gcm.DoFinal(output, len);

            // Tách cipher + tag
            byte[] tag = new byte[16];
            byte[] cipherBytes = new byte[output.Length - 16];

            Array.Copy(output, output.Length - 16, tag, 0, 16);
            Array.Copy(output, 0, cipherBytes, 0, cipherBytes.Length);

            string hash = ComputeSHA256(plainText);

            return (
                Convert.ToBase64String(cipherBytes),
                Convert.ToBase64String(iv),
                Convert.ToBase64String(tag),
                hash
            );
        }

        public (string PlainText, bool IsValid) DecryptMessageWithHashCheck(
            string cipherBase64, 
            string base64Key, 
            string ivBase64, 
            string tagBase64, 
            string expectedHash)
        {
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherBase64);
                byte[] key = Convert.FromBase64String(base64Key);
                byte[] iv = Convert.FromBase64String(ivBase64);
                byte[] tag = Convert.FromBase64String(tagBase64);

                byte[] combined = new byte[cipherBytes.Length + tag.Length];
                Array.Copy(cipherBytes, 0, combined, 0, cipherBytes.Length);
                Array.Copy(tag, 0, combined, cipherBytes.Length, tag.Length);

                var gcm = new GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
                var parameters = new AeadParameters(new KeyParameter(key), 128, iv);
                gcm.Init(false, parameters);

                byte[] plainBytes = new byte[gcm.GetOutputSize(combined.Length)];
                int len = gcm.ProcessBytes(combined, 0, combined.Length, plainBytes, 0);
                gcm.DoFinal(plainBytes, len);

                string plainText = Encoding.UTF8.GetString(plainBytes).TrimEnd('\0');
                string recomputedHash = ComputeSHA256(plainText);
                bool isValid = expectedHash == recomputedHash;

                return (plainText, isValid);
            }
            catch (Exception ex)
            {
                return ($"[Không thể giải mã] ({ex.Message})", false);
            }
        }

        //public void EncryptFile(string inputPath, string outputPath, string base64Key)
        //{
        //    byte[] key = Convert.FromBase64String(base64Key);
        //    byte[] iv = new byte[12];
        //    new SecureRandom().NextBytes(iv);

        //    byte[] plainBytes = File.ReadAllBytes(inputPath);

        //    var gcm = new GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
        //    var parameters = new AeadParameters(new KeyParameter(key), 128, iv);
        //    gcm.Init(true, parameters);

        //    byte[] output = new byte[gcm.GetOutputSize(plainBytes.Length)];
        //    int len = gcm.ProcessBytes(plainBytes, 0, plainBytes.Length, output, 0);
        //    gcm.DoFinal(output, len);

        //    using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        //    {
        //        fs.Write(iv, 0, iv.Length);
        //        fs.Write(output, 0, output.Length);
        //    }
        //}
        public (byte[] cipherBytes, byte[] iv, byte[] tag) EncryptFile(byte[] plainBytes, byte[] key)
        {
            byte[] iv = new byte[12];
            new SecureRandom().NextBytes(iv);

            var gcm = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), 128, iv);
            gcm.Init(true, parameters);

            byte[] output = new byte[gcm.GetOutputSize(plainBytes.Length)];
            int len = gcm.ProcessBytes(plainBytes, 0, plainBytes.Length, output, 0);
            gcm.DoFinal(output, len);

            byte[] tag = new byte[16];
            byte[] cipherBytes = new byte[output.Length - 16];
            Array.Copy(output, 0, cipherBytes, 0, cipherBytes.Length);
            Array.Copy(output, cipherBytes.Length, tag, 0, tag.Length);

            return (cipherBytes, iv, tag);
        }

        //public void DecryptFile(string inputPath, string outputPath, string base64Key)
        //{
        //    byte[] key = Convert.FromBase64String(base64Key);
        //    byte[] fileData = File.ReadAllBytes(inputPath);

        //    byte[] iv = new byte[12];
        //    Array.Copy(fileData, 0, iv, 0, iv.Length);

        //    byte[] cipherAndTag = new byte[fileData.Length - iv.Length];
        //    Array.Copy(fileData, iv.Length, cipherAndTag, 0, cipherAndTag.Length);

        //    var gcm = new GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
        //    var parameters = new AeadParameters(new KeyParameter(key), 128, iv);
        //    gcm.Init(false, parameters);

        //    byte[] plainBytes = new byte[gcm.GetOutputSize(cipherAndTag.Length)];
        //    int len = gcm.ProcessBytes(cipherAndTag, 0, cipherAndTag.Length, plainBytes, 0);
        //    gcm.DoFinal(plainBytes, len);

        //    File.WriteAllBytes(outputPath, plainBytes);
        //}
        public byte[] DecryptFile(byte[] cipherBytes, byte[] key, byte[] iv, byte[] tag)
        {
            byte[] combined = new byte[cipherBytes.Length + tag.Length];
            Array.Copy(cipherBytes, 0, combined, 0, cipherBytes.Length);
            Array.Copy(tag, 0, combined, cipherBytes.Length, tag.Length);

            var gcm = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), 128, iv);
            gcm.Init(false, parameters);

            byte[] plainBytes = new byte[gcm.GetOutputSize(combined.Length)];
            int len = gcm.ProcessBytes(combined, 0, combined.Length, plainBytes, 0);
            gcm.DoFinal(plainBytes, len);

            return plainBytes;
        }

        //public void EncryptFileWithStreams(Stream input, Stream output, byte[] key, byte[] iv)
        //{
        //    byte[] plainBytes = ReadStreamFully(input);

        //    var gcm = new GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
        //    var parameters = new AeadParameters(new KeyParameter(key), 128, iv);
        //    gcm.Init(true, parameters);

        //    byte[] outputBytes = new byte[gcm.GetOutputSize(plainBytes.Length)];
        //    int len = gcm.ProcessBytes(plainBytes, 0, plainBytes.Length, outputBytes, 0);
        //    gcm.DoFinal(outputBytes, len);

        //    output.Write(iv, 0, iv.Length);
        //    output.Write(outputBytes, 0, outputBytes.Length);
        //    output.Flush();
        //}
        public void EncryptFileWithStreams(Stream input, Stream output, byte[] key, byte[] iv, out byte[] tag)
        {
            byte[] plaintext = ReadStreamFully(input);

            var gcm = new GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), 128, iv);
            gcm.Init(true, parameters);

            byte[] outputBytes = new byte[gcm.GetOutputSize(plaintext.Length)];
            int len = gcm.ProcessBytes(plaintext, 0, plaintext.Length, outputBytes, 0);
            gcm.DoFinal(outputBytes, len);

            tag = new byte[16];
            Array.Copy(outputBytes, outputBytes.Length - 16, tag, 0, 16);

            byte[] ciphertext = new byte[outputBytes.Length - 16];
            Array.Copy(outputBytes, 0, ciphertext, 0, ciphertext.Length);

            output.Write(iv, 0, iv.Length);           // 12 bytes
            output.Write(tag, 0, tag.Length);         // 16 bytes
            output.Write(ciphertext, 0, ciphertext.Length);
        }

        //public void DecryptFileWithStreams(Stream input, Stream output, byte[] key)
        //{
        //    byte[] iv = new byte[12];
        //    input.Read(iv, 0, iv.Length);

        //    byte[] cipherAndTag = ReadStreamFully(input);

        //    var gcm = new GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
        //    var parameters = new AeadParameters(new KeyParameter(key), 128, iv);
        //    gcm.Init(false, parameters);

        //    byte[] plainBytes = new byte[gcm.GetOutputSize(cipherAndTag.Length)];
        //    int len = gcm.ProcessBytes(cipherAndTag, 0, cipherAndTag.Length, plainBytes, 0);
        //    gcm.DoFinal(plainBytes, len);

        //    output.Write(plainBytes, 0, plainBytes.Length);
        //    output.Flush();
        //}
        public void DecryptFileWithStreams(Stream input, Stream output, byte[] key, byte[] iv, byte[] tag)
        {
            byte[] header = new byte[iv.Length + tag.Length];
            input.Seek(0, SeekOrigin.Begin); // Đảm bảo stream bắt đầu từ đầu
            input.Read(header, 0, header.Length);

            byte[] ciphertext = new byte[input.Length - header.Length];
            input.Read(ciphertext, 0, ciphertext.Length);

            byte[] combined = new byte[ciphertext.Length + tag.Length];
            Array.Copy(ciphertext, 0, combined, 0, ciphertext.Length);
            Array.Copy(tag, 0, combined, ciphertext.Length, tag.Length);

            var gcm = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), 128, iv);
            gcm.Init(false, parameters);

            byte[] plainBytes = new byte[gcm.GetOutputSize(combined.Length)];
            int len = gcm.ProcessBytes(combined, 0, combined.Length, plainBytes, 0);
            gcm.DoFinal(plainBytes, len);

            output.Write(plainBytes, 0, plainBytes.Length);
        }

        private byte[] ReadStreamFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
