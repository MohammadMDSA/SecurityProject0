using SecurityProject0_client.Views;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SecurityProject0_shared.Models;
using Windows.Media.Core;
using SecurityProject0_client.Models;
using SecurityProject0_client.Core.Helpers;
using SecurityProject0_client.Services;
using Windows.Web.Http;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace SecurityProject0_client.Core.Services
{
    public static class MessageParser
    {
        public static int Id { get; private set; }
        public static UserData User { get; private set; }
        public static string PhysicalKey { get; set; }

        public static Dictionary<int, Contact> Contacts = new Dictionary<int, Contact>();
        public static event MessageEventHandler OnMessage;
        public static event PhysicalKeyChangeHandler OnPhysicalKeyChanged;

        public delegate void PhysicalKeyChangeHandler(string key);
        public delegate void MessageEventHandler(Message msg, int sender, int receiver);

        public static void Parse(string message)
        {

            if (message == null || message == "")
                return;
            var splited = message.Split(Helper.SocketMessageAttributeSeperator);
            string command = splited[0].ToLower();
            switch (command)
            {
                case "user":
                    AddUser(splited);
                    break;
                case "remove":
                    RemoveUser(splited);
                    break;
                case "id":
                    Identify(splited);
                    break;
                case "message":
                    GetMessage(splited);
                    break;
                case "file":
                    GetMessage(splited, true);
                    break;
                default:
                    break;
            }
        }

        public static void Identify(string[] splited)
        {
            if (!int.TryParse(splited[1], out var id))
                return;
            Id = id;
            //OnPhysicalKeyChanged?.Invoke(splited[2]);
            PhysicalKey = splited[2];
            User = Singleton<UserDataService>.Instance.GetUserData();
            var param = JsonConvert.DeserializeObject<RSAParameters>(splited[2]);
            MessageSender.Instance.ServerParams = param;
            MessageSender.Instance.Initialized = true;
        }

        public static void AddUser(string[] splited)
        {
            if (splited.Length < 3)
                return;
            if (!int.TryParse(splited[1], out var id))
                return;
            var con = new Contact(splited[2], id);
            Contacts.Add(id, con);
            ChatsPage.Instance.Add(con);
            //ChatsPage.GlobalContacts.Add(new Contact(splited[2], id));
        }

        public static void RemoveUser(string[] splited)
        {
            if (splited.Length < 2)
                return;
            if (!int.TryParse(splited[1], out var id))
                return;
            Contacts.Remove(id);
            ChatsPage.Instance.Remove(id);
            //ChatsPage.GlobalContacts.Remove(new Contact("", id));
        }

        public static void GetMessage(string[] splited, bool file = false)
        {
            if (splited.Length < 5)
                return;
            if (!int.TryParse(splited[1], out var sessionId) || !int.TryParse(splited[2], out var senderId) || !long.TryParse(splited[4], out var ticks))
                return;
            var fromMe = Id == senderId;
            Message mess = null;
            if(!file)
                mess = new Message(false)
                {
                    DeliveryTime = new DateTime(ticks),
                    _rawMessage = splited[3],
                    FromMe = fromMe
                };
            else
            {
                var dataSp = splited[3].Split(';');
                if (dataSp.Length < 2)
                    return;
                var fileName = dataSp[0];
                var data = splited[3].Substring(fileName.Length + 1);
                mess = new File(fileName)
                {
                    DeliveryTime = new DateTime(ticks),
                    _rawMessage = data,
                    FromMe = fromMe
                };
                (mess as File).Save();
            }
            if (fromMe)
                Contacts[sessionId].Messages.Add(mess);
            else
                Contacts[senderId].Messages.Add(mess);

            OnMessage?.Invoke(mess, sessionId, senderId);

        }
    }
}
