// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.IO.Compression;
// using Xunit;
// using GDL90.Core;

// namespace GDL90.Tests;

// public class ExternalDataTests
// {
//     [Theory]
//     [InlineData("ADS-B_TEST-DATA-SMALL.zip")]
//     [InlineData("ADS-B_TEST-DATA-SMALL-TRAFFIC.zip")]
//     public void Compressed_External_Data_RoundTrips_Through_Message_Parser(string zipFileName)
//     {
//         string zipPath = Path.Combine(AppContext.BaseDirectory, "data", zipFileName);
//         Assert.True(File.Exists(zipPath), $"Expected test zip file was not found: {zipPath}");

//         List<byte> reconstructedBytes = new List<byte>(1024);
//         int streamCorruptionCount = 0;

//         AsyncCallback onMessage = ar =>
//         {
//             Assert.NotNull(ar.AsyncState);
//             Message message = (Message)ar.AsyncState!;
//             reconstructedBytes.AddRange(message.MessageFrame);
//         };

//         AsyncCallback onCorruption = _ => { streamCorruptionCount++; };

//         MessageStreamParser parser = new MessageStreamParser(onMessage, onCorruption);
//         //using MemoryStream expectedBytesBuffer = new MemoryStream();

//         using ZipArchive zipArchive = ZipFile.OpenRead(zipPath);
//         ZipArchiveEntry zipEntry = Assert.Single(zipArchive.Entries);
//         using Stream decompressedStream = zipEntry.Open();
//         parser.StartAsync(decompressedStream);

//         byte[] expectedBytes = File.ReadAllBytes(zipPath);
//         byte[] actualBytes = reconstructedBytes.ToArray();

//         Assert.Equal(0, streamCorruptionCount);
//         Assert.Equal(expectedBytes, actualBytes);
//     }
// }