using System;
using System.Text;

namespace GDL90 {
    public class ForeflightStatus : Message {
        public byte MessageIdentifier = 0;
        public byte MessageVersion = 0;
        public byte[] DeviceSerialNumber = new byte[8];
        public string DeviceShortName = "";
        public string DeviceLongName = "";
        public byte ReservedByte1 = 0;
        public byte ReservedByte2 = 0;
        public byte ReservedByte3 = 0;
        public byte CapabilitiesMask = 0;

        public ForeflightStatus(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();
        }

        public override string ToShortString()
        {
            return string.Format("ForeFlight Status: MessageIdentifier {0} MessageVersion {1} DeviceShortName '{2}' DeviceLongName '{3}' Capabilities 0x{4:X2}", MessageIdentifier, MessageVersion, DeviceShortName, DeviceLongName, CapabilitiesMask);
        }

        public override string ToDetailedString()
        {
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.AppendLine(string.Format("                  MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format("                   ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));
            debugBuilder.AppendLine(string.Format("          MessageIdentifier: {0}", MessageIdentifier));
            debugBuilder.AppendLine(string.Format("             MessageVersion: {0}", MessageVersion));
            debugBuilder.AppendLine(string.Format("         DeviceSerialNumber: {0}", Convert.ToHexString(DeviceSerialNumber)));
            debugBuilder.AppendLine(string.Format("            DeviceShortName: {0}", DeviceShortName));
            debugBuilder.AppendLine(string.Format("             DeviceLongName: {0}", DeviceLongName));
            debugBuilder.AppendLine(string.Format("              ReservedByte1: 0x{0:X2}", ReservedByte1));
            debugBuilder.AppendLine(string.Format("              ReservedByte2: 0x{0:X2}", ReservedByte2));
            debugBuilder.AppendLine(string.Format("              ReservedByte3: 0x{0:X2}", ReservedByte3));
            debugBuilder.AppendLine(string.Format("           CapabilitiesMask: 0x{0:X2}", CapabilitiesMask));
            return debugBuilder.ToString();
        }

        private void ParseFromBytes()
        {
            int ForeflightStatusLength = 38;

            if (ValidCRC)
            {
                if (MessageData.Length == ForeflightStatusLength)
                {
                    MessageIdentifier = MessageData[0];
                    MessageVersion = MessageData[1];

                    Array.Copy(MessageData, 2, DeviceSerialNumber, 0, 8);

                    DeviceShortName = Encoding.ASCII.GetString(MessageData, 10, 8).TrimEnd('\0', ' ');
                    DeviceLongName = Encoding.ASCII.GetString(MessageData, 18, 16).TrimEnd('\0', ' ');

                    ReservedByte1 = MessageData[34];
                    ReservedByte2 = MessageData[35];
                    ReservedByte3 = MessageData[36];
                    CapabilitiesMask = MessageData[37];
                }
                else
                {
                    Console.WriteLine("Warning: Unexpected length in ForeFlight Status message: expected {0} got {1}. Skipping parse attempt.", ForeflightStatusLength, MessageData.Length);
                    Console.WriteLine(Convert.ToHexString(MessageData));
                }
            }
        }
    }
}