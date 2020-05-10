using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SecurityProject0_server
{
    class Client : IDisposable
    {

        public delegate void MessageHandler(string message, int id);

        public event MessageHandler OnIncommeingMessage;
        public int Id { get; private set; }
        public bool IsRunning { get; private set; }
        public string Name { get; set; }

        private Queue<string> SendQueue;
        private Queue<string> ReceiveQueue;
        private NetworkStream Stream;
        private TcpClient SocketClient;

        public Client(TcpClient client, NetworkStream stream, int id)
        {
            this.Stream = stream;
            this.SendQueue = new Queue<string>();
            this.ReceiveQueue = new Queue<string>();
            this.SocketClient = client;
            Task.Run(Run);
            Task.Run(ProcessIO);
        }

        public void Run()
        {
            IsRunning = true;
            int i;
            string data = null;
            byte[] bytes = new byte[256];

            try
            {
                while ((i = Stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.Unicode.GetString(bytes, 0, i);
                    Console.WriteLine("{0}@Received: {1}", Id, data);
                    ReceiveQueue.Enqueue(data);
                    //OnIncommeingMessage?.Invoke(data, Id);
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
            try
            {

                while (IsRunning)
                {
                    if (SendQueue.Count > 0)
                    {
                        while (SendQueue.Count > 0)
                        {
                            var msg = SendQueue.Dequeue();
                            var bs = System.Text.Encoding.Unicode.GetBytes(msg);
                            Stream.Write(bs, 0, bs.Length);
                            Console.WriteLine("{0}@Sent: {1}", Id, msg);
                        }
                    }
                    else
                    {
                        Task.Delay(100);
                    }

                }
            }
            catch (Exception) { }
            finally
            {
                IsRunning = false;
            }
        }

        public void Send(string msg)
        {
            if (!IsRunning)
                return;
            this.SendQueue.Enqueue(msg);
        }

        public void Dispose()
        {
            IsRunning = false;
            SocketClient.Close();

        }

    }
}
