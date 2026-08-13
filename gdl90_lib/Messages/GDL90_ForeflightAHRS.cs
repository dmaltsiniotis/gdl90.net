using System;
using System.Text;

namespace GDL90 {
    public class ForeflightAHRS : Message {
        public byte MessageIdentifier = 0;
        public short RollRaw = 0x7FFF;
        public short PitchRaw = 0x7FFF;
        public ushort HeadingRaw = 0xFFFF;
        public ushort IndicatedAirspeedRaw = 0xFFFF;
        public ushort TrueAirspeedRaw = 0xFFFF;

        public float RollDegrees = float.NaN;
        public float PitchDegrees = float.NaN;
        public float HeadingDegrees = float.NaN;
        public int IndicatedAirspeed = -1;
        public int TrueAirspeed = -1;

        public ForeflightAHRS(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();
        }

        public override string ToShortString()
        {
            return string.Format("ForeFlight AHRS: Roll {0} Pitch {1} Heading {2} IAS {3} TAS {4}", RollDegrees, PitchDegrees, HeadingDegrees, IndicatedAirspeed, TrueAirspeed);
        }

        public override string ToDetailedString()
        {
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.AppendLine(string.Format("                  MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format("                   ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));
            debugBuilder.AppendLine(string.Format("          MessageIdentifier: {0}", MessageIdentifier));
            debugBuilder.AppendLine(string.Format("                   RollRaw: 0x{0:X4}", (ushort)RollRaw));
            debugBuilder.AppendLine(string.Format("                  PitchRaw: 0x{0:X4}", (ushort)PitchRaw));
            debugBuilder.AppendLine(string.Format("                HeadingRaw: 0x{0:X4}", HeadingRaw));
            debugBuilder.AppendLine(string.Format("      IndicatedAirspeedRaw: 0x{0:X4}", IndicatedAirspeedRaw));
            debugBuilder.AppendLine(string.Format("           TrueAirspeedRaw: 0x{0:X4}", TrueAirspeedRaw));
            debugBuilder.AppendLine(string.Format("               RollDegrees: {0}", RollDegrees));
            debugBuilder.AppendLine(string.Format("              PitchDegrees: {0}", PitchDegrees));
            debugBuilder.AppendLine(string.Format("            HeadingDegrees: {0}", HeadingDegrees));
            debugBuilder.AppendLine(string.Format("         IndicatedAirspeed: {0}", IndicatedAirspeed));
            debugBuilder.AppendLine(string.Format("              TrueAirspeed: {0}", TrueAirspeed));

            return debugBuilder.ToString();
        }

        private void ParseFromBytes()
        {
            int ForeflightAHRSLength = 11;

            if (ValidCRC)
            {
                if (MessageData.Length == ForeflightAHRSLength)
                {
                    MessageIdentifier = MessageData[0];

                    RollRaw = ReadInt16BE(MessageData, 1);
                    PitchRaw = ReadInt16BE(MessageData, 3);
                    HeadingRaw = ReadUInt16BE(MessageData, 5);
                    IndicatedAirspeedRaw = ReadUInt16BE(MessageData, 7);
                    TrueAirspeedRaw = ReadUInt16BE(MessageData, 9);

                    if (RollRaw != 0x7FFF) RollDegrees = RollRaw / 10.0f;
                    if (PitchRaw != 0x7FFF) PitchDegrees = PitchRaw / 10.0f;
                    if (HeadingRaw != 0xFFFF) HeadingDegrees = HeadingRaw / 10.0f;
                    if (IndicatedAirspeedRaw != 0xFFFF) IndicatedAirspeed = IndicatedAirspeedRaw;
                    if (TrueAirspeedRaw != 0xFFFF) TrueAirspeed = TrueAirspeedRaw;
                }
                else
                {
                    Console.WriteLine("Warning: Unexpected length in ForeFlight AHRS message: expected {0} got {1}. Skipping parse attempt.", ForeflightAHRSLength, MessageData.Length);
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