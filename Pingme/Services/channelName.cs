using System;
using System.Text;
using System.Security.Cryptography;
namespace Pingme.Services
{
    public static class ChannelNameHelper
    {
        // Encode channel name to a safe Base64 string (URL-safe)
        public static string EncodeChannelName(string rawName)
        {
            var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(rawName));
            return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 32); // 32 ký tự hexa
        }

        // Decode the encoded Base64 string back to original name
        public static string DecodeChannelName(string encoded)
        {
            string base64 = encoded.Replace('-', '+').Replace('_', '/');
            switch (encoded.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
                case 1: base64 += "==="; break;
            }

            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
