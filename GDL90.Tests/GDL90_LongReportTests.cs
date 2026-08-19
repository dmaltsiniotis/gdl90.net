using Xunit;
using GDL90.Core;
using GDL90.Core.Messages;

namespace GDL90.Tests;

public class LongReportTests
{
    [Fact]
    public void LongReport_Parses_Tor_And_Payload()
    {
        byte[] message = new byte[436]; // This is just a variation of the UplinkData message, so we need to look for a larger message size then sub-divide.
        message[0] = 0x1F; // Long Report message ID.

        // TOR is LSB first. Raw value = 0x000002.
        message[1] = 0x02;
        message[2] = 0x00;
        message[3] = 0x00;

        // Fill 34-byte payload with a safe deterministic pattern.
        for (int i = 0; i < 34; i++)
        {
            message[4 + i] = 0x55;
        }

        Message parsed = MessageFactory.CreateMessageFromBytes(Message.AppendFlagBytes(Message.AppendCRC(message)));
        LongReport report = Assert.IsType<LongReport>(parsed);

        Assert.True(report.ValidCRC);
        Assert.Equal(2, report.TimeOfReceptionRaw);
        Assert.True(report.TimeOfReceptionValid);
        Assert.Equal(160e-9, report.TimeOfReceptionSeconds, 12);

        Assert.Equal(34, report.LongPayload.Length);
        Assert.Equal((byte)0x55, report.LongPayload[0]);
        Assert.Equal((byte)0x55, report.LongPayload[33]);
    }

    [Fact]
    public void LongReport_Parses_Invalid_Tor_Marker()
    {
        byte[] message = new byte[436]; // This is just a variation of the UplinkData message, so we need to look for a larger message size then sub-divide.
        message[0] = 0x1F;

        // TOR invalid marker per spec.
        message[1] = 0xFF;
        message[2] = 0xFF;
        message[3] = 0xFF;

        LongReport report = new LongReport(Message.AppendFlagBytes(Message.AppendCRC(message)));

        Assert.True(report.ValidCRC);
        Assert.Equal(0xFFFFFF, report.TimeOfReceptionRaw);
        Assert.False(report.TimeOfReceptionValid);
        Assert.Equal(34, report.LongPayload.Length);
    }
}