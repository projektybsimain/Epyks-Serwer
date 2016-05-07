
namespace Epyks_Serwer
{
    public class Contact
    {
        public string Login { get; }
        public string Name { get; }

        public Contact(string login, string name)
        {
            Login = login;
            Name = name;
        }

        public override string ToString()
        {
            return Login + ";" + Name;
        }
    }
}
