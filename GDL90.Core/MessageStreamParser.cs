using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GDL90.Core {

    public class MessageStreamParser
    {
        public readonly Channel<byte[]> MessageDataChannel;
        public readonly Channel<Message> MessageOutputChannel;

        private bool messageInProgress = false;
        private readonly byte[] messageBuffer = new byte[1024]; // The apparent largest message (Uplink) from the spec is 436 bytes.
        private int messageBufferIndex = 0;

        public MessageStreamParser()
        {
            MessageDataChannel = Channel.CreateUnbounded<byte[]>();
            MessageOutputChannel = Channel.CreateUnbounded<Message>();
        }

        public async Task ProcessMessageDataChannelAsync()
        {
            byte[] streamBuffer;
            while(await MessageDataChannel.Reader.WaitToReadAsync())
            {
                streamBuffer = await MessageDataChannel.Reader.ReadAsync();

                for (int i = 0; i < streamBuffer.Length; i++)
                {
                    if (streamBuffer[i] == Message.ByteConstants.FlagByte) { // Start or end of frame, figure out which.
                        if (!messageInProgress) { // If we're not in the middle of a message, set that we are.
                            // Console.WriteLine("0x7e found, I think this is a start frame at index {0}", i);
                            messageInProgress = true;
                        } else {
                            // Console.WriteLine("0x7e found, I think this is an end frame at index {0}", i);
                            messageBuffer[messageBufferIndex] = streamBuffer[i];
                            
                            // We are in a very specific edge-case here where a 0x7E Flag Byte was missed. Need to discard and reset.
                            if (messageBuffer[0] == Message.ByteConstants.FlagByte && messageBuffer[1] == Message.ByteConstants.FlagByte) {
                                // Console.WriteLine("Corrupted data detected, discarding and re-framing...");
                                // streamCorruptionDetectedCallback?.Invoke(new StreamCorruptionDetectedAsyncResult());
                                messageBufferIndex--; // This has the effect of re-using the end-frame flag 0x7E as the start frame and moving the index pointer back one to overwrite the duplicate start frame 0x7E.
                            }
                            else
                            {
                                messageInProgress = false;
                                Span<byte> finalMessage = messageBuffer.AsSpan(0, messageBufferIndex + 1); // We may want to copy this buffer before sending it off to ProcessMessage in the future to make this async'ish.
                                // Console.WriteLine("Attempting to process a message {0} bytes long: {1}", messageBufferIndex, Convert.ToHexString(finalMessage));

                                Message newGDL90Message = MessageFactory.CreateMessageFromBytes(finalMessage);
                                await MessageOutputChannel.Writer.WriteAsync(newGDL90Message);

                                messageBufferIndex = 0;
                                continue;
                            }
                        }
                    }
                    if (messageInProgress) {
                        // TODO: Test this overflow condition.
                        if (messageBufferIndex >= messageBuffer.Length) {
                            // Console.WriteLine("Message buffer overflow detected, discarding and re-framing...");
                            // streamCorruptionDetectedCallback?.Invoke(new StreamCorruptionDetectedAsyncResult());
                            messageInProgress = false;
                            messageBufferIndex = 0;
                        }
                        else
                        {
                            messageBuffer[messageBufferIndex++] = streamBuffer[i];
                        }
                    }
                }
            }
            MessageOutputChannel.Writer.Complete();
        }
    }
}