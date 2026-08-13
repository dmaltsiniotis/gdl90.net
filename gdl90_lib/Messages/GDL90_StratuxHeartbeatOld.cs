using System;
using System.Collections.Generic;
using System.Text;

namespace GDL90 {
    public class StratuxHeartbeatOld : Message {
        public byte HeaderByte = 0x00; // Expected to be 'X' (0x58).
        public byte MessageClass = 0;
        public byte MessageVersion = 0;

        public byte StratuxVersionMajor = 0;
        public byte StratuxVersionMinor = 0;
        public byte StratuxBuildType = 0;
        public byte StratuxBuildNumber = 0;

        public byte HardwareRevisionByte1 = 0;
        public byte HardwareRevisionByte2 = 0;
        public byte HardwareRevisionByte3 = 0;
        public byte HardwareRevisionByte4 = 0;

        public byte EnabledFlags = 0; // msg[12]
        public byte ValidFlags = 0;   // msg[13]
        public byte ReservedByte14 = 0;
        public byte ConnectedHardwareFlags = 0;

        public GPSFixQuality GPSFixQuality = GPSFixQuality.NoFix;
        public bool AHRSValid = false;
        public bool PressureAltitudeValid = false;
        public bool CPUTempValid = false;
        public bool UATEnabled = false;
        public bool ESEnabled = false;
        public bool GPSEnabled = false;
        public bool AHRSEnabled = false;

        public int NumberOfRadios = 0;
        public bool IMUConnected = false;

        public int GPSSatellitesLocked = 0;
        public int GPSSatellitesTracked = 0;
        public int UATTrafficTargetsTracking = 0;
        public int ESTrafficTargetsTracking = 0;
        public int UATMessagesLastMinute = 0;
        public int ESMessagesLastMinute = 0;
        public ushort CPUTemperatureRaw = 0;
        public float CPUTemperatureCelsius = 0.0f;

        public int ADSBTowerCount = 0;
        public List<Tower> ADSBTowers = new List<Tower>();

        public StratuxHeartbeatOld(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();
        }

        public override string ToShortString()
        {
            return string.Format("Stratux Heartbeat SX: v{0}.{1} t{2} b{3} GPSFix={4} Sats={5}/{6} Towers={7}", StratuxVersionMajor, StratuxVersionMinor, StratuxBuildType, StratuxBuildNumber, GPSFixQuality.ToString(), GPSSatellitesLocked, GPSSatellitesTracked, ADSBTowerCount);
        }

        public override string ToDetailedString()
        {
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.AppendLine(string.Format("                  MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format("                   ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));
            debugBuilder.AppendLine(string.Format("                 HeaderByte: 0x{0:X2}", HeaderByte));
            debugBuilder.AppendLine(string.Format("               MessageClass: {0}", MessageClass));
            debugBuilder.AppendLine(string.Format("             MessageVersion: {0}", MessageVersion));
            debugBuilder.AppendLine(string.Format("        StratuxVersionMajor: {0}", StratuxVersionMajor));
            debugBuilder.AppendLine(string.Format("        StratuxVersionMinor: {0}", StratuxVersionMinor));
            debugBuilder.AppendLine(string.Format("           StratuxBuildType: {0}", StratuxBuildType));
            debugBuilder.AppendLine(string.Format("         StratuxBuildNumber: {0}", StratuxBuildNumber));
            debugBuilder.AppendLine(string.Format("               EnabledFlags: 0x{0:X2}", EnabledFlags));
            debugBuilder.AppendLine(string.Format("                 ValidFlags: 0x{0:X2}", ValidFlags));
            debugBuilder.AppendLine(string.Format("              GPSFixQuality: {0}", GPSFixQuality));
            debugBuilder.AppendLine(string.Format("                  AHRSValid: {0}", AHRSValid));
            debugBuilder.AppendLine(string.Format("      PressureAltitudeValid: {0}", PressureAltitudeValid));
            debugBuilder.AppendLine(string.Format("               CPUTempValid: {0}", CPUTempValid));
            debugBuilder.AppendLine(string.Format("                 UATEnabled: {0}", UATEnabled));
            debugBuilder.AppendLine(string.Format("                  ESEnabled: {0}", ESEnabled));
            debugBuilder.AppendLine(string.Format("                 GPSEnabled: {0}", GPSEnabled));
            debugBuilder.AppendLine(string.Format("                AHRSEnabled: {0}", AHRSEnabled));
            debugBuilder.AppendLine(string.Format("             NumberOfRadios: {0}", NumberOfRadios));
            debugBuilder.AppendLine(string.Format("               IMUConnected: {0}", IMUConnected));
            debugBuilder.AppendLine(string.Format("        GPSSatellitesLocked: {0}", GPSSatellitesLocked));
            debugBuilder.AppendLine(string.Format("       GPSSatellitesTracked: {0}", GPSSatellitesTracked));
            debugBuilder.AppendLine(string.Format("  UATTrafficTargetsTracking: {0}", UATTrafficTargetsTracking));
            debugBuilder.AppendLine(string.Format("   ESTrafficTargetsTracking: {0}", ESTrafficTargetsTracking));
            debugBuilder.AppendLine(string.Format("      UATMessagesLastMinute: {0}", UATMessagesLastMinute));
            debugBuilder.AppendLine(string.Format("       ESMessagesLastMinute: {0}", ESMessagesLastMinute));
            debugBuilder.AppendLine(string.Format("          CPUTemperatureRaw: {0}", CPUTemperatureRaw));
            debugBuilder.AppendLine(string.Format("      CPUTemperatureCelsius: {0}", CPUTemperatureCelsius));
            debugBuilder.AppendLine(string.Format("             ADSBTowerCount: {0}", ADSBTowerCount));
            return debugBuilder.ToString();
        }

