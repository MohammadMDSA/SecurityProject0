using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SecurityProject0_server
{

    class TcpListener : IDisposable
    {
        public static TcpListener Instance { get; private set; }

        private Dictionary<int, Client> Clients;
        public bool IsRunning { get; private set; }

        public TcpListener()
        {
            if (Instance != null)
                Instance.Dispose();
            Instance = this;

            Clients = new Dictionary<int, Client>();
            IsRunning = true;
        }

        public void Run()
        {
            System.Net.Sockets.TcpListener server = null;
            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = 13000;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener server = new TcpListener(port);
                server = new System.Net.Sockets.TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;

                // Enter the listening loop.
                while (IsRunning)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");


                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();
                    int id = Clients.Count;
                    var c = new Client(client, stream, id);
                    c.OnIncommeingMessage += Parser;
                    Clients.Add(id, c);

                    //int i;

                    //// Loop to receive all the data sent by the client.
                    //while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    //{
                    //    // Translate data bytes to a ASCII string.
                    //    data = System.Text.Encoding.Unicode.GetString(bytes, 0, i);
                    //    Console.WriteLine("Received: {0}", data);

                    //    // Process the data sent by the client.
                    //    data = data.ToUpper();

                    //    byte[] msg = System.Text.Encoding.Unicode.GetBytes(data);

                    //    // Send back a response.
                    //    stream.Write(msg, 0, msg.Length);
                    //    Console.WriteLine("Sent: {0}", data);
                    //}

                    //// Shutdown and end connection
                    //client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                IsRunning = false;
                // Stop listening for new clients.
                server.Stop();
                foreach (var item in Clients)
                {
                    item.Value.Dispose();
                }
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        private void Parser(string message, int id)
        {
            if (message == null || message == "")
                return;
            var splited = message.Split('@');
            switch (splited[0].ToLower())
            {
                case "init":
                    if (splited.Length < 2 || splited[1] == "")
                        return;
                    InitClient(id, splited[1]);
                    break;
                default:
                    break;
            }
        }

        private void InitClient(int id, string name)
        {
            if (!Clients.ContainsKey(id))
                return;
            var cl = Clients[id];
            cl.Name = name;
            foreach (var item in Clients)
            {
                if (item.Key != id)
                    cl.Send($"user@{item.Key}@{item.Value.Name}");
            }
        }

        public void Dispose()
        {
            this.IsRunning = false;
        }
    }
}
