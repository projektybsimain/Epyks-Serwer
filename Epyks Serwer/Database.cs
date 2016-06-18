using Epyks_Serwer;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Net;

namespace Ekyps_Serwer
{
    public static class Database
    {
        static SQLiteConnection connection;

        public static void Connect()
        {
            string dataBaseName = "database.sqlite";
            bool isEmpty = false;
            // utworzenie pliku bazy danych
            if (!File.Exists(dataBaseName))
            {
                SQLiteConnection.CreateFile(dataBaseName);
                isEmpty = true;
            }
            Console.WriteLine("Łączenie z bazą danych...");
            connection = new SQLiteConnection("Data Source=" + dataBaseName + ";Version=3;");
            connection.Open();
            if (isEmpty) // jeśli baza danych była pusta musimy utworzyć w niej początkowe tabele
            {
                string commandText = "CREATE TABLE Users (UserID INTEGER PRIMARY KEY AUTOINCREMENT, Login varchar(24) NOT NULL, Name nvarchar(48) NOT NULL, Password varchar(255) NOT NULL);";
                ExecuteNonQuery(commandText);
                commandText = "CREATE TABLE Contacts (Login varchar(24) NOT NULL, TargetLogin varchar(24) NOT NULL);";
                ExecuteNonQuery(commandText);
                commandText = "CREATE TABLE Invitations (UserID INTEGER, TargetLogin varchar(24), Message nvarchar(256), FOREIGN KEY(UserID) REFERENCES Users(UserID));";
                ExecuteNonQuery(commandText);
                commandText = "CREATE TABLE Blocked (Login varchar(24) NOT NULL, TargetLogin varchar(24) NOT NULL);"; // tabela przechowująca informacje o zablokowanych użytkownikach
                ExecuteNonQuery(commandText);
            }
            Console.WriteLine("Połączono!");
        }

        public static bool TryGetUser(out User user, NetworkCredential credential, ref string message, bool isNewUser, string name) // parametr isNewUser określa czy chcemy jednocześnie zarejestrować danego użytkownika, parametr message informuje dlaczego nie da sie zarejestrować danego użytkownika lub zalogować
        {
            user = null;
            try
            {
                user = new User(credential);
            }
            catch (InvalidUsernameException)
            {
                message = ErrorMessageID.InvalidUsername;
                return false;
            }
            catch
            {
                message = ErrorMessageID.UnknownError;
                return false;
            }
            bool userExists = UserExists(user.Login);
            if (isNewUser && userExists) // jeśli użytkownik chce się zarejestrować przy użyciu zajętego już loginu
            {
                message = ErrorMessageID.UsernameTaken;
                return false;
            }
            else if (isNewUser && !userExists)
            {
                try
                {
                    user.SetName(name);
                }
                catch (Exception)
                {
                    message = ErrorMessageID.InvalidName;
                    return false;
                }
                AddUser(user);
                user.ID = GetUserID(user.Login);
                return true;
            }
            else if (!isNewUser && !VerifyUser(user.Login, user.PasswordHash))
            {
                message = ErrorMessageID.InvalidUserCredential;
                return false;
            }
            user.ID = GetUserID(user.Login);
            return true;
        }

        public static List<Contact> GetContactsList(string login)
        {
            List<Contact> friends = new List<Contact>();
            string commandText = "SELECT TargetLogin FROM Contacts WHERE Login = '" + login + "'";
            SQLiteDataReader reader = ExecuteReader(commandText);
            List<string> contacts = new List<string>();
            while (reader.Read())
            {
                contacts.Add(reader["TargetLogin"].ToString()); // wczytujemy identyfikatory wszystkich znajomych
            }
            reader.Close();
            foreach (string contactLogin in contacts)
            {
                commandText = "SELECT Name FROM Users WHERE Login = '" + contactLogin + "'";
                reader = ExecuteReader(commandText);
                reader.Read();
                string name = reader["Name"].ToString();
                friends.Add(new Contact(contactLogin, name));
                reader.Close();
            }
            return friends;
        }

