using System;
using System.Text;
using Xunit;
using GDL90.Core;
using GDL90.Core.Messages;

namespace GDL90.Tests;

public class ForeflightStatusTests
{
    [Fact]
    public void ForeflightStatus_Parses_Fields_From_Go_Layout()
    {
        byte[] message = new byte[39];
        message[0] = 0x65; // ForeFlight message type.
        message[1] = 0x00; // ID message identifier.
        message[2] = 0x01; // Message version.

        for (int i = 3; i <= 10; i++)
        {
            message[i] = 0xFF;
        }

        Array.Copy(Encoding.ASCII.GetBytes("Stratux"), 0, message, 11, 7);
        Array.Copy(Encoding.ASCII.GetBytes("v1.2.3-build999"), 0, message, 19, 15);

        message[35] = 0x00;
        message[36] = 0x00;
        message[37] = 0x00;
        message[38] = 0x00; // Capabilities mask.

        Message parsed = MessageFactory.CreateMessageFromBytes(Message.AppendFlagBytes(Message.AppendCRC(message)));
        ForeflightStatus ff = Assert.IsType<ForeflightStatus>(parsed);

        Assert.True(ff.ValidCRC);
        Assert.Equal((byte)0x00, ff.MessageIdentifier);
        Assert.Equal((byte)0x01, ff.MessageVersion);
        Assert.Equal("FFFFFFFFFFFFFFFF", Convert.ToHexString(ff.DeviceSerialNumber));
        Assert.Equal("Stratux", ff.DeviceShortName);
        Assert.Equal("v1.2.3-build999", ff.DeviceLongName);
        Assert.Equal((byte)0x00, ff.ReservedByte1);
        Assert.Equal((byte)0x00, ff.ReservedByte2);
        Assert.Equal((byte)0x00, ff.ReservedByte3);
        Assert.Equal((byte)0x00, ff.CapabilitiesMask);
    }
}