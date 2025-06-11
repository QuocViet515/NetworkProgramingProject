using System;
using System.IO;

namespace Pingme.Helpers
{
    internal class KeyManager
    {
        private const string KeyDir = "keys";

        // Loại bỏ ký tự bất hợp lệ và đường dẫn sai
        private static string SanitizeUserId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("❌ userId không được null hoặc rỗng.");

            if (userId.StartsWith("<RSAKeyValue"))
                throw new ArgumentException("🚫 Đã truyền nhầm publicKey XML thay vì userId!");

            string original = userId;

            // Nếu là đường dẫn → lấy tên file
            if (userId.Contains("\\") || userId.EndsWith(".xml"))
            {
                userId = Path.GetFileNameWithoutExtension(userId);
                Console.WriteLine($"⚠️ Đã phát hiện sai: truyền đường dẫn thay vì userId. Đã sửa thành: {userId}");
            }

            // Nếu userId đang là kiểu "userA_private" hoặc "userB_public"
            if (userId.EndsWith("_private"))
                userId = userId.Substring(0, userId.Length - "_private".Length);
            if (userId.EndsWith("_public"))
                userId = userId.Substring(0, userId.Length - "_public".Length);

            if (userId != original)
                Console.WriteLine($"🧼 userId sau khi xử lý: {userId}");

            return userId;
        }

        public static string GetPrivateKeyPath(string userId)
        {
            string path = Path.Combine(KeyDir, $"{SanitizeUserId(userId)}_private.xml");
            Console.WriteLine("📂 Private Key Path: " + path);
            return path;
        }

        public static string GetPublicKeyPath(string userId)
        {
            string path = Path.Combine(KeyDir, $"{SanitizeUserId(userId)}_public.xml");
            Console.WriteLine("📂 Public Key Path: " + path);
            return path;
        }

        public static bool HasPrivateKey(string userId) =>
            File.Exists(GetPrivateKeyPath(userId));

        public static bool HasPublicKey(string userId) =>
            File.Exists(GetPublicKeyPath(userId));

        public static void EnsureKeyFolder()
        {
            if (!Directory.Exists(KeyDir))
                Directory.CreateDirectory(KeyDir);
        }

        public static void SaveKeyFiles(string userId, string pubPath, string privPath)
        {
            File.Copy(pubPath, GetPublicKeyPath(userId), overwrite: true);
            File.Copy(privPath, GetPrivateKeyPath(userId), overwrite: true);
        }

        public static string LoadPrivateKeyContent(string userId) =>
            File.ReadAllText(GetPrivateKeyPath(userId));

        public static string LoadPublicKeyContent(string userId) =>
            File.ReadAllText(GetPublicKeyPath(userId));
    }
}
