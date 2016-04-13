using Epyks_Serwer;
using System;
using System.Data.SQLite;
using System.IO;
using System.Net;

namespace Ekyps_Serwer
{
    class Database
    {
        private SQLiteConnection connection;

        public Database()
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
                string commandText = "CREATE TABLE Users (UserID INTEGER PRIMARY KEY AUTOINCREMENT, Login varchar(255) NOT NULL, Name varchar(255) NOT NULL, Password varchar(255) NOT NULL);";
                SQLiteCommand command = new SQLiteCommand(commandText, connection);
                command.ExecuteNonQuery();
                commandText = "CREATE TABLE Contacts (UserID INTEGER, ContactID INTEGER, FOREIGN KEY(UserID) REFERENCES Users(UserID), FOREIGN KEY(ContactID) REFERENCES Users(UserID));";
                command = new SQLiteCommand(commandText, connection);
                command.ExecuteNonQuery();
            }
            Console.WriteLine("Połączono!");
        }

        public bool TryGetUser(out User user, NetworkCredential credential, ref int message, bool isNewUser) // parametr isNewUser określa czy chcemy jednocześnie zarejestrować danego użytkownika, parametr message informuje dlaczego nie da sie zarejestrować danego użytkownika lub zalogować
        {
            user = null;
            try
            {
                user = new User(credential);
            }
            catch (InvalidUsernameException)
            {
                message = (int)ErrorMessageID.InvalidUsername;
                return false;
            }
            catch
            {
                message = (int)ErrorMessageID.UnknownError;
                return false;
            }
            bool userExists = UserExists(user);
            if (isNewUser && userExists) // jeśli użytkownik chce się zarejestrować przy użyciu zajętego już loginu
            {
                message = (int)ErrorMessageID.UsernameTaken;
                return false;
            }
            else if (isNewUser && !userExists)
            {
                AddUser(user);
                return true;
            }
            else if (!isNewUser && !VerifyUser(user))
            {
                message = (int)ErrorMessageID.InvalidUserCredential;
                return false;
            }
            return true;
        }

        private void AddUser(User user)
        {
            string commandText = "INSERT INTO Users (Login, Name, Password) VALUES ('" + user.Login + "', '" + user.Name + "', '" + user.PasswordHash + "')";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            command.ExecuteNonQuery();
        }

        private bool UserExists(User user)
        {
            string commandText = "SELECT * FROM Users WHERE Login = '" + user.Login + "' LIMIT 1";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            return command.ExecuteScalar() != null;
        }

        private bool VerifyUser(User user)
        {
            string commandText = "SELECT * FROM Users WHERE Login = '" + user.Login + "' AND Password = '" + user.PasswordHash + "' LIMIT 1";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            return command.ExecuteScalar() != null;
        }
    }
}

