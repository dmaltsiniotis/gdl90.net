using System;
using System.Text;

namespace GDL90 {
    public class LongReport : Message {
        public int TimeOfReceptionRaw = 0;
        public bool TimeOfReceptionValid = false;
        public double TimeOfReceptionSeconds = 0.0;
        public byte[] LongPayload = new byte[34];

        public LongReport(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();
        }

        public override string ToShortString()
        {
            return string.Format("Long Report: TORRaw={0} TORValid={1} PayloadBytes={2}", TimeOfReceptionRaw, TimeOfReceptionValid, LongPayload.Length);
        }

        public override string ToDetailedString()
        {
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.AppendLine(string.Format("                  MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format("                    ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));
            debugBuilder.AppendLine(string.Format("          TimeOfReceptionRaw: {0}", TimeOfReceptionRaw));
            debugBuilder.AppendLine(string.Format("        TimeOfReceptionValid: {0}", TimeOfReceptionValid));
            debugBuilder.AppendLine(string.Format("      TimeOfReceptionSeconds: {0}", TimeOfReceptionSeconds));
            debugBuilder.AppendLine(string.Format("            LongPayloadBytes: {0}", LongPayload.Length));
            debugBuilder.AppendLine(string.Format("               LongPayload: 0x{0}", Convert.ToHexString(LongPayload)));
            return debugBuilder.ToString();
        }

        private void ParseFromBytes()
        {
            // This is just a variation of the UplinkData message, so we need to look for a larger message size then sub-divide.
            //int LongReportLength = 37; // 3-byte TOR + 34-byte payload
            int LongReportLength = 435; // 3-byte TOR + 34-byte payload

            if (ValidCRC)
            {
                if (MessageData.Length == LongReportLength)
                {
                    // TOR is 24-bit, least significant byte first.
                    TimeOfReceptionRaw = MessageData[0] | (MessageData[1] << 8) | (MessageData[2] << 16);
                    TimeOfReceptionValid = TimeOfReceptionRaw != 0xFFFFFF;
                    TimeOfReceptionSeconds = TimeOfReceptionRaw * 80e-9;

                    // The format of the 34 bytes of Long Payload is specified in RTCA/DO-282, Section 2.2. Which we don't have.
                    Array.Copy(MessageData, 3, LongPayload, 0, 34);
                }
                else
                {
                    Console.WriteLine("Warning: Unexpected length in Long Report message: expected {0} got {1}. Skipping parse attempt.", LongReportLength, MessageData.Length);
                    Console.WriteLine(Convert.ToHexString(MessageData));
                }
            }
        }
    }
}