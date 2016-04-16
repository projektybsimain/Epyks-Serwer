
namespace Epyks_Serwer
{
    public class Invitation
    {
        public string Login { get; private set; }
        public string Name { get; private set; }
        public string Message { get; private set; }

        public Invitation(string login, string name, string message)
        {
            Login = login;
            Name = name;
            Message = message;
        }

        public override string ToString()
        {
            return Login + ";" + Name + ";" + Message;
        }
    }
}