        private void ParseFromBytes()
        {
            const int StratuxHeartbeatOldMinimumLength = 28;

            if (ValidCRC)
            {
                if (MessageData.Length >= StratuxHeartbeatOldMinimumLength)
                {
                    HeaderByte = MessageData[0];
                    MessageClass = MessageData[1];
                    MessageVersion = MessageData[2];

                    StratuxVersionMajor = MessageData[3];
                    StratuxVersionMinor = MessageData[4];
                    StratuxBuildType = MessageData[5];
                    StratuxBuildNumber = MessageData[6];

                    HardwareRevisionByte1 = MessageData[7];
                    HardwareRevisionByte2 = MessageData[8];
                    HardwareRevisionByte3 = MessageData[9];
                    HardwareRevisionByte4 = MessageData[10];

                    EnabledFlags = MessageData[11];
                    ValidFlags = MessageData[12];
                    ReservedByte14 = MessageData[13];
                    ConnectedHardwareFlags = MessageData[14];

                    AHRSEnabled = (EnabledFlags & (1 << 0)) != 0;

                    GPSFixQuality = (GPSFixQuality)(ValidFlags & 0x03);
                    AHRSValid = (ValidFlags & (1 << 2)) != 0;
                    PressureAltitudeValid = (ValidFlags & (1 << 3)) != 0;
                    CPUTempValid = (ValidFlags & (1 << 4)) != 0;
                    UATEnabled = (ValidFlags & (1 << 5)) != 0;
                    ESEnabled = (ValidFlags & (1 << 6)) != 0;
                    GPSEnabled = (ValidFlags & (1 << 7)) != 0;

                    NumberOfRadios = ConnectedHardwareFlags & 0x03;
                    IMUConnected = (ConnectedHardwareFlags & (1 << 2)) != 0;

                    GPSSatellitesLocked = MessageData[15];
                    GPSSatellitesTracked = MessageData[16];
                    UATTrafficTargetsTracking = ReadUInt16BE(MessageData, 17);
                    ESTrafficTargetsTracking = ReadUInt16BE(MessageData, 19);
                    UATMessagesLastMinute = ReadUInt16BE(MessageData, 21);
                    ESMessagesLastMinute = ReadUInt16BE(MessageData, 23);

                    CPUTemperatureRaw = ReadUInt16BE(MessageData, 25);
                    CPUTemperatureCelsius = CPUTemperatureRaw / 10.0f;

                    ADSBTowerCount = MessageData[27];

                    ADSBTowers.Clear();
                    int bytesAvailableForTowers = MessageData.Length - 28;
                    int maxTowerCountFromPayload = bytesAvailableForTowers / 6;
                    int towerCountToDecode = Math.Min(ADSBTowerCount, maxTowerCountFromPayload);
                    int towerOffset = 28;

                    for (int i = 0; i < towerCountToDecode; i++)
                    {
                        float lat = DecodeLatLng24(MessageData[towerOffset], MessageData[towerOffset + 1], MessageData[towerOffset + 2]);
                        float lng = DecodeLatLng24(MessageData[towerOffset + 3], MessageData[towerOffset + 4], MessageData[towerOffset + 5]);
                        ADSBTowers.Add(new Tower { Latitude = lat, Longitude = lng });
                        towerOffset += 6;
                    }
                }
                else
                {
                    Console.WriteLine("Warning: Unexpected length in Stratux Heartbeat Old message: expected at least {0} got {1}. Skipping parse attempt.", StratuxHeartbeatOldMinimumLength, MessageData.Length);
                    Console.WriteLine(Convert.ToHexString(MessageData));
                }
            }
        }

        private static ushort ReadUInt16BE(byte[] data, int start)
        {
            return (ushort)((data[start] << 8) | data[start + 1]);
        }

        private static float DecodeLatLng24(byte msb, byte mid, byte lsb)
        {
            int raw24 = (msb << 16) | (mid << 8) | lsb;
            if ((raw24 & 0x800000) != 0)
            {
                raw24 |= unchecked((int)0xFF000000);
            }

            float resolution = 180.0f / (float)Math.Pow(2, 23);
            return raw24 * resolution;
        }
    }

    public class Tower
    {
        public float Latitude = 0.0f;
        public float Longitude = 0.0f;
    }

    public enum GPSFixQuality : int
    {
        NoFix = 0,
        TwoDFix = 1,
        ThreeDFix = 2
    }
}