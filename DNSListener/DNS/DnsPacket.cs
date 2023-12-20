using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNSListener.DNS
{
    public class DnsPacket
    {
        public ushort TransactionId { get; set; }
        public DnsPacketFlags Flags { get; set; } = new();
        public ushort NumQuestions { get; set; }
        public ushort NumAnswers { get; set; }
        public ushort NumAuthorities { get; set; }
        public ushort NumAdditional { get; set; }

        public List<DnsPacketQuestion> Questions { get; set; } = new();
        public List<IDnsResourceRecord> Answers { get; set; } = new();
        public List<IDnsResourceRecord> Authorities { get; set; } = new();
        public List<IDnsResourceRecord> Additional { get; set; } = new();

        public static DnsPacket FromBytes(byte[] bytes)
        {
            Span<byte> buffer = bytes.AsSpan();
            if (buffer.Length < 12)
                throw new ArgumentOutOfRangeException(nameof(bytes));

            DnsPacket packet = new();

            packet.TransactionId = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(0, 2));
            packet.Flags = DnsPacketFlags.FromSpan(buffer.Slice(2, 2));
            packet.NumQuestions = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(4, 2));
            packet.NumAnswers = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(6, 2));
            packet.NumAuthorities = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(8, 2));
            packet.NumAdditional = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(10, 2));

            int index = 12;

            for (int i = 0; i < packet.NumQuestions; i++)
            {
                DnsPacketQuestion question = new();
                question.Name = DnsStringHelper.ToString(buffer.Slice(index), out int numBytesParsed);
                index += numBytesParsed;

                question.Type = (DnsResourceRecordTypes)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(index, 2));
                question.Class = (DnsClassCodes)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(index + 2, 2));

                packet.Questions.Add(question);
            }

            return packet;
        }

        public byte[] ToBytes()
        {
            byte[] headerBytes = new byte[12];
            Span<byte> headerBuffer = new(headerBytes);

            BinaryPrimitives.TryWriteUInt16BigEndian(headerBuffer.Slice(0, 2), this.TransactionId);
            this.Flags.WriteSpan(headerBuffer.Slice(2, 2));
            BinaryPrimitives.TryWriteUInt16BigEndian(headerBuffer.Slice(4, 2), this.NumQuestions);
            BinaryPrimitives.TryWriteUInt16BigEndian(headerBuffer.Slice(6, 2), this.NumAnswers);
            BinaryPrimitives.TryWriteUInt16BigEndian(headerBuffer.Slice(8, 2), this.NumAuthorities);
            BinaryPrimitives.TryWriteUInt16BigEndian(headerBuffer.Slice(10, 2), this.NumAdditional);

            List<byte[]> chunks = new();

            foreach (DnsPacketQuestion question in this.Questions)
            {
                byte[] name = DnsStringHelper.ToDnsBytes(question.Name);
                byte[] chunk = new byte[name.Length + 4];
                name.CopyTo(chunk, 0);

                Span<byte> chunkBuffer = new(chunk);

                BinaryPrimitives.TryWriteUInt16BigEndian(chunkBuffer.Slice(name.Length, 2), (ushort)question.Type);
                BinaryPrimitives.TryWriteUInt16BigEndian(chunkBuffer.Slice(name.Length+2, 2), (ushort)question.Class);

                chunks.Add(chunk);
            }

            int chunksLength = chunks.Sum(x => x.Length);
            byte[] result = new byte[headerBytes.Length + chunksLength];

            headerBytes.CopyTo(result, 0);

            int index = 12;
            foreach (byte[] chunk in chunks)
            {
                chunk.CopyTo(result, index);
                index += chunk.Length;
            }

            return result;
        }
    }
}
