using SecurityProject0_client.Helpers;
using SecurityProject0_shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SecurityProject0_client.Core.Services
{
    public class MessageSender : IDisposable
    {

        public delegate void MessageHandler(string message);

        public static event MessageHandler OnIncommeingMessage;

        public static MessageSender Instance { get; private set; }

        private ConcurrentQueue<string> SendQueue;
        private ConcurrentQueue<string> ReceiveQueue;
        private bool IsRunning;
        private TcpClient Client;
        private NetworkStream Stream;
        private string Name;

        public MessageSender(string name)
        {
            if (Instance != null)
                Instance.Dispose();
            Instance = this;
            SendQueue = new ConcurrentQueue<string>();
            ReceiveQueue = new ConcurrentQueue<string>();
            IsRunning = false;
            this.Name = name;
        }

        public void Connect(string server, int port = 13000)
        {
            try
            {

                Client = new TcpClient(server, port);


                Stream = Client.GetStream();
                Stream.ReadTimeout = 100;

                Client.ReceiveBufferSize = 1_048_576;
                Client.SendBufferSize = 1_048_576;

                Task.Run(ListenToServer);
                Task.Run(ProcessIO);

                this.SendMessage($"init{Helper.SocketMessageSeperator}{Name}");
            }
            catch (Exception)
            {
            }

        }

        private void ListenToServer()
        {
            int i = 0;
            string data = null;
            byte[] bytes = new byte[256];

            try
            {
                IsRunning = true;
                while (IsRunning)
                {
                    try
                    {
                        if (Stream.DataAvailable)
                        {
                            data = "";
                            while (Stream.DataAvailable && (i = Stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                data += Encoding.Unicode.GetString(bytes, 0, i);
                            }
                            var splited = data.Split('|');
                            foreach (var item in splited)
                            {
                                if (item == null || item == string.Empty)
                                    continue;
                                ReceiveQueue.Enqueue(item);
                            }
                        }
                        else
                            Task.Delay(100).Wait();


                    }
                    catch (IOException)
                    {
                    }
                    while (SendQueue.Count > 0)
                    {
                        SendQueue.TryDequeue(out var msg);
                        var bs = System.Text.Encoding.Unicode.GetBytes(msg);
                        Stream.Write(bs, 0, bs.Length);
                        Stream.Flush();
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                IsRunning = false;
            }
        }



        public void ProcessIO()
        {
            IsRunning = true;
            while (IsRunning)
            {
                try

                {

                    if (ReceiveQueue.Count > 0)
                    {
                        while (ReceiveQueue.Count > 0)
                        {
                            ReceiveQueue.TryDequeue(out var msg);
                            OnIncommeingMessage?.Invoke(msg);
                            Console.WriteLine("Received: {0}", msg);
                        }
                    }

                }
                catch (Exception) { }
            }
        }

        public void SendMessage(string msg)
        {
            SendQueue.Enqueue(msg + "|");
        }

        public void Dispose()
        {
            Stream.Close();
            Client.Dispose();
            IsRunning = false;
        }

        public static void Init(object sender, EventArgs e)
        {
            var ee = e as LoginEventArg;
            var send = new MessageSender(ee.Name);
            send.Connect("127.0.0.1");
        }
    }
}
