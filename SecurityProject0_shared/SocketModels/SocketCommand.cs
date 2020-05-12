using System;
using System.Collections.Generic;
using System.Text;

namespace SecurityProject0_shared.SocketModels
{
    public abstract class SocketCommand
    {
        public CommandType Command { get; set; }
        public DateTime Time { get; set; }

        public enum CommandType
        {
            Message,
            AddUser,
            RemoveUser,
            Init,
            Id,
            Disconnect
        }
    }
}
