using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadiusPlayground
{
    internal class ClientRecord
    {
        public required string ClientMAC { get; set; }
        public bool Blocked;
        public bool Guest;
    }
}
