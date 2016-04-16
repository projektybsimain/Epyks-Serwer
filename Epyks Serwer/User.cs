﻿using Ekyps_Serwer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public bool IsBusy { get; private set; }
        private Thread thread;
        private Connection connection;

        public User(NetworkCredential credential)
        {
            Login = credential.UserName.ToLower();
            if (Login.Length > 24 || ValidateLogin() == false) // jeśli login jest dłuższy niż 24 znaki lub zawiera niedozwolone znaki
                throw new InvalidUsernameException();
            PasswordHash = CalculateSHA256(credential.Password.Trim(), Login);
            IsBusy = false;
            Name = null;
        }

        public void DoWork(Thread thread) // potrzebna referencja do wątku by móc zareagować na timeout
        {
            if (String.IsNullOrEmpty(Name))
                Name = Database.GetUserName(this);
            this.thread = thread;
            while (true)
            {
                connection.ReceiveMessage();
                if (connection.Command == CommandSet.Logout)
                    LogoutUser();
                else if (connection.Command == CommandSet.Contacts)
                    GetContacts();
                else if (connection.Command == CommandSet.ChangePass)
                    ChangePassword();
                else if (connection.Command == CommandSet.Call)
                    Call();
                else if (connection.Command == CommandSet.StartConversation)
                    IsBusy = true;
                else if (connection.Command == CommandSet.StopConversation)
                    IsBusy = false;
                else if (connection.Command == CommandSet.Find)
                    FindUsers();
                else if (connection.Command == CommandSet.ChangeName)
                    ChangeName();
                else if (connection.Command == CommandSet.GetName)
                    GetName();
                else if (connection.Command == CommandSet.Invite)
                    Invite();
                else if (connection.Command == CommandSet.Invitations)
                    GetInvitations();
                else if (connection.Command == CommandSet.AcceptInvite)
                    AcceptInvite();
                else if (connection.Command != CommandSet.LongMessage) // ignorujemy wiadomości typu LONG_MSG
                    connection.SendMessage(CommandSet.Error, ErrorMessageID.InvalidMessage);
            }
        }

        private void AcceptInvite()
        {

        }

        private void GetInvitations()
        {
            List<Invitation> invitations = Database.GetInvitationsList(this);
            string[] array = new string[invitations.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = invitations[i].ToString();
            }
            connection.SendMessage(CommandSet.Invitations, String.Join(";", array));
        }

        private void Invite() // WYSYŁANIE ZAPROSZENIA JEŚLI ZNAJOMY JEST ONLINE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        {
            string targetLogin = connection[0];
            if (String.IsNullOrEmpty(targetLogin) || targetLogin == Login)
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.UnknownError);
                return;
            }
            string message = connection[1];
            if (message.Trim().Length == 0) // pozbywamy się pustych wiadomości
                message = String.Empty;
            if (!Database.AddInvite(this, targetLogin, message))
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.UnknownError);
                return;
            }
            try
            {
                User targetUser = UserCollection.GetUserByLogin(targetLogin);
                targetUser.NewInvitation(this, message);
            }
            catch
            {
                // jeśli użytkownik nie jest online to nie robimy nic
            }
        }

        private void GetName()
        {
            connection.SendMessage(CommandSet.Name, Name);
        }

        private void ChangeName()
        {
            try
            {
                SetName(connection[0]);
            }
            catch
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.InvalidName);
                return;
            }
            Database.ChangeUserName(this);
        }

        public void SetName(string name)
        {
            if (String.IsNullOrEmpty(name.Trim()) || name.Length > 48 || name.IndexOf(';') >= 0)
                throw new Exception();
            Name = name;
        }

        private void FindUsers()
        {
            string target = connection[0];
            if (String.IsNullOrEmpty(target))
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.UnknownError);
                return;
            }
            List<Contact> usersFound = Database.FindUsers(target, Login);
            string[] array = new string[usersFound.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = usersFound[i].ToString();
            }
            connection.SendMessage(CommandSet.FoundUsers, String.Join(";", array));
        }

        private void Call()
        {
            string targetLogin = connection[0];
            if (String.IsNullOrEmpty(targetLogin))
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.UnknownError);
                return;
            }
            if (ContactsList.FindIndex(item => item.Login == targetLogin) == -1) // jeśli użytkownik nie jest w znajomych
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.NotInContacts);
                return;
            }
            if (!UserCollection.IsOnline(targetLogin))
            {
                connection.SendMessage(CommandSet.Call, ErrorMessageID.UserOffline);
                return;
            }
            User targetUser;
            try
            {
                targetUser = UserCollection.GetUserByLogin(targetLogin);
            }
            catch
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.UnknownError);
                return;
            }
            if (targetUser.IsBusy)
            {
                connection.SendMessage(CommandSet.Call, ErrorMessageID.UserBusy);
                return;
            }
            connection.SendMessage(CommandSet.Call, targetUser.GetIPString());
        }

        private void ChangePassword()
        {
            string oldPasswordHash = CalculateSHA256(connection[0], Login);
            if (oldPasswordHash != PasswordHash)
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.InvalidPassword);
                return;
            }
            PasswordHash = CalculateSHA256(connection[1].Trim(), Login);
            Database.ChangeUserPassword(this);
            connection.SendMessage(CommandSet.OK);
        }

        public void UpdateContactsList()
        {
            ContactsList = Database.GetContactsList(this);
        }

        public void GetContacts()
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
            connection.SendMessage(CommandSet.Contacts, String.Join(";", contacts));
        }

        public void SetConnection(Connection connection, int commandsPort)
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

        public void FriendStatusChanged(User calledBy, bool isLoggingIn)
        {
            string code = "0";
            if (isLoggingIn)
                code = "1";
            string message = CommandSet.StatusChanged + ";" + calledBy.Login + ";" + code;
            connection.SendMessageUDP(message);
            Console.WriteLine("DEBUG: " + Login + " powiadomiony o zmianie statusu użytkownika " + calledBy.Login);
        }

        public void NewInvitation(User calledBy, string message)
        {
            string msg = CommandSet.NewInvitation + ";" + calledBy.Login + ";" + calledBy.Name + ";" + message;
            connection.SendMessageUDP(msg);
            Console.WriteLine("DEBUG: " + Login + " powiadomiony o zmianie nowym zaproszeniu od " + calledBy.Login);
        }

        private string GetIPString()
        {
            return connection.GetIPString();
        }
    }

    public class InvalidUsernameException : Exception
    {

    }
}
