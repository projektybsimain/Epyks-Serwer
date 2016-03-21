using System;

namespace Epyks_Serwer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Epyks Serwer ===");
            int serverPort = 9000;
            Worker worker = null;
            try
            {
                worker = new Worker(serverPort);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Nie udało się uruchomić serwera: " + ex.Message);
                Console.ReadKey();
                return;
            }
            Console.WriteLine("Serwer uruchomiono pomyślnie na porcie {0}", serverPort);
            Console.ReadKey();
        }
    }
}
