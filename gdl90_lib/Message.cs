using System;

namespace GDL90 {
    public abstract class Message {
        public byte[] MessageFrame { get; }
        public byte[] MessageData { get; }
        public readonly MessageType MessageId;
        public readonly string MessageName;
        public readonly ushort MessageCRC;
        public readonly ushort ComputedCRC;
        public readonly bool ValidCRC = false;
        private static readonly ushort[] CRC16Table = GenerateCRC16Table();
        
        public Message(Span<byte> messageDataWithIdAndFcsAndFlagBytes) {
            MessageFrame = messageDataWithIdAndFcsAndFlagBytes.ToArray(); // Store the full end-to-end frame for later use.

            Span<byte> messageDataWithIdAndFcs = UnescapeMessage(messageDataWithIdAndFcsAndFlagBytes[1..^1]);
            MessageId = MessageFactory.GetMessageTypeFromByte(messageDataWithIdAndFcs[0]);
            MessageName = MessageFactory.GetMessageNameFromId(MessageId);
            MessageCRC = BitConverter.ToUInt16(messageDataWithIdAndFcs[^2..].ToArray(), 0); // Grab the last two bytes.

            byte[] messageDataWithIdNoFcs = messageDataWithIdAndFcs[..^2].ToArray(); // Grab everything BUT the last two bytes.
            ComputedCRC = ComputeCRC(messageDataWithIdNoFcs);
            if (ComputedCRC == MessageCRC) {
                ValidCRC = true;
            }
            else
            {
                // Console.WriteLine("GLD90 Message warning: Invalid CRC. Expected: {0:X4} Actual: {1:X4}", MessageCRC, ComputedCRC);
                // Console.WriteLine("{0}", Convert.ToHexString(messageDataWithIdAndFcsAndFlagBytes));
                // Console.WriteLine("  {0}", Convert.ToHexString(messageDataWithIdAndFcs));
                // Console.WriteLine("  {0}", Convert.ToHexString(messageDataWithIdNoFcs));
                // Console.WriteLine("");
            }

            MessageData = messageDataWithIdAndFcs[1..^2].ToArray(); // Fancy way of doing .Slice(1)
            //MessageData = UnescapeMessage(messageDataWithIdAndFcs.ToArray())[1..^2].ToArray(); // Fancy way of doing .Slice(1)
        }

        public abstract string ToDetailedString();

        public abstract string ToShortString();

        public static ushort ComputeCRC(Span<byte> messageBytes)
        {
            ushort crc = 0;
            for (int i = 0; i < messageBytes.Length; i++)
            {
                crc = (ushort)(CRC16Table[(ushort)(crc >> 8)] ^ (ushort)(crc << 8) ^ messageBytes[i]);
            }
            return crc;
        }

        private static ushort[] GenerateCRC16Table()
        {
            /*
            2.2.3. FCS Calculation
            The FCS used in this interface is a CRC-CCITT. For processing efficiency, a table-generated
            CRC calculation can be used. This table contains 256 elements. The table should be calculated
            at startup and left unchanged afterward.
            */
            ushort[] crc16Table = new ushort[256];
            for (ushort i = 0; i < 256; i++)
            {
                ushort crc = (ushort)(i << 8);
                for (ushort bitctr = 0; bitctr < 8; bitctr++)
                {
                    crc = (ushort)((ushort)(crc << 1) ^ ((crc & 0x8000) > 0 ? 0x1021 : 0));
                }

                crc16Table[i] = crc;
                // Console.WriteLine("crc16Table[{0}] = 0x{1:X}", i, crc16Table[i]);
            }
            return crc16Table;
        }
        
