using System;
using System.Reflection;

namespace Epyks_Serwer
{
    static class Command
    {
        public static string Register { get; private set; } = "REGISTER";
        public static string Login { get; private set; } = "LOGIN";

        public static bool IsKnownCommand(string command)
        {
            PropertyInfo[] properties = typeof(Command).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (command == property.GetValue(property).ToString())
                    return true;
            }
            return false;
        }
    }
}
