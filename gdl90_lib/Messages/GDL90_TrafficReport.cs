using System;
using System.Buffers;

namespace GDL90 {
    public class TrafficReport : Message {
        public TrafficAlertStatusEnum TrafficAlertStatus = TrafficAlertStatusEnum.Not_Set;
        public TargetIdentityEnum AddressType = TargetIdentityEnum.Not_Set;
        public int ParticipantAddress = 0;
        public float Latitude = 0.0f;
        public float Longitude = 0.0f;
        public int Altitude = 0;
        public AirGroundStateEnum AirGroundState = AirGroundStateEnum.Not_Set;
        public TrafficReportUpdateEnum TrafficReportUpdate = TrafficReportUpdateEnum.Not_Set;
        public HeadingTypeEnum HeadingType = HeadingTypeEnum.Not_Set;
        NavigationIntegrityCategoryEnum NavigationIntegrityCategory = NavigationIntegrityCategoryEnum.Not_Set;
        NavigationAccuracyCategoryEnum NavigationAccuracyCategory = NavigationAccuracyCategoryEnum.Not_Set;
        public uint HorizontalVelocity = 0xFFF;
        public int VerticalVelocity = 0x800;
        public uint Heading = 0;
        public EmitterCategoryEnum EmitterCategory = EmitterCategoryEnum.Not_Set;
        public string Callsign = "";
        public PriorityCodeEnum PriorityCode = PriorityCodeEnum.Not_Set;

        public TrafficReport(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            ParseFromBytes();

            // if (!ValidCRC) {
            //     Console.WriteLine("GLD90 Message warning: Invalid CRC. Expected: {0:X4} Actual: {1:X4}", MessageCRC, ComputedCRC);
            //     Console.WriteLine(Convert.ToHexString(messageDataWithIdAndFcsAndFlagBytes));
            //     Console.WriteLine("");
            // }
        }

        public override void PrintDebugInfo()
        {
            System.Text.StringBuilder debugBuilder = new System.Text.StringBuilder();
            debugBuilder.AppendLine(string.Format("                  MessageId: 0x{0:D2} ({1})", (int)MessageId, MessageId));
            debugBuilder.AppendLine(string.Format("                   ValidCRC: {0} (Expected: {1} Computed: {2})", ValidCRC, MessageCRC, ComputedCRC));
            debugBuilder.AppendLine(string.Format("         TrafficAlertStatus: {0} ({1})", (int)TrafficAlertStatus, TrafficAlertStatus));
            debugBuilder.AppendLine(string.Format("                AddressType: {0} ({1})", (int)AddressType, AddressType));
            debugBuilder.AppendLine(string.Format("         ParticipantAddress: 0x{0} (dec: {1}, oct: {2})", Convert.ToString(ParticipantAddress, 16).ToUpper(), ParticipantAddress, Convert.ToString(ParticipantAddress, 8)));
            debugBuilder.AppendLine(string.Format("                   Latitude: {0}", Latitude));
            debugBuilder.AppendLine(string.Format("                  Longitude: {0}", Longitude));
            debugBuilder.AppendLine(string.Format("                   Altitude: {0} ft.", Altitude));
            debugBuilder.AppendLine(string.Format("             AirGroundState: {0} ({1})", (int)AirGroundState, AirGroundState));
            debugBuilder.AppendLine(string.Format("        TrafficReportUpdate: {0} ({1})", (int)TrafficReportUpdate, TrafficReportUpdate));
            debugBuilder.AppendLine(string.Format("                HeadingType: {0} ({1})", (int)HeadingType, HeadingType));
            debugBuilder.AppendLine(string.Format("NavigationIntegrityCategory: {0} ({1})", (int)NavigationIntegrityCategory, NavigationIntegrityCategory));
            debugBuilder.AppendLine(string.Format(" NavigationAccuracyCategory: {0} ({1})", (int)NavigationAccuracyCategory, NavigationAccuracyCategory));
            debugBuilder.AppendLine(string.Format("         HorizontalVelocity: {0} Knots", HorizontalVelocity));
            debugBuilder.AppendLine(string.Format("           VerticalVelocity: {0} FPM", VerticalVelocity));
            debugBuilder.AppendLine(string.Format("                    Heading: {0:000}", Heading));
            debugBuilder.AppendLine(string.Format("            EmitterCategory: {0} ({1})", (int)EmitterCategory, EmitterCategory));
            debugBuilder.AppendLine(string.Format("                   Callsign: {0}", Callsign));
            debugBuilder.AppendLine(string.Format("               PriorityCode: {0} ({1})", (int)PriorityCode, PriorityCode));
            Console.WriteLine(debugBuilder);
        }