        private static Span<byte> UnescapeMessage(Span<byte> messageDataWithIdAndCRC)
        {
            byte[] escapedMessageWithIdAndCRC = new byte[messageDataWithIdAndCRC.Length];
            int controlEscapesFound = 0;
            for (int i = 0; i < messageDataWithIdAndCRC.Length; i++)
            {
                if (messageDataWithIdAndCRC[i] == ByteConstants.EncodingFlag)
                {
                    // Console.WriteLine("Found control-escape at index {0} in message: {1}",i, Convert.ToHexString(messageDataWithIdAndCRC));
                    escapedMessageWithIdAndCRC[i-controlEscapesFound] = (byte)(messageDataWithIdAndCRC[i+1] ^ ByteConstants.EncodingXor);
                    controlEscapesFound++;
                    i++;
                }
                else
                {
                    escapedMessageWithIdAndCRC[i-controlEscapesFound] = messageDataWithIdAndCRC[i];
                }
            }

            Span<byte> escapedMessageWithIdAndCRCSpan = escapedMessageWithIdAndCRC.AsSpan(0, escapedMessageWithIdAndCRC.Length - controlEscapesFound);
            // if (controlEscapesFound > 0)
            // {
            //     Console.WriteLine("Found {0} control-escapes in message.", controlEscapesFound);
            //     Console.WriteLine("{0}\n{1}", Convert.ToHexString(messageDataWithIdAndCRC), Convert.ToHexString(escapedMessageWithIdAndCRCSpan));
            // }
            return escapedMessageWithIdAndCRCSpan;
        }
        
        public static byte[] AppendFlagBytes(byte[] messageWithCRC)
        {
            byte[] messageWithFlagBytes = new byte[messageWithCRC.Length + 2];
            Array.Copy(messageWithCRC, 0, messageWithFlagBytes, 1, messageWithCRC.Length);
            messageWithCRC[0] = ByteConstants.FlagByte;
            messageWithCRC[messageWithCRC.Length-1] = ByteConstants.FlagByte;
            return messageWithFlagBytes;
        }
        
        public static byte[] AppendCRC(byte[] messageDataWithIdNoCRC) {
            ushort computedCRC = GDL90.Message.ComputeCRC(messageDataWithIdNoCRC);
            byte[] messageDataWithIdAndCRC = new byte[messageDataWithIdNoCRC.Length + 2];
            Array.Copy(messageDataWithIdNoCRC, messageDataWithIdAndCRC, messageDataWithIdNoCRC.Length);
            messageDataWithIdAndCRC[^1] = (byte)(computedCRC >> 8); // ^1 is shorthand for messageDataWithIdAndCRC.Length - 1
            messageDataWithIdAndCRC[^2] = (byte)(computedCRC & 0xFF); // ^2 is shorthand for messageDataWithIdAndCRC.Length - 2
            // Console.WriteLine("      Append CRC");
            // Console.WriteLine("-----------------------");
            // Console.WriteLine("           Computed CRC: 0x{0} (dec: {1})", Convert.ToHexString(BitConverter.GetBytes(computedCRC)), computedCRC);
            // Console.WriteLine("      messageDataWithId: 0x{0}", Convert.ToHexString(messageDataWithIdNoCRC));
            // Console.WriteLine("messageDataWithIdAndCRC: 0x{0}", Convert.ToHexString(messageDataWithIdAndCRC));
            // Console.WriteLine("");
            return messageDataWithIdAndCRC;
        }
        
        public static class ByteConstants
        {
            public const byte FlagByte = 0x7E;
            public const byte EncodingFlag = 0x7D;
            public const byte EncodingXor = 0x20;
        }
    }

    public enum MessageType : byte {
        GDL90_Heartbeat = 0x00,
        GDL90_UplinkData = 0x07,
        GDL90_OwnshipReport = 0x0A,
        GDL90_OwnshipGeometricAltitude = 0x0B,
        GDL90_TrafficReport = 0x14,
        GDL90_BasicReport = 0x1E,
        GDL90_LongReport = 0x1F,
        Stratux_Heartbeat = 0xCC,
        Stratux_Heartbeat_Old = 0x53, //+0x58 'S','X',
        Stratux_AHRS = 0x4C,
        Foreflight_Status = 0x65
    }

    public static class MessageFactory
    {
        public static MessageType GetMessageTypeFromByte(byte MessageType) {
            return Enum.Parse<MessageType>(MessageType.ToString());
        }

