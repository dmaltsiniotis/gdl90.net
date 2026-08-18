using System;
using System.IO;

namespace GDL90 {
    public class MessageStreamParser
    {
        const int StreamBufferSize = 4096;
        public AsyncCallback? messageReceivedCallback;
        public AsyncCallback? streamCorruptionDetectedCallback;

        public MessageStreamParser()
        {

        }

        public MessageStreamParser(AsyncCallback? messageReceivedCallback = null, AsyncCallback? streamCorruptionDetectedCallback = null)
        {
            this.messageReceivedCallback = messageReceivedCallback;
            this.streamCorruptionDetectedCallback = streamCorruptionDetectedCallback;
        }

        public void StartAsync(Stream dataStream)
        {
            _ = System.Threading.Tasks.Task.Run(() => ReadFromStream(dataStream));
        }

        public class MessageReceivedAsyncResult : IAsyncResult
        {
            private readonly Message _message;
            public MessageReceivedAsyncResult(Message message)
            {
                _message = message;
            }
            public object AsyncState => _message;
            public System.Threading.WaitHandle AsyncWaitHandle => throw new NotImplementedException();
            public bool CompletedSynchronously => true;
            public bool IsCompleted => true;
        }

        public class StreamCorruptionDetectedAsyncResult : IAsyncResult
        {
            public StreamCorruptionDetectedAsyncResult()
            {
                AsyncState = 1;
            }
            public object AsyncState { get; }
            public System.Threading.WaitHandle AsyncWaitHandle => throw new NotImplementedException();
            public bool CompletedSynchronously => true;
            public bool IsCompleted => true;
        }

        /// <summary>
        /// A method to parse a live stream of GDL90 messages.
        /// </summary>
        /// <param name="dataStream"></param>
        private void ReadFromStream(Stream dataStream)
        {
            byte[] streamBuffer = new byte[StreamBufferSize]; // Match 4kb default NTFS cluster alignment? I'm thinking about this too much...
            byte[] messageBuffer = new byte[StreamBufferSize]; // Match the streamBuffer length. Assumption: GDL90 messages are not larger than 4096 bytes.
            int messageBufferIndex = 0;

            bool messageInProgress = false;
            while (dataStream.CanRead) { // While the stream is readable, do work.
                int bytesRead = dataStream.Read(streamBuffer);
                if (bytesRead > 0)
                {
                    // Console.WriteLine("Bytes read: {0}", Convert.ToHexString(streamBuffer));
                    for (int i = 0; i < bytesRead; i++)
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
                                    streamCorruptionDetectedCallback?.Invoke(new StreamCorruptionDetectedAsyncResult());
                                    messageBufferIndex--; // This has the effect of re-using the end-frame flag 0x7E as the start frame and moving the index pointer back one to overwrite the duplicate start frame 0x7E.
                                }
                                else
                                {
                                    messageInProgress = false;
                                    Span<byte> finalMessage = messageBuffer.AsSpan(0, messageBufferIndex + 1); // We may want to copy this buffer before sending it off to ProcessMessage in the future to make this async'ish.
                                    // Console.WriteLine("Attempting to process a message {0} bytes long: {1}", messageBufferIndex, Convert.ToHexString(finalMessage));

                                    Message newGDL90Message = MessageFactory.CreateMessageFromBytes(finalMessage); 

                                    messageReceivedCallback?.Invoke(new MessageReceivedAsyncResult(newGDL90Message)); 

                                    messageBufferIndex = 0;
                                    continue;
                                }
                            }
                        }
                        if (messageInProgress) {
                            messageBuffer[messageBufferIndex] = streamBuffer[i];
                            messageBufferIndex += 1;
                        }
                    }
                }
            }
        }
    }
}