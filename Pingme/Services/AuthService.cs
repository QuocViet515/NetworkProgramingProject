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

            // 🔐 Nếu chưa có key cục bộ, tạo và đẩy public key nếu cần
            await EnsureKeyPairAsync(user.id, user.PublicKey);

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

            await EnsureKeyPairAsync(userId, null);

            return true;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        // 📌 Tạo public/private key nếu chưa có
        private static async Task EnsureKeyPairAsync(string userId, string publicKeyFromFirebase)
        {
            if (!KeyManager.HasPrivateKey(userId))
            {
                Console.WriteLine($"🔧 Tạo key mới cho {userId}...");
                bool ok = rsaService.GenerateKeysForUser(userId);

                if (!ok)
                    throw new Exception("RSA key generation failed.");
            }
            else
            {
                Console.WriteLine($"🟢 Private key đã tồn tại cho {userId}.");
            }

            // 🔄 Nếu Firebase chưa có publicKey → đẩy lên
            if (string.IsNullOrWhiteSpace(publicKeyFromFirebase))
            {
                string pubXml = KeyManager.LoadPublicKeyContent(userId);

                await client
                    .Child("users")
                    .Child(userId)
                    .PatchAsync(new { publicKey = pubXml });

                Console.WriteLine($"⬆️ Đã upload publicKey cho {userId} lên Firebase.");
            }
        }
    }
}
