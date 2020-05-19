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
        public AESKey PhysicalKey { get; set; }
        public SessionKey SessionKey { get; set; }

        public bool Initialized { get; set; }
        private RSAParameters RSAParams;
        private ConcurrentQueue<string> SendQueue;
        private ConcurrentQueue<string> ReceiveQueue;
        private bool IsRunning;
        private TcpClient Client;
        private NetworkStream Stream;
        private string Name;
        public EncryptionMode EncryptionMode { get; set; } = EncryptionMode.AES;

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

                //Client.ReceiveBufferSize = 10_048_576;
                //Client.SendBufferSize = 10_048_576;

                Task.Run(ListenToServer);
                Task.Run(ProcessIO);
                RSAParameters param;
                using (var rsa = new RSACryptoServiceProvider(512))
                {
                    param = rsa.ExportParameters(false);
                    this.RSAParams = rsa.ExportParameters(true);
                }
                using (var aes = new AesCryptoServiceProvider())
                {
                    this.PhysicalKey = new AESKey { IV = aes.IV, Key = aes.Key };
                }
                this.SessionKey = Helper.GenerateSessionKey();
                string encSession = Helper.AESEncrypt(JsonConvert.SerializeObject(this.SessionKey), this.PhysicalKey);
                this.SendMessage($"init{Helper.SocketMessageAttributeSeperator}{Name}{Helper.SocketMessageAttributeSeperator}{JsonConvert.SerializeObject(param)}{Helper.SocketMessageAttributeSeperator}{JsonConvert.SerializeObject(PhysicalKey)}{Helper.SocketMessageAttributeSeperator}{encSession}", EncryptionMode.RSA);

            }
            catch (Exception)
            {
            }

        }

        private void ListenToServer()
        {
            int i = 0;
            StringBuilder data = null;
            byte[] bytes = new byte[Client.ReceiveBufferSize / 2];

            try
            {
                IsRunning = true;
                while (IsRunning)
                {
                    try
                    {
                        if (Stream.DataAvailable)
                        {
                            data = new StringBuilder();
                            var str = "";
                            while ((Stream.DataAvailable || (str != string.Empty && !str.EndsWith(Helper.SocketMessageSplitter))) && (i = Stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                str = Encoding.Unicode.GetString(bytes, 0, i);
                                data.Append(str);
                            }
                            var splited = data.ToString().Split(new string[] { Helper.SocketMessageSplitter }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var item in splited)
                            {
                                if (item == null || item == string.Empty)
                                    continue;
                                ReceiveQueue.Enqueue(item);
                            }
                            splited = null;
                            data.Clear();
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

                        int maxLength = Client.SendBufferSize / 2;
                        int iterations = bs.Length / maxLength;
                        StringBuilder stringBuilder = new StringBuilder();
                        for (int ii = 0; ii <= iterations; ii++)
                        {


                            byte[] tempBytes = new byte[
                                    (bs.Length - maxLength * ii > maxLength) ? maxLength :
                                                  bs.Length - maxLength * ii];
                            Buffer.BlockCopy(bs, maxLength * ii, tempBytes, 0,
                                              tempBytes.Length);

                            Stream.Write(tempBytes, 0, tempBytes.Length);
                        }
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
                msg += Helper.MacSeperator + Helper.RSASign(msg, this.RSAParams);
                msg = Helper.RSAEncrypt(msg, this.ServerParams);
            }
            if (mode == EncryptionMode.AES)
            {
                msg += Helper.MacSeperator + Helper.RSASign(msg, this.RSAParams);
                var buffer = new StringBuilder();
                var maxSize = 100;
                int iters = (int)Math.Ceiling(1f * msg.Length / maxSize);

                for (var i = 0; i < iters; i++)
                {
                    string item;
                    if (i * maxSize + 100 > msg.Length)
                        item = msg.Substring(i * maxSize);
                    else
                        item = msg.Substring(i * maxSize, 100);
                    if (this.SessionKey.IsExpired)
                    {
                        buffer.Append(Helper.SessionKeySeperator);
                        this.SessionKey = Helper.GenerateSessionKey();
                        buffer.Append(Helper.AESEncrypt(JsonConvert.SerializeObject(this.SessionKey), this.PhysicalKey));
                        buffer.Append(Helper.SessionKeySeperator);
                        buffer.Append(Helper.AESChunkSeperator);
                    }
                    buffer.Append(Helper.AESEncrypt(item, this.SessionKey.AESKey));
                    buffer.Append(Helper.AESChunkSeperator);
                }
                msg = buffer.ToString();
            }

            this.SendQueue.Enqueue(encryptionIndicator + msg + Helper.SocketMessageSplitter);
        }

        public string Decrypt(string input)
        {
            if (input.StartsWith("non"))
            {
                return input.Substring(13);
            }
            if (input.StartsWith("rsa"))
            {
                input = input.Substring(13);
                input = Helper.RSADecrypt(input, this.RSAParams);
                var split = input.Split(new string[] { Helper.MacSeperator }, StringSplitOptions.RemoveEmptyEntries);
                var data = split[0];
                var hash = split[1];
                if (!Helper.RSAVerify(hash, data, this.ServerParams))
                    return "";
                return data;
            }
            else
            {
                input = input.Substring(13);
                var buffer = new StringBuilder();
                var aesChunk = input.Split(new string[] { Helper.AESChunkSeperator }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in aesChunk)
                {
                    if (item.StartsWith(Helper.SessionKeySeperator) && item.EndsWith(Helper.SessionKeySeperator))
                    {
                        var sepLen = Helper.SessionKeySeperator.Length;
                        var encSession = item.Substring(sepLen, item.Length - sepLen * 2);
                        this.SessionKey = JsonConvert.DeserializeObject<SessionKey>(Helper.AESDecrypt(encSession, this.PhysicalKey));
                        continue;
                    }
                    buffer.Append(Helper.AESDecrypt(item, this.SessionKey.AESKey));
                }
                input = buffer.ToString();
                var split = input.Split(new string[] { Helper.MacSeperator }, StringSplitOptions.RemoveEmptyEntries);
                var data = split[0];
                var hash = split[1];
                if (!Helper.RSAVerify(hash, data, this.ServerParams))
                    return "";
                return data;
            }
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

    }
}
