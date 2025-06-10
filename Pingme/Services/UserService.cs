using Firebase.Database;
using Firebase.Database.Query;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Services
{
    class UserService
    {
        private readonly FirebaseClient _firebase = new FirebaseClient(
            "https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult("haBViPv4zOuLMSuBt2mnaD2SYokqsRpbLIt5PcFL")
            });

        public async Task<User> GetUserByEmail(string email)
        {
            var users = await _firebase.Child("users").OnceAsync<User>();
            var user = users.FirstOrDefault(u => u.Object.Email == email);
            if (user != null)
            {
                user.Object.Id = user.Key;
                return user.Object;
            }

            return null;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = await _firebase.Child("users").OnceAsync<User>();
            return users.Select(u =>
            {
                u.Object.Id = u.Key;
                return u.Object;
            }).ToList();
        }
    }
}
