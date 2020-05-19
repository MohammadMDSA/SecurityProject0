using Newtonsoft.Json;
using SecurityProject0_shared.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace SecurityProject0_server
{

    class TcpListener : IDisposable
    {
        public static TcpListener Instance { get; private set; }
        public RSAParameters RSAPublicParameters { get; private set; }
        public RSAParameters RSAPrivateParameters { get; private set; }
        public SessionKey SessionKey { get; set; }
        private Dictionary<int, Client> Clients;
        private short IdCounter;
        public bool IsRunning { get; private set; }

        public TcpListener()
        {
            if (Instance != null)
                Instance.Dispose();
            Instance = this;
            IdCounter = 0;
            Clients = new Dictionary<int, Client>();
            IsRunning = true;
            using (var rsa = new RSACryptoServiceProvider(512))
            {
                this.RSAPublicParameters = rsa.ExportParameters(false);
                this.RSAPrivateParameters = rsa.ExportParameters(true);
            }
            this.SessionKey = new SessionKey { ExpirationDate = new DateTime(0) };
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

                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();


                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();
                    int id = IdCounter;
                    IdCounter++;
                    var c = new Client(client, stream, id);
                    c.OnIncommeingMessage += Parser;
                    c.OnDisconnect += OnClientDiconnect;
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

        private void OnClientDiconnect(object sender, EventArgs e)
        {
            Disconnect((sender as Client).Id);
        }

        private void Disconnect(int id)
        {
            Clients[id].Dispose();
            Clients.Remove(id);
            foreach (var item in Clients)
            {
                item.Value.Send($"remove{Helper.SocketMessageAttributeSeperator}{id}");
            }
            Console.WriteLine($"{id} Disconnected");

        }

        private void Parser(string message, int id)
        {
            if (message == null || message == "")
                return;
            var splited = message.Split(Helper.SocketMessageAttributeSeperator);
            switch (splited[0].ToLower())
            {
                case "init":
                    if (splited.Length < 5 || splited[1] == "")
                        return;
                    InitClient(id, splited[1], splited[2], splited[3], splited[4]);
                    break;
                case "disconnect":
                    Disconnect(id);
                    break;

                case "message":
                    if (splited.Length < 4)
                        return;
                    if (!int.TryParse(splited[1], out var receiver) || !long.TryParse(splited[3], out var ticks))
                        return;
                    SendMessage(receiver, id, splited[2], ticks);
                    break;
                case "file":
                    if (splited.Length < 4)
                        return;
                    if (!int.TryParse(splited[1], out receiver) || !long.TryParse(splited[3], out ticks))
                        return;
                    SendMessage(receiver, id, splited[2], ticks, true);
                    break;
                case "encryption":
                    if (splited.Length < 2)
                        return;
                    SetEncryptionType(id, splited[1]);
                    break;
                default:
                    break;
            }
        }

        private void SetEncryptionType(int id, string mode)
        {
            var client = Clients[id];
            EncryptionMode mo;
            if (mode == "sym")
                mo = EncryptionMode.AES;
            else
                mo = EncryptionMode.RSA;
            client.EncryptionMode = mo;
        }

        private void SendMessage(int receiver, int sender, string message, long time, bool isFile = false)
        {
            var command = isFile ? "file" : "message";
            var ren = Clients[sender];
            var res = Clients[receiver];
            var msg = $"{command}{Helper.SocketMessageAttributeSeperator}{receiver}{Helper.SocketMessageAttributeSeperator}{sender}{Helper.SocketMessageAttributeSeperator}{message}{Helper.SocketMessageAttributeSeperator}{time}";
            ren.Send(msg);
            res.Send(msg);
        }

        private void InitClient(int id, string name, string rsaParamStr, string physicalKey, string sessionStr)
        {
            if (!Clients.ContainsKey(id))
                return;
            var param = JsonConvert.DeserializeObject<RSAParameters>(rsaParamStr);
            var phys = JsonConvert.DeserializeObject<AESKey>(physicalKey);
            var sessionKey = JsonConvert.DeserializeObject<SessionKey>(Helper.AESDecrypt(sessionStr, phys));
            var cl = Clients[id];
            cl.Name = name;
            cl.RSAParameters = param;
            cl.PhysicalKey = phys;
            cl.SessionKey = sessionKey;
            foreach (var item in Clients)
            {
                if (item.Key != id)
                {
                    item.Value.Send($"user{Helper.SocketMessageAttributeSeperator}{id}{Helper.SocketMessageAttributeSeperator}{name}");
                    cl.Send($"user{Helper.SocketMessageAttributeSeperator}{item.Key}{Helper.SocketMessageAttributeSeperator}{item.Value.Name}");
                }
            }
        }

        public void Dispose()
        {
            this.IsRunning = false;
        }
    }
}
