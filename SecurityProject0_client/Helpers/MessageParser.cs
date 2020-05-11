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

namespace SecurityProject0_client.Core.Services
{
    public static class MessageParser
    {
        public static int Id { get; private set; }
        public static UserData User { get; private set; }

        public static Dictionary<int, Contact> Contacts = new Dictionary<int, Contact>();

        public static void Parse(string message)
        {

            if (message == null || message == "")
                return;
            var splited = message.Split('@');
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
                default:
                    break;
            }
        }

        public static void Identify(string[] splited)
        {
            if (!int.TryParse(splited[1], out var id))
                return;
            Id = id;
            User = Singleton<UserDataService>.Instance.GetUserData();
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

        public static void GetMessage(string[] splited)
        {
            if (splited.Length < 5)
                return;
            if (!int.TryParse(splited[1], out var sessionId) || !int.TryParse(splited[2], out var senderId) || !long.TryParse(splited[4], out var ticks))
                return;
            var fromMe = Id == senderId;
            var mess = new Message
            {
                DeliveryTime = new DateTime(ticks),
                _rawMessage = splited[3],
                FromMe = fromMe
            };
            if (fromMe)
                Contacts[sessionId].Messages.Add(mess);
            else
                Contacts[senderId].Messages.Add(mess);

        }
    }
}
