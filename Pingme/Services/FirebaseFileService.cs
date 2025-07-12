using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Pingme.Helpers;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
namespace Pingme.Services
{
    internal class FirebaseFileService
    {
        private readonly ChatService _chatService = new ChatService();
        private readonly FirebaseService _firebaseService = new FirebaseService();

        private readonly FirebaseClient _firebaseClient;
        private readonly AESService _aesService;
        private readonly RSAService _rsaService;
        private readonly StorageClient _storageClient;
        private readonly string _bucketName = "pingmeapp-1691-1703-1784.firebasestorage.app";

        public FirebaseFileService()
        {
            _aesService = new AESService();
            _rsaService = new RSAService();

            //var credential = GoogleCredential.FromFile("service-account.json");
            //_storageClient = StorageClient.Create(credential);
            try
            {
                var credential = GoogleCredential.FromFile("service-account.json");

                Console.WriteLine("[⚙️] Đang tạo StorageClient...");
                _storageClient = StorageClient.Create(credential);
                Console.WriteLine("[✅] StorageClient đã tạo thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[❌ FirebaseFileService] Lỗi khi tạo StorageClient: {ex.Message}");
                Console.WriteLine($"[Chi tiết lỗi] {ex.StackTrace}");
                throw; // Phải throw để không tạo đối tượng sai
            }

            // 🔧 Thêm dòng này
            SessionManager.EnsureValidTokenAsync().Wait(); // Hoặc dùng async nếu bạn refactor được constructor

            _firebaseClient = new FirebaseClient(
                "https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(SessionManager.IdToken)
                });
        }

