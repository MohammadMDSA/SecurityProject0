using Newtonsoft.Json;
using SecurityProject0_shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
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
        public RSAParameters RSAParameters { get; set; }
        private ConcurrentQueue<string> SendQueue;
        private ConcurrentQueue<string> ReceiveQueue;
        private NetworkStream Stream;
        private TcpClient SocketClient;
        private bool Disposed;
        private EncryptionMode EncryptionMode = EncryptionMode.RSA;

        public Client(TcpClient client, NetworkStream stream, int id)
        {
            Disposed = false;
            this.Stream = stream;
            this.SendQueue = new ConcurrentQueue<string>();
            this.ReceiveQueue = new ConcurrentQueue<string>();
            this.SocketClient = client;
            stream.ReadTimeout = 100;
            this.Id = id;
            client.ReceiveBufferSize = 1_048_576;
            client.SendBufferSize = 1_048_576;
            Task.Run(Run);
            Task.Run(ProcessIO);
        }

        public void Run()
        {
            int i;
            string data = null;
            byte[] bytes = new byte[256];
            IsRunning = true;
            Send($"id{Helper.SocketMessageAttributeSeperator}{this.Id}{Helper.SocketMessageAttributeSeperator}{JsonConvert.SerializeObject(TcpListener.Instance.RSAPublicParameters)}", EncryptionMode.None);
            try
            {
                while (IsRunning)
                {
                    if (!SocketClient.Connected)
                        throw new IOException("Disconnected from remote");
                    try
                    {
                        if (Stream.DataAvailable)
                        {
                            data = "";
                            while (Stream.DataAvailable && (i = Stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                data += Encoding.Unicode.GetString(bytes, 0, i);
                            }
                            var splited = data.Split(Helper.SocketMessageSplitter);
                            foreach (var item in splited)
                            {
                                if (item == null || item == string.Empty)
                                    continue;
                                ReceiveQueue.Enqueue(item);
                            }
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
                        msg = Decrypt(msg);
                        OnIncommeingMessage?.Invoke(msg, Id);
                        Console.WriteLine("{0}{1}Received: {2}", Id, Helper.SocketMessageAttributeSeperator, msg);
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

        public void Send(string msg, EncryptionMode mode)
        {
            string encryptionIndicator = mode switch
            {
                EncryptionMode.AES => "aes",
                EncryptionMode.None => "non",
                EncryptionMode.RSA => "rsa",
                _ => ""
            } + Helper.SocketMessageAttributeSeperator;

            if (mode == EncryptionMode.RSA)
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(this.RSAParameters);
                    var byteConverter = new UnicodeEncoding();
                    string res = "";
                    int maxLength = (128 - 44) / 2;
                    var bytes = byteConverter.GetBytes(msg);
                    int iterations = bytes.Length / maxLength;
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 0; i <= iterations; i++)
                    {
                        byte[] tempBytes = new byte[
                                (bytes.Length - maxLength * i > maxLength) ? maxLength :
                                              bytes.Length - maxLength * i];
                        Buffer.BlockCopy(bytes, maxLength * i, tempBytes, 0,
                                          tempBytes.Length);
                        byte[] encryptedBytes = rsa.Encrypt(tempBytes, true);
                        Array.Reverse(encryptedBytes);
                        stringBuilder.Append(Convert.ToBase64String(encryptedBytes));
                    }
                    msg = stringBuilder.ToString(); ;
                }
            }
            this.SendQueue.Enqueue(encryptionIndicator + msg + Helper.SocketMessageSplitter);
        }

        public void Send(string msg)
        {
            Send(msg, EncryptionMode);
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

        private string Decrypt(string input)
        {
            if (input.StartsWith("non"))
            {
                return input.Substring(13);
            }
            if (input.StartsWith("rsa"))
            {
                using var rsa = new RSACryptoServiceProvider();
                rsa.ImportParameters(TcpListener.Instance.RSAPrivateParameters);
                var msg = input.Substring(13);
                string res = "";
                var byteConverter = new UnicodeEncoding();
                byte[] bytes = new byte[128];
                var dwKeySize = rsa.KeySize;
                int base64BlockSize = ((dwKeySize / 8) % 3 != 0) ? (((dwKeySize / 8) / 3) * 4) + 4 : ((dwKeySize / 8) / 3) * 4;
                int iterations = msg.Length / base64BlockSize;
                for (int i = 0; i < iterations; i++)
                {
                    byte[] encryptedBytes = Convert.FromBase64String(
                         msg.Substring(base64BlockSize * i, base64BlockSize));
                    Array.Reverse(encryptedBytes);
                    res += byteConverter.GetString(rsa.Decrypt(encryptedBytes, true));
                }
                return res;
            }
            return "";
        }

    }
}
