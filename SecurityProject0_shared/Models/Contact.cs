using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SecurityProject0_shared.Models
{
    public class Contact
    {
        public string Name { get; private set; }
        public string Secret { get; set; }
        public SessionKey SessionKey { get; private set; }
        public List<Message> Messages { get;  set; }
        public Message LastMessage { get => Messages.LastOrDefault() ?? new Message(); }
        public int Id { get; private set; }

        public Contact(string name, int id)
        {
            this.Id = id;
            this.Name = name;
            Messages = new List<Message>();
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = obj as Contact;
            return other.Id == this.Id;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return this.Id;
        }

    }
}
