using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Helpers
{
    internal class KeyManager
    {
        private const string KeyDir = "keys";

        public static string GetPrivateKeyPath(string userId) => Path.Combine(KeyDir, $"{userId}_private.pem");
        public static string GetPublicKeyPath(string userId) => Path.Combine(KeyDir, $"{userId}_public.pem");

        public static bool HasPrivateKey(string userId) => File.Exists(GetPrivateKeyPath(userId));

        public static void EnsureKeyFolder()
        {
            if (!Directory.Exists(KeyDir))
                Directory.CreateDirectory(KeyDir);
        }

        public static void SaveKeyFiles(string userId, string pubPath, string privPath)
        {
            File.Copy(pubPath, GetPublicKeyPath(userId), true);
            File.Copy(privPath, GetPrivateKeyPath(userId), true);
        }

        public static string LoadPrivateKeyPath(string userId) => GetPrivateKeyPath(userId);
        public static string LoadPublicKeyContent(string userId) => File.ReadAllText(GetPublicKeyPath(userId));
    }
}
