using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Services
{
    public class FileTransferService
    {
        private const int BufferSize = 8192;

        public async Task SendFileAsync(string filePath, string receiverIp, int port)
        {
            TcpClient client = null;
            FileStream fileStream = null;
            NetworkStream networkStream = null;

            try
            {
                client = new TcpClient();
                await client.ConnectAsync(IPAddress.Parse(receiverIp), port);

                networkStream = client.GetStream();
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                var fileName = Path.GetFileName(filePath);
                var header = $"{fileName}|{fileStream.Length}\n";
                var headerBytes = Encoding.UTF8.GetBytes(header);
                await networkStream.WriteAsync(headerBytes, 0, headerBytes.Length);

                var buffer = new byte[BufferSize];
                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await networkStream.WriteAsync(buffer, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendFileAsync] Error: {ex.Message}");
                throw;
            }
            finally
            {
                fileStream?.Dispose();
                networkStream?.Dispose();
                client?.Close();
            }
        }

        public async Task ReceiveFileAsync(int port, string saveDirectory)
        {
            TcpListener listener = null;
            TcpClient client = null;
            FileStream fileStream = null;
            NetworkStream networkStream = null;
            StreamReader reader = null;

            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();

                client = await listener.AcceptTcpClientAsync();
                networkStream = client.GetStream();
                reader = new StreamReader(networkStream, Encoding.UTF8, true, 1024, true);

                var header = await reader.ReadLineAsync();
                if (header == null || !header.Contains('|'))
                    throw new InvalidDataException("Invalid header received.");

                var parts = header.Split('|');
                var fileName = parts[0];
                var fileSize = long.Parse(parts[1]);
                var savePath = Path.Combine(saveDirectory, fileName);

                fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                var buffer = new byte[BufferSize];
                long totalReceived = 0;

                while (totalReceived < fileSize)
                {
                    var bytesToRead = (int)Math.Min(BufferSize, fileSize - totalReceived);
                    var bytesRead = await networkStream.ReadAsync(buffer, 0, bytesToRead);
                    if (bytesRead == 0) break;

                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalReceived += bytesRead;
                }

                Console.WriteLine($"[ReceiveFileAsync] File saved to {savePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReceiveFileAsync] Error: {ex.Message}");
                throw;
            }
            finally
            {
                reader?.Dispose();
                fileStream?.Dispose();
                networkStream?.Dispose();
                client?.Close();
                listener?.Stop();
            }
        }
    }
}
