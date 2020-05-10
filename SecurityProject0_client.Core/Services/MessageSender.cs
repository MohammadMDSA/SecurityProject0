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
        private Stream Stream;

        public MessageSender()
        {
            if (Instance != null)
                Instance.Dispose();
            Instance = this;
            SendQueue = new ConcurrentQueue<string>();
            ReceiveQueue = new ConcurrentQueue<string>();
            IsRunning = false;
        }

        public void Connect(string server, string message, int port = 13000)
        {
            try
            {

                Client = new TcpClient(server, port);

                byte[] data = Encoding.Unicode.GetBytes(message);


                Stream = Client.GetStream();

                Task.Run(ListenToServer);
                Task.Run(ProcessIO);
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
                while ((i = Stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    data = Encoding.Unicode.GetString(bytes, 0, i);
                    ReceiveQueue.Enqueue(data);
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
            try
            {

                while (IsRunning)
                {
                    if (SendQueue.Count > 0)
                    {
                        while (SendQueue.Count > 0)
                        {
                            SendQueue.TryDequeue(out var msg);
                            var bs = System.Text.Encoding.Unicode.GetBytes(msg);
                            Stream.Write(bs, 0, bs.Length);
                            Console.WriteLine("{Sent: {0}", msg);
                        }
                    }
                    else
                    {
                        Task.Delay(100);
                    }
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
            }
            catch (Exception) { }
            finally
            {
                IsRunning = false;
            }
        }

        public void SendMessage(string msg)
        {
            SendQueue.Enqueue(msg);
        }

        public void Dispose()
        {
            Stream.Close();
            Client.Dispose();
            IsRunning = false;
        }
    }
}
