using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius
{
    public enum RadiusCode : byte
    {
        ACCESS_REQUEST = 1,
        ACCESS_ACCEPT = 2,
        ACCESS_REJECT = 3,
        ACCOUNTING_REQUEST = 4,
        ACCOUNTING_RESPONSE = 5
    };
}