        private static int ExecuteNonQuery(string commandText)
        {
            using (SQLiteCommand command = new SQLiteCommand(commandText, connection))
            {
                return command.ExecuteNonQuery();
            }
        }

        private static bool ExecuteScalar(string commandText)
        {
            using (SQLiteCommand command = new SQLiteCommand(commandText, connection))
            {
                return command.ExecuteScalar() != null;
            }
        }

        private static SQLiteDataReader ExecuteReader(string commandText)
        {
            using (SQLiteCommand command = new SQLiteCommand(commandText, connection))
            {
                return command.ExecuteReader();
            }
        }

        public static void AddContact(string userLogin, string contactLogin)
        {
            if (Exists("Contacts", userLogin, contactLogin) || !UserExists(userLogin) || !UserExists(contactLogin))
                return;
            string commandText = "INSERT INTO Contacts (Login, TargetLogin) VALUES ('" + userLogin + "', '" + contactLogin + "')";
            ExecuteNonQuery(commandText);
        }

        public static void AddBlocked(string userLogin, string blockedLogin)
        {
            if (Exists("Blocked", userLogin, blockedLogin) || !UserExists(userLogin) || !UserExists(blockedLogin))
                return;
            string commandText = "INSERT INTO Blocked (Login, TargetLogin) VALUES ('" + userLogin + "', '" + blockedLogin + "')";
            ExecuteNonQuery(commandText);
        }

        private static bool Exists(string table, string userLogin, string contactLogin)
        {
            string commandText = "SELECT * FROM " + table + " WHERE Login = '" + userLogin + "' AND TargetLogin = '" + contactLogin + "' LIMIT 1";
            return ExecuteScalar(commandText);
        }

        public static void RemoveContact(string userLogin, string contactLogin)
        {
            string commandText = "DELETE FROM Contacts WHERE Login = '" + userLogin + "' AND TargetLogin = '" + contactLogin + "'";
            ExecuteNonQuery(commandText);
        }

        public static void RemoveBlocked(string userLogin, string blockedLogin)
        {
            string commandText = "DELETE FROM Blocked WHERE Login = '" + userLogin + "' AND TargetLogin = '" + blockedLogin + "'";
            ExecuteNonQuery(commandText);
        }

        public static List<string> GetBlockedList(string login)
        {
            List<string> logins = new List<string>();
            string commandText = "SELECT * FROM Blocked WHERE Login = '" + login.ToLower() + "'";
            SQLiteDataReader reader = ExecuteReader(commandText);
            while (reader.Read())
            {
                logins.Add(reader[1].ToString());
            }
            reader.Close();
            return logins;
        }

        public static List<Invitation> GetInvitationsList(string login)
        {
            List<Invitation> invitations = new List<Invitation>();
            string commandText = "SELECT * FROM Invitations WHERE TargetLogin = '" + login.ToLower() + "'";
            SQLiteDataReader reader = ExecuteReader(commandText);
            while (reader.Read())
            {
                string inviterLogin = GetUserLogin(reader["UserID"].ToString());
                string inviterName = GetUserName(inviterLogin);
                string invitationMessage = reader["Message"].ToString();
                invitations.Add(new Invitation(inviterLogin, inviterName, invitationMessage));
            }
            reader.Close();
            return invitations;
        }

        public static bool RemoveInvitation(string login, string targetLogin)
        {
            int id = GetUserID(login);
            if (id == -1)
                return false;
            if (!InviteExists(id, targetLogin))
                return false;
            string commandText = "DELETE FROM Invitations WHERE UserID = '" + id + "' AND TargetLogin = '" + targetLogin + "'";
            return ExecuteNonQuery(commandText) == 1;
        }

