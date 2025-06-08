using Firebase.Database;
using Firebase.Database.Query;
using Pingme.Helpers;
using Pingme.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pingme.Services
{
    public static class AuthService
    {
        public static User CurrentUser { get; private set; }

        private static readonly FirebaseClient client = new FirebaseClient("https://fir-36ac0-default-rtdb.firebaseio.com/");
        private static readonly RSAService rsaService = new RSAService();

        public static async Task<bool> LoginAsync(string username, string password)
        {
            var users = await client.Child("users").OnceAsync<User>();

            var user = users
                .Select(u => u.Object)
                .FirstOrDefault(u => u.userName == username && u.password == password);

            if (user == null)
                return false;

            CurrentUser = user;

            // 🔐 Nếu chưa có public key, tạo và cập nhật
            if (string.IsNullOrWhiteSpace(user.PublicKey))
                await EnsureKeyPairAsync(user.id);

            return true;
        }

        public static async Task<bool> RegisterAsync(string username, string password)
        {
            string userId = Guid.NewGuid().ToString();

            var user = new User
            {
                id = userId,
                userName = username,
                password = password
            };

            await client.Child("users").Child(userId).PutAsync(user);

            CurrentUser = user;

            await EnsureKeyPairAsync(userId);

            return true;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        // 📌 Tự động tạo public/private key nếu chưa có
        private static async Task EnsureKeyPairAsync(string userId)
        {
            if (KeyManager.HasPrivateKey(userId))
                return;

            KeyManager.EnsureKeyFolder();

            string tempPub = Path.GetTempFileName();
            string tempPriv = Path.GetTempFileName();

            bool success = rsaService.GenerateKeys(tempPub, tempPriv);
            if (!success)
                throw new Exception("RSA key generation failed.");

            KeyManager.SaveKeyFiles(userId, tempPub, tempPriv);

            string publicKeyPem = File.ReadAllText(tempPub);

            await client
                .Child("users")
                .Child(userId)
                .PatchAsync(new { publicKey = publicKeyPem });

            File.Delete(tempPub);
            File.Delete(tempPriv);
        }
    }
}