        private void ParseFromBytes()
        {
            int TrafficReportLength = 27;

            if (ValidCRC)
            {
                if (MessageData.Length == TrafficReportLength)
                {
                    // The Traffic Report data consists of 27 bytes of binary data.
                    // Traffic Report data = st aa aa aa ll ll ll nn nn nn dd dm ia hh hv vv tt ee cc cc cc cc cc cc cc cc px
                    byte st = MessageData[0];
                    int s = st >> 4;   // Upper nibble
                    int t = st & 0x0F; // Lower nibble
                    TrafficAlertStatus = (TrafficAlertStatusEnum)s;
                    AddressType = (TargetIdentityEnum)t;
                    ParticipantAddress = BitConverter.ToInt32(new byte[]{MessageData[3], MessageData[2], MessageData[1], 0x00});

                    float encodedResolution = 180.0f / (float)Math.Pow(2, 23); // Resolution = 180 / 223 degrees, ~1.4 degree increments.

                    // 24-bit signed binary fraction.
                    // For Latitude, North is considered Positive. The maximum Latitude value is +90.0 degrees. The minimum Latitude value is -90.0 degrees.
                    Latitude = (BitConverter.ToInt32(new byte[]{0x00, MessageData[6], MessageData[5], MessageData[4]}) >> 8) * encodedResolution;

                    // For Longitude, East is considered Positive. The maximum Longitude value is +(180 minus LSB) degrees. The minimum Longitude value is -180.0 degrees.
                    //bool LSB_Set = Convert.ToBoolean(MessageData[9] & 0x01);
                    Longitude = (BitConverter.ToInt32(new byte[]{0x00, MessageData[9], MessageData[8], MessageData[7]}) >> 8) * encodedResolution;

                    byte dd = MessageData[10];
                    byte dm = MessageData[11];

                    // Build the bytes that comprise the Altitude.
                    int d0 = 0x00;
                    int d1 = (dd >> 4) << 8;
                    int d2 = (dd & 0x0F) << 4;
                    int d3 = dm >> 4;

                    Altitude = (d0 | d1 | d2| d3) * 25 - 1000; // 25 Ft resolution per value, offset by 1000 ft.

                    int m = dm & 0x0F;
                    AirGroundState = (AirGroundStateEnum)(m >> 3);
                    TrafficReportUpdate = (TrafficReportUpdateEnum)(m >> 2 & 0x1);
                    HeadingType = (HeadingTypeEnum)(m & 0x03);

                    byte ia = MessageData[12];
                    int i = ia >> 4;
                    int a = ia & 0x0F;
                    NavigationIntegrityCategory = (NavigationIntegrityCategoryEnum)i;
                    NavigationAccuracyCategory = (NavigationAccuracyCategoryEnum)a;

                    byte hh = MessageData[13];
                    byte hv = MessageData[14];
                    byte vv = MessageData[15];

                    int h1 = (hh >> 4) << 8;
                    int h2 = (hh & 0x0F) << 4;
                    int h3 = hv >> 4;
                    HorizontalVelocity = (uint)(h1 | h2| h3);

                    int v1 = (hv & 0x0F) << 28;
                    int v2 = (vv >> 4) << 24;
                    int v3 = (vv & 0x0F) << 20;
                    VerticalVelocity = ((v1 | v2| v3) >> 20 ) * 64; // Vertical velocity "vvv" is encoded as a 12-bit signed value, in units of 64 feet per minute (FPM).

                    Heading = (uint)(MessageData[16]*(360.0/256.0)); // Heading information is encoded in a single byte, providing only 8 bits (or 255 possible values) of resolution.
                    EmitterCategory = (EmitterCategoryEnum)MessageData[17];
                    Callsign = System.Text.Encoding.ASCII.GetString(new ArraySegment<byte>(MessageData, 18, 8)).Trim();
                    PriorityCode = (PriorityCodeEnum)(MessageData[26] >> 4);
                }
                else
                {
                    Console.WriteLine("Warning: Unexpected length in Traffic Report message: expected {0} got {1}. Skipping parse attempt.", TrafficReportLength, MessageData.Length);
                    Console.WriteLine(Convert.ToHexString(MessageData));
                }
            }
            else
            {
                // Console.WriteLine("Warning: Invalid CRC. Computed: 0x{0:X4}, actual: 0x{1:X4}. Skipping parse attempt.", ComputedCRC, MessageCRC);
            }
        }
        public enum HeadingTypeEnum : int {
            Not_Set = -1,
            NotValid = 0,
            TrueTrackAngle = 1,
            Heading_Magnetic = 2,
            Heading_True = 3
        }

