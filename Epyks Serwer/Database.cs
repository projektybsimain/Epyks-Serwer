using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                string commandText = "CREATE TABLE Users (UserID int PRIMARY KEY, Login varchar(255) NOT NULL, Name varchar(255) NOT NULL, Password varchar(255) NOT NULL);";
                SQLiteCommand command = new SQLiteCommand(commandText, connection);
                command.ExecuteNonQuery();
                commandText = "CREATE TABLE Contacts (UserID int, ContactID int, FOREIGN KEY(UserID) REFERENCES Users(UserID), FOREIGN KEY(ContactID) REFERENCES Users(UserID));";
                command = new SQLiteCommand(commandText, connection);
                command.ExecuteNonQuery();
            }
            Console.WriteLine("Połączono!");
        }

        /*public User GetUser(AnonymousUser anonUser) // metoda wymaga oprogramowania
        {
            throw new NotImplementedException("Database.GetUser");
        }*/
    }
}

