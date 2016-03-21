using Epyks_Serwer;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public bool TryGetUser(out User user, NetworkCredential credential, out string message, bool isNewUser)
        {
            message = "Resposne";
            user = new User(credential, "asd");
            return false;
        }

        private void AddUser(User user)
        {
            string commandText = "INSERT INTO Users (Login, Name, Password) VALUES ('" + user.Login + "', '" + user.Name + "', '" + user.Password + "')";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            command.ExecuteNonQuery();
        }

        private bool UserExists(User user)
        {
            string commandText = "SELECT Count(*) FROM Users WHERE Login='" + user.Login + "'";
            SQLiteCommand command = new SQLiteCommand(commandText, connection);
            return Convert.ToInt32(command.ExecuteScalar()) != 0;
        }
    }
}

