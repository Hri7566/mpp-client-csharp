using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mpp_client_csharp.Messages
{
    public class HiMessage : Message
    {
        public string m = "hi";
        public string motd { get; set; }
        public User u { get; set; }
    }
}
