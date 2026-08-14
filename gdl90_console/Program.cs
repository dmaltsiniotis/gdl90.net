using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GDL90 {
    public class ProgramOptions {
        public enum ProgramMode {
            ListenUDP,
            ReadFromFile,
            ExecuteTests
        }

        public ProgramMode Mode = ProgramMode.ListenUDP;
        public string InFile = ""; // Input from a recorded file.
        public string OutFile = ""; // Output to a recorded file.
        public bool Force; // Make sure options that don't make sense together are intended.
        public bool Stats; // Enable statistics output.
        public int UdpListenPort = 4000; // Default 4000 for Stratux GDL90
        public Logging.Verbosity LogLevel = Logging.Verbosity.Info;

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
        private readonly AsyncCallback ReceiveUDPDataCallbackDelegate;
        private readonly AsyncCallback GDL90MessageReceivedCallbackDelegate;
        private readonly AsyncCallback StreamCorruptionCallbackDelegate;
        private int MessageCount = 0;
        private int MessageCountBadCRC = 0;
        private int StreamCorruptionCount = 0;
        private Logging.Logger logger;
        private readonly MessageStreamParser MessageStreamParser;
        private readonly MemoryStream NetworkMessageStream = new MemoryStream();

        public Processor(ProgramOptions options) {
            this.options = options;
            this.logger = new Logging.Logger(options.LogLevel);
            if (!string.IsNullOrEmpty(options.OutFile)) {
                 binaryStratuxDataFile = new FileStream(options.OutFile, FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write);
            }
            ReceiveUDPDataCallbackDelegate = new AsyncCallback(ReceiveUDPDataCallback);
            GDL90MessageReceivedCallbackDelegate = new AsyncCallback(ProcessMessage);
            StreamCorruptionCallbackDelegate = new AsyncCallback(ProcessStreamCorruption);

            MessageStreamParser = new MessageStreamParser(GDL90MessageReceivedCallbackDelegate, StreamCorruptionCallbackDelegate);
            MessageStreamParser.StartAsync(NetworkMessageStream);
        }

        private void ProcessStreamCorruption(IAsyncResult ar) {
            logger.Warn(String.Format("{0} {1:0000000} Stream corruption detected. Discarding and re-framing.", DateTimeOffset.Now.ToUnixTimeMilliseconds(), MessageCount));
            StreamCorruptionCount++;
        }
        private void ProcessMessage(IAsyncResult ar) {
            if (ar.AsyncState == null)
            {
                throw new ArgumentException("Missing Message. Expected a Message object.");
            }

            // Congrats, we have a GDL90 message! Now what do we want to do with it?
            Message newGDL90Message = (Message)ar.AsyncState;

            MessageCount++;

            if (newGDL90Message.ValidCRC) {
                logger.Info(String.Format("{0} {1:0000000} {2}", DateTimeOffset.Now.ToUnixTimeMilliseconds(), MessageCount, newGDL90Message.ToShortString()));

                // If we're logging to file, dump the message bytes back out.
                if (!string.IsNullOrEmpty(options.OutFile) && binaryStratuxDataFile != null && binaryStratuxDataFile.CanWrite) {
                    receivedBytesBeforeFlush += newGDL90Message.MessageFrame.Length;
                    binaryStratuxDataFile.Write(newGDL90Message.MessageFrame);
                    if (receivedBytesBeforeFlush >= 1024) {
                        Console.WriteLine("Flushing save file contents...");
                        binaryStratuxDataFile.Flush(true);
                        receivedBytesBeforeFlush = 0;
                    }
                }
            }
            else
            {
                logger.Warn(String.Format("{0} {1:0000000} CRC failure. Computed: 0x{2:X}, actual: 0x{3:X}.", DateTimeOffset.Now.ToUnixTimeMilliseconds(), MessageCount, newGDL90Message.ComputedCRC, newGDL90Message.MessageCRC));
                MessageCountBadCRC++;
            }
        }

        public void ReceiveUDPDataCallback(IAsyncResult ar) {
            if (ar.AsyncState == null)
            {
                throw new ArgumentException("Missing asyncState. Expected a UdpState struct.");
            }

            UdpState asyncState = (UdpState)ar.AsyncState;

            // push these bytes into the GDL90 lib message stream parser instance.
            byte[] receiveBytes = asyncState.udpClient.EndReceive(ar, ref asyncState.ipEndpoint);

            if (NetworkMessageStream.CanWrite)
            {
                NetworkMessageStream.Write(receiveBytes, 0, receiveBytes.Length);    
            }
            else
            {
                throw new IOException("NetworkMessageStream is not writable.");
            }

            asyncState.udpClient.BeginReceive(ReceiveUDPDataCallbackDelegate, ar.AsyncState);
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

                logger.Info(String.Format("Starting listen for messages..."));
                udpClient.BeginReceive(ReceiveUDPDataCallbackDelegate, asyncState);;

                logger.Info(String.Format("Press any key to end..."));
                Console.ReadKey();

                udpClient.Close();
            }
            catch (Exception e ) {
                logger.Error(e.ToString());
            }
        }

        private void ReadFromFileStream(Stream dataStream)
        {
            MessageStreamParser.StartAsync(dataStream);
            Console.WriteLine("Press any key to end...");
            Console.ReadLine();
        }

        public void Start() {
            logger.Info("Starting");
            Stopwatch stopwatch = new();
            stopwatch.Start();
            switch (options.Mode)
            {
                case ProgramOptions.ProgramMode.ListenUDP:
                    ReadFromUDP(options.UdpListenPort);
                    break;
                case ProgramOptions.ProgramMode.ReadFromFile:
                    //ReadFromFile(options.InFile);
                    ReadFromFileStream(File.OpenRead(options.InFile));
                    break;
            }
            stopwatch.Stop();
            logger.Info("Done.");

            if(options.Stats)
            {
                logger.Info(String.Format("Processed {0:N0} messages in {1:N0} milliseconds, a rate of {2:N2} messages per second.", MessageCount, stopwatch.ElapsedMilliseconds, ((double)MessageCount / (double)stopwatch.ElapsedMilliseconds * 1000.0).ToString("00.00")));
                logger.Info(String.Format("Valid CRC: {0:N0}. Failed CRC: {1:N0} ({2:P})", MessageCount - MessageCountBadCRC, MessageCountBadCRC, (double)MessageCountBadCRC / (double)MessageCount));
                logger.Info(String.Format("Stream corruptions detected: {0:N0}.", StreamCorruptionCount));
            }
        }
    }
    public class Program {
        private static ProgramOptions ParseArgs(string[] args)
        {
            Console.WriteLine("GDL90 Decoder v0.1.0");
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
                    case "--loglevel":
                        options.LogLevel = (Logging.Verbosity)Convert.ToInt32(args[++i]);
                        break;
                    case "--force":
                        options.Force = true;
                        break;
                    case "--stats":
                        options.Stats = true;
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
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARN: --outfile and --infile used together don't make sense. Use --force to proceed anyway.");
                Console.ResetColor();
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
            usageString.AppendLine("  --loglevel 0-4 (Silent=0, Error=1, Warn=2, Info=3, Debug=4. Default is 1)");
            usageString.AppendLine("  --force (Continue anyway even if weird arguments are supplied, such as both --infile and --outfile)");
            usageString.AppendLine("  --stats (Enable statistics output after processing.)");
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