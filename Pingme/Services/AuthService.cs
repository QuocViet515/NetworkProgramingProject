using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using Pingme.Helpers;
using Pingme.Models;

namespace Pingme.Services
{
    internal class AuthService
    {
        private const string ApiKey = "AIzaSyDC_fCjmDw4IkAqhLjqWCzG02LRXmvKgB0";

        private static readonly HttpClient client = new HttpClient();
        public static User CurrentUser { get; private set; }

        public class AuthResult
        {
            public string LocalId { get; set; }
            public string IdToken { get; set; }
            public string Email { get; set; }
            public string ErrorMessage { get; set; }

        }

        //public static async Task<AuthResult> LoginAsync(string email, string password)
        //{
        //    var data = new
        //    {
        //        email,
        //        password,
        //        returnSecureToken = true
        //    };

        //    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        //    var res = await client.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}", content);
        //    var resContent = await res.Content.ReadAsStringAsync();

        //    if (res.IsSuccessStatusCode)
        //        return JsonConvert.DeserializeObject<AuthResult>(resContent);
        //    else
        //        return new AuthResult { ErrorMessage = resContent };
        //}
        public static async Task<AuthResult> LoginAsync(string email, string password)
        {
            var data = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var res = await client.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}", content);
            var resContent = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<AuthResult>(resContent);

                await EnsureKeyPairAsync(result.LocalId);

                var firebaseService = new FirebaseService();
                CurrentUser = await firebaseService.GetUserByIdAsync(result.LocalId);

                return result;
            }
            else
                return new AuthResult { ErrorMessage = resContent };
        }


        //public static async Task<AuthResult> RegisterAsync(string email, string password)
        //{
        //    var data = new
        //    {
        //        email,
        //        password,
        //        returnSecureToken = true
        //    };

        //    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        //    var res = await client.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}", content);
        //    var resContent = await res.Content.ReadAsStringAsync();

        //    if (res.IsSuccessStatusCode)
        //        return JsonConvert.DeserializeObject<AuthResult>(resContent);
        //    else
        //        return new AuthResult { ErrorMessage = resContent };
        //}
        public static async Task<AuthResult> RegisterAsync(string email, string password)
        {
            var data = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var res = await client.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}", content);
            var resContent = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<AuthResult>(resContent);

                // 🔐 Đảm bảo sinh key và upload publicKey nếu chưa có
                await EnsureKeyPairAsync(result.LocalId);
                var firebaseService = new FirebaseService();
                CurrentUser = await firebaseService.GetUserByIdAsync(result.LocalId);

                return result;
            }
            else
                return new AuthResult { ErrorMessage = resContent };
        }


        public static async Task<bool> SendPasswordResetEmail(string email)
        {
            var data = new
            {
                requestType = "PASSWORD_RESET",
                email = email
            };

            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var res = await client.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={ApiKey}", content);
            return res.IsSuccessStatusCode;
        }

        //public static User CurrentUser { get; private set; }

        //private static readonly FirebaseClient client = new FirebaseClient("https://fir-36ac0-default-rtdb.firebaseio.com/");
        //private static readonly RSAService rsaService = new RSAService();

        //public static async Task<bool> LoginAsync(string username, string password)
        //{
        //    var users = await client.Child("users").OnceAsync<User>();

        //    var user = users
        //        .Select(u => u.Object)
        //        .FirstOrDefault(u => u.userName == username && u.password == password);

        //    if (user == null)
        //        return false;

        //    CurrentUser = user;

        //    // 🔐 Nếu chưa có key cục bộ, tạo và đẩy public key nếu cần
        //    await EnsureKeyPairAsync(user.id, user.PublicKey);

        //    return true;
        //}

        //public static async Task<bool> RegisterAsync(string username, string password)
        //{
        //    string userId = Guid.NewGuid().ToString();

        //    var user = new User
        //    {
        //        id = userId,
        //        userName = username,
        //        password = password
        //    };

        //    await client.Child("users").Child(userId).PutAsync(user);

        //    CurrentUser = user;

        //    await EnsureKeyPairAsync(userId, null);

        //    return true;
        //}

        //public static void Logout()
        //{
        //    CurrentUser = null;
        //}

        //// 📌 Tạo public/private key nếu chưa có
        //private static async Task EnsureKeyPairAsync(string userId, string publicKeyFromFirebase)
        //{
        //    if (!KeyManager.HasPrivateKey(userId))
        //    {
        //        Console.WriteLine($"🔧 Tạo key mới cho {userId}...");
        //        bool ok = rsaService.GenerateKeysForUser(userId);

        //        if (!ok)
        //            throw new Exception("RSA key generation failed.");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"🟢 Private key đã tồn tại cho {userId}.");
        //    }

        //    // 🔄 Nếu Firebase chưa có publicKey → đẩy lên
        //    if (string.IsNullOrWhiteSpace(publicKeyFromFirebase))
        //    {
        //        string pubXml = KeyManager.LoadPublicKeyContent(userId);

        //        await client
        //            .Child("users")
        //            .Child(userId)
        //            .PatchAsync(new { publicKey = pubXml });

        //        Console.WriteLine($"⬆️ Đã upload publicKey cho {userId} lên Firebase.");
        //    }
        //}
        private static async Task EnsureKeyPairAsync(string userId)
        {
            var firebase = new FirebaseClient("https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/");

            // 🔄 Lấy publicKey hiện tại trên Firebase
            var user = await firebase.Child("users").Child(userId).OnceSingleAsync<User>();
            string publicKeyFromFirebase = user?.PublicKey;

            if (!KeyManager.HasPrivateKey(userId))
            {
                Console.WriteLine($"🔧 Tạo key mới cho {userId}...");
                var rsaService = new RSAService();
                bool ok = rsaService.GenerateKeysForUser(userId);  // sửa lại nếu là static

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

                await firebase
                    .Child("users")
                    .Child(userId)
                    .PatchAsync(new { publicKey = pubXml });

                Console.WriteLine($"⬆️ Đã upload publicKey cho {userId} lên Firebase.");
            }
        }
    }
}
