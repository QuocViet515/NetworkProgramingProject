using Pingme.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Pingme.Helpers
{
    internal class SessionManager
    {
        public static string UID { get; set; }
        public static User CurrentUser { get; set; }
        public static string IdToken { get; set; }
        public static string RefreshToken { get; set; }
        public static DateTime TokenExpiresAt { get; set; }

        public const string FirebaseApiKey = "AIzaSyDC_fCjmDw4IkAqhLjqWCzG02LRXmvKgB0";
        public static async Task EnsureValidTokenAsync()
        {
            if (DateTime.UtcNow < TokenExpiresAt.AddMinutes(-1))
                return; // Token vẫn còn hiệu lực

            using (HttpClient client = new HttpClient())
            {
                var payload = new
                {
                    grant_type = "refresh_token",
                    refresh_token = RefreshToken
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    $"https://securetoken.googleapis.com/v1/token?key={FirebaseApiKey}",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    dynamic result = JObject.Parse(resultJson);

                    IdToken = result.id_token;
                    RefreshToken = result.refresh_token;
                    int expiresIn = int.Parse((string)result.expires_in);
                    TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
                }
                else
                {
                    throw new Exception("Không thể refresh token: " + await response.Content.ReadAsStringAsync());
                }
            }
        }
        public static void Clear()
        {
            UID = null;
            IdToken = null;
            RefreshToken = null;
            CurrentUser = null;
        }

    }
}
