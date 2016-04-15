
namespace Epyks_Serwer
{
    public class Contact
    {
        public int ID { get; private set; }
        public string Login { get; private set; }
        public string Name { get; private set; }

        public Contact(int ID, string login, string name)
        {
            this.ID = ID;
            Login = login;
            Name = name;
        }

        public override string ToString()
        {
            return Login + ";" + Name;
        }
    }
}
