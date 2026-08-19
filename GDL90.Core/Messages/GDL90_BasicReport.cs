using System;
using System.Text;

namespace GDL90.Core.Messages {
    public class BasicReport : Message {
        public int TimeOfReceptionRaw = 0;
        public bool TimeOfReceptionValid = false;
        public double TimeOfReceptionSeconds = 0.0;
        public byte[] BasicPayload = new byte[18];

        public BasicReport(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();
        }

        public override string ToShortString()
        {
            return string.Format("Basic Report: TORRaw={0} TORValid={1} PayloadBytes={2}", TimeOfReceptionRaw, TimeOfReceptionValid, BasicPayload.Length);
        }

        public override string ToDetailedString()
        {
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.AppendLine(string.Format("                  MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format("                    ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));
            debugBuilder.AppendLine(string.Format("          TimeOfReceptionRaw: {0}", TimeOfReceptionRaw));
            debugBuilder.AppendLine(string.Format("        TimeOfReceptionValid: {0}", TimeOfReceptionValid));
            debugBuilder.AppendLine(string.Format("      TimeOfReceptionSeconds: {0}", TimeOfReceptionSeconds));
            debugBuilder.AppendLine(string.Format("           BasicPayloadBytes: {0}", BasicPayload.Length));
            debugBuilder.AppendLine(string.Format("              BasicPayload: 0x{0}", Convert.ToHexString(BasicPayload)));
            return debugBuilder.ToString();
        }

        private void ParseFromBytes()
        {
            // This is just a variation of the UplinkData message, so we need to look for a larger message size then sub-divide.
            //int BasicReportLength = 21; // 3-byte TOR + 18-byte payload
            int BasicReportLength = 435; // 3-byte TOR + 18-byte payload

            if (ValidCRC)
            {
                if (MessageData.Length == BasicReportLength)
                {
                    // TOR is 24-bit, least significant byte first.
                    TimeOfReceptionRaw = MessageData[0] | (MessageData[1] << 8) | (MessageData[2] << 16);
                    TimeOfReceptionValid = TimeOfReceptionRaw != 0xFFFFFF;
                    TimeOfReceptionSeconds = TimeOfReceptionRaw * 80e-9;

                    // The format of the 18 bytes of Basic Payload is specified in RTCA/DO-282, Section 2.2.
                    Array.Copy(MessageData, 3, BasicPayload, 0, 18);
                }
                else
                {
                    Console.WriteLine("Warning: Unexpected length in Basic Report message: expected {0} got {1}. Skipping parse attempt.", BasicReportLength, MessageData.Length);
                    Console.WriteLine(Convert.ToHexString(MessageData));
                }
            }
        }
    }
}