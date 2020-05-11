using System;
using System.Collections.Generic;
using System.Text;

namespace SecurityProject0_shared.Models
{
    public class Message
    {
        public string _rawMessage;

        public bool FromMe { get; set; }
        public DateTime DeliveryTime { get; set; }
        public string EncryptedMessage { get; private set; }
        public string RawMessage
        {
            get
            {
                if (!IsDecrypted)
                    Decrypt();
                return _rawMessage ?? "";
            }
        }
        public bool IsDecrypted { get; private set; }

        public Message()
        {
            this.IsDecrypted = false;
        }

        private void Decrypt()
        {
            IsDecrypted = true;
        }

        public override string ToString()
        {
            return RawMessage;
        }

    }
}
