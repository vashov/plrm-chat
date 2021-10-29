using Plrm.Chat.Shared.Models;
using Plrm.Chat.Shared.Validators;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Plrm.Chat.Client
{
    class Program
    {
        static string _login = null;
        static string _password = null;

        static bool? _loggedInToChat = null;
        static string _errorMsg = null;

        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            int port = 5000;

            TcpClient client = null;
            NetworkStream ns = null;
            Thread thread = null; 
            try
            {
                while (_loggedInToChat != true)
                {
                    InputCredentials();

                    client = new TcpClient();
                    client.Connect(ip, port);

                    Console.WriteLine("Connected");

                    ns = client.GetStream();
                    thread = new Thread(o => ReceiveData((TcpClient)o));
                    thread.Start(client);

                    var successLogin = LogInToChat(ns);
                    if (!successLogin.Value)
                    {
                        Console.WriteLine($"Authorization error: {_errorMsg}");

                        //client.Client.Shutdown(SocketShutdown.Send);
                        //thread.Join();
                        client.Close();
                        Console.WriteLine("Disconnected from server");
                    }
                }

                string s;
                while (!string.IsNullOrEmpty((s = Console.ReadLine())))
                {
                    try
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(s);
                        ns.Write(buffer, 0, buffer.Length);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e}");
                        break;
                    }
                }

            }
            finally
            {
                client?.Client.Shutdown(SocketShutdown.Send);
                thread?.Join();
                client?.Close();
                Console.WriteLine("Disconnected from server");
            }

            Console.WriteLine("Enter any KEY to exit...");
            Console.ReadKey();
        }

        static void InputCredentials()
        {
            _login = null;
            _password = null;

            while (!UserCredentialsValidator.IsLoginValid(_login))
            {
                Console.Write("Enter Login: ");
                _login = Console.ReadLine();
            }

            while (!UserCredentialsValidator.IsPasswordValid(_password))
            {
                Console.Write("Enter password: ");
                _password = Console.ReadLine();
            }
        }

        static bool? LogInToChat(NetworkStream stream)
        {
            var userCredentials = new UserCredentials
            {
                Login = _login,
                Password = _password
            };

            var json = JsonSerializer.Serialize(userCredentials);
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                stream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e}");
                return false;
            }

            while (_loggedInToChat == null)
            {
                Console.WriteLine("Wait authorization response ... ");
                Thread.Sleep(100);
            }

            return _loggedInToChat;
        }

        static void ReceiveData(TcpClient client)
        {
            try
            {
                NetworkStream ns = client.GetStream();
                byte[] receivedBytes = new byte[1024];
                int byte_count;

                while (client.Connected && ns.CanRead)
                {
                    if (!((byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0))
                    {
                        return;
                    }

                    if (_loggedInToChat != true)
                    {
                        var json = Encoding.UTF8.GetString(receivedBytes, 0, byte_count);
                        var authResponse = JsonSerializer.Deserialize<AuthorizationResponse>(json);
                        if (authResponse.IsOk)
                        {
                            _loggedInToChat = true;
                            continue;
                        }

                        _errorMsg = authResponse.Error;
                        _loggedInToChat = false;
                        continue;
                    }
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
