using System;
using System.Collections.Generic;
using System.Text;

namespace SecurityProject0_server
{
    class Program
    {
        public static void Main(string[] args)
        {
            new TcpListener().Run();

        }
    }
}
