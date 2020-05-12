using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SecurityProject0_server
{
    class Client : IDisposable
    {

        public delegate void MessageHandler(string message, int id);

        public event MessageHandler OnIncommeingMessage;
        public event EventHandler OnDisconnect;
        public int Id { get; private set; }
        public bool IsRunning { get; private set; }
        public string Name { get; set; }

        private ConcurrentQueue<string> SendQueue;
        private ConcurrentQueue<string> ReceiveQueue;
        private NetworkStream Stream;
        private TcpClient SocketClient;
        private bool Disposed;

        public Client(TcpClient client, NetworkStream stream, int id)
        {
            Disposed = false;
            this.Stream = stream;
            this.SendQueue = new ConcurrentQueue<string>();
            this.ReceiveQueue = new ConcurrentQueue<string>();
            this.SocketClient = client;
            stream.ReadTimeout = 100;
            this.Id = id;
            Task.Run(Run);
            Task.Run(ProcessIO);
        }

        public void Run()
        {
            int i;
            string data = null;
            byte[] bytes = new byte[256];
            IsRunning = true;

            try
            {
                while (IsRunning)
                {
                    if (!SocketClient.Connected)
                        throw new IOException("Disconnected from remote");
                    try
                    {
                        if (Stream.DataAvailable)
                            while ((i = Stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                data = Encoding.Unicode.GetString(bytes, 0, i);
                                ReceiveQueue.Enqueue(data);
                            }
                        else
                        {
                            Task.Delay(100).Wait();
                        }


                    }
                    catch (IOException)
                    {
                    }
                    while (SendQueue.Count > 0)
                    {
                        SendQueue.TryDequeue(out var msg);
                        var bs = System.Text.Encoding.Unicode.GetBytes(msg);
                        Stream.Write(bs, 0, bs.Length);
                        Stream.FlushAsync().Wait();
                        Console.WriteLine($"Sent: {msg} to {this.Id}");
                    }
                }

            }
            catch (Exception)
            {
                Dispose();
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



                    while (ReceiveQueue.Count > 0)
                    {
                        ReceiveQueue.TryDequeue(out var msg);
                        OnIncommeingMessage?.Invoke(msg, Id);
                        Console.WriteLine("{0}@Received: {1}", Id, msg);
                    }


                }
                catch (Exception)
                {

                }
                finally
                {
                }
            }
        }

        public void Send(string msg)
        {
            this.SendQueue.Enqueue(msg + "|");
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }
            Disposed = true;
            IsRunning = false;
            SocketClient.Close();
            OnDisconnect.Invoke(this, EventArgs.Empty);

        }

    }
}
