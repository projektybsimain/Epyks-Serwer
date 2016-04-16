using System.Collections.Generic;

namespace Epyks_Serwer
{
    public static class UserCollection
    {
        static List<User> usersOnline = new List<User>();

        public static void Add(User user)
        {
            List<User> queue = new List<User>(user.ContactsList.Count);
            lock (ThreadSync.Lock)
            {
                usersOnline.Add(user);
                foreach (Contact contact in user.ContactsList)
                {
                    foreach (User userOnline in usersOnline)
                    {
                        if (userOnline.ID == contact.ID)
                        {
                            queue.Add(userOnline);
                            break; // unikamy przeglądania całej listy, interesuje nas pierwsze wsytąpienie
                        }
                    }
                }
            }
            foreach (User userOnline in queue) // do każdego ze znajomych online wysyłamy powiadomeinie o zmianie statusu
                userOnline.FriendStatusChanged(user, true);
        }

        public static void Remove(User user)
        {
            List<User> queue = new List<User>(user.ContactsList.Count);
            lock (ThreadSync.Lock)
            {
                usersOnline.RemoveAll(item => user.Login == item.Login); // usuwamy danego użytkownika z listy online
                foreach (Contact contact in user.ContactsList)
                {
                    foreach (User userOnline in usersOnline)
                    {
                        if (userOnline.ID == contact.ID)
                        {
                            queue.Add(userOnline);
                            break; // unikamy przeglądania całej listy, interesuje nas pierwsze wsytąpienie
                        }
                    }
                }
            }
            foreach (User friend in queue)
                friend.FriendStatusChanged(user, false);
        }

        public static bool IsOnline(int ID)
        {
            return usersOnline.Exists(item => item.ID == ID);
        }

        public static bool IsOnline(string login)
        {
            return usersOnline.Exists(item => item.Login == login);
        }

        public static User GetUserByLogin(string login)
        {
            foreach(User user in usersOnline)
            {
                if (user.Login == login)
                    return user;
            }
            throw new KeyNotFoundException();
        }
    }
}
