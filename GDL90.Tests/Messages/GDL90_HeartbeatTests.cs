using Xunit;
using GDL90.Core;
using GDL90.Core.Messages;

namespace GDL90.Tests;

public class HeartbeatTests
{
    [Fact]
    public void Heartbeat_Parses_Fields_From_Spec_Example()
    {
        // Example from spec: 4 uplinks and 567 Basic/Long messages -> Byte 6 = 0x22, Byte 7 = 0x37
        // Status bytes intentionally match the spec: SB1 -> bit7=1 GPS valid, bit0=1 UAT init; SB2 -> bit0=1 UTC OK
        byte[] heartbeatMessage = new byte[]
        {
            0x00,
            0x81,
            0x01,
            0x00,
            0x00,
            0x22,
            0x37
        };

        Heartbeat hb = new Heartbeat(Message.AppendFlagBytes(Message.AppendCRC(heartbeatMessage)));

        Assert.True(hb.ValidCRC);
        Assert.True(hb.GPSPosValid);
        Assert.True(hb.UATInitialized);
        Assert.False(hb.MaintReq);
        Assert.Equal(0, hb.TimeStamp);
        Assert.Equal(4, hb.UplinkMessageCount);
        Assert.Equal(567, hb.BasicAndLongMessageCount);
    }
}