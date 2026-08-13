using System;

namespace GDL90 {
    public class StratuxAHRS : Message {
        public byte StatusByte1 = 0;
        public byte StatusByte2 = 0;
        public byte StatusByte3 = 0;

        public short RollRaw = 0x7FFF;
        public short PitchRaw = 0x7FFF;
        public short HeadingRaw = 0x7FFF;
        public short SlipSkidRaw = 0x7FFF;
        public short YawRateRaw = 0x7FFF;
        public short GLoadRaw = 0x7FFF;
        public short AirspeedRaw = 0x7FFF;
        public ushort PressureAltitudeRaw = 0xFFFF;
        public short VerticalSpeedRaw = 0x7FFF;

        public float RollDegrees = float.NaN;
        public float PitchDegrees = float.NaN;
        public float HeadingDegrees = float.NaN;
        public float SlipSkid = float.NaN;
        public float YawRate = float.NaN;
        public float GLoad = float.NaN;
        public float Airspeed = float.NaN;
        public int PressureAltitude = int.MinValue;
        public int VerticalSpeed = int.MinValue;
        public byte ReservedByte1 = 0;
        public byte ReservedByte2 = 0;

        public StratuxAHRS(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();
        }

        public override string ToShortString()
        {
            return string.Format("Stratux AHRS: Roll: {0} Pitch: {1} Heading: {2}", RollDegrees, PitchDegrees, HeadingDegrees);
        }

        public override string ToDetailedString()
        {
            System.Text.StringBuilder debugBuilder = new System.Text.StringBuilder();
            debugBuilder.AppendLine(string.Format("                  MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format("                   ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));
            debugBuilder.AppendLine(string.Format("                StatusByte1: 0x{0:X2}", StatusByte1));
            debugBuilder.AppendLine(string.Format("                StatusByte2: 0x{0:X2}", StatusByte2));
            debugBuilder.AppendLine(string.Format("                StatusByte3: 0x{0:X2}", StatusByte3));
            debugBuilder.AppendLine(string.Format("                      Roll: {0}", RollDegrees));
            debugBuilder.AppendLine(string.Format("                     Pitch: {0}", PitchDegrees));
            debugBuilder.AppendLine(string.Format("                   Heading: {0}", HeadingDegrees));
            debugBuilder.AppendLine(string.Format("                  SlipSkid: {0}", SlipSkid));
            debugBuilder.AppendLine(string.Format("                   YawRate: {0}", YawRate));
            debugBuilder.AppendLine(string.Format("                     GLoad: {0}", GLoad));
            debugBuilder.AppendLine(string.Format("                  Airspeed: {0}", Airspeed));
            debugBuilder.AppendLine(string.Format("          PressureAltitude: {0}", PressureAltitude));
            debugBuilder.AppendLine(string.Format("             VerticalSpeed: {0}", VerticalSpeed));
            debugBuilder.AppendLine(string.Format("             ReservedByte1: 0x{0:X2}", ReservedByte1));
            debugBuilder.AppendLine(string.Format("             ReservedByte2: 0x{0:X2}", ReservedByte2));
            return debugBuilder.ToString();
        }

        private void ParseFromBytes()
        {
            int StratuxAHRSLength = 23;

            if (ValidCRC)
            {
                if (MessageData.Length == StratuxAHRSLength)
                {
                    StatusByte1 = MessageData[0];
                    StatusByte2 = MessageData[1];
                    StatusByte3 = MessageData[2];

                    RollRaw = ReadInt16BE(MessageData, 3);
                    PitchRaw = ReadInt16BE(MessageData, 5);
                    HeadingRaw = ReadInt16BE(MessageData, 7);
                    SlipSkidRaw = ReadInt16BE(MessageData, 9);
                    YawRateRaw = ReadInt16BE(MessageData, 11);
                    GLoadRaw = ReadInt16BE(MessageData, 13);
                    AirspeedRaw = ReadInt16BE(MessageData, 15);
                    PressureAltitudeRaw = ReadUInt16BE(MessageData, 17);
                    VerticalSpeedRaw = ReadInt16BE(MessageData, 19);

                    ReservedByte1 = MessageData[21];
                    ReservedByte2 = MessageData[22];

                    if (RollRaw != 0x7FFF) RollDegrees = RollRaw / 10.0f;
                    if (PitchRaw != 0x7FFF) PitchDegrees = PitchRaw / 10.0f;
                    if (HeadingRaw != 0x7FFF) HeadingDegrees = HeadingRaw / 10.0f;
                    if (SlipSkidRaw != 0x7FFF) SlipSkid = SlipSkidRaw / 10.0f;
                    if (YawRateRaw != 0x7FFF) YawRate = YawRateRaw / 10.0f;
                    if (GLoadRaw != 0x7FFF) GLoad = GLoadRaw / 10.0f;
                    if (AirspeedRaw != 0x7FFF) Airspeed = AirspeedRaw / 10.0f;

                    // Go encoder stores pressure altitude as unsigned (altitude + 5000.5).
                    if (PressureAltitudeRaw != 0xFFFF) PressureAltitude = PressureAltitudeRaw - 5000;

                    if (VerticalSpeedRaw != 0x7FFF) VerticalSpeed = VerticalSpeedRaw;
                }
                else
                {
                    Console.WriteLine("Warning: Unexpected length in Stratux AHRS message: expected {0} got {1}. Skipping parse attempt.", StratuxAHRSLength, MessageData.Length);
                    Console.WriteLine(Convert.ToHexString(MessageData));
                }
            }
        }

        private static short ReadInt16BE(byte[] data, int start)
        {
            return (short)((data[start] << 8) | data[start + 1]);
        }

        private static ushort ReadUInt16BE(byte[] data, int start)
        {
            return (ushort)((data[start] << 8) | data[start + 1]);
        }
    }
}