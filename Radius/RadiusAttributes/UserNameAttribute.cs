using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    [RadiusAttribute(RadiusAttributeType.USER_NAME)]
    public class UserNameAttribute : BaseRadiusAttribute
    {
        public string Username
        {
            get
            {
                return Encoding.UTF8.GetString(Raw.Value);
            }

            set
            {
                Raw.Value = Encoding.UTF8.GetBytes(value);
            }
        }

        [SetsRequiredMembers]
        public UserNameAttribute(string username)
        {
            Raw = new()
            {
                Type = RadiusAttributeType.TUNNEL_TYPE,
                Value = []
            };

            this.Username = username;
        }

        private UserNameAttribute() { }
    }
}
