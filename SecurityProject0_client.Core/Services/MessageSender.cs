using Newtonsoft.Json;
using SecurityProject0_client.Helpers;
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
using System.Xml.Serialization;

namespace SecurityProject0_client.Core.Services
{
    public class MessageSender : IDisposable
    {

        public delegate void MessageHandler(string message);

        public static event MessageHandler OnIncommeingMessage;

        public static MessageSender Instance { get; private set; }
        public RSAParameters ServerParams { get; set; }
        public bool Initialized { get; set; }
        private RSAParameters RSAParams;
        private ConcurrentQueue<string> SendQueue;
        private ConcurrentQueue<string> ReceiveQueue;
        private bool IsRunning;
        private TcpClient Client;
        private NetworkStream Stream;
        private string Name;
        private EncryptionMode EncryptionMode = EncryptionMode.RSA;

        public MessageSender(string name)
        {
            if (Instance != null)
                Instance.Dispose();
            Instance = this;
            SendQueue = new ConcurrentQueue<string>();
            ReceiveQueue = new ConcurrentQueue<string>();
            IsRunning = false;
            this.Name = name;
            this.Initialized = false;
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

                using (var rsa = new RSACryptoServiceProvider())
                {
                    var param = rsa.ExportParameters(false);
                    this.RSAParams = rsa.ExportParameters(true);
                    this.SendMessage($"init{Helper.SocketMessageAttributeSeperator}{Name}{Helper.SocketMessageAttributeSeperator}{JsonConvert.SerializeObject(param)}", EncryptionMode.RSA);
                }

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
                            var splited = data.Split(Helper.SocketMessageSplitter.ToCharArray());
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

                            msg = Decrypt(msg);

                            OnIncommeingMessage?.Invoke(msg);
                            Console.WriteLine("Received: {0}", msg);
                        }
                    }

                }
                catch (Exception) { }
            }
        }

        public async void SendMessage(string msg, EncryptionMode mode)
        {
            while (!Initialized)
                await Task.Delay(100);
            string encryptionIndicator = "";
            switch (mode)
            {
                case EncryptionMode.None:
                    encryptionIndicator = "non";
                    break;
                case EncryptionMode.RSA:
                    encryptionIndicator = "rsa";
                    break;
                case EncryptionMode.AES:
                    encryptionIndicator = "aes";
                    break;
                default:
                    break;
            }
            encryptionIndicator += Helper.SocketMessageAttributeSeperator;

            if (mode == EncryptionMode.RSA)
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(this.ServerParams);
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

        public void SendMessage(string msg)
        {
            SendMessage(msg, EncryptionMode);
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

        private string Decrypt(string input)
        {
            if (input.StartsWith("non"))
            {
                return input.Substring(13);
            }
            if (input.StartsWith("rsa"))
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(this.RSAParams);
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
            }
            return "";
        }
    }
}
