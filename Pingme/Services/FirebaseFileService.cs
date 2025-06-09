using Firebase.Database;
using Firebase.Database.Query;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Services
{
    internal class FirebaseFileService
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly AESService _aesService;
        private readonly RSAService _rsaService;
        private readonly StorageClient _storageClient;
        private readonly string _bucketName = "fir-36ac0.firebasestorage.app";

        public FirebaseFileService()
        {
            _firebaseClient = new FirebaseClient("https://fir-36ac0-default-rtdb.firebaseio.com/");
            _aesService = new AESService();
            _rsaService = new RSAService();

            var credential = GoogleCredential.FromFile("service-account.json");
            _storageClient = StorageClient.Create(credential);
        }

        public async Task UploadEncryptedFileAsync(
            string filePath,
            string receiverPublicKeyXml,
            string senderId,
            string receiverId)
        {
            // Tạo AES key và IV
            string aesKeyString = _aesService.GenerateAesKey();
            byte[] aesKey = Convert.FromBase64String(aesKeyString);
            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(iv);

            // Mã hóa file vào temp
            string tempEncryptedPath = Path.GetTempFileName();
            using (var input = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var output = new FileStream(tempEncryptedPath, FileMode.Create, FileAccess.Write))
            {
                _aesService.EncryptFileWithStreams(input, output, aesKey, iv);
            }

            // Mã hóa AES key + IV
            string keyIVBase64 = Convert.ToBase64String(aesKey) + ":" + Convert.ToBase64String(iv);
            string encryptedKeyIV = _rsaService.EncryptWithXml(keyIVBase64, receiverPublicKeyXml);

            // Upload file
            string fileId = Guid.NewGuid().ToString();
            string storagePath = $"encrypted_files/{fileId}.enc";
            using (var stream = File.OpenRead(tempEncryptedPath))
            {
                await _storageClient.UploadObjectAsync(_bucketName, storagePath, null, stream);
            }

            // Lưu metadata vào Firebase
            var metadata = new
            {
                fileName = Path.GetFileName(filePath),
                storagePath = storagePath,
                encryptedAESKeyIV = encryptedKeyIV,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                senderId = senderId,
                receiverId = receiverId
            };

            await _firebaseClient
                .Child("file_metadata")
                .Child(fileId)
                .PutAsync(metadata);

            File.Delete(tempEncryptedPath);
        }

        public async Task DownloadAndDecryptFileAsync(
            string fileId, string receiverPrivateKeyPath, string saveDirectory)
        {
            var metadata = await _firebaseClient
                .Child("file_metadata")
                .Child(fileId)
                .OnceSingleAsync<dynamic>();

            string storagePath = metadata.storagePath;
            string encryptedAESKeyIV = metadata.encryptedAESKeyIV;
            string fileName = metadata.fileName;

            // Giải mã khóa
            string privateKeyXml = File.ReadAllText(receiverPrivateKeyPath);
            string decryptedKeyIV = _rsaService.Decrypt(encryptedAESKeyIV, privateKeyXml);
            var parts = decryptedKeyIV.Split(':');
            if (parts.Length != 2) throw new Exception("Sai định dạng key/IV");

            byte[] aesKey = Convert.FromBase64String(parts[0]);
            byte[] aesIV = Convert.FromBase64String(parts[1]);

            // Tải file tạm
            string tempPath = Path.GetTempFileName();
            using (var outStream = File.OpenWrite(tempPath))
            {
                await _storageClient.DownloadObjectAsync(_bucketName, storagePath, outStream);
            }

            // Giải mã
            string outputPath = Path.Combine(saveDirectory, fileName);
            using (var input = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
            using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                _aesService.DecryptFileWithStreams(input, output, aesKey, aesIV);
            }

            File.Delete(tempPath);
        }
    }
}
