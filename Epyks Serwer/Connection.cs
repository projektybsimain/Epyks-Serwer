using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

namespace Epyks_Serwer
{
    public class Connection
    {
        public string Command { get; private set; }
        string[] parameters;
        TcpClient connection;
        UdpClient udpClient;
        NetworkStream stream;
        Action onError;
        Timer timeoutTimer;

        public Connection(TcpClient client, int commandsPort, Action onError, Timer timeoutTimer)
        {
            connection = client;
            stream = connection.GetStream();
            Command = String.Empty;
            this.onError = onError;
            string IP = ((IPEndPoint)connection.Client.RemoteEndPoint).Address.ToString();
            udpClient = new UdpClient(IP, commandsPort);
            this.timeoutTimer = timeoutTimer;
        }

        public void ReceiveMessage()
        {
            int i;
            byte[] bytes = new byte[256];
            try
            {
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    string msg = Encoding.ASCII.GetString(bytes, 0, i);
                    string[] args = Regex.Split(msg, ";"); // automatyczny podział komunikatu na argumenty
                    if (args.Length > 1)
                    {
                        parameters = new string[args.Length - 1];
                        for (int j = 1; j < args.Length; j++)
                            parameters[j - 1] = args[j].Replace("&sem", ";");
                    }
                    if (args.Length > 0)
                        Command = args[0].Trim();
                    else
                        Command = String.Empty;
                    timeoutTimer.Stop(); // otrzymanie odpowiedzi od klienta oznacza że jest on wciąż połączony dlatego resetujemy timeout
                    timeoutTimer.Start();
                    return;
                }
            }
            catch
            {
                onError();
            }
        }

        public void SendMessage(params string[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = parameters[i].Replace(";", "&sem"); // usuwa średnik z wiadomości ze względu na ich użycie przy podziale komunikatów
            byte[] bytes = Encoding.ASCII.GetBytes(String.Join(";", parameters));
            try
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            catch
            {
                onError();
                return;
            }
            timeoutTimer.Stop();
            timeoutTimer.Start();
        }

        public void SendMessageUDP(string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            udpClient.Send(bytes, bytes.Length);
        }

        public void Disconnect()
        {
            stream.Close();
            connection.Close();
        }

        public string this[int index]
        {
            get
            {
                return parameters[index];
            }
        }
    }
}