        public static List<Contact> FindUsers(string searchFor, string except) // nie odnajdujemy loginu dla użytkownika który jest wyszukującym
        {
            List<Contact> usersFound = new List<Contact>();
            string commandText = "SELECT Login, Name FROM Users";
            SQLiteDataReader reader = ExecuteReader(commandText);
            while (reader.Read())
            {
                string login = reader["Login"].ToString();
                if (login != except)
                {
                    string name = reader["Name"].ToString();
                    if (LevenshteinDistance.AreSimilar(searchFor, login) || LevenshteinDistance.AreSimilar(searchFor, name))
                        usersFound.Add(new Contact(login, name));
                }
            }
            reader.Close();
            return usersFound;
        }

        private static void AddUser(User user)
        {
            string commandText = "INSERT INTO Users (Login, Name, Password) VALUES ('" + user.Login + "', '" + user.Name + "', '" + user.PasswordHash + "')";
            ExecuteNonQuery(commandText);
        }

        private static bool UserExists(string userLogin)
        {
            string commandText = "SELECT * FROM Users WHERE Login = '" + userLogin + "' LIMIT 1";
            return ExecuteScalar(commandText);
        }

        public static bool IsUserBlocked(string login, string blockedByLogin)
        {
            string commandText = "SELECT * FROM Blocked WHERE Login = '" + blockedByLogin + "' AND TargetLogin = '" + login + "' LIMIT 1";
            return ExecuteScalar(commandText);
        }

        public static string AddInvitation(User inviter, string targetLogin, string message)
        {
            if (Exists("Contacts", inviter.Login, targetLogin))
                return ErrorMessageID.ContactExists;
            else if (InviteExists(inviter.ID, targetLogin))
                return ErrorMessageID.InviteExists;
            else if (!UserExists(targetLogin))
                return ErrorMessageID.UnknownUser;
            else if (IsUserBlocked(inviter.Login, targetLogin))
                return ErrorMessageID.UserBlocked;
            message = message.Trim();
            if (message.Length > 256)
                message.Substring(0, 256);
            string commandText = "INSERT INTO Invitations (UserID, TargetLogin, Message) VALUES ('" + inviter.ID + "', '" + targetLogin + "', '" + message + "')";
            ExecuteNonQuery(commandText);
            return ErrorMessageID.OK;
        }

        private static bool InviteExists(int userID, string inviterLogin)
        {
            string commandText = "SELECT * FROM Invitations WHERE UserID = '" + userID + "' AND TargetLogin = '" + inviterLogin + "' LIMIT 1";
            return ExecuteScalar(commandText);
        }

        private static bool VerifyUser(string login, string password)
        {
            string commandText = "SELECT * FROM Users WHERE Login = '" + login + "' AND Password = '" + password + "' LIMIT 1";
            return ExecuteScalar(commandText);
        }

        public static void ChangeValue(int userID, string tableName, string valueName, string value)
        {
            string commandText = "UPDATE " + tableName + " SET " + valueName + " = '" + value + "' WHERE UserID = '" + userID + "'";
            ExecuteNonQuery(commandText);
        }

        public static string GetUserName(string login)
        {
            string commandText = "SELECT Name FROM Users WHERE Login = '" + login + "'";
            SQLiteDataReader reader = ExecuteReader(commandText);
            reader.Read();
            string userName = reader[0].ToString();
            reader.Close();
            return userName;
        }

        private static int GetUserID(string login)
        {
            string commandText = "SELECT UserID FROM Users WHERE Login = '" + login + "'";
            SQLiteDataReader reader = ExecuteReader(commandText);
            if (!reader.Read())
                return -1;
            int id = reader.GetInt32(0);
            reader.Close();
            return id;
        }

        private static string GetUserLogin(string userID)
        {
            string commandText = "SELECT Login FROM Users WHERE UserID = '" + userID + "'";
            SQLiteDataReader reader = ExecuteReader(commandText);
            reader.Read();
            string userLogin = reader[0].ToString();
            reader.Close();
            return userLogin;
        }
    }
}
