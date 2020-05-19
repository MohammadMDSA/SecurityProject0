using System;
using System.Collections.Generic;
using System.Text;

namespace SecurityProject0_shared.Models
{
    public struct AESKey
    {
        public byte[] IV { get; set; }
        public byte[] Key { get; set; }
    }
}
