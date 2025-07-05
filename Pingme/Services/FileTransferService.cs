using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Services
{
    public class FileTransferService
    {
        private const int BufferSize = 8192;
        private readonly AESService _aesService;
        private readonly RSAService _rsaService;

        public FileTransferService()
        {
            _aesService = new AESService();
            _rsaService = new RSAService();
        }

        public async Task SendEncryptedFileAsync(string filePath, string receiverIp, int port, string receiverPublicKeyPath)
        {
            string aesKeyString = _aesService.GenerateAesKey();
            byte[] aesKey = Convert.FromBase64String(aesKeyString);
            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(iv);

            string tempEncryptedPath = Path.GetTempFileName();
            EncryptFileWithIv(filePath, tempEncryptedPath, aesKey, iv);

            string keyIVBase64 = Convert.ToBase64String(aesKey) + ":" + Convert.ToBase64String(iv);
            string encryptedKeyIV = _rsaService.EncryptWithXml(keyIVBase64, File.ReadAllText(receiverPublicKeyPath));

            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Parse(receiverIp), port);
                using (NetworkStream netStream = client.GetStream())
                using (FileStream fileStream = new FileStream(tempEncryptedPath, FileMode.Open, FileAccess.Read))
                {
                    string fileName = Path.GetFileName(filePath);
                    string header = $"{fileName}|{fileStream.Length}|{encryptedKeyIV.Length}\n";
                    byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                    await netStream.WriteAsync(headerBytes, 0, headerBytes.Length);

                    byte[] keyBytes = Encoding.UTF8.GetBytes(encryptedKeyIV);
                    await netStream.WriteAsync(keyBytes, 0, keyBytes.Length);

                    byte[] buffer = new byte[BufferSize];
                    int bytesRead;
                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await netStream.WriteAsync(buffer, 0, bytesRead);
                    }
                }
            }

            File.Delete(tempEncryptedPath);
        }

        public async Task ReceiveEncryptedFileAsync(int port, string saveDirectory, string receiverPrivateKeyPath)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            using (TcpClient client = await listener.AcceptTcpClientAsync())
            using (NetworkStream netStream = client.GetStream())
            using (StreamReader reader = new StreamReader(netStream, Encoding.UTF8, true, 1024, true))
            {
                string header = await reader.ReadLineAsync();
                var parts = header.Split('|');
                if (parts.Length != 3) throw new InvalidOperationException("Header không hợp lệ");

                string fileName = parts[0];
                long encryptedFileSize = long.Parse(parts[1]);
                int encryptedKeyIVSize = int.Parse(parts[2]);

                byte[] keyBuffer = new byte[encryptedKeyIVSize];
                int keyBytesRead = 0;
                while (keyBytesRead < encryptedKeyIVSize)
                {
                    int read = await netStream.ReadAsync(keyBuffer, keyBytesRead, encryptedKeyIVSize - keyBytesRead);
                    if (read == 0) throw new EndOfStreamException("Không thể đọc đủ RSA key.");
                    keyBytesRead += read;
                }

                string encryptedKeyIV = Encoding.UTF8.GetString(keyBuffer);
                string decryptedKeyIV = _rsaService.Decrypt(encryptedKeyIV, receiverPrivateKeyPath);
                var keyParts = decryptedKeyIV.Split(':');
                if (keyParts.Length != 2) throw new InvalidOperationException("Key/IV bị lỗi định dạng");

                byte[] aesKey = Convert.FromBase64String(keyParts[0]);
                byte[] aesIV = Convert.FromBase64String(keyParts[1]);

                string tempEncryptedPath = Path.GetTempFileName();
                using (FileStream encryptedFs = new FileStream(tempEncryptedPath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[BufferSize];
                    long totalRead = 0;
                    while (totalRead < encryptedFileSize)
                    {
                        int toRead = (int)Math.Min(BufferSize, encryptedFileSize - totalRead);
                        int read = await netStream.ReadAsync(buffer, 0, toRead);
                        if (read == 0) break;
                        await encryptedFs.WriteAsync(buffer, 0, read);
                        totalRead += read;
                    }
                }

                string savePath = Path.Combine(saveDirectory, fileName);
                DecryptFileWithIv(tempEncryptedPath, savePath, aesKey);
                File.Delete(tempEncryptedPath);
            }

            listener.Stop();
        }

        private void EncryptFileWithIv(string inputFile, string outputFile, byte[] key, byte[] iv)
        {
            byte[] plainBytes = File.ReadAllBytes(inputFile);

            var gcm = new Org.BouncyCastle.Crypto.Modes.GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
            var parameters = new Org.BouncyCastle.Crypto.Parameters.AeadParameters(new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key), 128, iv);

            gcm.Init(true, parameters);

            byte[] cipherOutput = new byte[gcm.GetOutputSize(plainBytes.Length)];
            int len = gcm.ProcessBytes(plainBytes, 0, plainBytes.Length, cipherOutput, 0);
            gcm.DoFinal(cipherOutput, len);

            using (var outFs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                outFs.Write(iv, 0, iv.Length);            // Ghi IV đầu file
                outFs.Write(cipherOutput, 0, cipherOutput.Length);  // Ghi Cipher + Tag
            }
        }

        private void DecryptFileWithIv(string inputFile, string outputFile, byte[] key)
        {
            byte[] fileData = File.ReadAllBytes(inputFile);

            byte[] iv = new byte[12];  // 96-bit IV GCM
            Array.Copy(fileData, 0, iv, 0, iv.Length);

            byte[] cipherAndTag = new byte[fileData.Length - iv.Length];
            Array.Copy(fileData, iv.Length, cipherAndTag, 0, cipherAndTag.Length);

            var gcm = new Org.BouncyCastle.Crypto.Modes.GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
            var parameters = new Org.BouncyCastle.Crypto.Parameters.AeadParameters(new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key), 128, iv);

            gcm.Init(false, parameters);

            byte[] plainBytes = new byte[gcm.GetOutputSize(cipherAndTag.Length)];
            int len = gcm.ProcessBytes(cipherAndTag, 0, cipherAndTag.Length, plainBytes, 0);
            gcm.DoFinal(plainBytes, len);

            File.WriteAllBytes(outputFile, plainBytes);
        }

    }
}
