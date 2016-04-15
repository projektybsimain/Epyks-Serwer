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
        public int ID { get; set; }
        public string Login { get; private set; } // wykorzystywane przy sprawdzaniu kto ze znajomych jest online
        public string Name { get; private set; }
        public string PasswordHash { get; private set; }
        public List<Contact> ContactsList { get; set; }
        private Thread thread;
        private Connection connection;
        private System.Timers.Timer timeout;

        public User(NetworkCredential credential)
        {
            Login = credential.UserName.ToLower();
            if (Login.Length > 24 || ValidateLogin() == false) // jeśli login jest dłuższy niż 24 znaki lub zawiera niedozwolone znaki
                throw new InvalidUsernameException();
            PasswordHash = CalculateSHA256(credential.Password.Trim(), Login);
            timeout = new System.Timers.Timer(900000); // timer odpytujący klienta co 15 minut czy nadal jest online
            timeout.Elapsed += delegate { onUserTimeout(); };
        }

        public void DoWork(Thread thread) // potrzebna referencja do wątku by móc zareagować na timeout
        {
            this.thread = thread;
            timeout.Start();
            while (true)
            {
                connection.ReceiveMessage();
                if (connection.Command == CommandSet.Logout)
                    LogoutUser();
                else if (connection.Command == CommandSet.Contacts)
                    connection.SendMessage(CommandSet.Contacts, GetContactsString());
                else if (connection.Command == CommandSet.ChangePass)
                    ChangePassword();
            }
        }

        private void ChangePassword()
        {
            string oldPassword = null, newPassword = null;
            try
            {
                oldPassword = connection[0];
                newPassword = connection[1].Trim();
            }
            catch
            {
                connection.SendMessage(CommandSet.Error, ((int)ErrorMessageID.InvalidMessage).ToString());
                return;
            }
            string oldPasswordHash = CalculateSHA256(oldPassword, Login);
            if (oldPasswordHash != PasswordHash)
            {
                connection.SendMessage(CommandSet.Error, ((int)ErrorMessageID.InvalidPassword).ToString());
                return;
            }
            string newPasswordHash = CalculateSHA256(newPassword, Login);
            PasswordHash = newPasswordHash;
            Database.ChangeUserPassword(this);
            connection.SendMessage(CommandSet.OK);
        }

        public void UpdateContactsList()
        {
            ContactsList = Database.GetContactsList(this);
        }

        public string GetContactsString()
        {
            ContactsList = Database.GetContactsList(this);
            string[] contacts = new string[ContactsList.Count];
            for (int i = 0; i < contacts.Length; i++)
            {
                Contact contact = ContactsList[i];
                string code = "0";
                if (UserCollection.IsOnline(contact.ID));
                    code = "1";
                contacts[i] = contact.ToString() + ";" + code;
            }
            return String.Join(";", contacts);
        }

        public void SetConnection(TcpClient connection, int commandsPort)
        {
            this.connection = new Connection(connection, commandsPort, LogoutUser, timeout);
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
            timeout.Stop();
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
            Console.WriteLine("DEBUG: " + Login + " powiadomiony o zmianie statusu użytkownika " + user.Login);
        }
    }

    public class InvalidUsernameException : Exception
    {

    }
}
