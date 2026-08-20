#pragma warning disable IDE0090

using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace GDL90.Adapters
{
    public class FileLoader
    {
        private const int DefaultBufferSize = 4096; // Align with the NTFS default cluster size of 4kb. Am I overthinking this?

        public async static Task WriteFileToChannel(string filePath, ChannelWriter<byte[]> messageParserChannelWriter)
        {
            if (File.Exists(filePath))
            {
                using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                await WriteStreamToChannel(fileStream, messageParserChannelWriter);
            }
            else
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }
        }
    
        public async static Task WriteStreamToChannel(Stream inputStream, ChannelWriter<byte[]> messageParserChannelWriter)
        {
            if (inputStream.CanRead)
            {
                byte[] streamReadBuffer = new byte[DefaultBufferSize];
                int bytesRead = 0;
                while(inputStream.CanRead)
                {
                    bytesRead = inputStream.Read(streamReadBuffer, 0, DefaultBufferSize);
                    if (bytesRead > 0)
                    {
                        await messageParserChannelWriter.WriteAsync(streamReadBuffer.AsMemory(0, bytesRead).ToArray());
                    }
                    else
                    {
                        // Zero bytes on a read of a stream means we've reached the end of the stream.
                        break;
                    }
                }
                messageParserChannelWriter.Complete();
            }
            else
            {
                
                throw new IOException($"Input stream is not readable.");
            }
        }
    }
}