using GDL90;
using Xunit;

namespace GDL90.Tests;

public class BasicReportTests
{
    [Fact]
    public void BasicReport_Parses_Tor_And_Payload()
    {
        byte[] message = new byte[436]; // This is just a variation of the UplinkData message, so we need to look for a larger message size then sub-divide.
        message[0] = 0x1E; // Basic Report message ID.

        // TOR is LSB first. Raw value = 3.
        message[1] = 0x03;
        message[2] = 0x00;
        message[3] = 0x00;

        // Fill 18-byte payload with a safe deterministic pattern.
        for (int i = 0; i < 18; i++)
        {
            message[4 + i] = 0x55;
        }

        Message parsed = MessageFactory.CreateMessageFromBytes(Message.AppendFlagBytes(Message.AppendCRC(message)));
        BasicReport report = Assert.IsType<BasicReport>(parsed);

        Assert.True(report.ValidCRC);
        Assert.Equal(3, report.TimeOfReceptionRaw);
        Assert.True(report.TimeOfReceptionValid);
        Assert.Equal(240e-9, report.TimeOfReceptionSeconds, 12);

        Assert.Equal(18, report.BasicPayload.Length);
        Assert.Equal((byte)0x55, report.BasicPayload[0]);
        Assert.Equal((byte)0x55, report.BasicPayload[17]);
    }

    [Fact]
    public void BasicReport_Parses_Invalid_Tor_Marker()
    {
        byte[] message = new byte[436]; // This is just a variation of the UplinkData message, so we need to look for a larger message size then sub-divide.
        message[0] = 0x1E;

        // TOR invalid marker per spec.
        message[1] = 0xFF;
        message[2] = 0xFF;
        message[3] = 0xFF;

        BasicReport report = new BasicReport(Message.AppendFlagBytes(Message.AppendCRC(message)));

        Assert.True(report.ValidCRC);
        Assert.Equal(0xFFFFFF, report.TimeOfReceptionRaw);
        Assert.False(report.TimeOfReceptionValid);
        Assert.Equal(18, report.BasicPayload.Length);
    }
}