#pragma warning disable IDE0049
#pragma warning disable IDE0090

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using GDL90.Core;
using GDL90.Adapters;

namespace GDL90.Console {
    public class ProgramOptions {
        public enum ProgramMode {
            ListenUDP,
            ReadFromFile,
            ExecuteTests,
            Experimental
        }

        public ProgramMode Mode = ProgramMode.ListenUDP;
        public string InFile = String.Empty; // Input from a recorded file.
        public string OutFile = String.Empty; // Output to a recorded file.
        public bool Force = false; // Make sure options that don't make sense together are intended.
        public bool Stats = false; // Enable statistics output.
        public bool DontWaitForExit = false; // Enable DontWaitForExit mode.
        public int UdpListenPort = 4000; // Default 4000 for Stratux GDL90
        public Logging.Verbosity LogLevel = Logging.Verbosity.Info;

        public ProgramOptions()
        {
            
        }
    }

    public class Processor {
        private readonly Logging.Logger logger;
        private readonly ProgramOptions options;
        private readonly int flushThreshold = 1024;
        private int receivedBytesBeforeFlush = 0;
        private readonly FileStream? binaryStratuxDataFile = null;
        private int MessageCount = 0;
        private int MessageCountBadCRC = 0;

        public Processor(ProgramOptions options, Logging.Logger logger) {
            this.options = options;
            this.logger = logger;
            
            logger.Info("GDL90 Decoder v0.2.0");
            logger.Info("Log level: " + options.LogLevel.ToString());
            logger.Debug("Program starting in mode: " + options.Mode.ToString());

            if (!String.IsNullOrEmpty(options.OutFile)) {
                 binaryStratuxDataFile = new FileStream(options.OutFile, FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write);
            }
        }

        private async Task ReceiveMessages(System.Threading.Channels.ChannelReader<Message> messageOutputChannelReader)
        {
            await foreach (var newGDL90Message in messageOutputChannelReader.ReadAllAsync())
            {
                MessageCount++;

                    if (newGDL90Message.ValidCRC) {
                        logger.Debug(String.Format("{0} {1:0000000} {2:X} {3}", DateTimeOffset.Now.ToUnixTimeMilliseconds(), MessageCount, newGDL90Message.MessageId, newGDL90Message.ToShortString()));

                        // If we're logging to file, dump the message bytes back out.
                        if (!String.IsNullOrEmpty(options.OutFile) && binaryStratuxDataFile != null && binaryStratuxDataFile.CanWrite) {
                            receivedBytesBeforeFlush += newGDL90Message.MessageFrame.Length;
                            binaryStratuxDataFile.Write(newGDL90Message.MessageFrame);
                            if (receivedBytesBeforeFlush >= flushThreshold) {
                                logger.Info("Flushing save file contents...");
                                binaryStratuxDataFile.Flush(true);
                                receivedBytesBeforeFlush = 0;
                            }
                        }
                    }
                    else
                    {
                        logger.Warn(String.Format("{0} {1:0000000} {2:X} CRC failure. Computed: 0x{3:X}, actual: 0x{4:X}.", DateTimeOffset.Now.ToUnixTimeMilliseconds(), MessageCount, newGDL90Message.MessageId, newGDL90Message.ComputedCRC, newGDL90Message.MessageCRC));
                        MessageCountBadCRC++;
                    }
            }
            logger.Info("Done receiving messages from messageOutputChannelReader.");
        }

        private async Task ReadFromFileStream()
        {
            MessageStreamParser messageStreamParser = new MessageStreamParser();
            _ = Task.Run(async () => await FileLoader.WriteFileToChannel(options.InFile, messageStreamParser.MessageDataChannel.Writer));
            _ = Task.Run(async () => await messageStreamParser.ProcessMessageDataChannelAsync());
            await ReceiveMessages(messageStreamParser.MessageOutputChannel.Reader);
            logger.Info("Done reading from file.");
        }

        private async Task ReadFromUDPStream()
        {
            MessageStreamParser messageStreamParser = new MessageStreamParser();
            UDPListener udpListener = new UDPListener(messageStreamParser.MessageDataChannel.Writer);
            _ = Task.Run(async () => await udpListener.StartListening(options.UdpListenPort));
            _ = Task.Run(async () => await messageStreamParser.ProcessMessageDataChannelAsync());
            await ReceiveMessages(messageStreamParser.MessageOutputChannel.Reader);
            logger.Info("Done reading from UDP stream.");
        }

