using GDL90;
using Xunit;

namespace GDL90.Tests;

public class StratuxHeartbeatTests
{
    [Fact]
    public void StratuxHeartbeat_Parses_Protocol1_GpsAndAhrs_Set()
    {
        // Go logic: status starts 0x00, GPS sets bit1, AHRS sets bit0, protocol 1 sets bits7..2 with (1 << 2).
        // Result status byte: 0x07.
        byte[] heartbeatMessage = new byte[]
        {
            0xCC,
            0x07
        };

        Message parsed = MessageFactory.CreateMessageFromBytes(Message.AppendFlagBytes(Message.AppendCRC(heartbeatMessage)));
        StratuxHeartbeat hb = Assert.IsType<StratuxHeartbeat>(parsed);

        Assert.True(hb.ValidCRC);
        Assert.Equal((byte)0x07, hb.StatusByte);
        Assert.Equal(1, hb.ProtocolVersion);
        Assert.True(hb.GPSValid);
        Assert.True(hb.AHRSValid);
    }

    [Fact]
    public void StratuxHeartbeat_Parses_Protocol1_NoGpsNoAhrs()
    {
        // Protocol 1 only, no GPS/AHRS flags.
        byte[] heartbeatMessage = new byte[]
        {
            0xCC,
            0x04
        };

        StratuxHeartbeat hb = new StratuxHeartbeat(Message.AppendFlagBytes(Message.AppendCRC(heartbeatMessage)));

        Assert.True(hb.ValidCRC);
        Assert.Equal((byte)0x04, hb.StatusByte);
        Assert.Equal(1, hb.ProtocolVersion);
        Assert.False(hb.GPSValid);
        Assert.False(hb.AHRSValid);
    }
}