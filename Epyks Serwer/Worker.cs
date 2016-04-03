using Ekyps_Serwer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Epyks_Serwer
{
    class Worker
    {
        TcpListener server;
        Thread listener;
        List<User> usersOnline; // użytkownicy aktualnie zalogowani do serwera (online)
        Database database;

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
            database = new Database();
            usersOnline = new List<User>();
            listener = new Thread(() => Listen(port));
            listener.Start();
        }

        private void AcceptSession(Thread connectionThread, TcpClient userConnection)
        {
            System.Timers.Timer timeout = new System.Timers.Timer(5000); // timer ustawiony na 5 sekund, tyle czasu ma klient na przesłanie danych
            timeout.Elapsed += delegate { onConnectionTimeoutEvent(connectionThread, userConnection); };
            timeout.Start();
            NetworkStream stream = userConnection.GetStream();
            byte[] data = new byte[128]; // bufor do odbioru danych
            int count = stream.Read(data, 0, data.Length);
            if (count != 0)
            {
                string message = Encoding.UTF8.GetString(data, 0, count);
                string[] parameters = Regex.Split(message, ";"); // z otrzymanej wiadomości odczytujemy login oraz hasło
                if (parameters.Length == 3) // spodziewamy się tylko trzech parametrów, w innym przypadku kończymy połączenie
                {
                    User user;
                    NetworkCredential userCredential;
                    try
                    {
                        userCredential = new NetworkCredential(parameters[1], parameters[2]);
                    }
                    catch // jeśli dane podane przez klienta nie są prawidłowe
                    {
                        userConnection.Close();
                        return;
                    }

                    if (!Command.IsKnownCommand(parameters[0]))
                    {
                        userConnection.Close();
                        return;
                    }

                    bool isNewUser = parameters[0] == Command.Register;
                    string response = null;

                    bool isValidUser;

                    lock (ThreadSync.Lock)
                    {
                        isValidUser = database.TryGetUser(out user, userCredential, out response, isNewUser); // sprawdzamy czy użytkownik istnieje w bazie danych oraz czy podał prawidłowe hasło
                    }

                    if (!isValidUser)
                    {
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length); // odsyłamy informację do klienta, np. że dany użytkownik nie istnieje, albo nie można kogoś zalogować ponieważ dany login jest już zajęty
                        userConnection.Close();
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Zalogowano użytkownika: " + parameters[1]);
                        // przesyłamy referencje do danych które nie są znane bazie danych
                        user.SetConnection(userConnection);
                        user.SetDatabase(database);
                        user.SetUsers(usersOnline);
                        usersOnline.Add(user); // odnotowujemy że dany użytkownik stał się online 
                        user.DoWork(); // dalsza obsługa klienta
                    }
                }
                else
                {
                    userConnection.Close();
                    return;
                }
            }
            else
                userConnection.Close();
        }

        private void onConnectionTimeoutEvent(Thread connectionThread, TcpClient connection) // zdarzanie występujące przy zbyt długej odpowiedzi na klienta
        {
            connection.Close();
            connectionThread.Abort();
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
    }
}
