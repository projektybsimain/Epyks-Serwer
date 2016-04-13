using Ekyps_Serwer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Epyks_Serwer
{
    class User // doddać synchronizacje wątków
    {
        public string Login { get; private set; } // wykorzystywane przy sprawdzaniu kto ze znajomych jest online
        public string Name { get; private set; }
        public string PasswordHash { get; private set; }
        private Timer timer; // timer odpytujący klienta co 15 minut czy nadal jest online
        private Database database;
        private List<User> usersOnline; // dostęp do globalnej listy użytkowników którzy są aktualnie zalogowani na serwerze
        private TcpClient connection;
        private NetworkStream stream;

        public User(NetworkCredential credential)
        {
            Login = credential.UserName.ToLower();
            if (Login.Length > 24 || ValidateLogin() == false) // jeśli login jest dłuższy niż 24 znaki lub zawiera niedozwolone znaki
                throw new InvalidUsernameException();
            PasswordHash = CalculateSHA256(credential.Password, credential.UserName);
        }

        public void DoWork()
        {
            stream = connection.GetStream();
            // Obsługa klienta, na początek wymagane jest wczytanie listy znajomych
        }

        public void SetDatabase(Database database)
        {
            this.database = database;
        }

        public void SetUsers(List<User> users)
        {
            usersOnline = users;
        }

        public void SetConnection(TcpClient connection)
        {
            this.connection = connection;
        }

        private string[] ReceiveMessage()
        {
            int i;
            byte[] bytes = new byte[256];
            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                string msg = Encoding.ASCII.GetString(bytes, 0, i);
                string[] args = Regex.Split(msg, ";"); // automatyczny podział komunikatu na argumenty
                for (int j = 0; j < args.Length; j++)
                    args[j] = args[j].Replace("&sem", ";");
                return args;
            }
            return new string[] { String.Empty };
        }

        private void SendMessage(params string[] message)
        {
            for (int i = 0; i < message.Length; i++)
                message[i] = message[i].Replace(";", "&sem"); // usuwa średnik z wiadomości ze względu na ich użycie przy podziale komunikatów
            byte[] bytes = Encoding.ASCII.GetBytes(String.Join(";", message));
            stream.Write(bytes, 0, bytes.Length);
        }

        private string CalculateSHA256(string text, string salt)
        {
            byte[] _text = Encoding.UTF8.GetBytes(text);
            byte[] _salt = Encoding.UTF8.GetBytes(salt);
            SHA256Managed crypt = new SHA256Managed();
            StringBuilder hash = new StringBuilder();
            byte[] crypto = crypt.ComputeHash(_text.Concat(_salt).ToArray(), 0, _text.Length + _salt.Length);
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        private bool ValidateLogin()
        {
            return Regex.IsMatch(Login, "^([0-9]|[a-z]|_)+$");
        }
    }

    public class InvalidUsernameException : Exception
    {

    }
}
