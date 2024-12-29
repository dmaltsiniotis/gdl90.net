using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.Win32.SafeHandles;

namespace GDL90 {
    public class ProgramOptions {
        public enum ProgramMode {
            ListenUDP,
            ReadFromFile,
            ExecuteTests
        }
        public enum VerbosityEnum : int {
            None = 0,
            Default = 1,
            Extra = 2,
            Debug = 3
        }

        public ProgramMode Mode = ProgramMode.ListenUDP;
        public string InFile = ""; // Input from a recorded file.
        public string OutFile = ""; // Output to a recorded file.
        public bool Force; // Make sure options that don't make sense together are intended.
        public int UdpListenPort = 4000; // Default 4000 for Stratux GDL90
        public VerbosityEnum Verbosity = VerbosityEnum.Default;

        public ProgramOptions() {

        }
    }

    public struct UdpState
    {
        public UdpClient udpClient;
        public IPEndPoint? ipEndpoint;
    }

    public class Processor {
        private readonly ProgramOptions options;
        public int receivedBytesBeforeFlush = 0;
        public FileStream? binaryStratuxDataFile = null;
        private readonly AsyncCallback ReceiveCallbackDelegate;
        private int MessageCount = 0;
        private int MessageCountBadCRC = 0;
        private int StreamCorruptionCount = 0;

