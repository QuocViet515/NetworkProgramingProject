using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pingme.Services
{
    internal class AuthService
    {
        private const string ApiKey = "AIzaSyDC_fCjmDw4IkAqhLjqWCzG02LRXmvKgB0";

        private static readonly HttpClient client = new HttpClient();

        public class AuthResult
        {
            public string LocalId { get; set; }
            public string IdToken { get; set; }
            public string Email { get; set; }
            public string ErrorMessage { get; set; }
        }

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
                return JsonConvert.DeserializeObject<AuthResult>(resContent);
            else
                return new AuthResult { ErrorMessage = resContent };
        }

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
                return JsonConvert.DeserializeObject<AuthResult>(resContent);
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
    }
}
