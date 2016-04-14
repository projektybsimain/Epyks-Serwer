using System.Collections.Generic;

namespace Epyks_Serwer
{
    public static class UserCollection
    {
        static List<User> usersOnline = new List<User>();

        public static void Add(User user)
        {
            List<User> queue = new List<User>(user.FriendsList.Count);
            lock (ThreadSync.Lock)
            {
                usersOnline.Add(user);
                foreach (User friend in user.FriendsList)
                {
                    if (usersOnline.Exists(item => item.Login == friend.Login))
                        queue.Add(friend);
                }
            }
            foreach (User friend in queue) // dla każdego ze znajomych online wysyłamy powiadomeinie o zmianie statusu
                friend.FriendStatusChanged(user, true);
        }

        public static void Remove(User user)
        {
            List<User> queue = new List<User>(user.FriendsList.Count);
            lock (ThreadSync.Lock)
            {
                usersOnline.RemoveAll(item => user.Login == item.Login); // usuwamy danego użytkownika z listy online
                foreach (User friend in user.FriendsList)
                {
                    if (usersOnline.Exists(item => item.Login == friend.Login))
                        queue.Add(friend);
                }
            }
            foreach (User friend in queue)
                friend.FriendStatusChanged(user, false);
        }

        public static bool IsOnline(string login)
        {
            return usersOnline.Exists(item => item.Login == login);
        }
    }
}
