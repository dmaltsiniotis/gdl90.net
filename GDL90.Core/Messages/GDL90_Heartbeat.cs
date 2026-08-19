using System;
using System.Buffers;

namespace GDL90.Core.Messages
{
    public class Heartbeat : Message {

        // Status Byte 1
        public bool GPSPosValid; // 1 = Position is available for ADS-B Tx
        public bool MaintReq; // 1 = GDL 90 Maintenance Req'd
        public bool IDENT; // 1 = IDENT talkback
        public bool AddrType; // 1 = Address Type talkback
        public bool GPSBattLow; // 1 = GPS Battery low voltage
        public bool RATCS; // 1 = ATC Services talkback
        public bool Reserved1Bit1; // Reserved, should be ZERO.
        public bool UATInitialized; // 1 = GDL 90 is initialized

        // Status Byte 2
        public bool TimeStampMSbit; // Seconds since 0000Z, bit 16
        public bool CSARequested; // 1 = CSA has been requested
        public bool CSANotAvailable; // 1 = CSA is not available at this time
        public bool Reserved2Bit4; // Reserved, should be ZERO.
        public bool Reserved2Bit3; // Reserved, should be ZERO.
        public bool Reserved2Bit2; // Reserved, should be ZERO.
        public bool Reserved2Bit1; // Reserved, should be ZERO.
        public bool UTCOK; // 1 = UTC timing is valid

        public int TimeStamp; // Seconds since 0000Z, bits 15-0 (LS byte first)
        public int UplinkMessageCount; // Bits 7..3 of the first message count byte
        public int BasicAndLongMessageCount; // 10-bit count from bits 1..0 of first byte + second byte

        public Heartbeat(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();
        }

        public override string ToShortString()
        {
            return string.Format("Heartbeat: GPS: {0} MAINT: {1} IDENT: {2} ADDR: {3} GPSBATTLOW: {4} RATCS: {5}", GPSPosValid, MaintReq, IDENT, AddrType, GPSBattLow, RATCS);
        }

        public override string ToDetailedString()
        {
            System.Text.StringBuilder debugBuilder = new System.Text.StringBuilder();
            debugBuilder.AppendLine(string.Format("                  MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format("                   ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));
            debugBuilder.AppendLine(string.Format("                GPSPosValid: {0}", GPSPosValid));
            debugBuilder.AppendLine(string.Format("                   MaintReq: {0}", MaintReq));
            debugBuilder.AppendLine(string.Format("                      IDENT: {0}", IDENT));
            debugBuilder.AppendLine(string.Format("                   AddrType: {0}", AddrType));
            debugBuilder.AppendLine(string.Format("                 GPSBattLow: {0}", GPSBattLow));
            debugBuilder.AppendLine(string.Format("                      RATCS: {0}", RATCS));
            debugBuilder.AppendLine(string.Format("             UATInitialized: {0}", UATInitialized));
            debugBuilder.AppendLine(string.Format("             TimeStampMSbit: {0}", TimeStampMSbit));
            debugBuilder.AppendLine(string.Format("               CSARequested: {0}", CSARequested));
            debugBuilder.AppendLine(string.Format("            CSANotAvailable: {0}", CSANotAvailable));
            debugBuilder.AppendLine(string.Format("                      UTCOK: {0}", UTCOK));
            debugBuilder.AppendLine(string.Format("                  TimeStamp: {0} seconds", TimeStamp));
            debugBuilder.AppendLine(string.Format("         UplinkMessageCount: {0}", UplinkMessageCount));
            debugBuilder.AppendLine(string.Format("   BasicAndLongMessageCount: {0}", BasicAndLongMessageCount));
            return debugBuilder.ToString();
        }

        private void ParseFromBytes()
        {
            int HeartbeatLength = 6;

            if (ValidCRC)
            {
                if (MessageData.Length == HeartbeatLength)
                {
                    byte statusByte1 = MessageData[0];
                    byte statusByte2 = MessageData[1];

                    GPSPosValid = (statusByte1 & 0x80) != 0;
                    MaintReq = (statusByte1 & 0x40) != 0;
                    IDENT = (statusByte1 & 0x20) != 0;
                    AddrType = (statusByte1 & 0x10) != 0;
                    GPSBattLow = (statusByte1 & 0x08) != 0;
                    RATCS = (statusByte1 & 0x04) != 0;
                    Reserved1Bit1 = (statusByte1 & 0x02) != 0;
                    UATInitialized = (statusByte1 & 0x01) != 0;

                    TimeStampMSbit = (statusByte2 & 0x80) != 0;
                    CSARequested = (statusByte2 & 0x40) != 0;
                    CSANotAvailable = (statusByte2 & 0x20) != 0;
                    Reserved2Bit4 = (statusByte2 & 0x10) != 0;
                    Reserved2Bit3 = (statusByte2 & 0x08) != 0;
                    Reserved2Bit2 = (statusByte2 & 0x04) != 0;
                    Reserved2Bit1 = (statusByte2 & 0x02) != 0;
                    UTCOK = (statusByte2 & 0x01) != 0;

                    ushort timestampLow = BitConverter.ToUInt16(new byte[] { MessageData[2], MessageData[3] }, 0);
                    TimeStamp = (TimeStampMSbit ? 0x10000 : 0) | timestampLow;

                    byte messageCount1 = MessageData[4];
                    byte messageCount2 = MessageData[5];

                    UplinkMessageCount = (messageCount1 >> 3) & 0x1F;
                    BasicAndLongMessageCount = ((messageCount1 & 0x03) << 8) | messageCount2;
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