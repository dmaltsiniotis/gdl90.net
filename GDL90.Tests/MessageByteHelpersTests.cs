using System;
using Xunit;
using GDL90.Core;

namespace GDL90.Tests;

public class MessageByteHelpersTests
{
    [Fact]
    public void AppendFlagBytes_Wraps_Message_With_Flag_Bytes_And_Does_Not_Mutate_Input()
    {
        byte[] messageWithCRC = new byte[] { 0x10, 0x20, 0x30, 0x40 };
        byte[] original = (byte[])messageWithCRC.Clone();

        byte[] framed = Message.AppendFlagBytes(messageWithCRC);

        Assert.Equal(Message.ByteConstants.FlagByte, framed[0]);
        Assert.Equal(Message.ByteConstants.FlagByte, framed[^1]);
        Assert.Equal(messageWithCRC.Length + 2, framed.Length);
        byte[] framedPayload = new byte[messageWithCRC.Length];
        Array.Copy(framed, 1, framedPayload, 0, messageWithCRC.Length);
        Assert.Equal(messageWithCRC, framedPayload);

        Assert.Equal(original, messageWithCRC);
    }

    [Fact]
    public void AppendCRC_Appends_Computed_CRC_And_Does_Not_Mutate_Input()
    {
        byte[] messageDataWithIdNoCRC = new byte[] { 0x00, 0x81, 0x01, 0x00, 0x00, 0x22, 0x37 };
        byte[] original = (byte[])messageDataWithIdNoCRC.Clone();

        ushort computedCrc = Message.ComputeCRC(messageDataWithIdNoCRC);
        byte[] withCRC = Message.AppendCRC(messageDataWithIdNoCRC);

        Assert.Equal(messageDataWithIdNoCRC.Length + 2, withCRC.Length);
        byte[] payloadPrefix = new byte[messageDataWithIdNoCRC.Length];
        Array.Copy(withCRC, 0, payloadPrefix, 0, messageDataWithIdNoCRC.Length);
        Assert.Equal(messageDataWithIdNoCRC, payloadPrefix);

        Assert.Equal((byte)(computedCrc & 0xFF), withCRC[^2]);
        Assert.Equal((byte)(computedCrc >> 8), withCRC[^1]);

        Assert.Equal(original, messageDataWithIdNoCRC);
    }
}
