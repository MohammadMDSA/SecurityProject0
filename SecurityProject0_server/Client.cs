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
		public AESKey PhysicalKey { get; set; }
		public SessionKey SessionKey { get; set; }
		public RSAParameters RSAParameters { get; set; }
		public EncryptionMode EncryptionMode { get; set; } = EncryptionMode.AES;
		
		private Dictionary<int, int> MessageCounter;
		private ConcurrentQueue<string> SendQueue;
		private ConcurrentQueue<string> ReceiveQueue;
		private NetworkStream Stream;
		private TcpClient SocketClient;
		private bool Disposed;
		private short IdleCounter;

		public Client(TcpClient client, NetworkStream stream, int id)
		{
			MessageCounter = new Dictionary<int, int>();
			Disposed = false;
			this.Stream = stream;
			this.SendQueue = new ConcurrentQueue<string>();
			this.ReceiveQueue = new ConcurrentQueue<string>();
			this.SocketClient = client;
			stream.ReadTimeout = 100;
			this.Id = id;
			this.IdleCounter = 0;
			//client.ReceiveBufferSize = 10_048_576;
			//client.SendBufferSize = 10_048_576;
			Task.Run(Run);
			Task.Run(ProcessIO);
		}

		public void Run()
		{
			int i;
			StringBuilder data = null;
			byte[] bytes = new byte[SocketClient.ReceiveBufferSize / 2];
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
							IdleCounter = 0;
							data = new StringBuilder();
							var str = "";
							while ((Stream.DataAvailable || (str != string.Empty && !str.EndsWith(Helper.SocketMessageSplitter))) && (i = Stream.Read(bytes, 0, bytes.Length)) != 0)
							{
								str = Encoding.Unicode.GetString(bytes, 0, i);
								data.Append(str);
								Console.WriteLine(str);
							}
							var splited = data.ToString().Split(Helper.SocketMessageSplitter, StringSplitOptions.RemoveEmptyEntries);
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
						{
							if (Name != null && IdleCounter >= 100)
							{
								IdleCounter = 0;
								Send("0", EncryptionMode.None);
							}
							Task.Delay(1).Wait();
							IdleCounter++;
						}


					}
					catch (IOException)
					{
					}
					while (SendQueue.Count > 0)
					{
						SendQueue.TryDequeue(out var msg);
						var bs = System.Text.Encoding.Unicode.GetBytes(msg);

						int maxLength = SocketClient.SendBufferSize / 2;
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
							Console.WriteLine($"Sent: {bs} to {this.Id}");

						}
						Stream.Flush();
						//Console.WriteLine($"Sent: {msg} to {this.Id}");
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
				msg += Helper.MacSeperator + Helper.RSASign(msg, TcpListener.Instance.RSAPrivateParameters);
				msg = Helper.RSAEncrypt(msg, this.RSAParameters);
			}
			else if (mode == EncryptionMode.AES)
			{
				msg += Helper.MacSeperator + Helper.RSASign(msg, TcpListener.Instance.RSAPrivateParameters);
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
			//if (input.StartsWith("non"))
			//{
			//	return input.Substring(13);
			//}
			if (input.StartsWith("rsa"))
			{
				input = input.Substring(13);
				input = Helper.RSADecrypt(input, TcpListener.Instance.RSAPrivateParameters);
				var split = input.Split(new string[] { Helper.MacSeperator }, StringSplitOptions.RemoveEmptyEntries);
				var data = split[0];
				var hash = split[1];
				if (!data.StartsWith("init") && !Helper.RSAVerify(hash, data, this.RSAParameters))
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
				if (!Helper.RSAVerify(hash, data, this.RSAParameters))
					return "";
				return data;
			}
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

		public int GetNewMessageId(int targetId)
		{
			if (!MessageCounter.ContainsKey(targetId))
			{
				MessageCounter.Add(targetId, 0);
				return 0;
			}
			var res = MessageCounter[targetId] + 1;
			MessageCounter[targetId] = res;
			return res;

		}

	}
}
