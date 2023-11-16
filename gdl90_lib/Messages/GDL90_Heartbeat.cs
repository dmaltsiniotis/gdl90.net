using System;
using System.Buffers;

namespace GDL90 {
    public class Heartbeat : Message {

        // Byte 1
        public bool GPSPosValid; // 1 = Position is available for ADS-B Tx
        public bool MaintReq; // 1 = GDL 90 Maintenance Req'd
        public bool IDENT; // 1 = IDENT talkback
        public bool AddrType; // 1 = Address Type talkback
        public bool GPSBattLow; // 1 = GPS Battery low voltage
        public bool RATCS; // 1 = ATC Services talkback
        public bool reserved1_1; // -
        public bool UATInitialized; // 1 = GDL 90 is initialized

        // Byte 2
        public bool TimeStampMSbit; // Seconds since 0000Z, bit 16
        public bool CSARequested; // 1 = CSA has been requested
        public bool CSANotAvailable; // 1 = CSA is not available at this time
        public bool reserved2_4; // -
        public bool reserved2_3; // -
        public bool reserved2_2; // -
        public bool reserved2_1; // -
        public bool UTCOK; // 1 = UTC timing is valid

        public int TimeStamp; // Seconds since 0000Z, bits 15-0 (LS byte first)
        public short MessageCounts; // Number of UAT messages received by the GDL 90 during the previous second.

        public Heartbeat(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();
        }

        public override void PrintDebugInfo()
        {
            System.Text.StringBuilder debugBuilder = new System.Text.StringBuilder();
            debugBuilder.AppendLine(string.Format("MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format(" ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));

            Console.WriteLine(debugBuilder);
        }

        private void ParseFromBytes()
        {
            int HeartbeatLength = 6;

            if (ValidCRC)
            {
                if (MessageData.Length == HeartbeatLength)
                {

                }
                else
                {
                    Console.WriteLine("Warning: Unexpected length in Heartbeat message: expected {0} got {1}. Skipping parse attempt.", HeartbeatLength, MessageData.Length);
                    Console.WriteLine(Convert.ToHexString(MessageData));
                }
            }
        }
    }
}