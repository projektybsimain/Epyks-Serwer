using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Epyks_Serwer
{
    public class Connection
    {
        public Command Command { get; private set; }
        string[] parameters;
        TcpClient connection;
        UdpClient udpClient;
        NetworkStream stream;
        Action onError;
        bool isLongMsg; // zabezpiecza przed atakiem poprzez przepełnienie stosu

        public Connection(TcpClient conncetion)
        {
            connection = conncetion;
            stream = connection.GetStream();
            Command = new Command(String.Empty);
            onError = Disconnect;
        }

        public Connection(Connection userConnection, int commandsPort, Action onError)
        {
            connection = userConnection.connection;
            stream = userConnection.stream;
            Command = new Command(String.Empty);
            this.onError = onError;
            string IP = ((IPEndPoint)connection.Client.RemoteEndPoint).Address.ToString();
            udpClient = new UdpClient(IP, commandsPort);
            isLongMsg = false;
        }

        public void ReceiveMessage(int bufferSize = 256)
        {
            int i;
            byte[] bytes = new byte[bufferSize];
            try
            {
                parameters = null;
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    string msg = Encoding.UTF8.GetString(bytes, 0, i);
                    string[] args = Regex.Split(msg, ";"); // automatyczny podział komunikatu na argumenty
                    if (args.Length > 1)
                    {
                        parameters = new string[args.Length - 1];
                        for (int j = 1; j < args.Length; j++)
                            parameters[j - 1] = args[j];
                    }
                    if (args.Length > 0)
                    {
                        if (parameters != null && parameters.Length > 0)
                            Command = new Command(args[0].Trim(), parameters.Length);
                        else
                            Command = new Command(args[0].Trim());
                        if (!isLongMsg && Command == CommandSet.LongMessage)
                        {
                            ProcessLongMessage();
                            isLongMsg = false;
                        }
                    }
                    else
                        Command = new Command(String.Empty);
                    return;
                }
            }
            catch
            {
                onError();
            }
        }

        private void ProcessLongMessage()
        {
            isLongMsg = true;
            int size;
            if (Int32.TryParse(parameters[0], out size) && size > 256)
                ReceiveMessage(size);
            else
                ReceiveMessage();
        }

        public void SendMessage(Command command, params string[] parameters)
        {
            if (command.ParametersCount > 0 && command.ParametersCount != parameters.Length) // zabezpiecza przed wysłaniem niekomatybilnego komunikatu
                throw new ArgumentOutOfRangeException("Command", "Command has less or more parameters than required");
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = parameters[i]; // usuwa średnik z wiadomości ze względu na ich użycie przy podziale komunikatów
            byte[] bytes = Encoding.UTF8.GetBytes(String.Join(";", command.Text, String.Join(";", parameters)));
            try
            {
                if (bytes.Length > 256)
                {
                    NotifyLongMessage(bytes.Length);
                }
                stream.Write(bytes, 0, bytes.Length);
            }
            catch
            {
                onError();
                return;
            }
        }

        private void NotifyLongMessage(int size)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(CommandSet.LongMessage + ";" + size);
            stream.Write(bytes, 0, bytes.Length);
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

        public string GetIPString()
        {
            IPEndPoint remoteIpEndPoint = connection.Client.RemoteEndPoint as IPEndPoint;
            return remoteIpEndPoint.Address + ":" + remoteIpEndPoint.Port;
        }

        public string this[int index]
        {
            get
            {
                if (parameters == null || index >= parameters.Length)
                    return null;
                if (index == parameters.Length - 1)
                    return parameters[index].Trim(); // pozbywamy się pustych znaków będących pozostałością bufora
                return parameters[index];
            }
        }
    }
}
