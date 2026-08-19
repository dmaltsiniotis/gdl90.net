using System;
using System.Text;

namespace GDL90.Core.Messages
{
    public class StratuxHeartbeat : Message {
        public byte StatusByte = 0;
        public bool GPSValid = false;
        public bool AHRSValid = false;
        public int ProtocolVersion = 0;

        public StratuxHeartbeat(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();
        }

        public override string ToShortString()
        {
            return string.Format("Stratux Heartbeat: ProtocolVersion {0}, GPSValid {1}, AHRSValid {2}", ProtocolVersion, GPSValid, AHRSValid);
        }

        public override string ToDetailedString()
        {
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.AppendLine(string.Format("                  MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format("                   ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));
            debugBuilder.AppendLine(string.Format("                 StatusByte: 0x{0:X2}", StatusByte));
            debugBuilder.AppendLine(string.Format("            ProtocolVersion: {0}", ProtocolVersion));
            debugBuilder.AppendLine(string.Format("                   GPSValid: {0}", GPSValid));
            debugBuilder.AppendLine(string.Format("                  AHRSValid: {0}", AHRSValid));
            return debugBuilder.ToString();
        }

        private void ParseFromBytes()
        {
            int StratuxHeartbeatLength = 1;

            if (ValidCRC)
            {
                if (MessageData.Length == StratuxHeartbeatLength)
                {
                    StatusByte = MessageData[0];
                    AHRSValid = (StatusByte & 0x01) != 0;
                    GPSValid = (StatusByte & 0x02) != 0;
                    ProtocolVersion = (StatusByte >> 2) & 0x3F;
                }
                else
                {
                    Console.WriteLine("Warning: Unexpected length in Stratux Heartbeat message: expected {0} got {1}. Skipping parse attempt.", StratuxHeartbeatLength, MessageData.Length);
                    Console.WriteLine(Convert.ToHexString(MessageData));
                }
            }
        }
    }
}