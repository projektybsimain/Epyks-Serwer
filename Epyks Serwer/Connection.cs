using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Epyks_Serwer
{
    public class Connection
    {
        public string Command { get; private set; }
        public string[] Parameters { get; private set; }
        TcpClient connection;
        UdpClient udpClient;
        NetworkStream stream;
        Action onError;

        public Connection(TcpClient client, int commandsPort, Action onError)
        {
            connection = client;
            stream = connection.GetStream();
            Command = String.Empty;
            this.onError = onError;
            string IP = ((IPEndPoint)connection.Client.RemoteEndPoint).Address.ToString();
            udpClient = new UdpClient(IP, commandsPort);
        }

        public void ReceiveMessage()
        {
            int i;
            byte[] bytes = new byte[256];
            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                string msg = Encoding.ASCII.GetString(bytes, 0, i);
                string[] args = Regex.Split(msg, ";"); // automatyczny podział komunikatu na argumenty
                if (args.Length > 1)
                {
                    Parameters = new string[args.Length - 1];
                    for (int j = 1; j < args.Length; j++)
                        Parameters[j - 1] = args[j].Replace("&sem", ";");
                }
                if (args.Length > 0)
                    Command = args[0].Trim();
                else
                    Command = String.Empty;
                return;
            }
        }

        public void SendMessage(params string[] message)
        {
            for (int i = 0; i < message.Length; i++)
                message[i] = message[i].Replace(";", "&sem"); // usuwa średnik z wiadomości ze względu na ich użycie przy podziale komunikatów
            byte[] bytes = Encoding.ASCII.GetBytes(String.Join(";", message));
            try
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            catch
            {
                onError();
            }
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
    }
}
