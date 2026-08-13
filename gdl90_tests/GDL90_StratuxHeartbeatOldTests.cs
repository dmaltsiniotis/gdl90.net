using GDL90;
using Xunit;

namespace GDL90.Tests;

public class StratuxHeartbeatOldTests
{
    [Fact]
    public void StratuxHeartbeatOld_Parses_FixedFields_NoTowers()
    {
        byte[] message = new byte[]
        {
            0x53, // 'S'
            0x58, // 'X'
            0x01, // message class
            0x01, // message version
            0x02, // stratux version major
            0x03, // stratux version minor
            0x01, // build type
            0x10, // build number
            0xFF, 0xFF, 0xFF, 0xFF, // hardware revision bytes
            0x01, // enabled flags (AHRS enabled)
            0xFE, // valid flags
            0x00, // reserved byte 14
            0x07, // connected hw flags (2 radios + imu connected)
            0x08, // gps sats locked
            0x0A, // gps sats tracked
            0x01, 0x02, // uat traffic targets = 258
            0x00, 0x05, // es traffic targets = 5
            0x00, 0x64, // uat messages last minute = 100
            0x01, 0x2C, // es messages last minute = 300
            0x00, 0xFA, // cpu temp raw = 250 => 25.0 C
            0x00 // tower count
        };

        Message parsed = MessageFactory.CreateMessageFromBytes(Message.AppendFlagBytes(Message.AppendCRC(message)));
        StratuxHeartbeatOld sx = Assert.IsType<StratuxHeartbeatOld>(parsed);

        Assert.True(sx.ValidCRC);
        Assert.Equal((byte)0x58, sx.HeaderByte);
        Assert.Equal((byte)0x01, sx.MessageClass);
        Assert.Equal((byte)0x01, sx.MessageVersion);
        Assert.Equal((byte)0x02, sx.StratuxVersionMajor);
        Assert.Equal((byte)0x03, sx.StratuxVersionMinor);
        Assert.Equal((byte)0x01, sx.StratuxBuildType);
        Assert.Equal((byte)0x10, sx.StratuxBuildNumber);

        Assert.True(sx.AHRSEnabled);
        Assert.Equal(GPSFixQuality.ThreeDFix, sx.GPSFixQuality);
        Assert.True(sx.AHRSValid);
        Assert.True(sx.PressureAltitudeValid);
        Assert.True(sx.CPUTempValid);
        Assert.True(sx.UATEnabled);
        Assert.True(sx.ESEnabled);
        Assert.True(sx.GPSEnabled);

        Assert.Equal(3, sx.NumberOfRadios);
        Assert.True(sx.IMUConnected);
        Assert.Equal(8, sx.GPSSatellitesLocked);
        Assert.Equal(10, sx.GPSSatellitesTracked);
        Assert.Equal(258, sx.UATTrafficTargetsTracking);
        Assert.Equal(5, sx.ESTrafficTargetsTracking);
        Assert.Equal(100, sx.UATMessagesLastMinute);
        Assert.Equal(300, sx.ESMessagesLastMinute);
        Assert.Equal((ushort)250, sx.CPUTemperatureRaw);
        Assert.Equal(25.0f, sx.CPUTemperatureCelsius);
        Assert.Equal(0, sx.ADSBTowerCount);
        Assert.Empty(sx.ADSBTowers);
    }

    [Fact]
    public void StratuxHeartbeatOld_Parses_OneTower()
    {
        byte[] message = new byte[]
        {
            0x53, // 'S'
            0x58, // 'X'
            0x01,
            0x01,
            0x02,
            0x03,
            0x01,
            0x10,
            0xFF, 0xFF, 0xFF, 0xFF,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x01, // one tower
            0x20, 0x00, 0x00, // latitude 45.0
            0xA8, 0x89, 0x78  // longitude ~-122.99488
        };

        StratuxHeartbeatOld sx = new StratuxHeartbeatOld(Message.AppendFlagBytes(Message.AppendCRC(message)));

        Assert.True(sx.ValidCRC);
        Assert.Equal(1, sx.ADSBTowerCount);
        Assert.Single(sx.ADSBTowers);
        Assert.Equal(45.0f, sx.ADSBTowers[0].Latitude, precision: 4);
        Assert.Equal(-122.99488f, sx.ADSBTowers[0].Longitude, precision: 4);
    }
}