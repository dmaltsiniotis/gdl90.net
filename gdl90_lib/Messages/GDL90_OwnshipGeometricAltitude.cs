using System;

namespace GDL90 {
    public class OwnshipGeometricAltitude : Message {
        public int GeometricAltitude = 0;
        public bool VerticalWarningIndicator = false;
        public int VerticalFigureOfMerit = 0;
        public bool VerticalFigureOfMeritNotAvailable = false;
        public bool VerticalFigureOfMeritGreaterThanOrEqual32766 = false;

        public OwnshipGeometricAltitude(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();
        }

        public override string ToShortString()
        {
            return string.Format("Ownship Geometric Altitude: {0} ft, Vertical Warn: {1}, VerticalFigureOfMerit {2}", GeometricAltitude, VerticalWarningIndicator, VerticalFigureOfMerit);
        }

        public override string ToDetailedString()
        {
            System.Text.StringBuilder debugBuilder = new System.Text.StringBuilder();
            debugBuilder.AppendLine(string.Format("                  MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format("                   ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));
            debugBuilder.AppendLine(string.Format("          GeometricAltitude: {0} ft.", GeometricAltitude));
            debugBuilder.AppendLine(string.Format("   VerticalWarningIndicator: {0}", VerticalWarningIndicator));
            debugBuilder.AppendLine(string.Format("      VerticalFigureOfMerit: {0} m", VerticalFigureOfMerit));
            debugBuilder.AppendLine(string.Format("VFOM >= 32766 indicator set: {0}", VerticalFigureOfMeritGreaterThanOrEqual32766));
            debugBuilder.AppendLine(string.Format("   VFOM not available value: {0}", VerticalFigureOfMeritNotAvailable));
            return debugBuilder.ToString();
        }

        private void ParseFromBytes()
        {
            int OwnshipGeometricAltitudeLength = 4;

            if (ValidCRC)
            {
                if (MessageData.Length == OwnshipGeometricAltitudeLength)
                {
                    short geoAltitudeIn5FtIncrements = (short)((MessageData[0] << 8) | MessageData[1]);
                    GeometricAltitude = geoAltitudeIn5FtIncrements * 5;

                    ushort verticalMetrics = (ushort)((MessageData[2] << 8) | MessageData[3]);
                    VerticalWarningIndicator = (verticalMetrics & 0x8000) != 0;
                    VerticalFigureOfMerit = verticalMetrics & 0x7FFF;
                    VerticalFigureOfMeritNotAvailable = VerticalFigureOfMerit == 0x7FFF;
                    VerticalFigureOfMeritGreaterThanOrEqual32766 = VerticalFigureOfMerit == 0x7FFE;
                }
                else
                {
                    Console.WriteLine("Warning: Unexpected length in Ownship Geometric Altitude message: expected {0} got {1}. Skipping parse attempt.", OwnshipGeometricAltitudeLength, MessageData.Length);
                    Console.WriteLine(Convert.ToHexString(MessageData));
                }
            }
        }
    }
}