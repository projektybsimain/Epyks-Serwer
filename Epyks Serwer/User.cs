using Ekyps_Serwer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Epyks_Serwer
{
    class User // doddać synchronizacje wątków
    {
        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            private set
            {
                if (value.Length > 32)
                    name = value.Substring(0, 32);
                else
                    name = value;
            }
        }
        public string Login { get; private set; }
        public string Password { get; private set; }
        private Timer timer; // dodać Eventy
        private Database database;
        private List<User> users;
        private TcpClient connection;

        public User()
        {

        }

        public User(NetworkCredential credential, string name)
        {
            Login = credential.UserName;
            Password = credential.Password;
            Name = name;
        }

        public void DoWork()
        {

        }

        public void SetDatabase(Database database)
        {
            this.database = database;
        }

        public void SetUsers(List<User> users)
        {
            this.users = users;
        }

        public void SetConnection(TcpClient connection)
        {
            this.connection = connection;
        }
    }
}
