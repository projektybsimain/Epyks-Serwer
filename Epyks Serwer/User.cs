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
    public class User // doddać synchronizacje wątków
    {
        public string Login { get; private set; } // wykorzystywane przy sprawdzaniu kto ze znajomych jest online
        public string Name { get; private set; }
        public string PasswordHash { get; private set; }
        public List<User> FriendsList { get; private set; }
        private Thread thread;
        private Connection connection;

        public User(NetworkCredential credential)
        {
            Login = credential.UserName.ToLower();
            if (Login.Length > 24 || ValidateLogin() == false) // jeśli login jest dłuższy niż 24 znaki lub zawiera niedozwolone znaki
                throw new InvalidUsernameException();
            PasswordHash = CalculateSHA256(credential.Password, credential.UserName);
            FriendsList = Database.GetFriendsList(Login);
        }

        public void DoWork(Thread thread) // potrzebna referencja do wątku by móc zareagować na timeout
        {
            this.thread = thread;
            System.Timers.Timer timeout = new System.Timers.Timer(900000); // timer odpytujący klienta co 15 minut czy nadal jest online
            timeout.Elapsed += delegate { onUserTimeout(); };
            timeout.Start();
            while (true)
            {
                connection.ReceiveMessage();
                if (connection.Command == CommandSet.Logout)
                    LogoutUser();
                Console.WriteLine(connection.Command);
            }
        }

        public void SetConnection(TcpClient connection, int commandsPort)
        {
            this.connection = new Connection(connection, commandsPort, LogoutUser);
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

        private void LogoutUser()
        {
            connection.Disconnect();
            UserCollection.Remove(this);
            Console.WriteLine("Wylogowano użytkownika: " + Login);
            thread.Abort();
        }

        private void onUserTimeout() // zdarzanie występujące przy zbyt długej odpowiedzi na klienta
        {
            connection.SendMessage(CommandSet.Alive); // jeśli przesłanie komunikatu się nie powiedzie to znaczy, że użytkownik zostął niespodziewanie odłączony
        }

        public void FriendStatusChanged(User user, bool isLoggingIn)
        {
            string code = "0";
            if (isLoggingIn)
                code = "1";
            string message = CommandSet.StatusChanged + ";" + user.Login + ";" + code;
            connection.SendMessageUDP(message);
        }
    }

    public class InvalidUsernameException : Exception
    {

    }
}
