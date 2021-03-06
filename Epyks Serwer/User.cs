﻿using Ekyps_Serwer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
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
        public string Firstport { get; set; }
        public string SecondPort { get; set; }
        public List<Contact> ContactsList { get; set; }
        public bool IsBusy { get; private set; }
        private Thread thread;
        private Connection connection;

        public User(NetworkCredential credential)
        {
            Login = credential.UserName.ToLower();
            if (Login.Length > 24 || ValidateLogin() == false) // jeśli login jest dłuższy niż 24 znaki lub zawiera niedozwolone znaki
                throw new InvalidUsernameException();
            PasswordHash = ChecksumProvider.CalculateSHA256(credential.Password.Trim(), Login);
            IsBusy = false;
            Name = null;
        }

        public void DoWork(Thread thread) // potrzebna referencja do wątku by móc zareagować na timeout
        {
            if (String.IsNullOrEmpty(Name))
                Name = Database.GetUserName(Login);
            this.thread = thread;
            while (true)
            {
                connection.ReceiveMessage();
                if (connection.Command == CommandSet.Logout)
                    LogoutUser();
                else if (connection.Command == CommandSet.Contacts)
                    SendContacts();
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
                    SendName();
                else if (connection.Command == CommandSet.Invite)
                    Invite();
                else if (connection.Command == CommandSet.Invitations)
                    SendInvitations();
                else if (connection.Command == CommandSet.AcceptInvite)
                    AcceptInvite();
                else if (connection.Command == CommandSet.RejectInvite)
                    RejectInvite();
                else if (connection.Command == CommandSet.Remove)
                    RemoveContact();
                else if (connection.Command == CommandSet.Block)
                    BlockUser();
                else if (connection.Command == CommandSet.Unlock)
                    UnlockUser();
                else if (connection.Command == CommandSet.BlockedUsers)
                    SendBlockedList();
                else
                    connection.SendMessage(CommandSet.Error, ErrorMessageID.InvalidMessage);
            }
        }

        private void SendBlockedList()
        {
            string[] blockedLogins;
            lock (ThreadSync.Lock)
            {
                blockedLogins = Database.GetBlockedList(Login).ToArray();
            }
            connection.SendMessage(CommandSet.BlockedUsers, String.Join(";", blockedLogins));
        }

        private void UnlockUser()
        {
            string toUnlockLogin = connection[0];
            if (String.IsNullOrEmpty(toUnlockLogin) || toUnlockLogin == Login)
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.UnknownError);
                return;
            }
            lock (ThreadSync.Lock)
            {
                Database.RemoveBlocked(Login, toUnlockLogin);
            }
        }

        private void BlockUser()
        {
            string blockedLogin = connection[0];
            if (String.IsNullOrEmpty(blockedLogin) || blockedLogin == Login)
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.UnknownError);
                return;
            }
            lock (ThreadSync.Lock)
            {
                Database.RemoveContact(Login, blockedLogin);
                Database.RemoveContact(blockedLogin, Login);
                Database.AddBlocked(Login, blockedLogin);
            }
            SendContacts();
        }

        private void RemoveContact()
        {
            string contactLogin = connection[0];
            if (String.IsNullOrEmpty(contactLogin) || contactLogin == Login)
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.UnknownError);
                return;
            }
            lock (ThreadSync.Lock)
            {
                Database.RemoveContact(Login, contactLogin);
                Database.RemoveContact(contactLogin, Login);
            }
            SendContacts();
        }

        private void AcceptInvite()
        {
            string inviterLogin = connection[0];
            lock (ThreadSync.Lock)
            {
                if (!Database.RemoveInvitation(inviterLogin, Login))
                    return;
                Database.AddContact(inviterLogin, Login);
                Database.AddContact(Login, inviterLogin);
            }
            try
            {
                User targetUser = UserCollection.GetUserByLogin(inviterLogin);
                targetUser.InvitationAccepted(this);
            }
            catch
            {
                // jeśli użytkownik nie jest online to nie robimy nic
            }
            SendContacts();
        }

        private void RejectInvite()
        {
            string inviterLogin = connection[0];
            lock (ThreadSync.Lock)
            {
                Database.RemoveInvitation(inviterLogin, Login);
            }
        }

        private void SendInvitations()
        {
            List<Invitation> invitations;
            lock (ThreadSync.Lock)
            {
                invitations = Database.GetInvitationsList(Login);
            }
            string[] array = new string[invitations.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = invitations[i].ToString();
            }
            connection.SendMessage(CommandSet.Invitations, String.Join(";", array));
        }

        private void Invite()
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
            string result;
            lock (ThreadSync.Lock)
            {
                result = Database.AddInvitation(this, targetLogin, message);
            }
            if (result != ErrorMessageID.OK)
            {
                connection.SendMessage(CommandSet.Error, result);
                return;
            }
            try
            {
                User targetUser = UserCollection.GetUserByLogin(targetLogin);
                targetUser.NewInvitation(this, message);
            }
            catch
            {
                Debug.WriteLine("Użytkownik " + targetLogin + " nie jest online");
            }
            connection.SendMessage(CommandSet.OK);
        }

        private void SendName()
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
            lock (ThreadSync.Lock)
            {
                Database.ChangeValue(ID, "Users", "Name", Name);
            }
        }

        public void SetName(string name)
        {
            name = name.Trim();
            if (String.IsNullOrEmpty(name) || name.Length > 48 || name.IndexOf(';') >= 0)
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
            List<Contact> usersFound;
            lock (ThreadSync.Lock)
            {
                usersFound = Database.FindUsers(target, Login);
            }
            string[] array = new string[usersFound.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = usersFound[i].ToString();
            }
            connection.SendMessage(CommandSet.FoundUsers, String.Join(";", array));
        }

        private bool IsFriend(string targetLogin)
        {
            return ContactsList.FindIndex(item => item.Login == targetLogin) != -1;
        }

        private void Call()
        {
            string targetLogin = connection[0];
            if (String.IsNullOrEmpty(targetLogin))
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.UnknownError);
                return;
            }
            bool isFriend;
            lock (ThreadSync.Lock)
            {
                isFriend = IsFriend(targetLogin);
            }
            if (!isFriend)
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.NotInContacts);
                return;
            }
            bool isOnline;
            lock (ThreadSync.Lock)
            {
                isOnline = UserCollection.IsOnline(targetLogin);
            }
            if (!isOnline)
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
            connection.SendMessage(CommandSet.Call, targetUser.GetIPString(), targetUser.Firstport, targetUser.SecondPort);
        }

        private void ChangePassword()
        {
            string oldPasswordHash = ChecksumProvider.CalculateSHA256(connection[0], Login);
            if (oldPasswordHash != PasswordHash)
            {
                connection.SendMessage(CommandSet.Error, ErrorMessageID.InvalidPassword);
                return;
            }
            PasswordHash = ChecksumProvider.CalculateSHA256(connection[1], Login);
            lock (ThreadSync.Lock)
            {
                Database.ChangeValue(ID, "Users", "Password", PasswordHash);
            }
            connection.SendMessage(CommandSet.OK);
        }

        public void UpdateContactsList()
        {
            lock (ThreadSync.Lock)
            {
                ContactsList = Database.GetContactsList(Login);
            }
        }

        public void SendContacts()
        {
            UpdateContactsList();
            string[] contacts = new string[ContactsList.Count];
            for (int i = 0; i < contacts.Length; i++)
            {
                Contact contact = ContactsList[i];
                string code = "0";
                if (UserCollection.IsOnline(contact.Login))
                    code = "1";
                contacts[i] = contact.ToString() + ";" + code;
            }
            connection.SendMessage(CommandSet.Contacts, String.Join(";", contacts));
        }

        public void SetConnection(Connection connection)
        {
            this.connection = new Connection(connection, LogoutUser);
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
            connection.SendMessage(CommandSet.StatusChanged, calledBy.Login, code);
        }

        public void NewInvitation(User calledBy, string message)
        {
            connection.SendMessage(CommandSet.NewInvitation, calledBy.Login, calledBy.Name, message);
        }

        public void InvitationAccepted(User calledBy)
        {
            connection.SendMessage(CommandSet.InvitationAccepted, calledBy.Login, calledBy.Name);
        }

        private string GetIPString()
        {
            return connection.GetIP();
        }
    }

    public class InvalidUsernameException : Exception
    {

    }
}
