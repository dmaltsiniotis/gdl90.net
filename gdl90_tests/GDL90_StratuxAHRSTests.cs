using GDL90;
using Xunit;

namespace GDL90.Tests;

public class StratuxAHRSTests
{
    [Fact]
    public void StratuxAHRS_Parses_Fields_From_Message()
    {
        byte[] ahrsMessage = new byte[]
        {
            0x4C,
            0x45,
            0x01,
            0x01,
            0x00, 0x64, // Roll = 10.0
            0xFF, 0xE7, // Pitch = -2.5
            0x04, 0xD2, // Heading = 123.4
            0x7F, 0xFF, // Slip/skid invalid
            0xFF, 0xCE, // Yaw rate = -5.0
            0x00, 0x0A, // G = 1.0
            0x00, 0xFA, // Airspeed = 25.0
            0x17, 0x70, // Pressure altitude raw 6000 => 1000 ft
            0x01, 0xF4, // Vertical speed = 500
            0x7F, 0xFF  // Reserved
        };

        Message parsed = MessageFactory.CreateMessageFromBytes(Message.AppendFlagBytes(Message.AppendCRC(ahrsMessage)));
        StratuxAHRS ahrs = Assert.IsType<StratuxAHRS>(parsed);

        Assert.True(ahrs.ValidCRC);
        Assert.Equal((byte)0x45, ahrs.StatusByte1);
        Assert.Equal((byte)0x01, ahrs.StatusByte2);
        Assert.Equal((byte)0x01, ahrs.StatusByte3);

        Assert.Equal(10.0f, ahrs.RollDegrees);
        Assert.Equal(-2.5f, ahrs.PitchDegrees);
        Assert.Equal(123.4f, ahrs.HeadingDegrees, 3);
        Assert.True(float.IsNaN(ahrs.SlipSkid));
        Assert.Equal(-5.0f, ahrs.YawRate);
        Assert.Equal(1.0f, ahrs.GLoad);
        Assert.Equal(25.0f, ahrs.Airspeed);
        Assert.Equal(1000, ahrs.PressureAltitude);
        Assert.Equal(500, ahrs.VerticalSpeed);
        Assert.Equal((byte)0x7F, ahrs.ReservedByte1);
        Assert.Equal((byte)0xFF, ahrs.ReservedByte2);
    }

    [Fact]
    public void StratuxAHRS_Handles_Invalid_Sentinel_Values()
    {
        byte[] ahrsMessage = new byte[]
        {
            0x4C,
            0x45,
            0x01,
            0x01,
            0x7F, 0xFF,
            0x7F, 0xFF,
            0x7F, 0xFF,
            0x7F, 0xFF,
            0x7F, 0xFF,
            0x7F, 0xFF,
            0x7F, 0xFF,
            0xFF, 0xFF,
            0x7F, 0xFF,
            0x7F, 0xFF
        };

        StratuxAHRS ahrs = new StratuxAHRS(Message.AppendFlagBytes(Message.AppendCRC(ahrsMessage)));

        Assert.True(ahrs.ValidCRC);
        Assert.True(float.IsNaN(ahrs.RollDegrees));
        Assert.True(float.IsNaN(ahrs.PitchDegrees));
        Assert.True(float.IsNaN(ahrs.HeadingDegrees));
        Assert.True(float.IsNaN(ahrs.SlipSkid));
        Assert.True(float.IsNaN(ahrs.YawRate));
        Assert.True(float.IsNaN(ahrs.GLoad));
        Assert.True(float.IsNaN(ahrs.Airspeed));
        Assert.Equal(int.MinValue, ahrs.PressureAltitude);
        Assert.Equal(int.MinValue, ahrs.VerticalSpeed);
    }
}