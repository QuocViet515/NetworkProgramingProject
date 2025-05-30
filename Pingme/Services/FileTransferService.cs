using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
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
            var (aesKey, aesIV) = _aesService.GenerateKeyAndIV();
            string tempEncryptedPath = Path.GetTempFileName();
            _aesService.EncryptFile(filePath, tempEncryptedPath);

            string keyIVBase64 = Convert.ToBase64String(aesKey) + ":" + Convert.ToBase64String(aesIV);
            string encryptedKeyIV = _rsaService.Encrypt(keyIVBase64, receiverPublicKeyPath);

            TcpClient client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(receiverIp), port);

            var netStream = client.GetStream();
            var fileStream = new FileStream(tempEncryptedPath, FileMode.Open, FileAccess.Read);

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

            File.Delete(tempEncryptedPath);
        }

        public async Task ReceiveEncryptedFileAsync(int port, string saveDirectory, string receiverPrivateKeyPath)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            TcpClient client = await listener.AcceptTcpClientAsync();
            NetworkStream netStream = client.GetStream();
            StreamReader reader = new StreamReader(netStream, Encoding.UTF8, true, 1024, true);


            string header = await reader.ReadLineAsync();
            var parts = header.Split('|');
            string fileName = parts[0];
            long encryptedFileSize = long.Parse(parts[1]);
            int encryptedKeyIVSize = int.Parse(parts[2]);

            byte[] keyBuffer = new byte[encryptedKeyIVSize];
            await netStream.ReadAsync(keyBuffer, 0, encryptedKeyIVSize);
            string encryptedKeyIV = Encoding.UTF8.GetString(keyBuffer);
            string decryptedKeyIV = _rsaService.Decrypt(encryptedKeyIV, receiverPrivateKeyPath);

            var keyParts = decryptedKeyIV.Split(':');
            byte[] aesKey = Convert.FromBase64String(keyParts[0]);
            byte[] aesIV = Convert.FromBase64String(keyParts[1]);

            string tempEncryptedPath = Path.GetTempFileName();
            FileStream encryptedFs = new FileStream(tempEncryptedPath, FileMode.Create, FileAccess.Write);

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

            string savePath = Path.Combine(saveDirectory, fileName);
            _aesService.LoadKeyFromBytes(aesKey, aesIV);
            _aesService.DecryptFile(tempEncryptedPath, savePath);
            File.Delete(tempEncryptedPath);
        }
    }
}
