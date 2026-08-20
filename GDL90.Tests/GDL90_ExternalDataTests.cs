using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Channels;
using Xunit;
using GDL90.Core;
using GDL90.Adapters;
using System.Threading.Tasks;


namespace GDL90.Tests;

public class ExternalDataTests
{
    [Theory]
    [InlineData("ADS-B_TEST-DATA-SMALL.zip")]
    [InlineData("ADS-B_TEST-DATA-SMALL-TRAFFIC.zip")]
    public async Task Compressed_External_Data_RoundTrips_Through_Message_Parser(string zipFileName)
    {
        int messageCount = 0;
        MessageStreamParser messageStreamParser = new MessageStreamParser();
        ChannelReader<Message> messageParserChannelReader = messageStreamParser.MessageOutputChannel.Reader;

        string zipPath = Path.Combine(AppContext.BaseDirectory, "data", zipFileName);
        Assert.True(File.Exists(zipPath), $"Expected test zip file was not found: {zipPath}");
        using ZipArchive zipArchive = ZipFile.OpenRead(zipPath);
        ZipArchiveEntry zipEntry = Assert.Single(zipArchive.Entries);
        using Stream decompressedStream = zipEntry.Open();

        _ = Task.Run(async () => await FileLoader.WriteStreamToChannel(decompressedStream, messageStreamParser.MessageDataChannel.Writer));
        _ = Task.Run(async () => await messageStreamParser.ProcessMessageDataChannelAsync());

        await foreach (Message message in messageParserChannelReader.ReadAllAsync())
        {
            Assert.NotNull(message);
            Assert.True(message.ValidCRC, $"Message failed CRC check: {message}");
            messageCount++;
        }

        // TODO: There's got to be a better way to do this.
        switch (zipFileName)
        {
            case "ADS-B_TEST-DATA-SMALL.zip":
                Assert.Equal(48, messageCount);
                break;
            case "ADS-B_TEST-DATA-SMALL-TRAFFIC.zip":
                Assert.Equal(29455, messageCount);
                break;
            default:
                throw new InvalidOperationException($"Unexpected zip file name: {zipFileName}");
        }
    }
}