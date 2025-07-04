using System;
using System.IO;

namespace Pingme.Helpers
{
    internal class KeyManager
    {
        private const string KeyDir = "keys";

        private static string SanitizeUserId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("❌ userId không được null hoặc rỗng.");

            string original = userId;

            if (userId.Contains("\\") || userId.EndsWith(".pem"))
            {
                userId = Path.GetFileNameWithoutExtension(userId);
                Console.WriteLine($"⚠️ Đã phát hiện sai: truyền đường dẫn thay vì userId. Đã sửa thành: {userId}");
            }

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
            string path = Path.Combine(KeyDir, $"{SanitizeUserId(userId)}_private.pem");
            Console.WriteLine("📂 Private Key PEM Path: " + path);
            return path;
        }

        public static string GetPublicKeyPath(string userId)
        {
            string path = Path.Combine(KeyDir, $"{SanitizeUserId(userId)}_public.pem");
            Console.WriteLine("📂 Public Key PEM Path: " + path);
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
