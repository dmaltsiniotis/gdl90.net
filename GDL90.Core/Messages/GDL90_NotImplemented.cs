using System;

namespace GDL90.Core.Messages
{
    public class NotImplementedMessage : Message
    {
        public NotImplementedMessage(Span<byte> messageDataWithIdAndFcsAndFlagBytes) : base(messageDataWithIdAndFcsAndFlagBytes)
        {
            
        }
        public override string ToShortString()
        {
            return string.Format("GDL90 Message type 0x{0:X2} ({1}) is not yet implemented.", (int)MessageId, MessageName);
        }

        public override string ToDetailedString()
        {
            string debugInfo = string.Format("GDL90 Message type 0x{0:X2} ({1}) is not yet implemented.", (int)MessageId, MessageName);
            return debugInfo;
        }


    }

}