        public enum TrafficReportUpdateEnum : int {
            Not_Set = -1,
            ReportUpdated = 0,
            ReportExtrapolated = 1
        }
        public enum AirGroundStateEnum : int {
            Not_Set = -1,
            OnGround = 0,
            Airborne = 1
        }
        public enum TrafficAlertStatusEnum : int {
            Not_Set = -1,
            No_alert = 0,
            Traffic_Alert = 1,
            Reserved2 = 2,
            Reserved3 = 3,
            Reserved4 = 4,
            Reserved5 = 5,
            Reserved6 = 6,
            Reserved7 = 7,
            Reserved8 = 8,
            Reserved9 = 9,
            Reserved0 = 10,
            Reserved11 = 11,
            Reserved12 = 12,
            Reserved13 = 13,
            Reserved14 = 14,
            Reserved15 = 15
        }
        public enum TargetIdentityEnum : int {
            Not_Set = -1,
            ADS_B_with_ICAO_address = 0,
            ADS_B_with_Self_assigned_address = 1,
            TIS_B_with_ICAO_address = 2,
            TIS_B_with_track_file_ID = 3,
            Surface_Vehicle = 4,
            Ground_Station_Beacon = 5,
            Reserved6 = 6,
            Reserved7 = 7,
            Reserved8 = 8,
            Reserved9 = 9,
            Reserved10 = 10,
            Reserved11 = 11,
            Reserved12 = 12,
            Reserved13 = 13,
            Reserved14 = 14,
            Reserved15 = 15
        }
        public enum PriorityCodeEnum : int {
            Not_Set = -1,
            No_Emergency = 0,
            General_Emergency = 1,
            Medical_Emergency = 2,
            Minimum_Fuel = 3,
            No_Communication = 4,
            Unlawful_Interference = 5,
            Downed_Aircraft = 6,
            Reserved7 = 7,
            Reserved8 = 8,
            Reserved9 = 9,
            Reserved10 = 10,
            Reserved11 = 11,
            Reserved12 = 12,
            Reserved13 = 13,
            Reserved14 = 14,
            Reserved15 = 15
        }
        public enum EmitterCategoryEnum : int {
            Not_Set = -1,
            No_Aircraft_Type_Information = 0,
            Light = 1,
            Small = 2,
            Large = 3,
            HighVortexLarge = 4,
            Heavy = 5,
            HighlyManeuverable = 6,
            Rotorcraft = 7,
            Unassigned8 = 8,
            Glider_Sailplane = 9,
            Lighter_Than_Air = 10,
            Parachutist_Skydiver = 11,
            Ultralight_Hangglider_Paraglider = 12,
            Unassigned13 = 13,
            Unmanned_Aerial_Vehicle = 14,
            Space_Transatmospheric_Vehicle = 15,
            Unassigned16 = 16,
            SurfaceVehicle_EmergencyVehicle = 17,
            SurfaceVehicle_ServiceVehicle = 18,
            Point_Obstacle = 19,
            Cluster_Obstacle = 20,
            Line_Obstacle = 21,
            Reserved22 = 22,
            Reserved23 = 22,
            Reserved24 = 24,
            Reserved25 = 25,
            Reserved26 = 26,
            Reserved27 = 27,
            Reserved28 = 28,
            Reserved29 = 29,
            Reserved30 = 30,
            Reserved31 = 31,
            Reserved32 = 32,
            Reserved33 = 33,
            Reserved34 = 34,
            Reserved35 = 35,
            Reserved36 = 36,
            Reserved37 = 37,
            Reserved38 = 38,
            Reserved39 = 39
        }
        public enum NavigationIntegrityCategoryEnum : int {
            Not_Set = -1,
            Unknown = 0,
            LessThan_20NM = 1,
            LessThan_8_0_NM = 2,
            LessThan_4_0_NM = 3,
            LessThan_2_0_NM = 4,
            LessThan_1_0_NM = 5,
            LessThan_0_6_NM = 6,
            LessThan_0_2_NM = 7,
            LessThan_0_1_NM = 8,
            HPL_LessThan_75m_and_VPL_LessThan_112m = 9,
            HPL_LessThan_25m_and_VPL_LessThan_37_5m = 10,
            HPL_LessThan_7_5m_and_VPL_LessThan_11m = 11,
            Unused12 = 12,
            Unused13 = 13,
            Unused14 = 14,
            Unused15 = 15,
        }
        public enum NavigationAccuracyCategoryEnum : int {
            Not_Set = -1,
            Unknown = 0,
            LessThan_20NM = 1,
            LessThan_4_0_NM = 2,
            LessThan_2_0_NM = 3,
            LessThan_1_0_NM = 4,
            LessThan_0_5_NM = 5,
            LessThan_0_3_NM = 6,
            LessThan_0_1_NM = 7,
            LessThan_0_05_NM =- 8,
            HFOM_LessThan_30m_and_VFOM_LessThan_45m = 9,
            HFOM_LessThan_10m_and_VFOM_LessThan_15m = 10,
            HFOM_LessThan_3m_and_VFOM_LessThan_4m = 11,
            Unused12 = 12,
            Unused13 = 13,
            Unused14 = 14,
            Unused15 = 15,
        }
    }
}