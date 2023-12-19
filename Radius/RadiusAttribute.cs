using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius
{
    public class RadiusAttribute
    {
        public RadiusAttributeType Type { get; set; }
        public byte Length { get; set; }
        public byte[] Value { get; set; } = Array.Empty<byte>();

        public static RadiusAttribute Build(RadiusAttributeType type, byte[] value)
        {
            if (2 + value.Length > byte.MaxValue)
            {
                throw new RadiusException($"Malformed RADIUS Attribute. Length larger than {byte.MaxValue} bytes");
            }
            
            RadiusAttribute attribute = new()
            {
                Type = type,
                Length = (byte)(2 + value.Length),
                Value = value
            };

            return attribute;
        }

        /// <summary>
        /// Extract the first AVP from the byte array. Returns true if there are more values to extract
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="attribute"></param>
        /// <param name="remainder"></param>
        /// <returns></returns>
        /// <exception cref="RadiusException"></exception>
        public static bool FromBytes(byte[] bytes, out RadiusAttribute attribute, out byte[] remainder)
        {
            attribute = new();
            Span<byte> buffer = bytes.AsSpan();

            if (buffer.Length < 2)
            {
                throw new RadiusException("Malformed RADIUS Attribute. Length must be a minimum of 2 bytes!");
            }

            if (!Enum.IsDefined(typeof(RadiusAttributeType), buffer[0]))
            {
                throw new RadiusException("Malformed RADIUS Attribute. Unknown Type!");
            }

            attribute.Type = (RadiusAttributeType)buffer[0];
            attribute.Length = buffer[1];
            if (attribute.Length > 2)
            {
                attribute.Value = buffer[2..attribute.Length].ToArray();
            }

            if (buffer.Length > attribute.Length)
            {
                remainder = buffer[attribute.Length..].ToArray();
                return true;
            }

            remainder = Array.Empty<byte>();
            return false;
        }

        public static List<RadiusAttribute> ExtractAll(byte[] bytes)
        {
            List<RadiusAttribute> attributes = new();

            while (bytes.Length > 0)
            {
                _ = FromBytes(bytes, out RadiusAttribute attribute, out bytes);

                attributes.Add(attribute);
            }

            return attributes;
        }
    }
}
