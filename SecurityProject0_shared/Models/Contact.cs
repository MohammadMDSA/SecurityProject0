using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SecurityProject0_shared.Models
{
    public class Contact
    {
        public string Name { get; set; }
        public string Secret { get; set; }
        public SessionKey SessionKey { get; private set; }
        public List<Message> Messages { get; set; }
        public Message LastMessage { get => Messages.Last(); }

        public Contact()
        {
            Messages = new List<Message>();
        }

    }
}
