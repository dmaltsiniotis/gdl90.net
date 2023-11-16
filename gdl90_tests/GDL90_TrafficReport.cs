using System;
using System.Buffers;

namespace GDL90.Tests {
    public class TrafficReportTest
    {
        /*
        This section presents a fully worked-out example of a typical Traffic Report, for a target airborne
        over Salem OR, stated in byte order including the Message ID.
        Report Data:
            No Traffic Alert
            ICAO ADS-B Address (octal): 52642511
            Latitude: 44.90708 (North)
            Longitude: -122.99488 (West)
            Altitude: 5,000 feet (pressure altitude)
            Airborne with True Track
            HPL = 20 meters, HFOM = 25 meters (NIC = 10, NACp = 9)
            Horizontal velocity: 123 knots at 45 degrees (True Track)
            Vertical velocity: 64 FPM climb
            Emergency/Priority Code: none
            Emitter Category: Light
        */
        private readonly byte[] TrafficReportExample = new byte[]{ 0x14, 0x00, 0xAB, 0x45, 0x49, 0x1F, 0xEF, 0x15, 0xA8, 0x89, 0x78, 0x0F, 0x09, 0xA9, 0x07, 0xB0, 0x01, 0x20, 0x01, 0x4E, 0x38, 0x32, 0x35, 0x56, 0x20, 0x20, 0x20, 0x00};
        public TrafficReportTest()
        {
            TrafficReport tf = new TrafficReport(Message.AppendFlagBytes(Message.AppendCRC(TrafficReportExample)));

            Console.WriteLine("       Traffic Report");
            Console.WriteLine("---------------------------");
            tf.PrintDebugInfo();
            Console.WriteLine("");

            if (tf.TrafficAlertStatus != TrafficReport.TrafficAlertStatusEnum.No_alert)
                Console.WriteLine(string.Format("ERROR: Incorrect TrafficAlertStatus. Expected {0} got {1}", TrafficReport.TrafficAlertStatusEnum.No_alert, tf.TrafficAlertStatus));

            if (tf.ParticipantAddress != 0xAB4549)
                Console.WriteLine(string.Format("ERROR: Incorrect ParticipantAddress. Expected {0} got {1}", Convert.ToString(0xAB4549, 16), Convert.ToString(tf.ParticipantAddress, 8)));

            if (tf.Latitude != 44.90708f)
                Console.WriteLine(string.Format("ERROR: Incorrect Latitude. Expected {0} got {1}", 44.90708, tf.Latitude));

            if (tf.Longitude != -122.99488f)
                Console.WriteLine(string.Format("ERROR: Incorrect Longitude. Expected {0} got {1}", -122.99488, tf.Longitude));

            if (tf.Altitude != 5000)
                Console.WriteLine(string.Format("ERROR: Incorrect Altitude. Expected {0} got {1}", 5000, tf.Altitude));

            if (tf.HorizontalVelocity != 123)
                Console.WriteLine(string.Format("ERROR: Incorrect HorizontalVelocity. Expected {0} got {1}", 123, tf.HorizontalVelocity));

            if (tf.VerticalVelocity != 64)
                Console.WriteLine(string.Format("ERROR: Incorrect VerticalVelocity. Expected {0} got {1}", 64, tf.VerticalVelocity));

            if (tf.Heading != 45)
                Console.WriteLine(string.Format("ERROR: Incorrect Heading. Expected {0} got {1}", 45, tf.Heading));

            if (tf.Heading != 45)
                Console.WriteLine(string.Format("ERROR: Incorrect Heading. Expected {0} got {1}", 45, tf.Heading));

            if (tf.HeadingType != TrafficReport.HeadingTypeEnum.TrueTrackAngle)
                Console.WriteLine(string.Format("ERROR: Incorrect HeadingType. Expected {0} got {1}", TrafficReport.HeadingTypeEnum.TrueTrackAngle, tf.HeadingType));

            if (tf.AirGroundState != TrafficReport.AirGroundStateEnum.Airborne)
                Console.WriteLine(string.Format("ERROR: Incorrect AirGroundState. Expected {0} got {1}", TrafficReport.AirGroundStateEnum.Airborne, tf.AirGroundState));

            if (tf.TrafficReportUpdate != TrafficReport.TrafficReportUpdateEnum.ReportUpdated)
                Console.WriteLine(string.Format("ERROR: Incorrect TrafficReportUpdate. Expected {0} got {1}", TrafficReport.TrafficReportUpdateEnum.ReportUpdated, tf.TrafficReportUpdate));

            if (tf.Callsign != "N825V")
                Console.WriteLine(string.Format("ERROR: Incorrect Callsign. Expected '{0}' got '{1}'", "N825V", tf.Callsign));
        }
    }
}