        //public async Task UploadEncryptedFileAsync(
        //    string filePath,
        //    string filePath,
        //    string receiverPublicKeyXml,
        //    string senderId,
        //    string receiverId)
        public async Task<Message> UploadEncryptedFileAsync(
            string filePath, 
            string receiverPublicKeyXml, 
            string senderId, 
            string receiverId,
            string chatId)
        {
            // Tạo AES key và IV
            string aesKeyString = _aesService.GenerateAesKey();
            byte[] aesKey = Convert.FromBase64String(aesKeyString);
            //byte[] iv = new byte[16];
            byte[] iv = new byte[12];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(iv);

            // Mã hóa file vào temp
            string tempEncryptedPath = Path.GetTempFileName();
            byte[] tag;
            using (var input = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var output = new FileStream(tempEncryptedPath, FileMode.Create, FileAccess.Write))
            {
                //_aesService.EncryptFileWithStreams(input, output, aesKey, iv);
                _aesService.EncryptFileWithStreams(input, output, aesKey, iv, out tag);
            }

            // Mã hóa AES key + IV
            //string keyIVBase64 = Convert.ToBase64String(aesKey) + ":" + Convert.ToBase64String(iv);
            //string keyIVBase64 = Convert.ToBase64String(aesKey) + ":" + Convert.ToBase64String(iv) + ":" + Convert.ToBase64String(tag);
            //string encryptedKeyIV = _rsaService.EncryptWithXml(keyIVBase64, receiverPublicKeyXml);
            //string encryptedAESKey = _rsaService.EncryptWithXml(Convert.ToBase64String(aesKey), receiverPublicKeyXml);

            string encryptedIV = _rsaService.EncryptWithXml(Convert.ToBase64String(iv), receiverPublicKeyXml);
            string encryptedTag = _rsaService.EncryptWithXml(Convert.ToBase64String(tag), receiverPublicKeyXml);

            // Upload file
            string fileId = Guid.NewGuid().ToString();
            string storagePath = $"encrypted_files/{fileId}.enc";
            using (var stream = System.IO.File.OpenRead(tempEncryptedPath))
            {
                await _storageClient.UploadObjectAsync(_bucketName, storagePath, null, stream);
            }

            byte[] encryptedBytes = System.IO.File.ReadAllBytes(tempEncryptedPath);
            string hash = ComputeSHA256(encryptedBytes); // ✅ Tính hash của file mã hóa
            // Lưu metadata vào Firebase
            //var metadata = new
            //{
            //    fileName = Path.GetFileName(filePath),
            //    storagePath = storagePath,
            //    encryptedAESKeyIV = encryptedKeyIV,
            //    hash = hash,
            //    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            //    senderId = senderId,
            //    receiverId = receiverId
            //};

            var sender = await _firebaseService.GetUserByIdAsync(senderId);
            string encryptedIVForSender = _rsaService.EncryptWithXml(Convert.ToBase64String(iv), sender.PublicKey);
            string encryptedTagForSender = _rsaService.EncryptWithXml(Convert.ToBase64String(tag), sender.PublicKey);

            var receiver = await _firebaseService.GetUserByIdAsync(receiverId);
            string base64AesKey = Convert.ToBase64String(aesKey);
            string encryptedKeyForSender = _rsaService.EncryptWithXml(base64AesKey, sender.PublicKey);
            string encryptedKeyForReceiver = _rsaService.EncryptWithXml(base64AesKey, receiver.PublicKey);

            var metadata = new FileMetadata
            {
                fileName = Path.GetFileName(filePath),
                storagePath = storagePath,
                //encryptedAESKey = encryptedAESKey.Trim(), // nguoi nhan dung nay
                encryptedAESKey = encryptedKeyForReceiver.Trim(),           // dành cho người nhận
                encryptedAESKeyForSender = encryptedKeyForSender.Trim(),   // thêm dòng này cho nguoi gui
                encryptedIV = encryptedIV.Trim(),
                encryptedTag = encryptedTag.Trim(),
                encryptedIVForSender = encryptedIVForSender.Trim(),
                encryptedTagForSender = encryptedTagForSender.Trim(),
                hash = hash,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                senderId = senderId,
                receiverId = receiverId
            };


            // ✅ Sau khi lưu metadata xong...
            //var message = new Message
            //{
            //    SenderId = senderId,
            //    ReceiverId = receiverId,
            //    Type = "file",
            //    FileId = fileId,  // Tách riêng, không dùng Content
            //    FileName = Path.GetFileName(filePath),
            //    SentAt = DateTime.UtcNow,
            //    IsRead = false,
            //    SessionKeyEncrypted = new Dictionary<string, string>
            //    {
            //        { senderId, encryptedKeyForSender },
            //        { receiverId, encryptedKeyForReceiver }
            //    }
            //};

            //string chatRoomId = FirebaseService.GetChatRoomId(senderId, receiverId);
            // lưu tại messages/chat_{chatRoomId}/{msgId}
            string chatRoomId = $"chat_{chatId}";

            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),                        // ✅ Gán Id
                ChatId = chatRoomId,                                   // ✅ Gán ChatId
                SenderId = senderId,
                ReceiverId = receiverId,
                Type = "file",
                FileId = fileId,
                FileName = Path.GetFileName(filePath),
                SentAt = DateTime.UtcNow,
                IsRead = false,
                SessionKeyEncrypted = new Dictionary<string, string>
                {
                    { senderId, encryptedKeyForSender },
                    { receiverId, encryptedKeyForReceiver }
                }
            };


            //string chatRoomId = FirebaseService.GetChatRoomId(senderId, receiverId);
            //await _firebaseClient
            //    .Child("messages")
            //    .Child(message.Id)
            //    .PutAsync(message);
            //var data = new Dictionary<string, object>
            //{
            //    { "SenderId", message.SenderId },
            //    { "ReceiverId", message.ReceiverId },
            //    { "FileId", message.FileId },
            //    { "FileName", message.FileName },
            //    { "Type", message.Type },
            //    { "SentAt", message.SentAt },
            //    { "IsRead", message.IsRead },
            //    { "SessionKeyEncrypted", message.SessionKeyEncrypted }
            //};

            //await _firebaseClient
            //    .Child("messages")
            //    .Child(message.Id)
            //    .PutAsync(data);

            await _firebaseClient
                .Child("messages")
                .Child(chatRoomId)
                .Child(message.Id)
                .PutAsync(message); // sử dụng object Message đầy đủ


            _chatService.OnNewMessageReceived?.Invoke(message);


            await _firebaseClient
                .Child("file_metadata")
                .Child(fileId)
                .PutAsync(metadata);

            System.IO.File.Delete(tempEncryptedPath);

            return message;
        }
        private string ComputeSHA256(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }

        public async Task DownloadAndDecryptFileAsync(
            string fileId, 
            string receiverPrivateKeyPath, 
            string fullOutputPath)
        {
            var metadata = await _firebaseClient
                .Child("file_metadata")
                .Child(fileId)
                .OnceSingleAsync<FileMetadata>();
            
            string currentUserId = SessionManager.UID;

            string storagePath = metadata.storagePath;
            //string encryptedAESKeyIV = metadata.encryptedAESKeyIV;

            string privateKeyXml = System.IO.File.ReadAllText(receiverPrivateKeyPath);
            //string decryptedKeyIV = _rsaService.DecryptWithXml(encryptedAESKeyIV, privateKeyXml);

            //var parts = decryptedKeyIV.Split(':');

            //if (parts.Length != 2)
            //if (parts.Length != 3)
            //    throw new Exception("Sai định dạng key/IV");

            //byte[] aesKey = Convert.FromBase64String(parts[0]);
            //byte[] iv = Convert.FromBase64String(parts[1]);
            //byte[] tag = Convert.FromBase64String(parts[2]);

            string aesKeyBase64;

            //string aesKeyBase64 = _rsaService.DecryptWithXml(metadata.encryptedAESKey, privateKeyXml);

            if (currentUserId == metadata.senderId && !string.IsNullOrEmpty(metadata.encryptedAESKeyForSender))
            {
                aesKeyBase64 = _rsaService.DecryptWithXml(metadata.encryptedAESKeyForSender, privateKeyXml);
            }
            else
            {
                aesKeyBase64 = _rsaService.DecryptWithXml(metadata.encryptedAESKey, privateKeyXml);
            }

            //string ivBase64 = _rsaService.DecryptWithXml(metadata.encryptedIV, privateKeyXml);
            //string tagBase64 = _rsaService.DecryptWithXml(metadata.encryptedTag, privateKeyXml);
            string ivBase64, tagBase64;
            if (currentUserId == metadata.senderId &&
                !string.IsNullOrEmpty(metadata.encryptedIVForSender) &&
                !string.IsNullOrEmpty(metadata.encryptedTagForSender))
            {
                ivBase64 = _rsaService.DecryptWithXml(metadata.encryptedIVForSender, privateKeyXml);
                tagBase64 = _rsaService.DecryptWithXml(metadata.encryptedTagForSender, privateKeyXml);
            }
            else
            {
                ivBase64 = _rsaService.DecryptWithXml(metadata.encryptedIV, privateKeyXml);
                tagBase64 = _rsaService.DecryptWithXml(metadata.encryptedTag, privateKeyXml);
            }

            byte[] aesKey = Convert.FromBase64String(aesKeyBase64);
            byte[] iv = Convert.FromBase64String(ivBase64);
            byte[] tag = Convert.FromBase64String(tagBase64);

            // ✅ Tải file tạm
            string tempPath = Path.GetTempFileName();
            using (var outStream = System.IO.File.OpenWrite(tempPath))
            {
                await _storageClient.DownloadObjectAsync(_bucketName, storagePath, outStream);
            }
            byte[] encryptedBytes = System.IO.File.ReadAllBytes(tempPath);
            string downloadedHash = ComputeSHA256(encryptedBytes);

            if (downloadedHash != metadata.hash)
                throw new Exception("❌ Dữ liệu không toàn vẹn! File có thể đã bị sửa hoặc tải lỗi.");
    
            // ✅ Giải mã và ghi đúng nơi đã chọn
            using (var input = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
            using (var output = new FileStream(fullOutputPath, FileMode.Create, FileAccess.Write))
            {
                //_aesService.DecryptFileWithStreams(input, output, aesKey);
                _aesService.DecryptFileWithStreams(input, output, aesKey, iv, tag);
            }

            System.IO.File.Delete(tempPath);
        }

        //public async Task<string> UploadPlainFileAsync(string filePath)
        //{
        //    var stream = System.IO.File.OpenRead(filePath);
        //    var fileName = Path.GetFileName(filePath);
        //    var storage = StorageClient.Create();
        //    var objectName = $"files/{Guid.NewGuid()}_{fileName}";

        //    using var client = StorageClient.Create();
        //    await client.UploadObjectAsync("your-bucket-name", objectName, null, stream);
        //    return objectName; // hoặc URL nếu muốn
        //}
        public async Task<string> UploadPlainFileAsync(string filePath, string chatId, string senderId)
        //public async Task<string> UploadPlainFileAsync(string filePath)
        {
            var stream = System.IO.File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);
            var objectName = $"files/{Guid.NewGuid()}_{fileName}";

            try
            {
                await _storageClient.UploadObjectAsync(_bucketName, objectName, null, stream);
                var message = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    ChatId = chatId,
                    SenderId = senderId,
                    ReceiverId = null,
                    IsGroup = true,
                    Type = "file",
                    FileId = objectName,
                    FileName = fileName,
                    SentAt = DateTime.UtcNow
                };

                //await _firebaseClient
                //    .Child("messages")
                //    .Child($"group_{chatId}")
                //    .Child(message.Id)
                //    .PutAsync(message);
            }
            finally
            {
                stream.Dispose();
            }

            return objectName;
        }


        //public async Task DownloadPlainFileAsync(string objectName, string destinationPath)
        //{
        //    using var client = StorageClient.Create();
        //    using var output = System.IO.File.Create(destinationPath);
        //    await client.DownloadObjectAsync("your-bucket-name", objectName, output);
        //}
        public async Task DownloadPlainFileAsync(string objectName, string destinationPath)
        {
            var output = System.IO.File.Create(destinationPath);

            try
            {
                await _storageClient.DownloadObjectAsync(_bucketName, objectName, output);
            }
            finally
            {
                output.Dispose();
            }
        }
    }
}
