using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Plrm.Chat.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            int port = 5000;

            using TcpClient client = new TcpClient();
            client.Connect(ip, port);

            Console.WriteLine("Connected");
            using NetworkStream ns = client.GetStream();

            Thread thread = new Thread(o => ReceiveData((TcpClient)o));
            thread.Start(client);

            string s;
            while (!string.IsNullOrEmpty((s = Console.ReadLine())))
            {
                try
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(s);
                    ns.Write(buffer, 0, buffer.Length);

                }
                catch(Exception e)
                {
                    Console.WriteLine($"{e}");
                    break;
                }
            }

            client.Client.Shutdown(SocketShutdown.Send);
            thread.Join();

            Console.WriteLine("Disconnected from server");

            Console.WriteLine("Enter any KEY to exit...");
            Console.ReadKey();
        }

        static void ReceiveData(TcpClient client)
        {
            try
            {
                NetworkStream ns = client.GetStream();
                byte[] receivedBytes = new byte[1024];
                int byte_count;

                while ((byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0)
                {
                    Console.WriteLine(Encoding.UTF8.GetString(receivedBytes, 0, byte_count));
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"{e}");
            }
            
        }
    }
}
