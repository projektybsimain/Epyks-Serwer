using Ekyps_Serwer;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Epyks_Serwer
{
    class Worker
    {
        TcpListener server;
        Thread listener;

        public Worker(int port)
        {
            server = new TcpListener(IPAddress.Any, port);
            try
            {
                server.Start();
            }
            catch (SocketException)
            {
                throw new Exception("Port " + port + " jest już zajęty");
            }
            Database.Connect();
            listener = new Thread(() => Listen(port));
            listener.Start();
        }

        private void Listen(int port)
        {
            while (true)
            {
                TcpClient newClient = server.AcceptTcpClient();
                Thread tempReference = null;
                Thread connectionThread = new Thread(() => AcceptSession(tempReference, newClient)); // w przypadku pomyślnego zalogowania dalsza obsługa klienta odbywać się będzie w tym wątku
                tempReference = connectionThread;
                connectionThread.Start();
            }
        }

        private void AcceptSession(Thread connectionThread, TcpClient userConnection)
        {
            System.Timers.Timer timeout = new System.Timers.Timer(5000); // timer ustawiony na 5 sekund, tyle czasu ma klient na przesłanie danych
            timeout.Elapsed += delegate { onConnectionTimeoutEvent(connectionThread, userConnection); };
            timeout.Start();
            Connection connection = new Connection(userConnection);
            connection.ReceiveMessage();
            if (connection.Command == CommandSet.Login || connection.Command == CommandSet.Register)
            {
                if (UserCollection.IsOnline(connection[0]))
                {
                    connection.SendMessage(CommandSet.AuthFail, ErrorMessageID.UserAlreadyLoggedIn);
                    connection.Disconnect();
                    return;
                }
                int clientPort = 0;
                bool isNewUser = false;
                string login = connection[0].ToLower();
                string password = connection[1];
                string name = null;

                if (connection.Command == CommandSet.Login)
                {
                    if (!Int32.TryParse(connection[2], out clientPort) || clientPort < 1 || clientPort > 65535)
                    {
                        connection.Disconnect();
                        return;
                    }
                }
                else if (connection.Command == CommandSet.Register)
                {
                    if (!Int32.TryParse(connection[3], out clientPort) || clientPort < 1 || clientPort > 65535)
                    {
                        connection.Disconnect();
                        return;
                    }
                    isNewUser = true;
                    name = connection[2];
                }
                User user;
                NetworkCredential userCredential;
                try
                {
                    userCredential = new NetworkCredential(login, password);
                }
                catch // jeśli dane podane przez klienta mają nieprawidłowy format
                {
                    userConnection.Close();
                    return;
                }
                timeout.Stop();
                string errorMessageID = ErrorMessageID.UnknownError; //ewentualna odpowiedz bazy danych w przypadku błędu

                bool isValidUser;

                lock (ThreadSync.Lock)
                {
                    isValidUser = Database.TryGetUser(out user, userCredential, ref errorMessageID, isNewUser, name); // sprawdzamy czy użytkownik istnieje w bazie danych oraz czy podał prawidłowe hasło
                }

                if (!isValidUser)
                {
                    connection.SendMessage(CommandSet.AuthFail, errorMessageID);
                    connection.Disconnect();
                    return;
                }
                else
                {
                    Console.WriteLine("Zalogowano użytkownika: " + login);
                    connection.SendMessage(CommandSet.AuthSuccess);
                    // przesyłamy referencje do danych które nie są znane bazie danych
                    user.SetClientPort(clientPort);
                    user.SetConnection(connection);
                    user.UpdateContactsList();
                    UserCollection.Add(user); // odnotowujemy że dany użytkownik stał się online 
                    user.DoWork(connectionThread); // dalsza obsługa klienta
                }
            }
            else
                connection.Disconnect();
        }

        private void onConnectionTimeoutEvent(Thread connectionThread, TcpClient connection) // zdarzanie występujące przy zbyt długej odpowiedzi na klienta
        {
            connection.Close();
            connectionThread.Abort();
        }

        private void WriteMessage(NetworkStream stream, string message)
        {
            byte[] responseBytes = Encoding.UTF8.GetBytes(message);
            stream.Write(responseBytes, 0, responseBytes.Length);
        }
    }
}
