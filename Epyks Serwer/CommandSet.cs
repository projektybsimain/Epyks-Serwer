using System.Reflection;

namespace Epyks_Serwer
{
    static class CommandSet
    {
        public static readonly string Register = "REGISTER";
        public static readonly string Login = "LOGIN";
        public static readonly string AuthFail = "AUTH;FAIL";
        public static readonly string AuthSuccess = "AUTH;SUCCESS";
        public static readonly string Contacts = "CONTACTS";
        public static readonly string Invitations = "INVITATIONS";
        public static readonly string Call = "CALL";
        public static readonly string Logout = "LOGOUT";
        public static readonly string StartConversation = "START_CONV";
        public static readonly string StopConversation = "STOP_CONV";
        public static readonly string Alive = "ALIVE";
        public static readonly string ChangePass = "CHANGE_PASS";
        public static readonly string Find = "FIND";
        public static readonly string Invite = "INVITE";
        public static readonly string Block = "BLOCK";
        public static readonly string Unlock = "UNLOCK";
        public static readonly string StatusChanged = "STATUS_CHANGED";
        public static readonly string Status = "STATUS";
        public static readonly string Error = "ERROR";
        public static readonly string OK = "OK";
        public static readonly string FoundUsers = "FOUND_USERS";

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
