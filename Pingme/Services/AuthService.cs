using Firebase.Database;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Services
{
    class AuthService
    {
        public static User CurrentUser { get; private set; }

        private static readonly FirebaseClient client = new FirebaseClient("https://fir-36ac0-default-rtdb.firebaseio.com/");

        public static async Task<bool> LoginAsync(string username, string password)
        {
            var users = await client.Child("users").OnceAsync<User>();

            var user = users
                .Select(u => u.Object)
                .FirstOrDefault(u => u.userName == username && u.password == password);

            if (user != null)
            {
                CurrentUser = user;
                return true;
            }

            return false;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}
