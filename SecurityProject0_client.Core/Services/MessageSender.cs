using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;

namespace SecurityProject0_client.Core.Services
{
    public static class MessageSender
    {

        private static Queue<string> SendQueue;
        private static TcpClient Client;
        private static Stream Stream;

        static MessageSender()
        {
            SendQueue = new Queue<string>();
        }

        public static void Connect(String server, String message)
        {
            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer
                // connected to the same address as specified by the server, port
                // combination.
                Int32 port = 13000;
                Client = new TcpClient(server, port);

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.Unicode.GetBytes(message);

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                Stream = Client.GetStream();

                //data = SerializeObject(new )

                // Send the message to the connected TcpServer.
                Stream.Write(data, 0, data.Length);

                Console.WriteLine("Sent: {0}", message);

                // Receive the TcpServer.response.

                // Buffer to store the response bytes.
                data = new Byte[256];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = Stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.Unicode.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);

                // Close everything.
                Stream.Close();
                Client.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }


        public static void SendMessage(string msg)
        {
            SendQueue.Enqueue(msg);
        }

    }
}
