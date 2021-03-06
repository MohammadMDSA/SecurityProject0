﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SecurityProject0_shared.Models
{
    public class Message
    {
        public string _rawMessage;

        public int Id { get; private set; }
        public bool FromMe { get; set; }
        public DateTime DeliveryTime { get; set; }
        public string EncryptedMessage { get; private set; }
        public bool IsFile { get; private set; }
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

        public Message(bool isFile, int id)
        {
            this.IsDecrypted = false;
            this.IsFile = isFile;
            this.Id = id;
        }

        private void Decrypt()
        {
            IsDecrypted = true;
        }

        public override string ToString()
        {
            return RawMessage;
        }

        public override bool Equals(object obj)
        {
            if (!this.GetType().Equals(obj.GetType()))
                return false;
            return this.Id == (obj as Message).Id;
        }
        public override int GetHashCode()
        {
            return this.Id;
        }

    }

    public enum EncryptionMode
    {
        None,
        RSA,
        AES
    }
}
