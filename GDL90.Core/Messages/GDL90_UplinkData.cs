using System;
using System.Text;

namespace GDL90.Core.Messages
{
    public class UplinkData : Message {
        public int TimeOfReceptionRaw = 0;
        public bool TimeOfReceptionValid = false;
        public double TimeOfReceptionSeconds = 0.0;
        public byte[] UplinkPayload = new byte[432];

        public UplinkData(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();
        }

        public override string ToShortString()
        {
            return string.Format("Uplink Data: TORRaw={0} TORValid={1} PayloadBytes={2}", TimeOfReceptionRaw, TimeOfReceptionValid, UplinkPayload.Length);
        }

        public override string ToDetailedString()
        {
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.AppendLine(string.Format("                  MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format("                    ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));
            debugBuilder.AppendLine(string.Format("          TimeOfReceptionRaw: {0}", TimeOfReceptionRaw));
            debugBuilder.AppendLine(string.Format("        TimeOfReceptionValid: {0}", TimeOfReceptionValid));
            debugBuilder.AppendLine(string.Format("      TimeOfReceptionSeconds: {0}", TimeOfReceptionSeconds));
            debugBuilder.AppendLine(string.Format("          UplinkPayloadBytes: {0}", UplinkPayload.Length));
            debugBuilder.AppendLine(string.Format("             UplinkPayload: 0x{0}", Convert.ToHexString(UplinkPayload)));
            return debugBuilder.ToString();
        }

        private void ParseFromBytes()
        {
            int UplinkDataLength = 435; // 3-byte TOR + 432-byte payload

            if (ValidCRC)
            {
                if (MessageData.Length == UplinkDataLength)
                {
                    // TOR is 24-bit, least significant byte first.
                    TimeOfReceptionRaw = MessageData[0] | (MessageData[1] << 8) | (MessageData[2] << 16);
                    TimeOfReceptionValid = TimeOfReceptionRaw != 0xFFFFFF;
                    TimeOfReceptionSeconds = TimeOfReceptionRaw * 80e-9;

                    Array.Copy(MessageData, 3, UplinkPayload, 0, 432);
                }
                else
                {
                    Console.WriteLine("Warning: Unexpected length in Uplink Data message: expected {0} got {1}. Skipping parse attempt.", UplinkDataLength, MessageData.Length);
                    Console.WriteLine(Convert.ToHexString(MessageData));
                }
            }
        }
    }
}