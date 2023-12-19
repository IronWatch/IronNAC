using Microsoft.VisualBasic.FileIO;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius
{
    public class RadiusPacket
    {
        public RadiusCode Code { get; set; }

        public byte Identifier { get; set; }

        public ushort Length { get; private set; } = 20;

        public byte[] Authenticator { get; private set; } = Array.Empty<byte>();

        private List<RadiusAttribute> Attributes = [];

        private RadiusPacket() { }

        public static RadiusPacket Create(
            RadiusCode code,
            byte? identifier = null,
            byte[]? authenticator = null,
            params RadiusAttribute[] attributes)
        {
            RadiusPacket packet = new()
            {
                Code = code,
                Identifier = identifier ?? 0,
                Authenticator = authenticator ?? [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,],
                Attributes = [.. attributes]
            };

            return packet.UpdateLength();
        }

        public RadiusPacket UpdateLength()
        {
            // Calculate Length
            int packetLength = 20;

            foreach (RadiusAttribute attribute in this.Attributes)
            {
                int attributeLength = (2 + attribute.Value.Length);
                if (attributeLength > byte.MaxValue)
                {
                    throw new RadiusException($"Malformed RADIUS Attribute. Length larger than {byte.MaxValue} bytes");
                }

                attribute.Length = (byte)attributeLength;
                packetLength += attributeLength;
            }

            if (packetLength > ushort.MaxValue)
            {
                throw new RadiusException($"Malformed RADIUS Packet. Length larger than {ushort.MaxValue} bytes");
            }

            this.Length = (ushort)packetLength;

            return this;
        }

        public RadiusPacket ReplaceAuthenticator(byte[] authenticator)
        {
            this.Authenticator = authenticator;
            return this;
        }

        public RadiusPacket Sign(byte[] secret)
        {
            byte[] buffer = this.ToBytes();
            byte[] md5Buffer = new byte[buffer.Length + secret.Length];

            Array.Copy(
                sourceArray: buffer,
                sourceIndex: 0,
                destinationArray: md5Buffer,
                destinationIndex: 0,
                length: buffer.Length);
            Array.Copy(
                sourceArray: secret,
                sourceIndex: 0,
                destinationArray: md5Buffer,
                destinationIndex: buffer.Length,
                length: secret.Length);

            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();

            this.Authenticator = md5.ComputeHash(md5Buffer);

            return this;
        }

        public RadiusPacket AddMessageAuthenticator(byte[] secret)
        {
            byte[] buffer = this.ToBytes();

            using System.Security.Cryptography.HMACMD5 hmac = new(secret);

            byte[] hash = hmac.ComputeHash(buffer);

            RadiusAttribute messageAuthenticator
                    = RadiusAttribute.Build(RadiusAttributeType.MESSAGE_AUTHENTICATOR, hash);

            return this.AddAttribute(messageAuthenticator);
        }

        public RadiusPacket AddAttribute(RadiusAttribute? attribute)
        {
            if (attribute is null) return this;
            
            if (this.Attributes.Any(x => x.Type == attribute.Type))
            {
                throw new RadiusException($"Refusing to add duplicate attribute of type {attribute.Type}!");
            }

            this.Attributes.Add(attribute);
            this.Length += attribute.Length;

            return this;
        }

        public RadiusPacket AddAttributes(params RadiusAttribute[] attributes)
        {
            foreach (RadiusAttribute attribute in attributes)
            {
                this.AddAttribute(attribute);
            }

            return this;
        }

        public RadiusPacket RemoveAttribute(RadiusAttributeType type)
        {
            RadiusAttribute? attribute = this.Attributes
                .Where(x => x.Type == type)
                .FirstOrDefault();

            if (attribute is null) return this;

            this.Attributes.Remove(attribute);
            this.Length -= attribute.Length;

            return this;
        }

        public static RadiusPacket FromBytes(byte[] bytes)
        {
            Span<byte> buffer = bytes.AsSpan();

            if (buffer.Length < 20)
            {
                throw new RadiusException("Malformed RADIUS Packet. Length must be a minimum of 20 bytes!");
            }

            if (!Enum.IsDefined(typeof(RadiusCode), buffer[0]))
            {
                throw new RadiusException("Malformed RADIUS Packet. Unknown Code!");
            }

            RadiusPacket packet = new();
            packet.Code = (RadiusCode)buffer[0];
            packet.Identifier = buffer[1];
            packet.Length = BinaryPrimitives.ReverseEndianness(
                BitConverter.ToUInt16(buffer[2..4]));
            packet.Authenticator = buffer[4..20].ToArray();

            packet.Attributes = RadiusAttribute.ExtractAll(buffer[20..].ToArray());

            return packet;
        }

        public byte[] ToBytes()
        {
            byte[] buffer = new byte[this.Length];

            buffer[0] = (byte)this.Code;
            buffer[1] = this.Identifier;

            Array.Copy(
                sourceArray: BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(this.Length)), 
                sourceIndex: 0, 
                destinationArray: buffer, 
                destinationIndex: 2, 
                length: 2);

            if (this.Authenticator.Length != 16)
            {
                throw new RadiusException("Malformed RADIUS Packet. Authenticator length is not 16 bytes!");
            }

            Array.Copy(
                sourceArray: this.Authenticator,
                sourceIndex: 0,
                destinationArray: buffer,
                destinationIndex: 4,
                length: 16);

            int index = 20;
            foreach (RadiusAttribute attribute in this.Attributes)
            {
                buffer[index] = (byte)attribute.Type;
                buffer[index + 1] = attribute.Length;

                Array.Copy(
                    sourceArray: attribute.Value,
                    sourceIndex: 0,
                    destinationArray: buffer,
                    destinationIndex: index + 2,
                    length: attribute.Length - 2);

                index += attribute.Length;
            }

            return buffer;
        }

        public RadiusAttribute? GetAttribute(RadiusAttributeType type)
        {
            return this.Attributes
                .Where(x => x.Type == type)
                .FirstOrDefault();
        }

        public string? GetAttributeString(RadiusAttributeType type)
        {
            RadiusAttribute? attribute = this.GetAttribute(type);
            if (attribute is null) return null;

            return Encoding.ASCII.GetString(attribute.Value);
        }
    }
}