        public static string GetMessageNameFromId(MessageType id) {
            string MessageName = "<Unknown>";
            switch (id)
            {
                case MessageType.GDL90_Heartbeat:
                    MessageName = "GDL90 - Heartbeat";
                    break;
                case MessageType.GDL90_UplinkData:
                    MessageName = "GDL90 - Uplink Data";
                    break;
                case MessageType.GDL90_OwnshipReport:
                    MessageName = "GDL90 - Ownship Report";
                    break;
                case MessageType.GDL90_OwnshipGeometricAltitude:
                    MessageName = "GDL90 - Ownship Geometric Altitude";
                    break;
                case MessageType.GDL90_TrafficReport:
                    MessageName = "GDL90 - Traffic Report";
                    break;
                case MessageType.GDL90_BasicReport:
                    MessageName = "GDL90 - Basic Report";
                    break;
                case MessageType.GDL90_LongReport:
                    MessageName = "GDL90 - Long Report";
                    break;
                case MessageType.Stratux_AHRS:
                    MessageName = "Stratux - AHRS";
                    break;
                case MessageType.Stratux_Heartbeat_Old:
                    MessageName = "Stratux - Heartbeat";
                    break;
                case MessageType.Foreflight_Status:
                    MessageName = "Foreflight - Status";
                    break;
                case MessageType.Stratux_Heartbeat:
                    MessageName = "Stratux - Heartbeat";
                    break;
                default:
                    break;
            }
            return MessageName;
        }
        
        public static Message CreateMessageFromBytes(Span<byte> messageDataWithIdAndFcsAndFlagBytes) {
            Message GDL90Message;
            MessageType MessageType = GetMessageTypeFromByte(messageDataWithIdAndFcsAndFlagBytes[1]);

            switch (MessageType)
            {
                case GDL90.MessageType.GDL90_Heartbeat:
                    GDL90Message = new Heartbeat(messageDataWithIdAndFcsAndFlagBytes);
                    break;
                case GDL90.MessageType.GDL90_TrafficReport:
                    GDL90Message = new TrafficReport(messageDataWithIdAndFcsAndFlagBytes);
                    break;
                case GDL90.MessageType.GDL90_OwnshipReport:
                    GDL90Message = new OwnshipReport(messageDataWithIdAndFcsAndFlagBytes);
                    break;
                case GDL90.MessageType.GDL90_OwnshipGeometricAltitude:
                    GDL90Message = new OwnshipGeometricAltitude(messageDataWithIdAndFcsAndFlagBytes);
                    break;
                case GDL90.MessageType.GDL90_UplinkData:
                    GDL90Message = new UplinkData(messageDataWithIdAndFcsAndFlagBytes);
                    break;
                case GDL90.MessageType.GDL90_BasicReport:
                    GDL90Message = new BasicReport(messageDataWithIdAndFcsAndFlagBytes);
                    break;
                case GDL90.MessageType.GDL90_LongReport:
                    GDL90Message = new LongReport(messageDataWithIdAndFcsAndFlagBytes);
                    break;
                case GDL90.MessageType.Stratux_AHRS:
                    GDL90Message = new StratuxAHRS(messageDataWithIdAndFcsAndFlagBytes);
                    break;
                case GDL90.MessageType.Stratux_Heartbeat:
                    GDL90Message = new StratuxHeartbeat(messageDataWithIdAndFcsAndFlagBytes);
                    break;
                case GDL90.MessageType.Stratux_Heartbeat_Old:
                    GDL90Message = new StratuxHeartbeatOld(messageDataWithIdAndFcsAndFlagBytes);
                    break;
                case GDL90.MessageType.Foreflight_Status:
                    // This is a special case, foreflight status message are further subdivided by the next byte in the message, which is the "Message Identifier" field.
                    switch (messageDataWithIdAndFcsAndFlagBytes[2]) { // Message Identifier
                        case 0x00: // Foreflight Status
                            GDL90Message = new ForeflightStatus(messageDataWithIdAndFcsAndFlagBytes);
                            break;
                        case 0x01: // Foreflight AHRS
                            GDL90Message = new ForeflightAHRS(messageDataWithIdAndFcsAndFlagBytes);
                            break;
                        default:
                            GDL90Message = new NotImplementedMessage(messageDataWithIdAndFcsAndFlagBytes);
                            break;
                    }
                    break;
                default:
                    GDL90Message = new NotImplementedMessage(messageDataWithIdAndFcsAndFlagBytes);
                    break;
            }

            return GDL90Message;
        }
    }
}