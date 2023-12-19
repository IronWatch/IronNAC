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

        public List<RadiusAttribute> Attributes { get; set; } = [];

        private RadiusPacket() { }

        public byte[] CreateResponse(RadiusCode code, byte[] secret, params RadiusAttribute[] attributes)
        {
            RadiusPacket response = new();
            response.Code = code;
            response.Identifier = this.Identifier;
            response.Authenticator = [0,0,0,0,0,0,0,0, 0, 0, 0, 0, 0, 0, 0, 0,]; // zerod for message-authenticator calc
            response.Attributes = attributes.ToList();

            // Calculate Length
            int packetLength = 20;

            foreach (RadiusAttribute attribute in response.Attributes)
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

            response.Length = (ushort)packetLength;

            // Calculate HMAC MD5 Attribute if needed
            if (this.Attributes.Any(x => x.Type == RadiusAttributeType.MESSAGE_AUTHENTICATOR))
            {
                byte[] hmacPacketBuffer = response.ToBytes();

                using System.Security.Cryptography.HMACMD5 hmacmd5 = new(secret);
                byte[] hmacmd5Hash = hmacmd5.ComputeHash(hmacPacketBuffer);

                RadiusAttribute messageAuthenticator 
                    = RadiusAttribute.Build(RadiusAttributeType.MESSAGE_AUTHENTICATOR, hmacmd5Hash);

                response.Attributes.Add(messageAuthenticator);
                response.Length += messageAuthenticator.Length;
            }

            response.Authenticator = this.Authenticator; // set to request authenticator for resposne authenticator calc

            // Get bytes
            byte[] packetBuffer = response.ToBytes();

            byte[] md5Buffer = new byte[packetBuffer.Length + secret.Length];
            Array.Copy(
                sourceArray: packetBuffer,
                sourceIndex: 0,
                destinationArray: md5Buffer,
                destinationIndex: 0,
                length: packetBuffer.Length);
            Array.Copy(
                sourceArray: secret,
                sourceIndex: 0,
                destinationArray: md5Buffer,
                destinationIndex: packetBuffer.Length,
                length: secret.Length);

            // Calculate response hash
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();

            byte[] newAuthenticator = md5.ComputeHash(md5Buffer);
            Array.Copy(
                sourceArray: newAuthenticator,
                sourceIndex: 0,
                destinationArray: packetBuffer,
                destinationIndex: 4,
                length: 16);

            return packetBuffer;

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

        public string? ReadString(RadiusAttributeType type)
        {
            return this.Attributes
                .Where(x => x.Type == type)
                .Select(x => Encoding.ASCII.GetString(x.Value))
                .FirstOrDefault();
        }
    }
}
