using GDL90;
using Xunit;

namespace GDL90.Tests;

public class UplinkDataTests
{
    [Fact]
    public void UplinkData_Parses_Tor_And_Payload()
    {
        byte[] message = new byte[436];
        message[0] = 0x07; // Uplink Data message ID.

        // TOR is LSB first. Raw value = 1 -> 80ns.
        message[1] = 0x01;
        message[2] = 0x00;
        message[3] = 0x00;

        // Fill 432-byte payload with a deterministic pattern that avoids control-escape bytes.
        for (int i = 0; i < 432; i++)
        {
            message[4 + i] = 0x55;
        }

        Message parsed = MessageFactory.CreateMessageFromBytes(Message.AppendFlagBytes(Message.AppendCRC(message)));
        UplinkData uplink = Assert.IsType<UplinkData>(parsed);

        Assert.True(uplink.ValidCRC);
        Assert.Equal(1, uplink.TimeOfReceptionRaw);
        Assert.True(uplink.TimeOfReceptionValid);
        Assert.Equal(80e-9, uplink.TimeOfReceptionSeconds, 12);

        Assert.Equal(432, uplink.UplinkPayload.Length);
        Assert.Equal((byte)0x55, uplink.UplinkPayload[0]);
        Assert.Equal((byte)0x55, uplink.UplinkPayload[1]);
        Assert.Equal((byte)0x55, uplink.UplinkPayload[431]);
    }

    [Fact]
    public void UplinkData_Parses_Invalid_Tor_Marker()
    {
        byte[] message = new byte[436];
        message[0] = 0x07;

        // TOR invalid marker per spec.
        message[1] = 0xFF;
        message[2] = 0xFF;
        message[3] = 0xFF;

        UplinkData uplink = new UplinkData(Message.AppendFlagBytes(Message.AppendCRC(message)));

        Assert.True(uplink.ValidCRC);
        Assert.Equal(0xFFFFFF, uplink.TimeOfReceptionRaw);
        Assert.False(uplink.TimeOfReceptionValid);
        Assert.Equal(432, uplink.UplinkPayload.Length);
    }
}