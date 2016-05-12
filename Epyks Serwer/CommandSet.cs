using System.Reflection;

namespace Epyks_Serwer
{
    public static class CommandSet
    {
        public static readonly Command Register = new Command("REGISTER", 4);
        public static readonly Command Login = new Command("LOGIN", 3);
        public static readonly Command AuthFail = new Command("AUTH;FAIL");
        public static readonly Command AuthSuccess = new Command("AUTH;SUCCESS");
        public static readonly Command Contacts = new Command("CONTACTS");
        public static readonly Command Invitations = new Command("INVITATIONS");
        public static readonly Command Call = new Command("CALL", 1);
        public static readonly Command Logout = new Command("LOGOUT");
        public static readonly Command StartConversation = new Command("START_CONV");
        public static readonly Command StopConversation = new Command("STOP_CONV");
        public static readonly Command Alive = new Command("ALIVE");
        public static readonly Command ChangePass = new Command("CHANGE_PASS", 2);
        public static readonly Command Find = new Command("FIND", 1);
        public static readonly Command Invite = new Command("INVITE", 2);
        public static readonly Command Block = new Command("BLOCK", 1);
        public static readonly Command Unlock = new Command("UNLOCK", 1);
        public static readonly Command StatusChanged = new Command("STATUS_CHANGED");
        public static readonly Command Status = new Command("STATUS");
        public static readonly Command Error = new Command("ERROR");
        public static readonly Command OK = new Command("OK");
        public static readonly Command FoundUsers = new Command("FOUND_USERS");
        public static readonly Command ChangeName = new Command("CHANGE_NAME", 1);
        public static readonly Command GetName = new Command("GET_NAME");
        public static readonly Command Name = new Command("NAME");
        public static readonly Command NewInvitation = new Command("NEW_INVITATION", 3);
        public static readonly Command AcceptInvite = new Command("ACCEPT_INVIT", 1);
        public static readonly Command RejectInvite = new Command("REJECT_INVIT", 1);
        public static readonly Command InvitationAccepted = new Command("INVIT_ACCEPTED", 2);
        public static readonly Command Remove = new Command("REMOVE", 1);
        public static readonly Command BlockedUsers = new Command("BLOCKED_USERS");

        public static bool IsKnownCommand(string command)
        {
            PropertyInfo[] properties = typeof(CommandSet).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (command == property.GetValue(property).ToString())
                    return true;
            }
            return false;
        }
    }
}
