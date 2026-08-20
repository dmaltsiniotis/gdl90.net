using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using GDL90.Core;

namespace GDL90.Tests;

public class MessageStreamParserTests
{
    [Fact]
    public async Task Oversized_Message_Frame_Is_Dropped_And_Parser_Recovers()
    {
        MessageStreamParser parser = new MessageStreamParser();

        byte[] oversizedFrame = BuildOversizedFrame(Message.MaximumMessageLength + 1);

        byte[] validPayload = new byte[]
        {
            (byte)MessageType.GDL90_Heartbeat,
            0x00,
            0x01,
            0x02,
            0x03,
            0x04,
            0x05
        };
        byte[] validFrame = FrameWithFlags(validPayload);

        byte[] streamBytes = new byte[oversizedFrame.Length + validFrame.Length];
        Buffer.BlockCopy(oversizedFrame, 0, streamBytes, 0, oversizedFrame.Length);
        Buffer.BlockCopy(validFrame, 0, streamBytes, oversizedFrame.Length, validFrame.Length);

        Task parserTask = parser.ProcessMessageDataChannelAsync();
        await parser.MessageDataChannel.Writer.WriteAsync(streamBytes);
        parser.MessageDataChannel.Writer.Complete();

        List<Message> parsedMessages = new List<Message>();
        await foreach (Message message in parser.MessageOutputChannel.Reader.ReadAllAsync())
        {
            parsedMessages.Add(message);
        }

        await parserTask;

        Assert.Single(parsedMessages);
        Assert.Equal(validFrame, parsedMessages[0].MessageFrame);
        Assert.True(parsedMessages[0].ValidCRC);
    }

    private static byte[] BuildOversizedFrame(int payloadLength)
    {
        byte[] frame = new byte[payloadLength + 2];
        frame[0] = Message.ByteConstants.FlagByte;
        frame[frame.Length - 1] = Message.ByteConstants.FlagByte;

        for (int i = 1; i < frame.Length - 1; i++)
        {
            frame[i] = 0x01;
        }

        return frame;
    }

    private static byte[] FrameWithFlags(byte[] messageDataWithIdNoCRC)
    {
        byte[] withCRC = Message.AppendCRC(messageDataWithIdNoCRC);
        byte[] framed = new byte[withCRC.Length + 2];
        framed[0] = Message.ByteConstants.FlagByte;
        Buffer.BlockCopy(withCRC, 0, framed, 1, withCRC.Length);
        framed[framed.Length - 1] = Message.ByteConstants.FlagByte;
        return framed;
    }
}
