using Radius.RadiusAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RadiusPlayground
{
    internal class SessionRecord
    {
        public string ClientMAC { get; set; }        
        public UserNameAttribute? UserName { get; set; }
        public NasIpAddressAttribute? NasIpAddress { get; set; }
        public NasIdentifierAttribute? NasIdentifier { get; set; }
        public CallingStationIdAttribute? CallingStationId { get; set; }
        public CalledStationIdAttribute? CalledStationId { get; set; }
        public AccountingSessionIdAttribute? AccountingSessionId { get; set; }
    }
}
