using System;
using System.Collections.Generic;
using System.Text;

namespace SecurityProject0_shared.Models
{
    public struct SessionKey
    {
        public AESKey AESKey { get; set; }
        public DateTime ExpirationDate { get; set; }

        public bool IsExpired => DateTime.Now > ExpirationDate;
    }
}