        public async Task Start() {
            logger.Info("Starting");
            Stopwatch stopwatch = new();
            stopwatch.Start();
            switch (options.Mode)
            {
                case ProgramOptions.ProgramMode.ListenUDP:
                    await ReadFromUDPStream();
                    break;
                case ProgramOptions.ProgramMode.ReadFromFile:
                    await ReadFromFileStream();
                    break;
                case ProgramOptions.ProgramMode.Experimental:
                    await ExecuteExperimentalMode();
                    break;
                case ProgramOptions.ProgramMode.ExecuteTests:
                default:
                    throw new NotImplementedException($"Program Mode {options.Mode} not implemented yet.");
                
            }
            if (options.DontWaitForExit == false)
            {
                logger.Info("Press any key to exit...");
                System.Console.ReadLine();
            }
            stopwatch.Stop();
            logger.Info("Done, exiting.");

            if(options.Stats)
            {
                logger.Info("Stats:");
                logger.Info(String.Format("Processed {0:N0} messages in {1:N0} milliseconds, a rate of {2:N2} messages per second.", MessageCount, stopwatch.ElapsedMilliseconds, ((double)MessageCount / (double)stopwatch.ElapsedMilliseconds * 1000.0).ToString("00.00")));
                logger.Info(String.Format("Valid CRC: {0:N0}. Failed CRC: {1:N0} ({2:P})", MessageCount - MessageCountBadCRC, MessageCountBadCRC, (double)MessageCountBadCRC / (double)MessageCount));
            }
            System.Environment.Exit(0);
        }

        private async Task ExecuteExperimentalMode()
        {
            logger.Info("There are currently no experimental methods implemented. Reserved for future use.");
            await Task.CompletedTask;
        }
    }

    public class Program {
        private static ProgramOptions ParseArgs(string[] args, Logging.Logger logger)
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
                    case "--loglevel":
                        options.LogLevel = (Logging.Verbosity)Convert.ToInt32(args[++i]);
                        break;
                    case "--force":
                        options.Force = true;
                        break;
                    case "--stats":
                        options.Stats = true;
                        break;
                    case "--dontwaitforexit":
                        options.DontWaitForExit = true;
                        break;
                    case "--experimental":
                        options.Mode = ProgramOptions.ProgramMode.Experimental;
                        break;
                    case "--help":
                    default:
                        logger.Info(GetUsage());
                        Environment.Exit(0);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(options.OutFile) && !string.IsNullOrEmpty(options.InFile) && options.Force == false)
            {
                logger.Warn("--outfile and --infile used together don't make sense. Use --force to proceed anyway.");
                logger.Info(GetUsage());
                Environment.Exit(1);
            }

            if (options.Mode == ProgramOptions.ProgramMode.Experimental && string.IsNullOrEmpty(options.InFile))
            {
                logger.Warn("--experimental mode requires --infile to be specified.");
                logger.Info(GetUsage());
                Environment.Exit(1);
            }

            return options;
        }
        
        private static string GetUsage()
        {
            StringBuilder usageString = new StringBuilder();
            usageString.AppendLine("GDL90 Decoder Usage:");
            usageString.AppendLine("gdl90.exe");
            usageString.AppendLine("  --infile \"data.bin\" (Reads from a binary file rather than a UDP port.)");
            usageString.AppendLine("  --outfile \"data.bin\" (Saves binary data received to a file before processing.)");
            usageString.AppendLine("  --port 4000 (Use a different UDP port to listen for GLD90 messages. Default 4000.)");
            usageString.AppendLine("  --loglevel 0-4 (Silent=0, Error=1, Warn=2, Info=3, Debug=4. Default is 3 - Info.)");
            usageString.AppendLine("  --force (Continue anyway even if weird arguments are supplied, such as both --infile and --outfile)");
            usageString.AppendLine("  --stats (Enable statistics output after processing.)");
            usageString.AppendLine("  --dontwaitforexit (Enable DontWaitForExit mode, does not wait for user input to exit after reading all data.)");
            usageString.AppendLine("Example 1:");
            usageString.AppendLine("gdl90.exe (Start with default options, listening on UDP port 4000.)");
            usageString.AppendLine("");
            usageString.AppendLine("Example 2:");
            usageString.AppendLine("gdl90.exe --outfile \"gdl90data.bin\" (Start with and log all raw data to file called gdl90data.bin.)");
            usageString.AppendLine("");
            usageString.AppendLine("Example 2:");
            usageString.AppendLine("gdl90.exe --infile \"gdl90data.bin\" (Start and read from a raw data to file called gdl90data.bin.)");
            usageString.AppendLine("");
            return usageString.ToString();
        }

        public static async Task<int> Main(params string[] args) {
            Logging.Logger logger = new Logging.Logger(Logging.Verbosity.Info); // Create a logger with default log level (Info) until we parse the args and know what the user wants.
            ProgramOptions Options = ParseArgs(args, logger);
            logger.LogLevel = Options.LogLevel;
            await new Processor(Options, logger).Start();
            return 0;
        }
    }
}