using GDL90;
using Xunit;

namespace GDL90.Tests;

public class ForeflightAHRSTests
{
    [Fact]
    public void ForeflightAHRS_Parses_Fields_From_Go_Layout()
    {
        byte[] message = new byte[]
        {
            0x65, // ForeFlight message type
            0x01, // AHRS message identifier
            0x00, 0x7B, // Roll = 12.3
            0xFF, 0xE8, // Pitch = -2.4
            0x07, 0x08, // Heading = 180.0
            0x00, 0x64, // IAS = 100
            0x00, 0x6E  // TAS = 110
        };

        Message parsed = MessageFactory.CreateMessageFromBytes(Message.AppendFlagBytes(Message.AppendCRC(message)));
        ForeflightAHRS ahrs = Assert.IsType<ForeflightAHRS>(parsed);

        Assert.True(ahrs.ValidCRC);
        Assert.Equal((byte)0x01, ahrs.MessageIdentifier);
        Assert.Equal(12.3f, ahrs.RollDegrees, precision: 3);
        Assert.Equal(-2.4f, ahrs.PitchDegrees, precision: 3);
        Assert.Equal(180.0f, ahrs.HeadingDegrees, precision: 3);
        Assert.Equal(100, ahrs.IndicatedAirspeed);
        Assert.Equal(110, ahrs.TrueAirspeed);
    }

    [Fact]
    public void ForeflightAHRS_Handles_Invalid_Sentinel_Values()
    {
        byte[] message = new byte[]
        {
            0x65,
            0x01,
            0x7F, 0xFF,
            0x7F, 0xFF,
            0xFF, 0xFF,
            0xFF, 0xFF,
            0xFF, 0xFF
        };

        ForeflightAHRS ahrs = new ForeflightAHRS(Message.AppendFlagBytes(Message.AppendCRC(message)));

        Assert.True(ahrs.ValidCRC);
        Assert.Equal((byte)0x01, ahrs.MessageIdentifier);
        Assert.True(float.IsNaN(ahrs.RollDegrees));
        Assert.True(float.IsNaN(ahrs.PitchDegrees));
        Assert.True(float.IsNaN(ahrs.HeadingDegrees));
        Assert.Equal(-1, ahrs.IndicatedAirspeed);
        Assert.Equal(-1, ahrs.TrueAirspeed);
    }
}