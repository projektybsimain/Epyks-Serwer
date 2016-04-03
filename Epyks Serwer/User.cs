using Ekyps_Serwer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Epyks_Serwer
{
    class User // doddać synchronizacje wątków
    {
        public string Login { get; private set; } // wykorzystywane przy sprawdzaniu kto ze znajomych jest online
        private Timer timer; // timer odpytujący klienta co 15 minut czy nadal jest online
        private Database database;
        private List<User> usersOnline; // dostęp do globalnej listy użytkowników którzy są aktualnie zalogowani na serwerze
        private TcpClient connection;
        private NetworkStream stream;

        public User(NetworkCredential credential, string name)
        {
            Login = credential.UserName;
        }

        public void DoWork()
        {
            stream = connection.GetStream();
            // Obsługa klienta, na początek wymagane jest wczytanie listy znajomych
        }

        public void SetDatabase(Database database)
        {
            this.database = database;
        }

        public void SetUsers(List<User> users)
        {
            this.usersOnline = users;
        }

        public void SetConnection(TcpClient connection)
        {
            this.connection = connection;
        }

        private string[] ReceiveMessage()
        {
            int i;
            byte[] bytes = new byte[256];
            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                string msg = Encoding.ASCII.GetString(bytes, 0, i);
                return Regex.Split(msg, ";"); // automatyczny podział komunikatu na argumenty
            }
            return new string[] { String.Empty };
        }

        private void SendMessage(params string[] message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(String.Join(";", message));
            stream.Write(bytes, 0, bytes.Length);
        }
    }
    }
}
