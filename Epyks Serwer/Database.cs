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
                SQLiteCommand command = new SQLiteCommand(commandText, connection);
                command.ExecuteNonQuery();
                commandText = "CREATE TABLE Contacts (UserID INTEGER, ContactID INTEGER, FOREIGN KEY(UserID) REFERENCES Users(UserID), FOREIGN KEY(ContactID) REFERENCES Users(UserID));";
                command = new SQLiteCommand(commandText, connection);
                command.ExecuteNonQuery();
                commandText = "CREATE TABLE Invitations (UserID INTEGER, TargetLogin varchar(24), Message nvarchar(256), FOREIGN KEY(UserID) REFERENCES Users(UserID));";
                command = new SQLiteCommand(commandText, connection);
                command.ExecuteNonQuery();
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
            bool userExists = UserExists(user);
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
            else if (!isNewUser && !VerifyUser(user))
            {
                message = ErrorMessageID.InvalidUserCredential;
                return false;
            }
            user.ID = GetUserID(user.Login);
            return true;
        }

        private static int GetUserID(string login)
        {
            string commandText = "SELECT UserID FROM Users WHERE Login = '" + login + "'";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            if (!reader.Read())
                return -1;
            return reader.GetInt32(0);
        }

        public static List<Contact> GetContactsList(User user)
        {
            List<Contact> friends = new List<Contact>();
            string commandText = "SELECT ContactID FROM Contacts WHERE UserID = '" + user.ID + "'";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<int> contacts = new List<int>();
            while (reader.Read())
            {
                contacts.Add((int)reader["ContactID"]); // wczytujemy identyfikatory wszystkich znajomych
            }
            reader.Close();
            foreach (int contactID in contacts)
            {
                command.CommandText = "SELECT Login, Name FROM Users WHERE UserID = '" + contactID + "'";
                reader = command.ExecuteReader();
                reader.Read();
                string login = reader["Login"].ToString();
                string name = reader["Name"].ToString();
                friends.Add(new Contact(contactID, login, name));
            }
            return friends;
        }

        public static List<Invitation> GetInvitationsList(User user)
        {
            List<Invitation> invitations = new List<Invitation>();
            string commandText = "SELECT * FROM Invitations WHERE UserID = '" + user.ID + "'";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                invitations.Add(new Invitation(reader[0].ToString(), reader[1].ToString(), reader[2].ToString()));
            }
            return invitations;
        }

        public static List<Contact> FindUsers(string searchFor, string except) // nie odnajdujemy loginu dla użytkownika który jest wyszukującm
        {
            List<Contact> usersFound = new List<Contact>();
            string commandText = "SELECT Login, Name FROM Users";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string login = reader["Login"].ToString();
                if (login != except)
                {
                    
                }
                string name = reader["Name"].ToString();
                if (LevenshteinDistance.AreSimilar(searchFor, login) || LevenshteinDistance.AreSimilar(searchFor, name))
                    usersFound.Add(new Contact(login, name));
            }
            return usersFound;
        }

        private static void AddUser(User user)
        {
            string commandText = "INSERT INTO Users (Login, Name, Password) VALUES ('" + user.Login + "', '" + user.Name + "', '" + user.PasswordHash + "')";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            command.ExecuteNonQuery();
        }

        private static bool UserExists(User user)
        {
            string commandText = "SELECT * FROM Users WHERE Login = '" + user.Login + "' LIMIT 1";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            return command.ExecuteScalar() != null;
        }

        public static bool AddInvite(User user, string inviterLogin, string message)
        {
            if (message.Length > 256)
                message.Substring(0, 256);
            /*int inviterID = GetUserID(inviterLogin);
            if (inviterID == -1 || InviteExists(user, inviterLogin, inviterID))
                return false;*/
            if (InviteExists(user, inviterLogin))
            {
                return false;
            }
            string commandText = "INSERT INTO Invitations (UserID, TargetLogin, Message) VALUES ('" + user.ID + "', '" + inviterLogin + "', '" + message + "')";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            command.ExecuteNonQuery();
            return true;
        }

        private static bool InviteExists(User user, string inviterLogin)
        {
            string commandText = "SELECT * FROM Invitations WHERE UserID = '" + user.ID + "' AND TargetLogin = '" + inviterLogin + "' LIMIT 1";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            return command.ExecuteScalar() != null;
        }

        private static bool VerifyUser(User user)
        {
            string commandText = "SELECT * FROM Users WHERE Login = '" + user.Login + "' AND Password = '" + user.PasswordHash + "' LIMIT 1";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            return command.ExecuteScalar() != null;
        }

        public static void ChangeUserPassword(User user)
        {
            string commandText = "UPDATE Users SET Password = '" + user.PasswordHash + "' WHERE UserID = '" + user.ID + "'";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            command.ExecuteNonQuery();
        }

        public static void ChangeUserName(User user)
        {
            string commandText = "UPDATE Users SET Name = '" + user.Name + "' WHERE UserID = '" + user.ID + "'";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            command.ExecuteNonQuery();
        }

        public static string GetUserName(User user)
        {
            string commandText = "SELECT Name FROM Users WHERE UserID = '" + user.ID + "'";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            reader.Read();
            return reader[0].ToString();
        }
    }
}
