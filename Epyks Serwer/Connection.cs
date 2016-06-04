using System;
using System.Linq;
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
        NetworkStream stream;
        Action onError;
        readonly string packetEndSign = "!$"; // znacznik końca pakietu
        string message;

        public Connection(TcpClient conncetion)
        {
            connection = conncetion;
            stream = connection.GetStream();
            Command = new Command(String.Empty);
            onError = Disconnect;
        }

        public Connection(Connection userConnection, Action onError)
        {
            connection = userConnection.connection;
            stream = userConnection.stream;
            Command = new Command(String.Empty);
            this.onError = onError;
        }

        public void ReceiveMessage(bool recurrentCall = false)
        {
            string[] messages = null;
            int i;
            Command = new Command(String.Empty);
            byte[] bytes = new byte[256];
            parameters = null;
            try
            {
                if ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    message += Encoding.UTF8.GetString(bytes, 0, i);
                    if (message.IndexOf(packetEndSign) == -1) // jeśli odczytana wiadomość nie zawiera znacznika końca wiadomości
                        ReceiveMessage(true);
                    if (recurrentCall)
                        return;
                    messages = SplitMessages(message);
                    message = messages[0];
                    string[] args = Regex.Split(message, ";"); // automatyczny podział komunikatu na argumenty
                    ReplaceInArray(args, "%1", ";");
                    if (args.Length > 1)
                    {
                        parameters = new string[args.Length - 1];
                        for (int j = 1; j < args.Length; j++)
                            parameters[j - 1] = args[j].Trim();
                    }
                    if (args.Length > 0)
                    {
                        if (parameters != null && parameters.Length > 0)
                            Command = new Command(args[0].Trim(), parameters.Length);
                        else
                            Command = new Command(args[0].Trim());
                    }
                    if (messages.Length == 2)
                        message = messages[1].TrimStart();
                    else
                        message = String.Empty;
                }
            }
            catch
            {
                onError();
            }
        }

        public void SendMessage(Command command, params string[] parameters)
        {
            if (parameters.Length < command.ParametersCount) // jeśli liczba parametrów nie spełnia wymogów komendy
            {
                Console.WriteLine("Komenda '{0}' nie spełnia wymaganych warunków. Wysyłanie nie powiodło się.", command.Text);
                return;
            }
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = parameters[i].Trim();
            ReplaceInArray(parameters, ";", "%1");
            string msg = String.Join(";", command.Text, String.Join(";", parameters)) + packetEndSign;
            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            try
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            catch
            {
                onError();
                return;
            }
        }

        private void ReplaceInArray(string[] array, string replaceFrom, string replaceTo)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = array[i].Replace(replaceFrom, replaceTo);
        }

        private string[] SplitMessages(string message)
        {
            string[] array;
            int index = message.IndexOf(packetEndSign);
            if (index == -1)
            {
                array = new string[1];
                array[0] = message;
                return array;
            }
            array = new string[2];
            array[0] = message.Substring(0, index);
            array[1] = message.Substring(index + packetEndSign.Length);
            return array;
        }

        public void Disconnect()
        {
            stream.Close();
            connection.Close();
        }

        public string GetIP()
        {
            IPEndPoint remoteIpEndPoint = connection.Client.RemoteEndPoint as IPEndPoint;
            string ip = remoteIpEndPoint.Address.ToString();
            if (ip == "127.0.0.1")
            {
                return LocalIPAddress();
            }
            return ip;
        }

        private string LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
        }

        public string this[int index]
        {
            get
            {
                if (parameters == null || index >= parameters.Length)
                    return null;
                return parameters[index];
            }
        }
    }
}