        public Processor(ProgramOptions options) {
            this.options = options;
            if (!string.IsNullOrEmpty(options.OutFile)) {
                 binaryStratuxDataFile = new FileStream(options.OutFile, FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write);
            }
            ReceiveCallbackDelegate = new AsyncCallback(ReceiveCallback);

        }
        private void ProcessMessage(Span<byte> messageDataWithIdAndFcsAndFlagBytes) {
            if (!string.IsNullOrEmpty(options.OutFile) && binaryStratuxDataFile != null && binaryStratuxDataFile.CanWrite) {
                receivedBytesBeforeFlush += messageDataWithIdAndFcsAndFlagBytes.Length;
                binaryStratuxDataFile.Write(messageDataWithIdAndFcsAndFlagBytes);
                if (receivedBytesBeforeFlush >= 4096) {
                    Console.WriteLine("Flushing save file contents...");
                    binaryStratuxDataFile.Flush(true);
                    receivedBytesBeforeFlush = 0;
                }
            }

            MessageCount++;
            //Console.WriteLine("Processing: {0}", Convert.ToHexString(messageDataWithIdAndFcsAndFlagBytes));
            Message newGDL90Message = MessageFactory.CreateMessageFromBytes(messageDataWithIdAndFcsAndFlagBytes);
            //Console.WriteLine("{3} {0:0000000} MessageId received: 0x{1:X2} {2}", MessageCount, (int)newGDL90Message.MessageId, newGDL90Message.MessageName, DateTimeOffset.Now.ToUnixTimeMilliseconds());
            if (newGDL90Message.ValidCRC) {
                if (newGDL90Message.MessageId == MessageType.GDL90_TrafficReport)
                {
                    TrafficReport newGDL90TrafficMessage = (TrafficReport)newGDL90Message;
                    Console.WriteLine("{0} {1:0000000} TF: {2:8} ({3}, {4}) {5} ft {6} kts {7} deg", DateTimeOffset.Now.ToUnixTimeMilliseconds(), MessageCount, newGDL90TrafficMessage.Callsign, newGDL90TrafficMessage.Latitude, newGDL90TrafficMessage.Longitude, newGDL90TrafficMessage.Altitude, newGDL90TrafficMessage.HorizontalVelocity, newGDL90TrafficMessage.Heading);
                }

                if (newGDL90Message.MessageId == MessageType.GDL90_Heartbeat)
                {
                    Heartbeat newGDL90Heartbeat = (Heartbeat)newGDL90Message;
                    Console.WriteLine("{0} {1:0000000} HB: ", DateTimeOffset.Now.ToUnixTimeMilliseconds(), MessageCount);
                    //newGDL90Heartbeat.PrintDebugInfo();
                }
            }
            else
            {
                //Console.WriteLine("WARNING: TrafficReport CRC failure. Computed: 0x{0:X}, actual: 0x{1:X}.", newGDL90Message.ComputedCRC, newGDL90Message.MessageCRC);
                MessageCountBadCRC++;
            }
        }
        public void ReceiveCallback(IAsyncResult ar) {
            if (ar.AsyncState == null)
            {
                throw new ArgumentException("Missing asyncState. Expected a UdpState struct.");
            }

            UdpState asyncState = (UdpState)ar.AsyncState;

            // if (asyncState.ipEndpoint == null) {
            //     throw new ArgumentException("Missing ipEndpoint in UdpState asyncState.");
            // }

            byte[] receiveBytes = asyncState.udpClient.EndReceive(ar, ref asyncState.ipEndpoint);

            // check for messages...
            bool currentlyInAFrame = false;
            int frameStartIndex = -1;
            for (int byteIndex = 0; byteIndex < receiveBytes.Length; byteIndex++)
            {
                if (receiveBytes[byteIndex] == Message.ByteConstants.FlagByte) {
                    // Start of a frame reached.
                    if (currentlyInAFrame == false) {
                        currentlyInAFrame = true;
                        frameStartIndex = byteIndex;
                        // Console.WriteLine("Found 0x7e START OF FRAME. MSG TYPE: {0}", Convert.ToHexString(new byte[] {receiveBytes[byteIndex+1]}));
                        continue;
                    }

                    // End of a frame reached.
                    if (currentlyInAFrame) {
                        // Console.WriteLine("Found 0x7e END OF FRAME.");
                        ProcessMessage(receiveBytes.AsSpan(frameStartIndex, byteIndex - frameStartIndex + 1));

                        currentlyInAFrame = false;
                        frameStartIndex = -1;
                        continue;
                    }
                    
                }
            }

            asyncState.udpClient.BeginReceive(ReceiveCallbackDelegate, ar.AsyncState);
        }
        private void ReadFromUDP(int udpListenPort) {
            UdpClient udpClient = new UdpClient(udpListenPort);
            try {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);

                UdpState asyncState = new UdpState
                {
                    udpClient = udpClient,
                    ipEndpoint = ipEndPoint
                };

                Console.WriteLine("Starting listen for messages...");
                udpClient.BeginReceive(ReceiveCallbackDelegate, asyncState);;

                Console.WriteLine("Press any key to end...");
                Console.ReadKey();

                udpClient.Close();
            }
            catch (Exception e ) {
                Console.WriteLine(e.ToString());
            }
        }
        private void ReadFromStream(Stream dataStream)
        {
            byte[] streamBuffer = new byte[4096]; // Match 4kb default NTFS cluster alignment? I'm thinking about this too much...
            byte[] messageBuffer = new byte[4096]; // Match the streamBuffer length. Assumption: GDL90 messages are not larger than 4096 bytes.
            int messageBufferIndex = 0;

            bool messageInProgress = false;
            int bytesRead = dataStream.Read(streamBuffer);
            while (bytesRead > 0)
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
                                StreamCorruptionCount++;
                                messageBufferIndex--; // This has the effect of re-using the end-frame flag 0x7E as the start frame and moving the index pointer back one to overwrite the duplicate start frame 0x7E.
                            }
                            else
                            {
                                messageInProgress = false;
                                Span<byte> finalMessage = messageBuffer.AsSpan(0, messageBufferIndex + 1);
                                // Console.WriteLine("Attempting to process a message {0} bytes long: {1}", messageBufferIndex, Convert.ToHexString(finalMessage));
                                ProcessMessage(finalMessage); // We may want to copy this buffer before sending it off to ProcessMessage in the future to make this async'ish.

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
                //Console.WriteLine("press a key to process next x bytes from dataStream...");
                //Console.ReadKey();
                bytesRead = dataStream.Read(streamBuffer);
            }
        }
        // private void ReadFromFile(string filePath)
        // {
        //     FileStream fileData = File.OpenRead(filePath);
        //     ReadFromStream(fileData);
        //     Console.WriteLine("End of file.");
        // }
        public void Start() {
            Console.WriteLine("Starting");
            Stopwatch stopwatch = new();
            stopwatch.Start();
            switch (options.Mode)
            {
                case ProgramOptions.ProgramMode.ListenUDP:
                    ReadFromUDP(options.UdpListenPort);
                    break;
                case ProgramOptions.ProgramMode.ReadFromFile:
                    //ReadFromFile(options.InFile);
                    ReadFromStream(File.OpenRead(options.InFile));
                    break;
            }
            stopwatch.Stop();
            Console.WriteLine("Done.");

            Console.WriteLine("Processed {0:N0} messages in {1:N0} milliseconds, a rate of {2:N2} messages per second.", MessageCount, stopwatch.ElapsedMilliseconds, ((double)MessageCount / (double)stopwatch.ElapsedMilliseconds * 1000.0).ToString("00.00"));
            Console.WriteLine("Valid CRC: {0:N0}. Failed CRC: {1:N0} ({2:P})", MessageCount - MessageCountBadCRC, MessageCountBadCRC, (double)MessageCountBadCRC / (double)MessageCount);
            Console.WriteLine("Stream corruptions detected: {0:N0}.", StreamCorruptionCount);
        }
    }
    public class Program {
        private static ProgramOptions ParseArgs(string[] args)
        {
            ProgramOptions options = new();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--outfile":
                        options.OutFile = args[++i];
                        break;
                    case "--infile":
                        options.InFile = args[++i];
                        options.Mode = ProgramOptions.ProgramMode.ReadFromFile;
                        break;
                    case "--port":
                        options.UdpListenPort = Convert.ToInt32(args[++i]);
                        break;
                    case "--verbosity":
                        options.Verbosity = (ProgramOptions.VerbosityEnum)Convert.ToInt32(args[++i]);
                        break;
                    case "--force":
                        options.Force = true;
                        break;
                    case "--help":
                    default:
                        PrintUsage();
                        Environment.Exit(0);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(options.OutFile) && !string.IsNullOrEmpty(options.InFile) && options.Force == false)
            {
                Console.WriteLine("Warning: --outfile and --infile used together don't make sense. Use --force to proceed anyway.");
                PrintUsage();
                Environment.Exit(1);
            }

            return options;
        }
        private static void PrintUsage()
        {
            StringBuilder usageString = new StringBuilder();
            usageString.AppendLine("GDL90 Decoder Usage:");
            usageString.AppendLine("gdl90.exe");
            usageString.AppendLine("  --infile \"data.bin\" (Reads from a binary file rather than a UDP port.)");
            usageString.AppendLine("  --outfile \"data.bin\" (Saves binary data received to a file before processing.)");
            usageString.AppendLine("  --port 4000 (Use a different UDP port to listen for GLD90 messages. Default 4000.)");
            usageString.AppendLine("  --verbosity 0-4 (Use 0 for silent, 4 for _extreme_ debugging info. Default 1)");
            usageString.AppendLine("  --force (Continue anyway even if weird arguments are supplied, such as both --infile and --outfile)");
            usageString.AppendLine("Example 1:");
            usageString.AppendLine("gdl90.exe (Start with default options, listening on UDP port 4000.)");
            usageString.AppendLine("");
            usageString.AppendLine("Example 2:");
            usageString.AppendLine("gdl90.exe --outfile \"gdl90data.bin\" (Start with and log all raw data to file called gdl90data.bin.)");
            usageString.AppendLine("");
            usageString.AppendLine("Example 2:");
            usageString.AppendLine("gdl90.exe --infile \"gdl90data.bin\" (Start and read from a raw data to file called gdl90data.bin.)");
            usageString.AppendLine("");
            Console.WriteLine(usageString);
        }
        public static int Main(params string[] args) {
            new Processor(ParseArgs(args)).Start();
            return 0;
        }
    }
}