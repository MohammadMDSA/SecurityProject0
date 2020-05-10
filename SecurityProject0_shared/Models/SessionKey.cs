using System;
using System.Collections.Generic;
using System.Text;

namespace SecurityProject0_shared.Models
{
    public class SessionKey
    {
        public string Key { get; set; }
        public DateTime ExpirationDate { get; private set; }

        public bool IsExpired { get => DateTime.Now > ExpirationDate; }
    }
}
