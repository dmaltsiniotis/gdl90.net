using System;
using Xunit;

namespace GDL90.Tests {
    public class OwnshipReportTest
    {
        /*
        This section presents a fully worked-out example of a typical Ownship Report, for a target airborne
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
        private readonly byte[] OwnshipReportExample = new byte[]{ 0x14, 0x00, 0xAB, 0x45, 0x49, 0x1F, 0xEF, 0x15, 0xA8, 0x89, 0x78, 0x0F, 0x09, 0xA9, 0x07, 0xB0, 0x01, 0x20, 0x01, 0x4E, 0x38, 0x32, 0x35, 0x56, 0x20, 0x20, 0x20, 0x00};

        [Fact]
        public void OwnshipReport_Parses_Example_Message()
        {
            OwnshipReport tf = new OwnshipReport(Message.AppendFlagBytes(Message.AppendCRC(OwnshipReportExample)));

            Assert.True(tf.ValidCRC);
            Assert.Equal(OwnshipReport.TrafficAlertStatusEnum.No_alert, tf.TrafficAlertStatus);
            Assert.Equal(0xAB4549, tf.ParticipantAddress);
            Assert.Equal(44.90708f, tf.Latitude, precision: 4);
            Assert.Equal(-122.99488f, tf.Longitude, precision: 4);
            Assert.Equal(5000, tf.Altitude);
            Assert.Equal((uint)123, tf.HorizontalVelocity);
            Assert.Equal(64, tf.VerticalVelocity);
            Assert.Equal(45u, tf.Heading);
            Assert.Equal(OwnshipReport.HeadingTypeEnum.TrueTrackAngle, tf.HeadingType);
            Assert.Equal(OwnshipReport.AirGroundStateEnum.Airborne, tf.AirGroundState);
            Assert.Equal(OwnshipReport.TrafficReportUpdateEnum.ReportUpdated, tf.TrafficReportUpdate);
            Assert.Equal("N825V", tf.Callsign);
        }
    }
}